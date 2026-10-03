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

namespace HPParking.Services.Parking.Handlers
{
    public class SharedVehicleWorkflowHandler(
        IRepository<VehicleDispatchTrip> tripRepository,
        IRepository<Vehicle> vehicleRepository,
        IRepository<GateRouteConfig> gateRouteRepository,
        IRepository<Gate> gateRepository,
        IImageStorageService imageStorageService,
        ILaneHardwareOrchestrator hardwareOrchestrator) : ISharedVehicleWorkflowHandler
    {
        private readonly IRepository<VehicleDispatchTrip> _tripRepository = tripRepository;
        private readonly IRepository<Vehicle> _vehicleRepository = vehicleRepository;
        private readonly IRepository<GateRouteConfig> _gateRouteRepository = gateRouteRepository;
        private readonly IRepository<Gate> _gateRepository = gateRepository;
        private readonly IImageStorageService _imageStorageService = imageStorageService;
        private readonly ILaneHardwareOrchestrator _hardwareOrchestrator = hardwareOrchestrator;

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

            string registeredPlateNorm = _hardwareOrchestrator.NormalizePlate(vehicle.PlateNumber);
            string detectedPlateNorm = _hardwareOrchestrator.NormalizePlate(detectedPlate);

            if (string.IsNullOrEmpty(detectedPlateNorm))
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

            if (detectedPlateNorm != registeredPlateNorm)
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

            if (activeTrip == null)
            {
                // Chưa có chuyến đang chạy: Chỉ được phép bắt đầu chuyến bằng cách quẹt RA
                if (isEntry)
                {
                    var (warnSmallPlate, warnFaceSnap, warnOverviewSnap) = _hardwareOrchestrator.ExtractWorkflowImages(images, lprResult);
                    return new ProcessResult
                    {
                        Status = ProcessStatus.ConfirmRequired,
                        Vehicle = vehicle,
                        DepartmentName = assignedRoute != null ? $"Tuyến: {assignedRoute.RouteName}" : "Phương tiện nội bộ / Điều vận",
                        LprResult = lprResult,
                        OverviewImage = warnOverviewSnap,
                        PlateImage = warnSmallPlate,
                        FaceImage = warnFaceSnap,
                        Message = $"CẢNH BÁO XE ĐI SAI TUYẾN: Phương tiện {vehicle.PlateNumber} chưa có bản ghi quẹt ra nhưng lại quẹt vào cổng {currentGateName}!",
                        DispatchTrip = null
                    };
                }

                // Xuất phát chuyến mới
                int firstDeadlineMinutes = defaultTravel;
                if (assignedRoute != null && assignedRoute.GateSteps.Count > 0)
                {
                    var firstStep = assignedRoute.GateSteps.FirstOrDefault(s => s.StepIndex == 1);
                    if (firstStep != null) firstDeadlineMinutes = firstStep.MaxTravelMinutes;
                }

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
                        var (warnSmallPlate, warnFaceSnap, warnOverviewSnap) = _hardwareOrchestrator.ExtractWorkflowImages(images, lprResult);
                        return new ProcessResult
                        {
                            Status = ProcessStatus.ConfirmRequired,
                            Vehicle = vehicle,
                            DepartmentName = assignedRoute != null ? $"Tuyến: {assignedRoute.RouteName}" : "Phương tiện nội bộ / Điều vận",
                            LprResult = lprResult,
                            OverviewImage = warnOverviewSnap,
                            PlateImage = warnSmallPlate,
                            FaceImage = warnFaceSnap,
                            Message = $"CẢNH BÁO XE ĐI SAI TUYẾN: Phương tiện {vehicle.PlateNumber} chưa có bản ghi quẹt ra khỏi cổng trước đó nhưng lại quẹt vào cổng {currentGateName}!",
                            DispatchTrip = activeTrip
                        };
                    }
                }
                else
                {
                    if (activeTrip.Status == TripStatus.InTransit || activeTrip.Status == TripStatus.OverdueTransit)
                    {
                        var (warnSmallPlate, warnFaceSnap, warnOverviewSnap) = _hardwareOrchestrator.ExtractWorkflowImages(images, lprResult);
                        return new ProcessResult
                        {
                            Status = ProcessStatus.ConfirmRequired,
                            Vehicle = vehicle,
                            DepartmentName = assignedRoute != null ? $"Tuyến: {assignedRoute.RouteName}" : "Phương tiện nội bộ / Điều vận",
                            LprResult = lprResult,
                            OverviewImage = warnOverviewSnap,
                            PlateImage = warnSmallPlate,
                            FaceImage = warnFaceSnap,
                            Message = $"CẢNH BÁO XE ĐI SAI TUYẾN: Phương tiện {vehicle.PlateNumber} chưa có bản ghi quẹt vào cổng tiếp theo nhưng lại quẹt ra!",
                            DispatchTrip = activeTrip
                        };
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

                    SaveImagesBackground(images, isEntry, imageBasePath, onSaved: (pPath, oPath, fPath) =>
                    {
                        _ = Task.Run(async () =>
                        {
                            if (activeTrip != null && (!string.IsNullOrEmpty(oPath) || !string.IsNullOrEmpty(pPath)))
                            {
                                devCheckpoint.OverviewImagePath = oPath;
                                devCheckpoint.PlateImagePath = pPath;
                                await _tripRepository.UpdateAsync(activeTrip);
                            }
                        });
                    });

                    var (warnSmallPlate, warnFaceSnap, warnOverviewSnap) = _hardwareOrchestrator.ExtractWorkflowImages(images, lprResult);
                    return new ProcessResult
                    {
                        Status = ProcessStatus.ConfirmRequired,
                        Vehicle = vehicle,
                        DepartmentName = $"Tuyến: {assignedRoute?.RouteName ?? "Lạc tuyến"}",
                        LprResult = lprResult,
                        OverviewImage = warnOverviewSnap,
                        PlateImage = warnSmallPlate,
                        FaceImage = warnFaceSnap,
                        Message = $"CẢNH BÁO LẠC TUYẾN: Xe {vehicle.PlateNumber} quẹt tại {currentGateName} không đúng lộ trình tuyến {assignedRoute?.RouteName}!",
                        DispatchTrip = activeTrip
                    };
                }

                // Tính toán vi phạm SLA của chặng vừa hoàn thành TRƯỚC KHI cập nhật trạng thái/NextDeadline mới
                slaInfo = activeTrip.CalculateOverdue(now);

                // Lộ trình hợp lệ -> Áp dụng chuyển đổi máy trạng thái qua Rich Domain Model
                if (isEntry)
                {
                    bool isCompleted = activeTrip.IsTripCompletedOnEntry(assignedRoute, currentGateId);
                    int stayMinutes = defaultStay;
                    if (assignedRoute != null && assignedRoute.GateSteps.Count > 0)
                    {
                        var currentStep = assignedRoute.GateSteps.FirstOrDefault(s => s.StepIndex == activeTrip.CurrentStepIndex);
                        if (currentStep != null) stayMinutes = currentStep.MaxStayMinutes;
                    }
                    activeTrip.ArriveAtGate(currentGateId, stayMinutes, isCompleted, now);
                }
                else
                {
                    int nextTravelMinutes = defaultTravel;
                    if (assignedRoute != null && assignedRoute.GateSteps.Count > 0)
                    {
                        var nextStep = assignedRoute.GateSteps.FirstOrDefault(s => s.StepIndex == (activeTrip.CurrentStepIndex + 1));
                        if (nextStep != null) nextTravelMinutes = nextStep.MaxTravelMinutes;
                    }
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
                    DepartmentName = assignedRoute != null ? $"Tuyến: {assignedRoute.RouteName}" : "Phương tiện nội bộ / Điều vận",
                    LprResult = lprResult,
                    OverviewImage = overviewSnap,
                    PlateImage = smallPlate,
                    FaceImage = faceSnap,
                    DispatchTrip = activeTrip
                };
            }

