using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Interfaces;
using HPParking.Models;
using HPParking.Services.Controller;
using HPParking.Services.Devices;
using HPParking.Services.LPR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace HPParking.Services.Parking
{
    public class ParkingWorkflowService(
        IRepository<Client> clientRepository,
        IRepository<ParkingSession> sessionRepository,
        ILprService lprService,
        IImageStorageService imageStorageService,
        IRepository<Department>? departmentRepository = null,
        IRepository<Contractor>? contractorRepository = null,
        IRepository<Company>? companyRepository = null,
        IRepository<Vehicle>? vehicleRepository = null) : IParkingWorkflowService
    {
        private readonly IRepository<Client> _clientRepository = clientRepository;
        private readonly IRepository<ParkingSession> _sessionRepository = sessionRepository;
        private readonly ILprService _lprService = lprService;
        private readonly IImageStorageService _imageStorageService = imageStorageService;
        private readonly IRepository<Department>? _departmentRepository = departmentRepository;
        private readonly IRepository<Contractor>? _contractorRepository = contractorRepository;
        private readonly IRepository<Company>? _companyRepository = companyRepository;
        private readonly IRepository<Vehicle>? _vehicleRepository = vehicleRepository;

        private async Task<string> GetDepartmentNameAsync(Client client)
        {
            switch (client.Type)
            {
                case ClientType.Contractor:
                    if (!string.IsNullOrWhiteSpace(client.ContractorId) && _contractorRepository != null)
                    {
                        var contractor = await _contractorRepository.GetByIdAsync(client.ContractorId);
                        if (contractor != null && !string.IsNullOrWhiteSpace(contractor.Name))
                            return contractor.Name;
                    }
                    return "Nhà thầu / Đối tác";

                case ClientType.Visitor:
                    if (!string.IsNullOrWhiteSpace(client.CompanyId) && _companyRepository != null)
                    {
                        var company = await _companyRepository.GetByIdAsync(client.CompanyId);
                        if (company != null && !string.IsNullOrWhiteSpace(company.Name))
                            return company.Name;
                    }
                    return "Khách vãng lai";

                case ClientType.VIP:
                    if (!string.IsNullOrWhiteSpace(client.DepartmentId) && _departmentRepository != null)
                    {
                        var dept = await _departmentRepository.GetByIdAsync(client.DepartmentId);
                        if (dept != null && !string.IsNullOrWhiteSpace(dept.Name))
                            return dept.Name;
                    }
                    return "Khách VIP / Ban giám đốc";

                case ClientType.Employee:
                default:
                    if (!string.IsNullOrWhiteSpace(client.DepartmentId) && _departmentRepository != null)
                    {
                        var dept = await _departmentRepository.GetByIdAsync(client.DepartmentId);
                        if (dept != null && !string.IsNullOrWhiteSpace(dept.Name))
                            return dept.Name;
                    }
                    if (!string.IsNullOrWhiteSpace(client.CompanyId) && _companyRepository != null)
                    {
                        var company = await _companyRepository.GetByIdAsync(client.CompanyId);
                        if (company != null && !string.IsNullOrWhiteSpace(company.Name))
                            return company.Name;
                    }
                    return string.Empty;
            }
        }

        private static async Task<(bool PlateSuccess, Bitmap? PlateImage, bool OverviewSuccess, Bitmap? OverviewImage)> CaptureCamerasParallelAsync(
            LaneCamera cameras,
            bool capturePlateCamera = true,
            int timeoutMs = 1000)
        {
            var plateCaptureTask = capturePlateCamera
                ? Task.Run(() =>
                {
                    try { return cameras.LicensePlateCamera?.Capture(); }
                    catch { return null; }
                })
                : Task.FromResult<Bitmap?>(null);

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

            bool plateSuccess = !capturePlateCamera || (plateBmp != null);
            return (plateSuccess, plateBmp, overviewBmp != null, overviewBmp);
        }

        private static bool BarrierOpen(LaneRuntimeContext context)
        {
            return context.OpenBarrier();
        }

        private bool IsClientExpired(Client client)
        {
            // Nếu được đánh dấu ra vào thoải mái thì không chặn hết hạn
            if (client.Expired.Enable) return false;

            DateTime now = DateTime.Now;
            // Chưa đến ngày bắt đầu (chỉ chặn nếu sang trước ngày StartDay)
            if (client.Expired.StartDay.Date > now.Date) return true;

            // Đã quá ngày kết thúc (chỉ chặn khi đã sang ngày hôm sau của EndDay)
            if (client.Expired.EndDay.Date < now.Date) return true;

            return false;
        }

        public async Task<ProcessResult> ProcessEntryAsync(
            LaneRuntimeContext context,
            RealtimeLog data,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed = null,
            Func<LaneRuntimeContext, string?, Task<string?>>? onManualPlateInput = null)
        {
            ParkingSession? parking = null;
            string phone = (data.CardNo?.StartsWith("0") ?? false) ? data.CardNo : $"0{data.CardNo}";
            var client = await _clientRepository.FindOneAsync(x => x.PhoneNumber == phone);
            if (client == null)
                return new ProcessResult { Status = ProcessStatus.ClientNotFound, Message = "Không tìm thấy người dùng." };

            string departmentName = await GetDepartmentNameAsync(client);

            if (IsClientExpired(client))
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.ConfirmRequired,
                    Message = $"Người dùng chỉ được ra vào từ {client.Expired.StartDay:dd/MM/yyyy} - {client.Expired.EndDay:dd/MM/yyyy}",
                    Client = client,
                    DepartmentName = departmentName
                };
            }

            var parkingInProgress = await _sessionRepository.FindOneAsync(x => x.PersonId == client.Id && x.Status == ParkingSessionStatus.Active && !x.IsDeleted);
            if (parkingInProgress != null)
                return new ProcessResult
                {
                    Status = ProcessStatus.AlreadyInParking,
                    Message = "Khách hàng này đang có xe trong bãi.",
                    Client = client,
                    DepartmentName = departmentName
                };

            if (context.Cameras == null)
                return new ProcessResult
                {
                    Status = ProcessStatus.CaptureFailed,
                    Message = "Camera chưa được khởi tạo.",
                    Client = client,
                    DepartmentName = departmentName
                };

            LaneCamera cameras = context.Cameras;

            // 0. Lấy danh sách xe đã đăng ký của khách hàng (nếu có)
            List<Vehicle> clientVehicles = [];
            if (_vehicleRepository != null && !string.IsNullOrEmpty(client.Id))
            {
                var vehicles = await _vehicleRepository.FindAsync(v => v.OwnerClientId == client.Id && v.IsActive && !v.IsDeleted);
                clientVehicles = vehicles?.ToList() ?? [];
            }

            bool isVipWithoutVehicle = (client.Type == ClientType.VIP && clientVehicles.Count == 0);

            // Các client type còn lại bắt buộc phải đăng ký biển số xe mới được cho vào bãi
            if (!isVipWithoutVehicle && client.Type != ClientType.VIP && _vehicleRepository != null && clientVehicles.Count == 0)
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.PlateMismatch,
                    Message = "Khách hàng chưa đăng ký biển số xe trong hệ thống.",
                    Client = client,
                    DepartmentName = departmentName
                };
            }

            string defaultPlate = clientVehicles.Count > 0
                ? string.Join("; ", clientVehicles.Select(v => v.PlateNumber).Where(p => !string.IsNullOrWhiteSpace(p)))
                : "";

            // Khách VIP chưa đăng ký xe -> Không kích hoạt chụp ảnh Camera Biển Số
            var (plateSuccess, plateImage, overviewSuccess, overviewImage) = await CaptureCamerasParallelAsync(cameras, capturePlateCamera: !isVipWithoutVehicle);

            string recognizedPlate = "";
            LprResult? lprResult = null;
            Vehicle? matchedVehicle = null;

            if (isVipWithoutVehicle)
            {
                recognizedPlate = "";
            }
            else if (!plateSuccess || plateImage == null)
            {
                bool manualCancelled = false;
                if (onManualPlateInput != null)
                {
                    string? manual = await onManualPlateInput(context, defaultPlate);
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
                        Message = manualCancelled ? "" : "Camera biển số lỗi và không có biển số nhập tay.",
                        Client = client,
                        Vehicle = clientVehicles.FirstOrDefault(),
                        DepartmentName = departmentName
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
                        string? manual = await onManualPlateInput(context, defaultPlate);
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
                            Message = manualCancelled ? "" : "Nhận diện biển số thất bại.",
                            Client = client,
                            Vehicle = clientVehicles.FirstOrDefault(),
                            DepartmentName = departmentName
                        };
                    }
                }
            }

            if (!isVipWithoutVehicle)
            {
                // Đối soát biển số đăng ký với biển số xe thực tế: Chặn cứng nếu danh sách xe có khai báo mà không khớp
                string actualPlate = (recognizedPlate ?? "").Replace(" ", "").Replace("-", "").Replace(".", "").ToUpperInvariant();
                matchedVehicle = clientVehicles.FirstOrDefault(v =>
                    (v.PlateNumber ?? "").Replace(" ", "").Replace("-", "").Replace(".", "").ToUpperInvariant() == actualPlate);

                if (matchedVehicle == null && clientVehicles.Count > 0)
                {
                    plateImage?.Dispose();
                    overviewImage?.Dispose();
                    return new ProcessResult
                    {
                        Status = ProcessStatus.PlateMismatch,
                        Message = "Biển số xe không đúng với biển số đăng ký.",
                        Client = client,
                        Vehicle = clientVehicles.FirstOrDefault(),
                        DepartmentName = departmentName,
                        LprResult = lprResult
                    };
                }
            }

            // Mở Barrier
            if (!BarrierOpen(context))
            {
                bool handledManually = onBarrierOpenFailed?.Invoke(context) ?? false;
                if (!handledManually)
                {
                    plateImage?.Dispose();
                    overviewImage?.Dispose();
                    return new ProcessResult
                    {
                        Status = ProcessStatus.BarrierFailed,
                        Message = "Không thể mở barrier. Vui lòng kiểm tra thiết bị.",
                        Client = client,
                        Vehicle = matchedVehicle ?? clientVehicles.FirstOrDefault(),
                        DepartmentName = departmentName,
                        LprResult = lprResult
                    };
                }
            }

            // Chuẩn bị bản ghi gửi xe để trả về cho UI ngay lập tức
            DateTime timeIn = (data.Time != default && data.Time != DateTime.MinValue) ? data.Time : DateTime.Now;
            parking = new ParkingSession
            {
                PersonId = client.Id,
                PlateNumber = isVipWithoutVehicle ? "" : (matchedVehicle?.PlateNumber ?? recognizedPlate ?? ""),
                VehicleType = matchedVehicle?.Type ?? VehicleType.Car,
                InTime = timeIn,
                InLaneName = context.Lane.Name,
                Status = ParkingSessionStatus.Active
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

                        parking.InPlateImagePath = platePath;
                        parking.InOverviewImagePath = overviewPath;

                        await _sessionRepository.AddAsync(parking);
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
                Vehicle = matchedVehicle ?? clientVehicles.FirstOrDefault(),
                DepartmentName = departmentName,
                LprResult = lprResult,
                ParkingSession = parking
            };
        }

        public async Task<ProcessResult> ProcessExitAsync(
            LaneRuntimeContext context,
            RealtimeLog data,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed = null,
            Func<LaneRuntimeContext, string?, Task<string?>>? onManualPlateInput = null)
        {
            string phone = (data.CardNo?.StartsWith("0") ?? false) ? data.CardNo : $"0{data.CardNo}";
            var client = await _clientRepository.FindOneAsync(x => x.PhoneNumber == phone);
            if (client == null)
                return new ProcessResult { Status = ProcessStatus.ClientNotFound, Message = "Không tìm thấy khách hàng." };

            string departmentName = await GetDepartmentNameAsync(client);

            if (IsClientExpired(client))
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.ConfirmRequired,
                    Message = $"Người dùng chỉ được ra vào từ {client.Expired.StartDay:dd/MM/yyyy} - {client.Expired.EndDay:dd/MM/yyyy}",
                    Client = client,
                    DepartmentName = departmentName
                };
            }

            var parking = await _sessionRepository.FindOneAsync(x => x.PersonId == client.Id && x.Status == ParkingSessionStatus.Active && !x.IsDeleted);
            if (parking == null)
                return new ProcessResult
                {
                    Status = ProcessStatus.NotInParking,
                    Message = "Khách hàng này không có xe trong bãi.",
                    Client = client,
                    DepartmentName = departmentName
                };

            // Lấy danh sách xe đã đăng ký của khách hàng (nếu có)
            List<Vehicle> clientVehicles = [];
            if (_vehicleRepository != null && !string.IsNullOrEmpty(client.Id))
            {
                var vehicles = await _vehicleRepository.FindAsync(v => v.OwnerClientId == client.Id && v.IsActive && !v.IsDeleted);
                clientVehicles = vehicles?.ToList() ?? [];
            }

            Vehicle? matchedVehicle = clientVehicles.FirstOrDefault(v =>
                !string.IsNullOrWhiteSpace(parking.PlateNumber) &&
                (v.PlateNumber ?? "").Replace(" ", "").Replace("-", "").Replace(".", "").ToUpperInvariant() ==
                (parking.PlateNumber ?? "").Replace(" ", "").Replace("-", "").Replace(".", "").ToUpperInvariant())
                ?? clientVehicles.FirstOrDefault();

            if (context.Cameras == null)
                return new ProcessResult
                {
                    Status = ProcessStatus.CaptureFailed,
                    Message = "Camera chưa được khởi tạo.",
                    Client = client,
                    Vehicle = matchedVehicle,
                    DepartmentName = departmentName,
                    ParkingSession = parking
                };

            bool isVipWithoutVehicle = (client.Type == ClientType.VIP && clientVehicles.Count == 0);

            // Khách VIP chưa đăng ký xe -> Không kích hoạt chụp ảnh Camera Biển Số
            var (plateSuccess, plateImage, overviewSuccess, overviewImage) = await CaptureCamerasParallelAsync(context.Cameras, capturePlateCamera: !isVipWithoutVehicle);

            string exitPlate = "";
            LprResult? lprResult = null;

            if (isVipWithoutVehicle)
            {
                exitPlate = "";
            }
            else if (!plateSuccess || plateImage == null)
            {
                bool manualCancelled = false;
                if (onManualPlateInput != null)
                {
                    string? manual = await onManualPlateInput(context, parking.PlateNumber);
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
                        Message = manualCancelled ? "" : "Camera biển số lỗi và không có biển số nhập tay.",
                        Client = client,
                        Vehicle = matchedVehicle,
                        DepartmentName = departmentName,
                        ParkingSession = parking
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
                        string? manual = await onManualPlateInput(context, parking.PlateNumber);
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
                            Message = manualCancelled ? "" : "Nhận diện biển số thất bại.",
                            Client = client,
                            Vehicle = matchedVehicle,
                            DepartmentName = departmentName,
                            ParkingSession = parking
                        };
                    }
                }
            }

            if (!isVipWithoutVehicle)
            {
                // Chặn cứng an ninh tuyệt đối nếu biển số xe ra không khớp với biển số xe lúc vào
                string cleanExitPlate = (exitPlate ?? "").Replace(" ", "").Replace("-", "").Replace(".", "").ToUpperInvariant();
                string cleanInPlate = (parking.PlateNumber ?? "").Replace(" ", "").Replace("-", "").Replace(".", "").ToUpperInvariant();
                if (cleanExitPlate != cleanInPlate)
                {
                    plateImage?.Dispose();
                    overviewImage?.Dispose();
                    return new ProcessResult
                    {
                        Status = ProcessStatus.PlateMismatch,
                        Message = "Biển số không khớp với biển số xe đã gửi.",
                        Client = client,
                        Vehicle = matchedVehicle,
                        DepartmentName = departmentName,
                        ParkingSession = parking,
                        LprResult = lprResult
                    };
                }
            }

            if (!BarrierOpen(context))
            {
                bool handledManually = onBarrierOpenFailed?.Invoke(context) ?? false;
                if (!handledManually)
                {
                    plateImage?.Dispose();
                    overviewImage?.Dispose();
                    return new ProcessResult
                    {
                        Status = ProcessStatus.BarrierFailed,
                        Message = "Không thể mở barrier. Vui lòng kiểm thiết bị.",
                        Client = client,
                        Vehicle = matchedVehicle,
                        DepartmentName = departmentName,
                        ParkingSession = parking,
                        LprResult = lprResult
                    };
                }
            }

            Bitmap? plateSave = plateImage != null ? (Bitmap)plateImage.Clone() : null;
            Bitmap? overviewSave = overviewImage != null ? (Bitmap)overviewImage.Clone() : null;
            plateImage?.Dispose();
            overviewImage?.Dispose();

            DateTime timeOut = (data.Time != default && data.Time != DateTime.MinValue) ? data.Time : DateTime.Now;
            parking.OutTime = timeOut;
            parking.OutLaneName = context.Lane.Name;
            parking.Status = ParkingSessionStatus.Completed;

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

                        parking.OutPlateImagePath = platePath;
                        parking.OutOverviewImagePath = overviewPath;
                        parking.UpdatedAt = DateTime.Now;

                        await _sessionRepository.UpdateAsync(parking);
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
                Vehicle = matchedVehicle,
                DepartmentName = departmentName,
                ParkingSession = parking,
                LprResult = lprResult
            };
        }
    }
}