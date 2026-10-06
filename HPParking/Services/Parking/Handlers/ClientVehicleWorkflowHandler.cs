using HPParking.Core.Interfaces;
using HPParking.Core.Models.Common;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Interfaces;
using HPParking.Models;
using HPParking.Services.Hardware;
using HPParking.Services.LPR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace HPParking.Services.Parking.Handlers
{
    /// <summary>
    /// Trình xử lý chuyên trách quy trình kiểm soát phương tiện cá nhân gắn với khách hàng / nhân sự (Client)
    /// </summary>
    public class ClientVehicleWorkflowHandler(
        IRepository<Vehicle> vehicleRepository,
        IRepository<ParkingSession> sessionRepository,
        IImageStorageService imageStorageService,
        ILaneHardwareOrchestrator hardwareOrchestrator,
        ILogger<ClientVehicleWorkflowHandler>? logger = null) : IClientVehicleWorkflowHandler
    {
        private readonly IRepository<Vehicle> _vehicleRepository = vehicleRepository;
        private readonly IRepository<ParkingSession> _sessionRepository = sessionRepository;
        private readonly IImageStorageService _imageStorageService = imageStorageService;
        private readonly ILaneHardwareOrchestrator _hardwareOrchestrator = hardwareOrchestrator;
        private readonly ILogger<ClientVehicleWorkflowHandler>? _logger = logger;

        public async Task<ProcessResult> ProcessEntryAsync(
            LaneRuntimeContext context,
            WorkflowTriggerEvent trigger,
            Client client,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed = null,
            Func<LaneRuntimeContext, string?, Task<string?>>? onManualPlateInput = null,
            string? departmentName = null)
        {
            if (client == null)
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.ClientNotFound,
                    Message = "Không tìm thấy người dùng."
                };
            }

            // 1. Kiểm tra điều kiện qua cổng (Hạn sử dụng và trạng thái kích hoạt)
            if (!client.CanPassGate(trigger.TriggerTime, out string reason))
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.ConfirmRequired,
                    Message = reason,
                    Client = client,
                    DepartmentName = departmentName ?? string.Empty
                };
            }

            // 2. Kiểm tra phiên gửi đang hoạt động (Anti-passback xe vào)
            var parkingInProgress = await _sessionRepository.FindOneAsync(x =>
                x.PersonId == client.Id &&
                x.Status == ParkingSessionStatus.Active &&
                !x.IsDeleted);

            if (parkingInProgress != null)
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.AlreadyInParking,
                    Message = "Khách hàng này đang có xe trong bãi.",
                    Client = client,
                    DepartmentName = departmentName ?? string.Empty
                };
            }

            // 3. Tra cứu danh sách phương tiện đã đăng ký của khách hàng
            List<Vehicle> clientVehicles = [];
            if (_vehicleRepository != null && !string.IsNullOrEmpty(client.Id))
            {
                var vehicles = await _vehicleRepository.FindAsync(v =>
                    v.OwnerClientId == client.Id &&
                    v.IsActive &&
                    !v.IsDeleted);
                clientVehicles = vehicles?.ToList() ?? [];
            }

            bool requirePlateVerification = client.RequiresPlateVerification();
            if (requirePlateVerification && clientVehicles.Count == 0)
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.PlateMismatch,
                    Message = "Khách hàng chưa đăng ký biển số xe trong hệ thống.",
                    Client = client,
                    DepartmentName = departmentName ?? string.Empty
                };
            }

            string defaultPlate = clientVehicles.Count > 0
                ? string.Join("; ", clientVehicles.Select(v => v.PlateNumber).Where(p => !string.IsNullOrWhiteSpace(p)))
                : "";

            // 4. Chụp ảnh song song đa camera và nhận diện biển số LPR
            var images = await _hardwareOrchestrator.CaptureLaneImagesAsync(context,
                needOverview: context.Lane?.UseOverviewCam ?? true,
                needPlate: context.Lane?.UsePlateCam ?? true,
                needFace: context.Lane != null && (context.Lane.UseFaceCam || !string.IsNullOrEmpty(context.Lane.FaceDeviceId)));

            var (plateSuccess, recognizedPlate, lprResult) = await _hardwareOrchestrator.RecognizePlateAsync(
                context, images.Plate, defaultPlate, onManualPlateInput);

            Vehicle? matchedVehicle = null;

            if (!requirePlateVerification)
            {
                if (string.IsNullOrEmpty(recognizedPlate)) recognizedPlate = defaultPlate;
                matchedVehicle = client.FindMatchingVehicle(recognizedPlate, clientVehicles)
                    ?? clientVehicles.FirstOrDefault();
            }
            else
            {
                if (!plateSuccess || string.IsNullOrEmpty(recognizedPlate))
                {
                    bool capturedPlate = images.Plate != null;
                    var (smallPlate, faceSnap, overviewSnap) = _hardwareOrchestrator.ExtractWorkflowImages(images, lprResult);
                    return new ProcessResult
                    {
                        Status = !capturedPlate ? ProcessStatus.CaptureFailed : ProcessStatus.LprFailed,
                        Message = "Không nhận diện được biển số và không có biển số nhập tay.",
                        Client = client,
                        Vehicle = clientVehicles.FirstOrDefault(),
                        DepartmentName = departmentName ?? string.Empty,
                        LprResult = lprResult,
                        PlateImage = smallPlate,
                        FaceImage = faceSnap,
                        OverviewImage = overviewSnap
                    };
                }

                // Đối soát biển số qua phương thức giàu hành vi Client.FindMatchingVehicle
                matchedVehicle = client.FindMatchingVehicle(recognizedPlate, clientVehicles);

                if (matchedVehicle == null && clientVehicles.Count > 0)
                {
                    var (smallPlate, faceSnap, overviewSnap) = _hardwareOrchestrator.ExtractWorkflowImages(images, lprResult);
                    return new ProcessResult
                    {
                        Status = ProcessStatus.PlateMismatch,
                        Message = "Biển số xe không đúng với biển số đăng ký.",
                        Client = client,
                        Vehicle = clientVehicles.FirstOrDefault(),
                        DepartmentName = departmentName ?? string.Empty,
                        LprResult = lprResult,
                        PlateImage = smallPlate,
                        FaceImage = faceSnap,
                        OverviewImage = overviewSnap
                    };
                }
            }

            // 5. Thao tác mở thanh chắn Barrier
            if (!_hardwareOrchestrator.TryOpenBarrier(context, onBarrierOpenFailed))
            {
                var (smallPlate, faceSnap, overviewSnap) = _hardwareOrchestrator.ExtractWorkflowImages(images, lprResult);
                return new ProcessResult
                {
                    Status = ProcessStatus.BarrierFailed,
                    Message = "Không thể mở barrier. Vui lòng kiểm tra thiết bị.",
                    Client = client,
                    Vehicle = matchedVehicle ?? clientVehicles.FirstOrDefault(),
                    DepartmentName = departmentName ?? string.Empty,
                    LprResult = lprResult,
                    PlateImage = smallPlate,
                    FaceImage = faceSnap,
                    OverviewImage = overviewSnap
                };
            }

            // 6. Ghi nhận phiên đỗ xe (ParkingSession) mới
            var parking = new ParkingSession
            {
                PersonId = client.Id,
                PlateNumber = !requirePlateVerification ? defaultPlate : (matchedVehicle?.PlateNumber ?? recognizedPlate ?? ""),
                VehicleType = matchedVehicle?.Type ?? clientVehicles.FirstOrDefault()?.Type ?? VehicleType.Car,
                TargetType = LaneTargetType.Vehicle,
                InTime = (trigger.TriggerTime != default && trigger.TriggerTime != DateTime.MinValue) ? trigger.TriggerTime : DateTime.Now,
                InLaneName = context.Lane?.Name ?? string.Empty,
                Status = ParkingSessionStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            await _sessionRepository.AddAsync(parking);
            SaveImagesBackground(parking, images, isEntry: true, imageBasePath, client: client);

            var (succSmallPlate, succFaceSnap, succOverviewSnap) = _hardwareOrchestrator.ExtractWorkflowImages(images, lprResult);
            return new ProcessResult
            {
                Status = ProcessStatus.Success,
                Client = client,
                Vehicle = matchedVehicle ?? clientVehicles.FirstOrDefault(),
                DepartmentName = departmentName ?? string.Empty,
                LprResult = lprResult,
                ParkingSession = parking,
                OverviewImage = succOverviewSnap,
                PlateImage = succSmallPlate,
                FaceImage = succFaceSnap
            };
        }

        public async Task<ProcessResult> ProcessExitAsync(
            LaneRuntimeContext context,
            WorkflowTriggerEvent trigger,
            Client client,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed = null,
            Func<LaneRuntimeContext, string?, Task<string?>>? onManualPlateInput = null,
            string? departmentName = null)
        {
            if (client == null)
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.ClientNotFound,
                    Message = "Không tìm thấy người dùng."
                };
            }

            // 1. Kiểm tra điều kiện qua cổng
            if (!client.CanPassGate(trigger.TriggerTime, out string reason))
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.ConfirmRequired,
                    Message = reason,
                    Client = client,
                    DepartmentName = departmentName ?? string.Empty
                };
            }

            // 2. Kiểm tra phiên gửi đang hoạt động (Anti-passback xe ra)
            var parking = await _sessionRepository.FindOneAsync(x =>
                x.PersonId == client.Id &&
                x.Status == ParkingSessionStatus.Active &&
                !x.IsDeleted);

            if (parking == null)
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.NotInParking,
                    Message = "Khách hàng này không có xe trong bãi.",
                    Client = client,
                    DepartmentName = departmentName ?? string.Empty
                };
            }

            // 3. Tra cứu xe tương ứng với phiên gửi
            List<Vehicle> clientVehicles = [];
            if (_vehicleRepository != null && !string.IsNullOrEmpty(client.Id))
            {
                var vehicles = await _vehicleRepository.FindAsync(v =>
                    v.OwnerClientId == client.Id &&
                    v.IsActive &&
                    !v.IsDeleted);
                clientVehicles = vehicles?.ToList() ?? [];
            }

            Vehicle? matchedVehicle = client.FindMatchingVehicle(parking.PlateNumber, clientVehicles)
                ?? clientVehicles.FirstOrDefault();

            bool requirePlateVerification = client.RequiresPlateVerification();
            var images = await _hardwareOrchestrator.CaptureLaneImagesAsync(context,
                needOverview: context.Lane?.UseOverviewCam ?? true,
                needPlate: context.Lane?.UsePlateCam ?? true,
                needFace: context.Lane != null && (context.Lane.UseFaceCam || !string.IsNullOrEmpty(context.Lane.FaceDeviceId)));

            var (plateSuccess, exitPlate, lprResult) = await _hardwareOrchestrator.RecognizePlateAsync(
                context, images.Plate, parking.PlateNumber ?? "", onManualPlateInput);

            // 4. Đối soát biển số ra và thực thi chính sách Strict Exit Lockout
            if (!requirePlateVerification)
            {
                if (string.IsNullOrEmpty(exitPlate)) exitPlate = parking.PlateNumber ?? "";
            }
            else
            {
                string cleanInPlate = _hardwareOrchestrator.NormalizePlate(parking.PlateNumber);
                string cleanExitPlate = _hardwareOrchestrator.NormalizePlate(exitPlate);

                if (string.IsNullOrEmpty(cleanExitPlate))
                {
                    bool capturedPlate = images.Plate != null;
                    var (smallPlate, faceSnap, overviewSnap) = _hardwareOrchestrator.ExtractWorkflowImages(images, lprResult);
                    return new ProcessResult
                    {
                        Status = !capturedPlate ? ProcessStatus.CaptureFailed : ProcessStatus.LprFailed,
                        Message = "Không nhận diện được biển số ra và không có biển số nhập tay.",
                        Client = client,
                        Vehicle = matchedVehicle,
                        DepartmentName = departmentName ?? string.Empty,
                        ParkingSession = parking,
                        LprResult = lprResult,
                        PlateImage = smallPlate,
                        FaceImage = faceSnap,
                        OverviewImage = overviewSnap
                    };
                }

                bool plateMatches = (matchedVehicle != null && matchedVehicle.MatchesPlate(exitPlate))
                    || cleanExitPlate == cleanInPlate;

                if (!plateMatches)
                {
                    var (smallPlate, faceSnap, overviewSnap) = _hardwareOrchestrator.ExtractWorkflowImages(images, lprResult);
                    return new ProcessResult
                    {
                        Status = ProcessStatus.PlateMismatch,
                        Message = $"Biển số ra ({exitPlate}) không khớp với biển số vào ({parking.PlateNumber}).",
                        Client = client,
                        Vehicle = matchedVehicle,
                        DepartmentName = departmentName ?? string.Empty,
                        ParkingSession = parking,
                        LprResult = lprResult,
                        PlateImage = smallPlate,
                        FaceImage = faceSnap,
                        OverviewImage = overviewSnap
                    };
                }
            }

            // 5. Thao tác mở thanh chắn Barrier
            if (!_hardwareOrchestrator.TryOpenBarrier(context, onBarrierOpenFailed))
            {
                var (smallPlate, faceSnap, overviewSnap) = _hardwareOrchestrator.ExtractWorkflowImages(images, lprResult);
                return new ProcessResult
                {
                    Status = ProcessStatus.BarrierFailed,
                    Message = "Không thể mở barrier. Vui lòng kiểm tra thiết bị.",
                    Client = client,
                    Vehicle = matchedVehicle,
                    DepartmentName = departmentName ?? string.Empty,
                    ParkingSession = parking,
                    LprResult = lprResult,
                    PlateImage = smallPlate,
                    FaceImage = faceSnap,
                    OverviewImage = overviewSnap
                };
            }

            // 6. Cập nhật hoàn tất phiên gửi xe
            parking.OutTime = (trigger.TriggerTime != default && trigger.TriggerTime != DateTime.MinValue) ? trigger.TriggerTime : DateTime.Now;
            parking.OutLaneName = context.Lane?.Name ?? string.Empty;
            parking.Status = ParkingSessionStatus.Completed;
            parking.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(parking);
            SaveImagesBackground(parking, images, isEntry: false, imageBasePath, client: client);

            var (succSmallPlate, succFaceSnap, succOverviewSnap) = _hardwareOrchestrator.ExtractWorkflowImages(images, lprResult);
            return new ProcessResult
            {
                Status = ProcessStatus.Success,
                Client = client,
                Vehicle = matchedVehicle,
                DepartmentName = departmentName ?? string.Empty,
                ParkingSession = parking,
                LprResult = lprResult,
                OverviewImage = succOverviewSnap,
                PlateImage = succSmallPlate,
                FaceImage = succFaceSnap
            };
        }

        private static Bitmap? SafeClone(Bitmap? src)
        {
            if (src == null) return null;
            try
            {
                return (Bitmap)src.Clone();
            }
            catch
            {
                return null;
            }
        }

        private void SaveImagesBackground(
            ParkingSession? session,
            CapturedLaneImages images,
            bool isEntry,
            string imageBasePath,
            Action<string, string, string>? onSaved = null,
            Client? client = null)
        {
            Bitmap? plateSave = SafeClone(images?.Plate);
            Bitmap? overviewSave = SafeClone(images?.Overview);
            Bitmap? faceSave = SafeClone(images?.Face);

            string folder = isEntry ? "ImageIn" : "ImageOut";

            _ = Task.Run(async () =>
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

                        if (string.IsNullOrEmpty(fPath) && client != null && !string.IsNullOrWhiteSpace(client.Avatar))
                        {
                            fPath = client.Avatar.StartsWith("Avatar", StringComparison.OrdinalIgnoreCase)
                                ? client.Avatar
                                : $"Avatar/{client.Avatar}".Replace('\\', '/');
                        }

                        if (session != null)
                        {
                            if (isEntry)
                            {
                                session.InPlateImagePath = pPath;
                                session.InOverviewImagePath = oPath;
                                session.InFaceImagePath = fPath;
                            }
                            else
                            {
                                session.OutPlateImagePath = pPath;
                                session.OutOverviewImagePath = oPath;
                                session.OutFaceImagePath = fPath;
                            }
                            session.UpdatedAt = DateTime.UtcNow;

                            if (!string.IsNullOrEmpty(session.Id))
                            {
                                await _sessionRepository.UpdateAsync(session);
                            }
                        }

                        onSaved?.Invoke(pPath, oPath, fPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Lỗi khi lưu ảnh nền xe cá nhân");
                }
            });
        }
    }
}