            // 9. Lưu ảnh nền
            SaveImagesBackground(images, isEntry, imageBasePath, onSaved: (pPath, oPath, fPath) =>
            {
                _ = Task.Run(async () =>
                {
                    if (activeTrip != null && (!string.IsNullOrEmpty(oPath) || !string.IsNullOrEmpty(pPath)))
                    {
                        currentCheckpoint.OverviewImagePath = oPath;
                        currentCheckpoint.PlateImagePath = pPath;

                        try
                        {
                            var tripFilter = Builders<VehicleDispatchTrip>.Filter.And(
                                Builders<VehicleDispatchTrip>.Filter.Eq(t => t.Id, activeTrip.Id),
                                Builders<VehicleDispatchTrip>.Filter.ElemMatch(t => t.Checkpoints,
                                    c => c.StepIndex == currentCheckpoint.StepIndex && c.Direction == currentCheckpoint.Direction)
                            );

                            var updateDef = Builders<VehicleDispatchTrip>.Update
                                .Set("Checkpoints.$.OverviewImagePath", oPath)
                                .Set("Checkpoints.$.PlateImagePath", pPath);

                            var updated = await _tripRepository.UpdateOneAsync(tripFilter, updateDef);
                            if (!updated)
                            {
                                await _tripRepository.UpdateAsync(activeTrip);
                            }
                        }
                        catch
                        {
                            await _tripRepository.UpdateAsync(activeTrip);
                        }
                    }
                });
            });

