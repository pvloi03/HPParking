using HPParking.Core.Helpers;
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
        ILogger<ClientVehicleWorkflowHandler>? logger = null,
        IWorkflowImageStorageOrchestrator? imageOrchestrator = null) : IClientVehicleWorkflowHandler
    {
        private readonly IRepository<Vehicle> _vehicleRepository = vehicleRepository;
        private readonly IRepository<ParkingSession> _sessionRepository = sessionRepository;
        private readonly IImageStorageService _imageStorageService = imageStorageService;
        private readonly ILaneHardwareOrchestrator _hardwareOrchestrator = hardwareOrchestrator;
        private readonly ILogger<ClientVehicleWorkflowHandler>? _logger = logger;
        private readonly IWorkflowImageStorageOrchestrator _imageOrchestrator = imageOrchestrator ??
            new WorkflowImageStorageOrchestrator(imageStorageService, sessionRepository, logger);

        public async Task<ProcessResult> ProcessEntryAsync(ClientVehicleExecutionContext request)
        {
            var (context, trigger, client, imageBasePath, onBarrierOpenFailed, onManualPlateInput, departmentName) = request;

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
            var clientVehicles = await GetActiveClientVehiclesAsync(client.Id);

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
                    return BuildResult(
                        !capturedPlate ? ProcessStatus.CaptureFailed : ProcessStatus.LprFailed,
                        "Không nhận diện được biển số và không có biển số nhập tay.",
                        client,
                        clientVehicles.FirstOrDefault(),
                        departmentName,
                        null,
                        images,
                        lprResult);
                }

                // Đối soát biển số qua phương thức giàu hành vi Client.FindMatchingVehicle
                matchedVehicle = client.FindMatchingVehicle(recognizedPlate, clientVehicles);

                if (matchedVehicle == null && clientVehicles.Count > 0)
                {
                    return BuildResult(
                        ProcessStatus.PlateMismatch,
                        "Biển số xe không đúng với biển số đăng ký.",
                        client,
                        clientVehicles.FirstOrDefault(),
                        departmentName,
                        null,
                        images,
                        lprResult);
                }
            }

            // 5. Thao tác mở thanh chắn Barrier
            if (!_hardwareOrchestrator.TryOpenBarrier(context, onBarrierOpenFailed))
            {
                return BuildResult(
                    ProcessStatus.BarrierFailed,
                    "Không thể mở barrier. Vui lòng kiểm tra thiết bị.",
                    client,
                    matchedVehicle ?? clientVehicles.FirstOrDefault(),
                    departmentName,
                    null,
                    images,
                    lprResult);
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
            _imageOrchestrator.SaveSessionImagesBackground(parking, images, isEntry: true, imageBasePath, client: client);

            return BuildResult(
                ProcessStatus.Success,
                string.Empty,
                client,
                matchedVehicle ?? clientVehicles.FirstOrDefault(),
                departmentName,
                parking,
                images,
                lprResult);
        }

        public async Task<ProcessResult> ProcessExitAsync(ClientVehicleExecutionContext request)
        {
            var (context, trigger, client, imageBasePath, onBarrierOpenFailed, onManualPlateInput, departmentName) = request;

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
            var clientVehicles = await GetActiveClientVehiclesAsync(client.Id);

            Vehicle? matchedVehicle = client.FindMatchingVehicle(parking.PlateNumber, clientVehicles);

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
                matchedVehicle ??= client.FindMatchingVehicle(exitPlate, clientVehicles) ?? clientVehicles.FirstOrDefault();
            }
            else
            {
                Vehicle vehicleInSession = matchedVehicle ?? new Vehicle { PlateNumber = parking.PlateNumber ?? string.Empty };

                if (string.IsNullOrEmpty(PlateHelper.Normalize(exitPlate)))
                {
                    bool capturedPlate = images.Plate != null;
                    return BuildResult(
                        !capturedPlate ? ProcessStatus.CaptureFailed : ProcessStatus.LprFailed,
                        "Không nhận diện được biển số ra và không có biển số nhập tay.",
                        client,
                        matchedVehicle ?? clientVehicles.FirstOrDefault(),
                        departmentName,
                        parking,
                        images,
                        lprResult);
                }

                // Strict Exit Lockout: Biển số ra PHẢI khớp với biển số lúc vào của phiên gửi xe theo quy chuẩn Vehicle.MatchesPlate.
                // Tuyệt đối không cho phép tráo sang xe khác của cùng một chủ xe.
                bool plateMatches = vehicleInSession.MatchesPlate(exitPlate);

                if (!plateMatches)
                {
                    return BuildResult(
                        ProcessStatus.PlateMismatch,
                        $"Biển số ra ({exitPlate}) không khớp với biển số vào ({parking.PlateNumber}).",
                        client,
                        matchedVehicle ?? client.FindMatchingVehicle(exitPlate, clientVehicles) ?? clientVehicles.FirstOrDefault(),
                        departmentName,
                        parking,
                        images,
                        lprResult);
                }

                matchedVehicle = vehicleInSession;
            }

            // 5. Thao tác mở thanh chắn Barrier
            if (!_hardwareOrchestrator.TryOpenBarrier(context, onBarrierOpenFailed))
            {
                return BuildResult(
                    ProcessStatus.BarrierFailed,
                    "Không thể mở barrier. Vui lòng kiểm tra thiết bị.",
                    client,
                    matchedVehicle,
                    departmentName,
                    parking,
                    images,
                    lprResult);
            }

            // 6. Cập nhật hoàn tất phiên gửi xe
            parking.OutTime = (trigger.TriggerTime != default && trigger.TriggerTime != DateTime.MinValue) ? trigger.TriggerTime : DateTime.Now;
            parking.OutLaneName = context.Lane?.Name ?? string.Empty;
            parking.Status = ParkingSessionStatus.Completed;
            parking.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(parking);
            _imageOrchestrator.SaveSessionImagesBackground(parking, images, isEntry: false, imageBasePath, client: client);

            return BuildResult(
                ProcessStatus.Success,
                string.Empty,
                client,
                matchedVehicle,
                departmentName,
                parking,
                images,
                lprResult);
        }

        private async Task<List<Vehicle>> GetActiveClientVehiclesAsync(string? clientId)
        {
            if (_vehicleRepository == null || string.IsNullOrEmpty(clientId))
                return [];

            var vehicles = await _vehicleRepository.FindAsync(v =>
                v.OwnerClientId == clientId &&
                v.IsActive &&
                !v.IsDeleted);

            return vehicles?.ToList() ?? [];
        }

        private ProcessResult BuildResult(
            ProcessStatus status,
            string message,
            Client? client,
            Vehicle? vehicle,
            string? departmentName,
            ParkingSession? session,
            CapturedLaneImages images,
            LprResult? lprResult)
        {
            var (smallPlate, faceSnap, overviewSnap) = _hardwareOrchestrator.ExtractWorkflowImages(images, lprResult);
            return new ProcessResult
            {
                Status = status,
                Message = message,
                Client = client,
                Vehicle = vehicle,
                DepartmentName = departmentName ?? string.Empty,
                ParkingSession = session,
                LprResult = lprResult,
                PlateImage = smallPlate,
                FaceImage = faceSnap,
                OverviewImage = overviewSnap
            };
        }
    }
}
