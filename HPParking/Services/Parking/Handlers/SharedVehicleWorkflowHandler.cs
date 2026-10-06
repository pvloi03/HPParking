using HPParking.Core.Helpers;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Common;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Interfaces;
using HPParking.Models;
using HPParking.Services.Controller;
using HPParking.Services.Hardware;
using HPParking.Services.LPR;
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;

namespace HPParking.Services.Parking.Handlers
{
    public class SharedVehicleWorkflowHandler(
        IRepository<VehicleDispatchTrip> tripRepository,
        IRepository<Vehicle> vehicleRepository,
        IRepository<GateRouteConfig> gateRouteRepository,
        IRepository<Gate> gateRepository,
        IImageStorageService imageStorageService,
        ILaneHardwareOrchestrator hardwareOrchestrator,
        Microsoft.Extensions.Logging.ILogger<SharedVehicleWorkflowHandler>? logger = null,
        IWorkflowImageStorageOrchestrator? imageOrchestrator = null) : ISharedVehicleWorkflowHandler
    {
        private readonly IRepository<VehicleDispatchTrip> _tripRepository = tripRepository;
        private readonly IRepository<Vehicle> _vehicleRepository = vehicleRepository;
        private readonly IRepository<GateRouteConfig> _gateRouteRepository = gateRouteRepository;
        private readonly IRepository<Gate> _gateRepository = gateRepository;
        private readonly ILaneHardwareOrchestrator _hardwareOrchestrator = hardwareOrchestrator;
        private readonly Microsoft.Extensions.Logging.ILogger<SharedVehicleWorkflowHandler>? _logger = logger;
        private readonly IWorkflowImageStorageOrchestrator _imageOrchestrator = imageOrchestrator ??
            new WorkflowImageStorageOrchestrator(imageStorageService, null, logger);

        public async Task<ProcessResult> ProcessSharedVehicleTripAsync(
            LaneRuntimeContext context,
            WorkflowTriggerEvent trigger,
            Card vehicleCard,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed = null,
            Func<LaneRuntimeContext, string?, Task<string?>>? onManualPlateInput = null)
        {
            // 1. Kiểm tra tính hợp lệ của phương tiện gắn với thẻ
            if (string.IsNullOrEmpty(vehicleCard.VehicleId))
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.ClientNotFound,
                    Message = "Thẻ phương tiện nội bộ chưa được gán xe."
                };
            }

