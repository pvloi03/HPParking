using HPParking.Core.Helpers;
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
        IRepository<Department> departmentRepository,
        IRepository<Contractor> contractorRepository,
        IRepository<Company> companyRepository,
        IRepository<Vehicle> vehicleRepository,
        IRepository<Card> cardRepository,
        IRepository<VehicleDispatchTrip> tripRepository,
        IRepository<GateRouteConfig> gateRouteRepository,
        IRepository<Gate> gateRepository) : IParkingWorkflowService
    {
        private readonly IRepository<Client> _clientRepository = clientRepository;
        private readonly IRepository<ParkingSession> _sessionRepository = sessionRepository;
        private readonly ILprService _lprService = lprService;
        private readonly IImageStorageService _imageStorageService = imageStorageService;
        private readonly IRepository<Department> _departmentRepository = departmentRepository;
        private readonly IRepository<Contractor> _contractorRepository = contractorRepository;
        private readonly IRepository<Company> _companyRepository = companyRepository;
        private readonly IRepository<Vehicle> _vehicleRepository = vehicleRepository;
        private readonly IRepository<Card> _cardRepository = cardRepository;
        private readonly IRepository<VehicleDispatchTrip> _tripRepository = tripRepository;
        private readonly IRepository<GateRouteConfig> _gateRouteRepository = gateRouteRepository;
        private readonly IRepository<Gate> _gateRepository = gateRepository;

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

                case ClientType.Guest:
                    if (!string.IsNullOrWhiteSpace(client.ContractorId) && _contractorRepository != null)
                    {
                        var contractor = await _contractorRepository.GetByIdAsync(client.ContractorId);
                        if (contractor != null && !string.IsNullOrWhiteSpace(contractor.Name))
                            return contractor.Name;
                    }
                    return "Khách đến thăm";

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
            int timeoutMs = 2500)
        {
            var plateCaptureTask = capturePlateCamera
                ? Task.Run(() =>
                {
                    try { return cameras.LicensePlateCamera?.Capture(); }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[CaptureCamerasParallelAsync] Lỗi chụp camera biển số: {ex.Message}");
                        return null;
                    }
                })
                : Task.FromResult<Bitmap?>(null);

            var overviewCaptureTask = Task.Run(() =>
            {
                try { return cameras.OverviewCamera?.Capture(); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CaptureCamerasParallelAsync] Lỗi chụp camera toàn cảnh: {ex.Message}");
                    return null;
                }
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
            if (client.Expired.Enable) return false;

            DateTime now = DateTime.Now;
            if (client.Expired.StartDay.Date > now.Date) return true;
            if (client.Expired.EndDay.Date < now.Date) return true;

            return false;
        }

        /// <summary>
        /// Xử lý điều vận xe công vụ / xe dùng chung qua lại giữa các nhà máy theo SLA
        /// </summary>
        private async Task<ProcessResult> ProcessSharedVehicleTripAsync(
            LaneRuntimeContext context,
            Card vehicleCard,
            RealtimeLog data,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed,
            Func<LaneRuntimeContext, string?, Task<string?>>? onManualPlateInput = null)
        {
            var vehicle = await _vehicleRepository!.GetByIdAsync(vehicleCard.VehicleId!);
            if (vehicle == null || !vehicle.IsActive)
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.ClientNotFound,
                    Message = "Phương tiện công vụ gắn với thẻ này không tồn tại hoặc đã bị khóa."
                };
            }

            string currentGateId = context.Lane?.GateId ?? "";
            bool isEntry = context.Direction == LaneDirection.In;

            Gate? currentGate = !string.IsNullOrEmpty(currentGateId) ? await _gateRepository.GetByIdAsync(currentGateId) : null;
            string currentGateName = currentGate?.Name ?? (string.IsNullOrEmpty(currentGateId) ? "Cổng không xác định" : currentGateId);

            // Tìm chuyến đang chạy của xe công vụ
            VehicleDispatchTrip? activeTrip = await _tripRepository.FindOneAsync(t =>
                t.VehicleId == vehicle.Id &&
                t.Status != TripStatus.Completed &&
                !t.IsDeleted);

            // Chụp ảnh camera lưu vết
            Bitmap? plateImage = null;
            Bitmap? overviewImage = null;
            LprResult? lprResult = null;
            string detectedPlate = "";
            bool plateSuccess = false;
            bool overviewSuccess = false;

            if (context.Cameras != null)
            {
                (plateSuccess, plateImage, overviewSuccess, overviewImage) =
                    await CaptureCamerasParallelAsync(context.Cameras, capturePlateCamera: true, timeoutMs: 2500);

                if (plateSuccess && plateImage != null)
                {
                    lprResult = await Task.Run(() => _lprService.Recognize(plateImage));
                    if (lprResult != null && lprResult.Success && !string.IsNullOrWhiteSpace(lprResult.Plate))
                    {
                        detectedPlate = lprResult.Plate.Trim().ToUpper();
                    }
                }
            }

            // 1. XÁC THỰC BIỂN SỐ XE CÔNG VỤ NGHIÊM NGẶT
            string registeredPlateNorm = (vehicle.PlateNumber ?? "")
                .Replace(" ", "").Replace("-", "").Replace(".", "").ToUpperInvariant();
            string detectedPlateNorm = detectedPlate
                .Replace(" ", "").Replace("-", "").Replace(".", "").ToUpperInvariant();

            // TRƯỜNG HỢP A: Camera lỗi hoặc OCR hoàn toàn không đọc được biển số -> Cho bảo vệ nhập tay
            if (string.IsNullOrEmpty(detectedPlateNorm))
            {
                bool manualCancelled = false;
                if (onManualPlateInput != null)
                {
                    string promptPlate = vehicle.PlateNumber ?? "";
                    string? manualInput = await onManualPlateInput(context, promptPlate);
                    if (!string.IsNullOrWhiteSpace(manualInput))
                    {
                        detectedPlate = manualInput.Trim().ToUpper();
                        detectedPlateNorm = detectedPlate.Replace(" ", "").Replace("-", "").Replace(".", "").ToUpperInvariant();
                        lprResult = new LprResult
                        {
                            Success = true,
                            Plate = detectedPlate,
                            PlateImage = plateImage != null ? (Bitmap)plateImage.Clone() : null
                        };
                    }
                    else
                    {
                        manualCancelled = true;
                    }
                }

                // Nếu bảo vệ hủy hoặc không nhập -> CHẶN
                if (string.IsNullOrEmpty(detectedPlateNorm))
                {
                    plateImage?.Dispose();
                    overviewImage?.Dispose();
                    return new ProcessResult
                    {
                        Status = (!plateSuccess || plateImage == null) ? ProcessStatus.CaptureFailed : ProcessStatus.LprFailed,
                        Message = manualCancelled ? "" : $"Không nhận diện được biển số xe công vụ {vehicle.PlateNumber} và không có biển số nhập tay.",
                        Vehicle = vehicle,
                        DepartmentName = "Không nhận diện được biển số",
                        LprResult = lprResult,
                        DispatchTrip = activeTrip
                    };
                }
            }

            // TRƯỜNG HỢP B: Đã có biển số (hoặc do AI đọc được, hoặc do bảo vệ nhập tay) -> So sánh đối soát
            if (detectedPlateNorm != registeredPlateNorm)
            {
                // Biển số không trùng khớp -> BÁO LỖI LỆCH BIỂN VÀ CHẶN NGAY, KHÔNG BẬT FORM NHẬP TAY LÀM PHIỀN!
                plateImage?.Dispose();
                overviewImage?.Dispose();
                return new ProcessResult
                {
                    Status = ProcessStatus.PlateMismatch,
                    Message = $"BIỂN SỐ KHÔNG ĐÚNG VỚI BIỂN SỐ ĐÃ ĐĂNG KÝ!",
                    Vehicle = vehicle,
                    DepartmentName = "Cảnh báo sai biển số xe công vụ",
                    LprResult = lprResult,
                    DispatchTrip = activeTrip
                };
            }

            GateRouteConfig? assignedRoute = null;
            if (!string.IsNullOrEmpty(vehicle.AssignedRouteId))
            {
                assignedRoute = await _gateRouteRepository.GetByIdAsync(vehicle.AssignedRouteId);
            }
            assignedRoute ??= await _gateRouteRepository.FindOneAsync(r => (r.IsDefault || r.RouteCode == "DEFAULT") && !r.IsDeleted);

            DateTime now = (data.Time != default && data.Time != DateTime.MinValue)
                ? (data.Time.Kind == DateTimeKind.Utc ? data.Time : data.Time.ToUniversalTime())
                : DateTime.UtcNow;

            // Đánh giá tình trạng quá hạn SLA tại thời điểm quẹt thẻ hiện tại (trước khi NextDeadline bị cập nhật hoặc reset)
            bool isCheckpointOverdue = false;
            double checkpointOverdueSeconds = 0;
            if (activeTrip != null && activeTrip.NextDeadline.HasValue && now > activeTrip.NextDeadline.Value)
            {
                isCheckpointOverdue = true;
                checkpointOverdueSeconds = Math.Round((now - activeTrip.NextDeadline.Value).TotalSeconds);
            }

            int defaultTravel = assignedRoute?.DefaultTravelMinutes ?? 15;
            int defaultStay = assignedRoute?.DefaultStayMinutes ?? 15;

            // 2. KIỂM TRA CHIỀU QUẸT VÀ LỘ TRÌNH ĐIỀU VẬN
            // Nghiệp vụ: Xe công vụ đang ở cơ quan/bãi. Bắt đầu chuyến phải quẹt ở LÀN RA để đi làm việc!
            if (activeTrip == null)
            {
                if (isEntry)
                {
                    plateImage?.Dispose();
                    overviewImage?.Dispose();
                    return new ProcessResult
                    {
                        Status = ProcessStatus.ConfirmRequired,
                        Message = $"Di chuyển sai làn!",
                        Vehicle = vehicle,
                        DepartmentName = "Sai chiều xuất phát",
                        LprResult = lprResult
                    };
                }

                // Kiểm tra Cổng xuất phát theo quy định của Tuyến (nếu có)
                if (assignedRoute != null && assignedRoute.GateSteps.Count > 0)
                {
                    var firstStep = assignedRoute.GateSteps.OrderBy(s => s.StepIndex).First();
                    if (!string.IsNullOrEmpty(firstStep.GateId) && firstStep.GateId != currentGateId)
                    {
                        plateImage?.Dispose();
                        overviewImage?.Dispose();
                        return new ProcessResult
                        {
                            Status = ProcessStatus.ConfirmRequired,
                            Message = $"CẢNH BÁO SAI CỔNG XUẤT PHÁT: Xe {vehicle.PlateNumber} quẹt tại cổng '{currentGateName}'. Tuyến '{assignedRoute.RouteName}' quy định xuất phát từ cổng '{firstStep.GateName}'!",
                            Vehicle = vehicle,
                            DepartmentName = $"Sai tuyến: {assignedRoute.RouteName}",
                            LprResult = lprResult
                        };
                    }
                }
            }
            else
            {
                // activeTrip != null: Xe đang thực hiện hành trình
                if (activeTrip.Status == TripStatus.InTransit)
                {
                    // Xe đang di chuyển trên đường: Bắt buộc phải quẹt LÀN VÀO tại cổng đến!
                    if (!isEntry)
                    {
                        plateImage?.Dispose();
                        overviewImage?.Dispose();
                        return new ProcessResult
                        {
                            Status = ProcessStatus.ConfirmRequired,
                            Message = "Di chuyển sai làn!",
                            Vehicle = vehicle,
                            DepartmentName = "Sai chiều di chuyển",
                            LprResult = lprResult
                        };
                    }

                    // Xe quẹt vào một cổng: Kiểm tra có đúng chặng theo tuyến quy định không
                    if (assignedRoute != null && assignedRoute.GateSteps.Count > 0)
                    {
                        var expectedStep = assignedRoute.GateSteps.FirstOrDefault(s => s.StepIndex == activeTrip.CurrentStepIndex);
                        if (expectedStep != null && !string.IsNullOrEmpty(expectedStep.GateId) && expectedStep.GateId != currentGateId)
                        {
                            plateImage?.Dispose();
                            overviewImage?.Dispose();

                            // Ghi nhận mốc vi phạm lạc tuyến vào lịch sử hành trình (không mở barrier)
                            var violationCheckpoint = new TripCheckpoint
                            {
                                StepIndex = activeTrip.CurrentStepIndex,
                                GateId = currentGateId,
                                GateName = currentGateName,
                                Direction = context.Direction,
                                Timestamp = now,
                                PlateDetected = lprResult?.Plate ?? vehicle.PlateNumber,
                                IsRouteCompliant = false,
                                Note = $"Lạc tuyến: Quẹt vào {currentGateName} nhưng lộ trình chặng {activeTrip.CurrentStepIndex} là {expectedStep.GateName}",
                                SlaOverdue = new SlaOverdueInfo
                                {
                                    IsOverdue = isCheckpointOverdue,
                                    OverdueSeconds = checkpointOverdueSeconds
                                }
                            };
                            activeTrip.Checkpoints ??= [];
                            activeTrip.Checkpoints.Add(violationCheckpoint);
                            await _tripRepository.UpdateAsync(activeTrip);

                            return new ProcessResult
                            {
                                Status = ProcessStatus.ConfirmRequired,
                                Message = $"CẢNH BÁO LẠC TUYẾN: Xe {vehicle.PlateNumber} quẹt tại cổng '{currentGateName}'. Tuyến '{assignedRoute.RouteName}' chặng {activeTrip.CurrentStepIndex} yêu cầu đến cổng '{expectedStep.GateName}'!",
                                Vehicle = vehicle,
                                DepartmentName = $"Lạc tuyến: {assignedRoute.RouteName}",
                                LprResult = lprResult
                            };
                        }
                    }
                }
                else if (activeTrip.Status == TripStatus.WorkingAtGate)
                {
                    // Xe đang dừng làm việc tại cổng: Bắt buộc phải quẹt LÀN RA để rời khỏi cổng!
                    if (isEntry)
                    {
                        plateImage?.Dispose();
                        overviewImage?.Dispose();
                        return new ProcessResult
                        {
                            Status = ProcessStatus.ConfirmRequired,
                            Message = $"Di chuyển sai làn!",
                            Vehicle = vehicle,
                            DepartmentName = "Xe đang trong cổng",
                            LprResult = lprResult
                        };
                    }
                }
            }

            // 3. KHỞI TẠO HOẶC CẬP NHẬT CHUYẾN ĐI
            if (activeTrip == null)
            {
                // Bắt đầu một chuyến điều vận mới (luôn là LÀN RA)
                int travelMinutes = defaultTravel;
                if (assignedRoute != null && assignedRoute.GateSteps.Count > 0)
                {
                    var firstStep = assignedRoute.GateSteps.OrderBy(s => s.StepIndex).First();
                    travelMinutes = firstStep.MaxTravelMinutes;
                }

                activeTrip = new VehicleDispatchTrip
                {
                    VehicleId = vehicle.Id,
                    PlateNumber = vehicle.PlateNumber ?? "",
                    CardId = vehicleCard.Id,
                    CardNumber = vehicleCard.CardNumber,
                    OriginGateId = currentGateId,
                    CurrentGateId = currentGateId,
                    AssignedRouteId = vehicle.AssignedRouteId,
                    CurrentStepIndex = 1,
                    Status = TripStatus.InTransit, // Bắt đầu xuất phát ra khỏi cơ quan để đi làm việc
                    StartTime = now,
                    LastExitTime = now,
                    LastEntryTime = null,
                    NextDeadline = now.AddMinutes(travelMinutes),
                    IsAlertSent = false,
                    LastDriverImagePath = "",
                    Checkpoints = []
                };

                await _tripRepository.AddAsync(activeTrip);
            }
            else
            {
                // Cập nhật chuyến đang chạy
                activeTrip.CurrentGateId = currentGateId;
                if (isEntry)
                {
                    activeTrip.LastEntryTime = now;
                    bool isReturnOrigin = (!string.IsNullOrEmpty(activeTrip.OriginGateId) &&
                                          activeTrip.OriginGateId == currentGateId &&
                                          activeTrip.CurrentStepIndex >= (assignedRoute?.GateSteps.Count ?? 1));

                    if (isReturnOrigin)
                    {
                        activeTrip.Status = TripStatus.Completed;
                        activeTrip.EndTime = now;
                        activeTrip.NextDeadline = null;
                    }
                    else
                    {
                        activeTrip.Status = TripStatus.WorkingAtGate;
                        int stayMinutes = defaultStay;
                        if (assignedRoute != null && assignedRoute.GateSteps.Count > 0)
                        {
                            var currentStep = assignedRoute.GateSteps.FirstOrDefault(s => s.StepIndex == activeTrip.CurrentStepIndex);
                            if (currentStep != null) stayMinutes = currentStep.MaxStayMinutes;
                        }
                        activeTrip.NextDeadline = now.AddMinutes(stayMinutes);
                        activeTrip.IsAlertSent = false;
                    }
                }
                else
                {
                    activeTrip.LastExitTime = now;
                    activeTrip.CurrentStepIndex++;
                    activeTrip.Status = TripStatus.InTransit;
                    int travelMinutes = defaultTravel;
                    if (assignedRoute != null && assignedRoute.GateSteps.Count > 0)
                    {
                        var nextStep = assignedRoute.GateSteps.FirstOrDefault(s => s.StepIndex == activeTrip.CurrentStepIndex);
                        if (nextStep != null) travelMinutes = nextStep.MaxTravelMinutes;
                    }
                    activeTrip.NextDeadline = now.AddMinutes(travelMinutes);
                    activeTrip.IsAlertSent = false;
                }

                await _tripRepository.UpdateAsync(activeTrip);
            }

            // Ghi nhận mốc kiểm soát hành trình (Checkpoint)
            var currentCheckpoint = new TripCheckpoint
            {
                StepIndex = activeTrip.CurrentStepIndex,
                GateId = currentGateId,
                GateName = currentGateName,
                Direction = context.Direction,
                Timestamp = now,
                ImagePath = "",
                PlateDetected = lprResult?.Plate ?? vehicle.PlateNumber,
                IsRouteCompliant = true,
                Note = isEntry ? "Quẹt vào cổng" : "Quẹt ra khỏi cổng",
                SlaOverdue = new SlaOverdueInfo
                {
                    IsOverdue = isCheckpointOverdue,
                    OverdueSeconds = checkpointOverdueSeconds
                }
            };
            activeTrip.Checkpoints ??= [];
            activeTrip.Checkpoints.Add(currentCheckpoint);
            await _tripRepository.UpdateAsync(activeTrip);

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
                        Message = "Không thể mở barrier cho xe công vụ. Vui lòng kiểm tra thiết bị.",
                        Vehicle = vehicle,
                        DepartmentName = assignedRoute != null ? $"Tuyến: {assignedRoute.RouteName}" : "Xe công vụ / Điều vận",
                        LprResult = lprResult
                    };
                }
            }

            // Lưu ảnh ngầm và cập nhật đường dẫn ảnh vào Checkpoint
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
                        string overviewPath = overviewSave != null
                            ? _imageStorageService.SaveImage(overviewSave, isEntry ? "ImageIn" : "ImageOut", "ToanCanh", imageBasePath)
                            : "";

                        if (activeTrip != null && !string.IsNullOrEmpty(overviewPath))
                        {
                            activeTrip.LastDriverImagePath = overviewPath;
                            currentCheckpoint.ImagePath = overviewPath;
                            await _tripRepository.UpdateAsync(activeTrip);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SharedVehicleTrip Image Error] {ex.Message}");
                }
            });

            string routeDesc = assignedRoute != null ? assignedRoute.RouteName : "Tuyến tự do";
            int totalSteps = assignedRoute?.GateSteps.Count ?? 1;
            return new ProcessResult
            {
                Status = ProcessStatus.Success,
                Vehicle = vehicle,
                DepartmentName = $"Tuyến: {routeDesc} (Chặng {activeTrip.CurrentStepIndex}/{totalSteps})",
                LprResult = lprResult,
                Message = $"Xe công vụ {vehicle.PlateNumber} - Chặng {activeTrip.CurrentStepIndex}/{totalSteps} ({activeTrip.Status})",
                DispatchTrip = activeTrip
            };
        }

        public async Task<ProcessResult> ProcessEntryAsync(
            LaneRuntimeContext context,
            RealtimeLog data,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed = null,
            Func<LaneRuntimeContext, string?, Task<string?>>? onManualPlateInput = null)
        {
            string rawCard = data.CardNo?.Trim() ?? string.Empty;
            string normalizedCard = CardHelper.NormalizeCardCode(rawCard);

            Debug.WriteLine($"[ProcessEntryAsync] Quẹt thẻ: CardNo='{data.CardNo}', Raw='{rawCard}', Normalized='{normalizedCard}'");

            if (!CardHelper.IsValidCardCode(rawCard) && !CardHelper.IsValidCardCode(normalizedCard))
            {
                // Silently ignore noise events (sensor 0, barrier open/close logs)
                return new ProcessResult { Status = ProcessStatus.ClientNotFound, Message = string.Empty };
            }

            // 1. Phân nhánh Thẻ Xe Công Vụ / Thẻ định danh qua Card repository
            var cardEntity = await _cardRepository.FindOneAsync(c =>
                (c.CardNumber == normalizedCard || c.CardNumber == rawCard) &&
                !c.IsDeleted);

            if (cardEntity != null && cardEntity.TargetType == CardTargetType.Vehicle)
            {
                Debug.WriteLine($"[ProcessEntryAsync] Đã nhận diện Thẻ Xe Công Vụ: Card='{cardEntity.CardNumber}', VehicleId='{cardEntity.VehicleId}'");
                if (!string.IsNullOrEmpty(cardEntity.VehicleId))
                {
                    return await ProcessSharedVehicleTripAsync(context, cardEntity, data, imageBasePath, onBarrierOpenFailed, onManualPlateInput);
                }
            }

            // 2. Tìm kiếm nhân sự/khách hàng qua ClientId (từ Card) hoặc trực tiếp CardCode / PhoneNumber
            Client? client = null;
            if (cardEntity != null && cardEntity.TargetType == CardTargetType.Person && !string.IsNullOrEmpty(cardEntity.ClientId))
            {
                client = await _clientRepository.GetByIdAsync(cardEntity.ClientId);
            }

            if (client == null || client.IsDeleted)
            {
                client = await _clientRepository.FindOneAsync(x =>
                    (x.CardCode == normalizedCard || x.CardCode == rawCard || x.PhoneNumber == rawCard || x.PhoneNumber == normalizedCard) &&
                    !x.IsDeleted);
            }

            if (client == null)
            {
                Debug.WriteLine($"[ProcessEntryAsync] Không tìm thấy người dùng cho thẻ: '{normalizedCard}' (raw: '{rawCard}')");
                return new ProcessResult { Status = ProcessStatus.ClientNotFound, Message = "Không tìm thấy người dùng." };
            }

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

            // Lấy danh sách xe đã đăng ký của khách hàng (nếu có)
            List<Vehicle> clientVehicles = [];
            if (_vehicleRepository != null && !string.IsNullOrEmpty(client.Id))
            {
                var vehicles = await _vehicleRepository.FindAsync(v => v.OwnerClientId == client.Id && v.IsActive && !v.IsDeleted);
                clientVehicles = vehicles?.ToList() ?? [];
            }

            // Khối xác thực biển số: Quyết định dựa trên cờ VerifyVehiclePlate của Client
            bool requirePlateVerification = client.VerifyVehiclePlate;

            if (requirePlateVerification && _vehicleRepository != null && clientVehicles.Count == 0)
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

            // Luôn chụp ảnh Camera Biển Số và Toàn Cảnh để lưu vết
            var (plateSuccess, plateImage, overviewSuccess, overviewImage) =
                await CaptureCamerasParallelAsync(cameras, capturePlateCamera: true, timeoutMs: 2500);

            string recognizedPlate = "";
            LprResult? lprResult = null;
            Vehicle? matchedVehicle = null;

            if (plateSuccess && plateImage != null)
            {
                lprResult = await Task.Run(() => _lprService.Recognize(plateImage));
                if (lprResult != null && lprResult.Success && !string.IsNullOrWhiteSpace(lprResult.Plate))
                {
                    recognizedPlate = lprResult.Plate.Trim().ToUpper();
                }
            }

            if (!requirePlateVerification)
            {
                // Nếu khách không bắt buộc đối soát biển số:
                // Ưu tiên lấy biển số nhận diện từ camera, nếu camera không đọc được thì fallback sang biển số đăng ký
                if (string.IsNullOrEmpty(recognizedPlate))
                {
                    recognizedPlate = defaultPlate;
                }
            }
            else if (!plateSuccess || plateImage == null || string.IsNullOrEmpty(recognizedPlate))
            {
                // Yêu cầu xác thực nhưng camera lỗi hoặc OCR không ra biển số -> Cho bảo vệ nhập tay
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
                        Status = (!plateSuccess || plateImage == null) ? ProcessStatus.CaptureFailed : ProcessStatus.LprFailed,
                        Message = manualCancelled ? "" : "Không nhận diện được biển số và không có biển số nhập tay.",
                        Client = client,
                        Vehicle = clientVehicles.FirstOrDefault(),
                        DepartmentName = departmentName
                    };
                }
            }

            if (requirePlateVerification)
            {
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

            DateTime timeIn = (data.Time != default && data.Time != DateTime.MinValue) ? data.Time : DateTime.Now;
            var parking = new ParkingSession
            {
                PersonId = client.Id,
                PlateNumber = !requirePlateVerification ? defaultPlate : (matchedVehicle?.PlateNumber ?? recognizedPlate ?? ""),
                VehicleType = matchedVehicle?.Type ?? VehicleType.Car,
                InTime = timeIn,
                InLaneName = context.Lane.Name,
                Status = ParkingSessionStatus.Active
            };

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
            string rawCard = data.CardNo?.Trim() ?? string.Empty;
            string normalizedCard = CardHelper.NormalizeCardCode(rawCard);

            Debug.WriteLine($"[ProcessExitAsync] Quẹt thẻ: CardNo='{data.CardNo}', Raw='{rawCard}', Normalized='{normalizedCard}'");

            if (!CardHelper.IsValidCardCode(rawCard) && !CardHelper.IsValidCardCode(normalizedCard))
            {
                // Silently ignore noise events (sensor 0, barrier open/close logs)
                return new ProcessResult { Status = ProcessStatus.ClientNotFound, Message = string.Empty };
            }

            // 1. Phân nhánh Thẻ Xe Công Vụ / Thẻ định danh qua Card repository
            var cardEntity = await _cardRepository.FindOneAsync(c =>
                (c.CardNumber == normalizedCard || c.CardNumber == rawCard) &&
                !c.IsDeleted);

            if (cardEntity != null && cardEntity.TargetType == CardTargetType.Vehicle)
            {
                Debug.WriteLine($"[ProcessExitAsync] Đã nhận diện Thẻ Xe Công Vụ: Card='{cardEntity.CardNumber}', VehicleId='{cardEntity.VehicleId}'");
                if (!string.IsNullOrEmpty(cardEntity.VehicleId))
                {
                    return await ProcessSharedVehicleTripAsync(context, cardEntity, data, imageBasePath, onBarrierOpenFailed, onManualPlateInput);
                }
            }

            // 2. Tìm kiếm nhân sự/khách hàng qua ClientId (từ Card) hoặc trực tiếp CardCode / PhoneNumber
            Client? client = null;
            if (cardEntity != null && cardEntity.TargetType == CardTargetType.Person && !string.IsNullOrEmpty(cardEntity.ClientId))
            {
                client = await _clientRepository.GetByIdAsync(cardEntity.ClientId);
            }

            if (client == null || client.IsDeleted)
            {
                client = await _clientRepository.FindOneAsync(x =>
                    (x.CardCode == normalizedCard || x.CardCode == rawCard || x.PhoneNumber == rawCard || x.PhoneNumber == normalizedCard) &&
                    !x.IsDeleted);
            }

            if (client == null)
            {
                Debug.WriteLine($"[ProcessExitAsync] Không tìm thấy khách hàng cho thẻ: '{normalizedCard}' (raw: '{rawCard}')");
                return new ProcessResult { Status = ProcessStatus.ClientNotFound, Message = "Không tìm thấy người dùng." };
            }

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

            // Khối xác thực biển số: Quyết định theo VerifyVehiclePlate của Client
            bool requirePlateVerification = client.VerifyVehiclePlate;

            // Luôn chụp ảnh Camera Biển Số và Toàn Cảnh để lưu vết
            var (plateSuccess, plateImage, overviewSuccess, overviewImage) =
                await CaptureCamerasParallelAsync(context.Cameras, capturePlateCamera: true, timeoutMs: 2500);

            string exitPlate = "";
            LprResult? lprResult = null;

            if (plateSuccess && plateImage != null)
            {
                lprResult = await Task.Run(() => _lprService.Recognize(plateImage));
                if (lprResult != null && lprResult.Success && !string.IsNullOrWhiteSpace(lprResult.Plate))
                {
                    exitPlate = lprResult.Plate.Trim().ToUpper();
                }
            }

            if (!requirePlateVerification)
            {
                // Ưu tiên lấy biển nhận diện từ camera, nếu không có thì lấy biển số gửi lúc vào
                if (string.IsNullOrEmpty(exitPlate))
                {
                    exitPlate = parking.PlateNumber ?? "";
                }
            }
            else
            {
                string cleanInPlate = (parking.PlateNumber ?? "").Replace(" ", "").Replace("-", "").Replace(".", "").ToUpperInvariant();
                string cleanExitPlate = (exitPlate ?? "").Replace(" ", "").Replace("-", "").Replace(".", "").ToUpperInvariant();

                // Trường hợp A: Camera không chụp được hoặc OCR không đọc được chữ -> Cho bảo vệ nhập tay
                if (string.IsNullOrEmpty(cleanExitPlate))
                {
                    bool manualCancelled = false;
                    if (onManualPlateInput != null)
                    {
                        string promptPlate = parking.PlateNumber ?? "";
                        string? manual = await onManualPlateInput(context, promptPlate);
                        if (!string.IsNullOrWhiteSpace(manual))
                        {
                            exitPlate = manual.Trim().ToUpper();
                            cleanExitPlate = exitPlate.Replace(" ", "").Replace("-", "").Replace(".", "").ToUpperInvariant();
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

                    if (string.IsNullOrEmpty(cleanExitPlate))
                    {
                        plateImage?.Dispose();
                        overviewImage?.Dispose();
                        return new ProcessResult
                        {
                            Status = (!plateSuccess || plateImage == null) ? ProcessStatus.CaptureFailed : ProcessStatus.LprFailed,
                            Message = manualCancelled ? "" : "Không nhận diện được biển số xe ra và không có biển số nhập tay hợp lệ.",
                            Client = client,
                            Vehicle = matchedVehicle,
                            DepartmentName = departmentName,
                            ParkingSession = parking,
                            LprResult = lprResult
                        };
                    }
                }

                // Trường hợp B: Đã có biển số -> So sánh với biển số lúc vào
                if (cleanExitPlate != cleanInPlate)
                {
                    plateImage?.Dispose();
                    overviewImage?.Dispose();
                    return new ProcessResult
                    {
                        Status = ProcessStatus.PlateMismatch,
                        Message = $"Biển số xe ra ({exitPlate}) không khớp với biển số xe lúc vào ({parking.PlateNumber}).",
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