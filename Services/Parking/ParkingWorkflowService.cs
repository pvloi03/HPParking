using HPParking.Interfaces;
using HPParking.Models.Entities;
using HPParking.Services.Controller;
using HPParking.Services.LPR;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;

namespace HPParking.Services.Parking
{
    public class ParkingWorkflowService(
        IClientRepository clientRepository,
        IEventParkingRepository eventRepository,
        ILprService lprService,
        IImageStorageService imageStorageService,
        IDepartmentRepository? departmentRepository = null) : IParkingWorkflowService
    {
        private readonly IClientRepository _clientRepository = clientRepository;
        private readonly IEventParkingRepository _eventRepository = eventRepository;
        private readonly ILprService _lprService = lprService;
        private readonly IImageStorageService _imageStorageService = imageStorageService;
        private readonly IDepartmentRepository? _departmentRepository = departmentRepository;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _deptNameCache = new();

        private async Task<string> GetDepartmentNameAsync(string? departmentCode)
        {
            if (string.IsNullOrWhiteSpace(departmentCode))
                return string.Empty;

            if (_deptNameCache.TryGetValue(departmentCode, out var cachedName))
                return cachedName;

            if (_departmentRepository != null)
            {
                try
                {
                    var dept = await _departmentRepository.GetByDepartmentCode(departmentCode);
                    if (dept != null && !string.IsNullOrWhiteSpace(dept.Name))
                    {
                        _deptNameCache[departmentCode] = dept.Name;
                        return dept.Name;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Department Lookup Error] {ex.Message}");
                }
            }

            return departmentCode;
        }

        private static async Task<(bool PlateSuccess, Bitmap? PlateImage, bool OverviewSuccess, Bitmap? OverviewImage)> CaptureCamerasParallelAsync(LaneCamera cameras, int timeoutMs = 1000)
        {
            var plateCaptureTask = Task.Run(() =>
            {
                try { return cameras.LicensePlateCamera?.Capture(); }
                catch { return null; }
            });

            var overviewCaptureTask = Task.Run(() =>
            {
                try { return cameras.OverviewCamera?.Capture(); }
                catch { return null; }
            });

            var allTasks = Task.WhenAll(plateCaptureTask, overviewCaptureTask);
            var timeoutTask = Task.Delay(timeoutMs);

            await Task.WhenAny(allTasks, timeoutTask);

            Bitmap? plateBmp = null;
            if (plateCaptureTask.IsCompletedSuccessfully)
            {
                plateBmp = plateCaptureTask.Result;
            }
            else
            {
                _ = plateCaptureTask.ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully && t.Result != null)
                    {
                        t.Result.Dispose();
                    }
                }, TaskContinuationOptions.OnlyOnRanToCompletion);
            }

            Bitmap? overviewBmp = null;
            if (overviewCaptureTask.IsCompletedSuccessfully)
            {
                overviewBmp = overviewCaptureTask.Result;
            }
            else
            {
                _ = overviewCaptureTask.ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully && t.Result != null)
                    {
                        t.Result.Dispose();
                    }
                }, TaskContinuationOptions.OnlyOnRanToCompletion);
            }

            return (plateBmp != null, plateBmp, overviewBmp != null, overviewBmp);
        }

        private bool BarrierOpen(Lane lane)
        {
            int relayPort = lane.OutputRelay > 0 ? lane.OutputRelay : lane.InputReader;
            return lane.Ctrl != null && lane.Ctrl.OpenBarrier(relayPort, 1);
        }

        private bool IsClientExpired(Client client)
        {
            DateTime now = DateTime.Now;
            if (client.Expired.StartDay > now) return true;
            if (client.Expired.EndDay < now) return true;
            return false;
        }

        public async Task<ProcessResult> ProcessEntryAsync(
            Lane lane,
            RealtimeLog data,
            string imageBasePath,
            Func<Lane, bool>? onBarrierOpenFailed = null,
            Func<Lane, string?, Task<string?>>? onManualPlateInput = null,
            Func<Lane, Client, string, Task<bool>>? onPlateMismatchConfirm = null)
        {
            EventParking? parking = null;
            var client = await _clientRepository.GetByCardCode($"0{data.CardNo}");
            if (client == null)
                return new ProcessResult { Status = ProcessStatus.ClientNotFound, Message = "Không tìm thấy người dùng." };

            string departmentName = await GetDepartmentNameAsync(client.Department_Code);

            if (IsClientExpired(client))
            {
                return new ProcessResult { Status = ProcessStatus.ConfirmRequired, Message = $"Người dùng chỉ được ra vào từ {client.Expired.StartDay:dd/MM/yyyy} - {client.Expired.EndDay:dd/MM/yyyy}" };
            }

            var parkingInProgress = await _eventRepository.GetParkingInProgress($"0{data.CardNo}");
            if (parkingInProgress != null)
                return new ProcessResult { Status = ProcessStatus.AlreadyInParking, Message = "Khách hàng này đang có xe trong bãi." };

            if (lane.Cameras == null)
                return new ProcessResult { Status = ProcessStatus.CaptureFailed, Message = "Camera chưa được khởi tạo." };

            var (plateSuccess, plateImage, overviewSuccess, overviewImage) = await CaptureCamerasParallelAsync(lane.Cameras);

            string recognizedPlate = "";
            LprResult? lprResult = null;

            if (!plateSuccess || plateImage == null)
            {
                bool manualCancelled = false;
                if (onManualPlateInput != null)
                {
                    string? manual = await onManualPlateInput(lane, client.LicensePlate);
                    if (!string.IsNullOrWhiteSpace(manual))
                    {
                        recognizedPlate = manual.Trim().ToUpper();
                        lprResult = new LprResult { Success = true, Plate = recognizedPlate };
                    }
                    else
                    {
                        manualCancelled = true;
                    }
                }

                if (string.IsNullOrEmpty(recognizedPlate))
                {
                    overviewImage?.Dispose();
                    return new ProcessResult
                    {
                        Status = ProcessStatus.CaptureFailed,
                        Message = manualCancelled ? "" : "Camera biển số lỗi và không có biển số nhập tay."
                    };
                }
            }
            else
            {
                lprResult = await Task.Run(() => _lprService.Recognize(plateImage));
                if (lprResult != null && lprResult.Success && !string.IsNullOrWhiteSpace(lprResult.Plate))
                {
                    recognizedPlate = lprResult.Plate.Trim().ToUpper();
                }
                else
                {
                    bool manualCancelled = false;
                    if (onManualPlateInput != null)
                    {
                        string? manual = await onManualPlateInput(lane, client.LicensePlate);
                        if (!string.IsNullOrWhiteSpace(manual))
                        {
                            recognizedPlate = manual.Trim().ToUpper();
                            lprResult = new LprResult
                            {
                                Success = true,
                                Plate = recognizedPlate,
                                PlateImage = plateImage != null ? (Bitmap)plateImage.Clone() : null
                            };
                        }
                        else
                        {
                            manualCancelled = true;
                        }
                    }

                    if (string.IsNullOrEmpty(recognizedPlate))
                    {
                        plateImage?.Dispose();
                        overviewImage?.Dispose();
                        return new ProcessResult
                        {
                            Status = ProcessStatus.LprFailed,
                            Message = manualCancelled ? "" : "Nhận diện biển số thất bại."
                        };
                    }
                }
            }

            // Đối soát biển số đăng ký với biển số xe thực tế: Chặn cứng tuyệt đối nếu không khớp
            string registeredPlate = (client.LicensePlate ?? "").Replace(" ", "").Replace("-", "").Replace(".", "").ToUpperInvariant();
            string actualPlate = (recognizedPlate ?? "").Replace(" ", "").Replace("-", "").Replace(".", "").ToUpperInvariant();
            if (registeredPlate != actualPlate)
            {
                plateImage?.Dispose();
                overviewImage?.Dispose();
                return new ProcessResult
                {
                    Status = ProcessStatus.PlateMismatch,
                    Message = "Biển số xe không đúng với biển số đăng ký.",
                    Client = client,
                    DepartmentName = departmentName
                };
            }

            // Mở Barrier
            if (!BarrierOpen(lane))
            {
                bool handledManually = onBarrierOpenFailed?.Invoke(lane) ?? false;
                if (!handledManually)
                {
                    plateImage?.Dispose();
                    overviewImage?.Dispose();
                    return new ProcessResult
                    {
                        Status = ProcessStatus.BarrierFailed,
                        Message = "Không thể mở barrier. Vui lòng kiểm tra thiết bị."
                    };
                }
            }

            // Chuẩn bị bản ghi gửi xe để trả về cho UI ngay lập tức
            DateTime timeIn = (data.Time != default && data.Time != DateTime.MinValue) ? data.Time : DateTime.Now;
            parking = new EventParking
            {
                PhoneNumber = client.PhoneNumber,
                ClientName = client.Name,
                Card_Code = client.PhoneNumber,
                Card_Category = client.CardCategory,
                LicensePlate = client.LicensePlate != recognizedPlate ? "" : client.LicensePlate,
                LicensePlateIn = recognizedPlate,
                TimeIn = timeIn,
                Status = "IN"
            };

            // Lưu dữ liệu ngầm - clone để lưu trữ
            Bitmap? plateSave = plateImage != null ? (Bitmap)plateImage.Clone() : null;
            Bitmap? overviewSave = overviewImage != null ? (Bitmap)overviewImage.Clone() : null;
            plateImage?.Dispose();
            overviewImage?.Dispose();

            _ = Task.Run(async () =>
            {
                try
                {
                    using (plateSave)
                    using (overviewSave)
                    {
                        string platePath = plateSave != null
                            ? _imageStorageService.SaveImage(plateSave, "ImageIn", "BienSo", imageBasePath)
                            : "";
                        string overviewPath = overviewSave != null
                            ? _imageStorageService.SaveImage(overviewSave, "ImageIn", "ToanCanh", imageBasePath)
                            : "";

                        parking.UrlImageLicensePlateIn = platePath;
                        parking.UrlImageClientIn = overviewPath;

                        await _eventRepository.Insert(parking);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ParkingEntry Error] {ex.Message}");
                }
            });

            return new ProcessResult
            {
                Status = ProcessStatus.Success,
                Client = client,
                DepartmentName = departmentName,
                LprResult = lprResult,
                EventParking = parking
            };
        }

        public async Task<ProcessResult> ProcessExitAsync(
            Lane lane,
            RealtimeLog data,
            string imageBasePath,
            Func<Lane, bool>? onBarrierOpenFailed = null,
            Func<Lane, string?, Task<string?>>? onManualPlateInput = null)
        {
            var client = await _clientRepository.GetByCardCode($"0{data.CardNo}");
            if (client == null)
                return new ProcessResult { Status = ProcessStatus.ClientNotFound, Message = "Không tìm thấy khách hàng." };

            string departmentName = await GetDepartmentNameAsync(client.Department_Code);

            if (IsClientExpired(client))
            {
                return new ProcessResult { Status = ProcessStatus.ConfirmRequired, Message = $"Người dùng chỉ được ra vào từ {client.Expired.StartDay:dd/MM/yyyy} - {client.Expired.EndDay:dd/MM/yyyy}" };
            }

            var parking = await _eventRepository.GetParkingInProgress($"0{data.CardNo}");
            if (parking == null)
                return new ProcessResult { Status = ProcessStatus.NotInParking, Message = "Khách hàng này không có xe trong bãi." };

            if (lane.Cameras == null)
                return new ProcessResult { Status = ProcessStatus.CaptureFailed, Message = "Camera chưa được khởi tạo." };

            var (plateSuccess, plateImage, overviewSuccess, overviewImage) = await CaptureCamerasParallelAsync(lane.Cameras);

            string exitPlate = "";
            LprResult? lprResult = null;

            if (!plateSuccess || plateImage == null)
            {
                bool manualCancelled = false;
                if (onManualPlateInput != null)
                {
                    string? manual = await onManualPlateInput(lane, parking.LicensePlateIn);
                    if (!string.IsNullOrWhiteSpace(manual))
                    {
                        exitPlate = manual.Trim().ToUpper();
                        lprResult = new LprResult { Success = true, Plate = exitPlate };
                    }
                    else
                    {
                        manualCancelled = true;
                    }
                }

                if (string.IsNullOrEmpty(exitPlate))
                {
                    overviewImage?.Dispose();
                    return new ProcessResult
                    {
                        Status = ProcessStatus.CaptureFailed,
                        Message = manualCancelled ? "" : "Camera biển số lỗi và không có biển số nhập tay."
                    };
                }
            }
            else
            {
                lprResult = await Task.Run(() => _lprService.Recognize(plateImage));
                if (lprResult != null && lprResult.Success && !string.IsNullOrWhiteSpace(lprResult.Plate))
                {
                    exitPlate = lprResult.Plate.Trim().ToUpper();
                }
                else
                {
                    bool manualCancelled = false;
                    if (onManualPlateInput != null)
                    {
                        string? manual = await onManualPlateInput(lane, parking.LicensePlateIn);
                        if (!string.IsNullOrWhiteSpace(manual))
                        {
                            exitPlate = manual.Trim().ToUpper();
                            lprResult = new LprResult
                            {
                                Success = true,
                                Plate = exitPlate,
                                PlateImage = plateImage != null ? (Bitmap)plateImage.Clone() : null
                            };
                        }
                        else
                        {
                            manualCancelled = true;
                        }
                    }

                    if (string.IsNullOrEmpty(exitPlate))
                    {
                        plateImage?.Dispose();
                        overviewImage?.Dispose();
                        return new ProcessResult
                        {
                            Status = ProcessStatus.LprFailed,
                            Message = manualCancelled ? "" : "Nhận diện biển số thất bại."
                        };
                    }
                }
            }

            // Chặn cứng an ninh tuyệt đối nếu biển số xe ra không khớp với biển số xe lúc vào
            string cleanExitPlate = (exitPlate ?? "").Replace(" ", "").Replace("-", "").Replace(".", "").ToUpperInvariant();
            string cleanInPlate = (parking.LicensePlateIn ?? "").Replace(" ", "").Replace("-", "").Replace(".", "").ToUpperInvariant();
            if (cleanExitPlate != cleanInPlate)
            {
                plateImage?.Dispose();
                overviewImage?.Dispose();
                return new ProcessResult
                {
                    Status = ProcessStatus.PlateMismatch,
                    Message = "Biển số không khớp với biển số xe đã gửi.",
                    Client = client,
                    DepartmentName = departmentName
                };
            }

            if (!BarrierOpen(lane))
            {
                bool handledManually = onBarrierOpenFailed?.Invoke(lane) ?? false;
                if (!handledManually)
                {
                    plateImage?.Dispose();
                    overviewImage?.Dispose();
                    return new ProcessResult
                    {
                        Status = ProcessStatus.BarrierFailed,
                        Message = "Không thể mở barrier. Vui lòng kiểm thiết bị."
                    };
                }
            }

            Bitmap? plateSave = plateImage != null ? (Bitmap)plateImage.Clone() : null;
            Bitmap? overviewSave = overviewImage != null ? (Bitmap)overviewImage.Clone() : null;
            plateImage?.Dispose();
            overviewImage?.Dispose();

            DateTime timeOut = (data.Time != default && data.Time != DateTime.MinValue) ? data.Time : DateTime.Now;
            parking.LicensePlateOut = exitPlate;
            parking.Status = "OUT";
            parking.StatusInOut = true;
            parking.TimeOut = timeOut;

            _ = Task.Run(async () =>
            {
                try
                {
                    using (plateSave)
                    using (overviewSave)
                    {
                        string platePath = plateSave != null
                            ? _imageStorageService.SaveImage(plateSave, "ImageOut", "BienSo", imageBasePath)
                            : "";
                        string overviewPath = overviewSave != null
                            ? _imageStorageService.SaveImage(overviewSave, "ImageOut", "ToanCanh", imageBasePath)
                            : "";

                        parking.UrlImageLicensePlateOut = platePath;
                        parking.UrlImageClientOut = overviewPath;

                        await _eventRepository.Update(parking.Id, parking);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ParkingExit Error] {ex.Message}");
                }
            });

            return new ProcessResult
            {
                Status = ProcessStatus.Success,
                Client = client,
                DepartmentName = departmentName,
                EventParking = parking,
                LprResult = lprResult
            };
        }
    }
}