            var vehicle = await _vehicleRepository.GetByIdAsync(vehicleCard.VehicleId);
            if (vehicle == null || !vehicle.IsActive)
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.ClientNotFound,
                    Message = "Phương tiện nội bộ gắn với thẻ này không tồn tại hoặc đã bị khóa."
                };
            }

            string currentGateId = context.Lane?.GateId ?? string.Empty;
            bool isEntry = context.Direction == LaneDirection.In;

            Gate? currentGate = !string.IsNullOrEmpty(currentGateId) ? await _gateRepository.GetByIdAsync(currentGateId) : null;
            string currentGateName = currentGate?.Name ?? (string.IsNullOrEmpty(currentGateId) ? "Cổng không xác định" : currentGateId);

            // 2. Tra cứu chuyến đi đang hoạt động
            VehicleDispatchTrip? activeTrip = await _tripRepository.FindOneAsync(t =>
                t.VehicleId == vehicle.Id &&
                t.Status != TripStatus.Completed &&
                !t.IsDeleted);

            // 3. Chụp ảnh làn và nhận diện biển số LPR
            var images = await _hardwareOrchestrator.CaptureLaneImagesAsync(context,
                needOverview: context.Lane?.UseOverviewCam ?? true,
                needPlate: context.Lane?.UsePlateCam ?? true,
                needFace: context.Lane != null && (context.Lane.UseFaceCam || !string.IsNullOrEmpty(context.Lane.FaceDeviceId)));

            var (plateSuccess, detectedPlate, lprResult) = await _hardwareOrchestrator.RecognizePlateAsync(
                context, images.Plate, vehicle.PlateNumber ?? string.Empty, onManualPlateInput);

            if (string.IsNullOrEmpty(PlateHelper.Normalize(detectedPlate)))
            {
                bool capturedPlate = images.Plate != null;
                var (smallPlate, faceSnap, overviewSnap) = _hardwareOrchestrator.ExtractWorkflowImages(images, lprResult);
                return new ProcessResult
                {
                    Status = !capturedPlate ? ProcessStatus.CaptureFailed : ProcessStatus.LprFailed,
                    Message = $"Không nhận diện được biển số phương tiện {vehicle.PlateNumber} và không có biển số nhập tay.",
                    Vehicle = vehicle,
                    DepartmentName = "Không nhận diện được biển số",
                    LprResult = lprResult,
                    DispatchTrip = activeTrip,
                    PlateImage = smallPlate,
                    FaceImage = faceSnap,
                    OverviewImage = overviewSnap
                };
            }

            if (!vehicle.MatchesPlate(detectedPlate))
            {
                var (smallPlate, faceSnap, overviewSnap) = _hardwareOrchestrator.ExtractWorkflowImages(images, lprResult);
                return new ProcessResult
                {
                    Status = ProcessStatus.PlateMismatch,
                    Message = "BIỂN SỐ KHÔNG ĐÚNG VỚI BIỂN SỐ ĐÃ ĐĂNG KÝ!",
                    Vehicle = vehicle,
                    DepartmentName = "Cảnh báo sai biển số phương tiện",
                    LprResult = lprResult,
                    DispatchTrip = activeTrip,
                    PlateImage = smallPlate,
                    FaceImage = faceSnap,
                    OverviewImage = overviewSnap
                };
            }

            // 4. Xác định cấu hình tuyến đường
            GateRouteConfig? assignedRoute = null;
            if (!string.IsNullOrEmpty(vehicle.AssignedRouteId))
            {
                assignedRoute = await _gateRouteRepository.GetByIdAsync(vehicle.AssignedRouteId);
            }
            assignedRoute ??= await _gateRouteRepository.FindOneAsync(r => (r.IsDefault || r.RouteCode == "DEFAULT") && !r.IsDeleted);

            int defaultTravel = assignedRoute?.DefaultTravelMinutes ?? 15;
            int defaultStay = assignedRoute?.DefaultStayMinutes ?? 15;

            DateTime now = (trigger.TriggerTime != default && trigger.TriggerTime != DateTime.MinValue)
                ? (trigger.TriggerTime.Kind == DateTimeKind.Utc ? trigger.TriggerTime : trigger.TriggerTime.ToUniversalTime())
                : DateTime.UtcNow;

            // 5. PRE-VALIDATION & CHUYỂN ĐỔI MÁY TRẠNG THÁI (RICH DOMAIN MODEL)
            bool isNewTrip = false;
            SlaOverdueInfo slaInfo;
            string defaultDeptName = assignedRoute != null ? $"Tuyến: {assignedRoute.RouteName}" : "Phương tiện nội bộ / Điều vận";

            if (activeTrip == null)
            {
                // Chưa có chuyến đang chạy: Chỉ được phép bắt đầu chuyến bằng cách quẹt RA
                if (isEntry)
                {
                    return CreateConfirmRequiredResult(
                        vehicle,
                        defaultDeptName,
                        $"CẢNH BÁO XE ĐI SAI TUYẾN: Phương tiện {vehicle.PlateNumber} chưa có bản ghi quẹt ra nhưng lại quẹt vào cổng {currentGateName}!",
                        images,
                        lprResult,
                        dispatchTrip: null);
                }

                // Kiểm tra xuất phát đúng cổng của tuyến cố định
                if (assignedRoute != null && !assignedRoute.IsFreeRoam)
                {
                    string? originGateId = assignedRoute.GetOriginGateId();
                    if (!string.IsNullOrEmpty(originGateId) &&
                        !string.Equals(originGateId, currentGateId, StringComparison.OrdinalIgnoreCase))
                    {
                        string originGateName = assignedRoute.GetOriginGateDisplayName();

                        return CreateConfirmRequiredResult(
                            vehicle,
                            $"Tuyến: {assignedRoute.RouteName}",
                            $"CẢNH BÁO SAI CỔNG XUẤT PHÁT: Phương tiện {vehicle.PlateNumber} phải xuất phát tại cổng {originGateName}!",
                            images,
                            lprResult,
                            dispatchTrip: null);
                    }
                }

                // Xuất phát chuyến mới: Sử dụng domain method trên assignedRoute
                int firstDeadlineMinutes = assignedRoute?.GetTravelMinutesForLeg(1) ?? defaultTravel;

                activeTrip = new VehicleDispatchTrip
                {
                    VehicleId = vehicle.Id,
                    PlateNumber = vehicle.PlateNumber ?? string.Empty,
                    CardId = vehicleCard.Id,
                    CardNumber = vehicleCard.CardNumber,
                    AssignedRouteId = assignedRoute?.Id
                };

                // Đóng gói hành vi xuất phát qua Rich Domain Model
                activeTrip.Start(originGateId: currentGateId, travelMinutes: firstDeadlineMinutes, now: now);
                isNewTrip = true;
                slaInfo = new SlaOverdueInfo { IsOverdue = false, OverdueSeconds = 0 };
            }
            else
            {
                // Đang có chuyến hoạt động: Chống quẹt kép toàn diện
                if (isEntry)
                {
                    if (activeTrip.Status == TripStatus.WorkingAtGate || activeTrip.Status == TripStatus.OverdueStay)
                    {
                        return CreateConfirmRequiredResult(
                            vehicle,
                            defaultDeptName,
                            $"CẢNH BÁO XE ĐI SAI TUYẾN: Phương tiện {vehicle.PlateNumber} chưa có bản ghi quẹt ra khỏi cổng trước đó nhưng lại quẹt vào cổng {currentGateName}!",
                            images,
                            lprResult,
                            activeTrip);
                    }
                }
                else
                {
                    if (activeTrip.Status == TripStatus.InTransit || activeTrip.Status == TripStatus.OverdueTransit)
                    {
                        return CreateConfirmRequiredResult(
                            vehicle,
                            defaultDeptName,
                            $"CẢNH BÁO XE ĐI SAI TUYẾN: Phương tiện {vehicle.PlateNumber} chưa có bản ghi quẹt vào cổng tiếp theo nhưng lại quẹt ra!",
                            images,
                            lprResult,
                            activeTrip);
                    }
                }

                // PRE-VALIDATION: Kiểm tra tuân thủ lộ trình trước khi thay đổi trạng thái entity
                bool isRouteCompliant = activeTrip.CheckRouteCompliance(assignedRoute, currentGateId, isEntry);
                if (!isRouteCompliant)
                {
                    var overdue = activeTrip.CalculateOverdue(now);
                    var devCheckpoint = activeTrip.RecordCheckpoint(
                        gateId: currentGateId,
                        gateName: currentGateName,
                        direction: context.Direction,
                        plateDetected: lprResult?.Plate ?? vehicle.PlateNumber,
                        isRouteCompliant: false,
                        note: isEntry ? "Quẹt vào lệch tuyến" : "Quẹt ra lệch cổng",
                        timestamp: now,
                        slaOverdue: overdue);

                    await _tripRepository.UpdateAsync(activeTrip);

                    _imageOrchestrator.SaveSessionImagesBackground(null, images, isEntry, imageBasePath, onSaved: (pPath, oPath, fPath) =>
                    {
                        _ = Task.Run(async () =>
                        {
                            await UpdateCheckpointImagePathsAsync(activeTrip.Id, devCheckpoint, oPath, pPath);
                        });
                    });

                    return CreateConfirmRequiredResult(
                        vehicle,
                        $"Tuyến: {assignedRoute?.RouteName ?? "Lạc tuyến"}",
                        $"CẢNH BÁO LẠC TUYẾN: Xe {vehicle.PlateNumber} quẹt tại {currentGateName} không đúng lộ trình tuyến {assignedRoute?.RouteName}!",
                        images,
                        lprResult,
                        activeTrip);
                }

                // Tính toán vi phạm SLA của chặng vừa hoàn thành TRƯỚC KHI cập nhật trạng thái/NextDeadline mới
                slaInfo = activeTrip.CalculateOverdue(now);

                // Lộ trình hợp lệ -> Áp dụng chuyển đổi máy trạng thái qua Rich Domain Model
                if (isEntry)
                {
                    bool isCompleted = activeTrip.IsTripCompletedOnEntry(assignedRoute, currentGateId);
                    int stayMinutes = assignedRoute?.GetStayMinutesForLeg(activeTrip.CurrentStepIndex) ?? defaultStay;
                    activeTrip.ArriveAtGate(currentGateId, stayMinutes, isCompleted, now);
                }
                else
                {
                    int nextTravelMinutes = assignedRoute?.GetTravelMinutesForLeg(activeTrip.CurrentStepIndex + 1) ?? defaultTravel;
                    activeTrip.DepartToNextStep(currentGateId, nextTravelMinutes, now);
                }
            }

            // 6. Ghi nhận mốc kiểm soát hợp lệ
            var currentCheckpoint = activeTrip.RecordCheckpoint(
                gateId: currentGateId,
                gateName: currentGateName,
                direction: context.Direction,
                plateDetected: lprResult?.Plate ?? vehicle.PlateNumber,
                isRouteCompliant: true,
                note: isEntry ? (activeTrip.Status == TripStatus.Completed ? "Quẹt vào kết thúc chuyến" : "Quẹt vào cổng") : "Quẹt ra khỏi cổng",
                timestamp: now,
                slaOverdue: slaInfo);

            // 7. Lưu CSDL
            if (isNewTrip)
            {
                await _tripRepository.AddAsync(activeTrip);
            }
            else
            {
                await _tripRepository.UpdateAsync(activeTrip);
            }

            // 8. Kích hoạt mở barrier
            if (!_hardwareOrchestrator.TryOpenBarrier(context, onBarrierOpenFailed))
            {
                var (smallPlate, faceSnap, overviewSnap) = _hardwareOrchestrator.ExtractWorkflowImages(images, lprResult);
                return new ProcessResult
                {
                    Status = ProcessStatus.BarrierFailed,
                    Message = "Không thể mở barrier cho phương tiện. Vui lòng kiểm tra thiết bị.",
                    Vehicle = vehicle,
                    DepartmentName = defaultDeptName,
                    LprResult = lprResult,
                    OverviewImage = overviewSnap,
                    PlateImage = smallPlate,
                    FaceImage = faceSnap,
                    DispatchTrip = activeTrip
                };
            }

            // 9. Lưu ảnh nền
            _imageOrchestrator.SaveSessionImagesBackground(null, images, isEntry, imageBasePath, onSaved: (pPath, oPath, fPath) =>
            {
                _ = Task.Run(async () =>
                {
                    await UpdateCheckpointImagePathsAsync(activeTrip.Id, currentCheckpoint, oPath, pPath);
                });
            });

            // 10. Trả về kết quả thành công
            string routeDesc = assignedRoute != null ? assignedRoute.RouteName : "Tuyến tự do";
            bool isFixedRoute = assignedRoute != null && !assignedRoute.IsFreeRoam;

            string departmentName;
            string message;
            if (isFixedRoute)
            {
                // isFixedRoute ⇒ !IsFreeRoam ⇒ GateSteps.Count > 0, không cần clamp
                int totalSteps = assignedRoute!.TotalSteps;
                departmentName = $"Tuyến: {routeDesc} (Chặng {activeTrip.CurrentStepIndex}/{totalSteps})";
                message = $"Phương tiện nội bộ {vehicle.PlateNumber} - Chặng {activeTrip.CurrentStepIndex}/{totalSteps} ({activeTrip.Status})";
            }
            else
            {
                departmentName = $"Tuyến: {routeDesc}";
                message = $"Phương tiện nội bộ {vehicle.PlateNumber} ({activeTrip.Status})";
            }

            var (succSmallPlate, succFaceSnap, succOverviewSnap) = _hardwareOrchestrator.ExtractWorkflowImages(images, lprResult);

            return new ProcessResult
            {
                Status = ProcessStatus.Success,
                Vehicle = vehicle,
                DepartmentName = departmentName,
                LprResult = lprResult,
                OverviewImage = succOverviewSnap,
                PlateImage = succSmallPlate,
                FaceImage = succFaceSnap,
                Message = message,
                DispatchTrip = activeTrip
            };
        }

        private ProcessResult CreateConfirmRequiredResult(
            Vehicle vehicle,
            string deptName,
            string message,
            CapturedLaneImages images,
            LprResult? lprResult,
            VehicleDispatchTrip? dispatchTrip = null)
        {
            var (smallPlate, faceSnap, overviewSnap) = _hardwareOrchestrator.ExtractWorkflowImages(images, lprResult);
            return new ProcessResult
            {
                Status = ProcessStatus.ConfirmRequired,
                Vehicle = vehicle,
                DepartmentName = deptName,
                Message = message,
                OverviewImage = overviewSnap,
                PlateImage = smallPlate,
                FaceImage = faceSnap,
                LprResult = lprResult,
                DispatchTrip = dispatchTrip
            };
        }

        private async Task UpdateCheckpointImagePathsAsync(
            string tripId,
            TripCheckpoint cp,
            string? overviewPath,
            string? platePath)
        {
            try
            {
                cp.OverviewImagePath = overviewPath;
                cp.PlateImagePath = platePath;

                var filter = Builders<VehicleDispatchTrip>.Filter.And(
                    Builders<VehicleDispatchTrip>.Filter.Eq(x => x.Id, tripId),
                    Builders<VehicleDispatchTrip>.Filter.ElemMatch(x => x.Checkpoints,
                        c => c.Id == cp.Id)
                );

                var update = Builders<VehicleDispatchTrip>.Update
                    .Set("Checkpoints.$.OverviewImagePath", overviewPath)
                    .Set("Checkpoints.$.PlateImagePath", platePath);

                var updated = await _tripRepository.UpdateOneAsync(filter, update);
                if (!updated)
                {
                    _logger?.LogWarning("Không tìm thấy checkpoint {CheckpointId} để cập nhật ảnh nền cho chuyến {TripId}", cp.Id, tripId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Lỗi khi cập nhật ảnh nền checkpoint {CheckpointId} cho chuyến {TripId}", cp.Id, tripId);
            }
        }
    }
}