            // 10. Trả về kết quả thành công
            string routeDesc = assignedRoute != null ? assignedRoute.RouteName : "Tuyến tự do";
            int totalSteps = assignedRoute?.GateSteps.Count ?? 1;
            var (succSmallPlate, succFaceSnap, succOverviewSnap) = _hardwareOrchestrator.ExtractWorkflowImages(images, lprResult);

            return new ProcessResult
            {
                Status = ProcessStatus.Success,
                Vehicle = vehicle,
                DepartmentName = $"Tuyến: {routeDesc} (Chặng {activeTrip.CurrentStepIndex}/{totalSteps})",
                LprResult = lprResult,
                OverviewImage = succOverviewSnap,
                PlateImage = succSmallPlate,
                FaceImage = succFaceSnap,
                Message = $"Phương tiện nội bộ {vehicle.PlateNumber} - Chặng {activeTrip.CurrentStepIndex}/{totalSteps} ({activeTrip.Status})",
                DispatchTrip = activeTrip
            };
        }

        private void SaveImagesBackground(
            CapturedLaneImages images,
            bool isEntry,
            string imageBasePath,
            Action<string, string, string>? onSaved = null)
        {
            Bitmap? plateSave = images.Plate != null ? (Bitmap)images.Plate.Clone() : null;
            Bitmap? overviewSave = images.Overview != null ? (Bitmap)images.Overview.Clone() : null;
            Bitmap? faceSave = images.Face != null ? (Bitmap)images.Face.Clone() : null;

            string folder = isEntry ? "ImageIn" : "ImageOut";

            _ = Task.Run(() =>
            {
                try
                {
                    using (plateSave)
                    using (overviewSave)
                    using (faceSave)
                    {
                        string pPath = plateSave != null
                            ? _imageStorageService.SaveImage(plateSave, folder, "BienSo", imageBasePath)
                            : "";
                        string oPath = overviewSave != null
                            ? _imageStorageService.SaveImage(overviewSave, folder, "ToanCanh", imageBasePath)
                            : "";
                        string fPath = faceSave != null
                            ? _imageStorageService.SaveImage(faceSave, folder, "KhuonMat", imageBasePath)
                            : "";

                        onSaved?.Invoke(pPath, oPath, fPath);
                    }
                }
                catch
                {
                    // Catch exception trong background thread
                }
            });
        }
    }
}
