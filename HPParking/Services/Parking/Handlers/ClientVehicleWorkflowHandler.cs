using HPParking.Core.Helpers;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Core.Services.Pricing;
using HPParking.Interfaces;
using HPParking.Models;
using HPParking.Services.Hardware;
using HPParking.Services.LPR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HPParking.Services.Parking.Handlers
{
    /// <summary>
    /// Trình xử lý chuyên trách quy trình kiểm soát phương tiện cá nhân gắn với khách hàng / nhân sự (Client)
    /// </summary>
    public class ClientVehicleWorkflowHandler : IClientVehicleWorkflowHandler
    {
        private readonly IRepository<Vehicle> _vehicleRepository;
        private readonly IRepository<ParkingSession> _sessionRepository;
        private readonly ILaneHardwareOrchestrator _hardwareOrchestrator;
        private readonly IWorkflowImageStorageOrchestrator _imageOrchestrator;
        private readonly IRepository<Tariff>? _tariffRepository;
        private readonly IParkingFeeCalculator? _feeCalculator;
        private readonly ILogger<ClientVehicleWorkflowHandler>? _logger;
        private readonly ITariffCache _tariffCache;

        [ActivatorUtilitiesConstructor]
        public ClientVehicleWorkflowHandler(
            IRepository<Vehicle> vehicleRepository,
            IRepository<ParkingSession> sessionRepository,
            IImageStorageService imageStorageService,
            ILaneHardwareOrchestrator hardwareOrchestrator,
            IRepository<Tariff> tariffRepository,
            IParkingFeeCalculator feeCalculator,
            ILogger<ClientVehicleWorkflowHandler>? logger = null,
            IWorkflowImageStorageOrchestrator? imageOrchestrator = null,
            ITariffCache? tariffCache = null)
        {
            _vehicleRepository = vehicleRepository;
            _sessionRepository = sessionRepository;
            _hardwareOrchestrator = hardwareOrchestrator;
            _tariffRepository = tariffRepository;
            _feeCalculator = feeCalculator;
            _logger = logger;
            _imageOrchestrator = imageOrchestrator ??
                new WorkflowImageStorageOrchestrator(imageStorageService, sessionRepository, logger);
            _tariffCache = tariffCache ?? new TariffCache(tariffRepository);
        }

        /// <summary>
        /// Testing/Fallback Constructor: Dùng cho các kịch bản kiểm thử không cần dịch vụ tính phí hoặc caller cũ.
        /// Đặt internal để DI Container chỉ nhận biết constructor chính [ActivatorUtilitiesConstructor],
        /// đảm bảo fail-fast InvalidOperationException tại thời điểm khởi động nếu thiếu đăng ký service.
        /// Khi thẻ có IsChargeable = true, handler sẽ fail-closed và trả về TariffError theo ADR-0043 §3.1.
        /// </summary>
        internal ClientVehicleWorkflowHandler(
            IRepository<Vehicle> vehicleRepository,
            IRepository<ParkingSession> sessionRepository,
            IImageStorageService imageStorageService,
            ILaneHardwareOrchestrator hardwareOrchestrator,
            ILogger<ClientVehicleWorkflowHandler>? logger = null,
            IWorkflowImageStorageOrchestrator? imageOrchestrator = null)
            : this(
                vehicleRepository,
                sessionRepository,
                imageStorageService,
                hardwareOrchestrator,
                tariffRepository: null!,
                feeCalculator: null!,
                logger: logger,
                imageOrchestrator: imageOrchestrator,
                tariffCache: null)
        {
        }

        public async Task<ProcessResult> ProcessEntryAsync(ClientVehicleExecutionContext request)
        {
            var (context, trigger, client, imageBasePath, onBarrierOpenFailed, onManualPlateInput, departmentName, card, _) = request;

            if (client == null)
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.ClientNotFound,
                    Message = "Không tìm thấy người dùng."
                };
            }

            // 0. Kiểm tra thẻ nếu có
            if (card != null && !card.CanPassGate(trigger.TriggerTime, LaneDirection.In, out string cardReason))
            {
                return new ProcessResult
                {
                    Status = card.IsExpired(trigger.TriggerTime) ? ProcessStatus.CardExpired : ProcessStatus.AccessDenied,
                    Message = cardReason,
                    Client = client,
                    Card = card,
                    DepartmentName = departmentName ?? string.Empty
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
                    Card = card,
                    DepartmentName = departmentName ?? string.Empty
                };
            }

            // 2. Kiểm tra phiên gửi đang hoạt động (Anti-passback xe vào)
            var parkingInProgress = await _sessionRepository.FindOneAsync(x =>
                ((card != null && x.CardId == card.Id) || x.PersonId == client.Id) &&
                x.TargetType == LaneTargetType.Vehicle &&
                x.Status == ParkingSessionStatus.Active &&
                !x.IsDeleted);

            if (parkingInProgress != null)
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.AlreadyInParking,
                    Message = "Khách hàng này đang có xe trong bãi.",
                    Client = client,
                    Card = card,
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
                    Card = card,
                    DepartmentName = departmentName ?? string.Empty
                };
            }

            string defaultPlate = clientVehicles.Count > 0
                ? string.Join("; ", clientVehicles.Select(v => v.PlateNumber).Where(p => !string.IsNullOrWhiteSpace(p)))
                : "";

            // 4. Chụp ảnh song song đa camera và nhận diện biển số LPR
            bool needFace = (context.Lane?.HasFaceDevice() ?? false) && client.UsesFaceAuth();

            var images = await _hardwareOrchestrator.CaptureLaneImagesAsync(context,
                needOverview: context.Lane?.UseOverviewCam ?? true,
                needPlate: context.Lane?.UsePlateCam ?? true,
                needFace: needFace);

            var (plateSuccess, recognizedPlate, lprResult) = await _hardwareOrchestrator.RecognizePlateAsync(
                context, images.Plate, requirePlateVerification ? defaultPlate : string.Empty, requirePlateVerification ? onManualPlateInput : null);

            Vehicle? matchedVehicle = null;

            if (!requirePlateVerification)
            {
                if (!string.IsNullOrWhiteSpace(recognizedPlate))
                {
                    matchedVehicle = client.FindMatchingVehicle(recognizedPlate, clientVehicles);
                }
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
                        lprResult,
                        card: card);
                }

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
                        lprResult,
                        card: card);
                }
            }

            // 5. Ghi nhận phiên đỗ xe (ParkingSession) mới vào CSDL trước (Save-First)
            string sessionPlate = matchedVehicle?.PlateNumber
                ?? (!string.IsNullOrWhiteSpace(recognizedPlate) ? recognizedPlate : string.Empty);

            bool isManualPlateIn = lprResult?.IsManualOverride == true;
            string rawInPlateOcr = lprResult?.RawPlateText ?? (plateSuccess ? recognizedPlate : string.Empty);

            var inOrderNumber = await _sessionRepository.GetNextInOrderNumberAsync(LaneTargetType.Vehicle);
            var parking = new ParkingSession
            {
                PersonId = client.Id,
                CardId = card?.Id,
                CardNumber = card?.CardNumber,
                PlateNumber = sessionPlate,
                RawInPlateOcr = rawInPlateOcr,
                IsManualPlateIn = isManualPlateIn,
                TariffId = (card?.IsChargeable == true) ? card.TariffId : null,
                VehicleType = matchedVehicle?.Type ?? clientVehicles.FirstOrDefault()?.Type ?? VehicleType.Car,
                TargetType = LaneTargetType.Vehicle,
                InOrderNumber = inOrderNumber,
                InTime = VietnamTime.ToUtcInstant(
                    (trigger.TriggerTime != default && trigger.TriggerTime != DateTime.MinValue) ? trigger.TriggerTime : DateTime.Now),
                InLaneName = context.Lane?.Name ?? string.Empty,
                Status = ParkingSessionStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _sessionRepository.AddAsync(parking);
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                return BuildResult(
                    ProcessStatus.AlreadyInParking,
                    "Phiên đỗ xe của thẻ/khách hàng đã tồn tại (DuplicateKey).",
                    client,
                    matchedVehicle ?? clientVehicles.FirstOrDefault(),
                    departmentName,
                    null,
                    images,
                    lprResult,
                    card: card);
            }

            // 6. Thao tác mở thanh chắn Barrier
            if (client.ShouldAutoOpenBarrier(LaneDirection.In) && !_hardwareOrchestrator.TryOpenBarrier(context, onBarrierOpenFailed))
            {
                // Barrier thất bại -> Rollback xóa sạch phiên khỏi CSDL (Hard delete, xe chưa vào bãi)
                try
                {
                    await _sessionRepository.DeleteAsync(parking.Id, softDelete: false);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Lỗi rollback phiên gửi xe {SessionId} sau khi mở barrier thất bại", parking.Id);
                }

                return BuildResult(
                    ProcessStatus.BarrierFailed,
                    "Không thể mở barrier. Vui lòng kiểm tra thiết bị.",
                    client,
                    matchedVehicle ?? clientVehicles.FirstOrDefault(),
                    departmentName,
                    null,
                    images,
                    lprResult,
                    card: card);
            }

            _imageOrchestrator.SaveSessionImagesBackground(parking, images, isEntry: true, imageBasePath, client: client);

            return BuildResult(
                ProcessStatus.Success,
                string.Empty,
                client,
                matchedVehicle ?? clientVehicles.FirstOrDefault(),
                departmentName,
                parking,
                images,
                lprResult,
                card: card);
        }

        public async Task<ProcessResult> ProcessExitAsync(ClientVehicleExecutionContext request)
        {
            var (context, trigger, client, imageBasePath, onBarrierOpenFailed, onManualPlateInput, departmentName, card, onFeeConfirmationRequired) = request;

            if (client == null)
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.ClientNotFound,
                    Message = "Không tìm thấy người dùng."
                };
            }

            // 0. Kiểm tra thẻ nếu có (Hướng Out: Thẻ tháng hết hạn vẫn cho ra, kèm cảnh báo; Thẻ Locked/Lost thì chặn)
            string? cardExitWarning = null;
            if (card != null)
            {
                bool canPass = card.CanPassGate(trigger.TriggerTime, LaneDirection.Out, out string cardReason);
                if (!canPass)
                {
                    return new ProcessResult
                    {
                        Status = ProcessStatus.AccessDenied,
                        Message = cardReason,
                        Client = client,
                        Card = card,
                        DepartmentName = departmentName ?? string.Empty
                    };
                }

                if (!string.IsNullOrEmpty(cardReason))
                {
                    cardExitWarning = cardReason;
                }
            }

            // 1. Kiểm tra điều kiện qua cổng
            if (!client.CanPassGate(trigger.TriggerTime, out string reason))
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.ConfirmRequired,
                    Message = reason,
                    Client = client,
                    Card = card,
                    DepartmentName = departmentName ?? string.Empty
                };
            }

            // 2. Kiểm tra phiên gửi đang hoạt động (Anti-passback xe ra)
            var parking = await _sessionRepository.FindOneAsync(x =>
                ((card != null && x.CardId == card.Id) || x.PersonId == client.Id) &&
                x.TargetType == LaneTargetType.Vehicle &&
                x.Status == ParkingSessionStatus.Active &&
                !x.IsDeleted);

            if (parking == null)
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.NotInParking,
                    Message = "Khách hàng này không có xe trong bãi.",
                    Client = client,
                    Card = card,
                    DepartmentName = departmentName ?? string.Empty
                };
            }

            // 3. Tra cứu xe tương ứng với phiên gửi
            var clientVehicles = await GetActiveClientVehiclesAsync(client.Id);
            Vehicle? matchedVehicle = client.FindMatchingVehicle(parking.PlateNumber, clientVehicles);

            bool requirePlateVerification = client.RequiresPlateVerification();
            bool needFace = (context.Lane?.HasFaceDevice() ?? false) && client.UsesFaceAuth();

            var images = await _hardwareOrchestrator.CaptureLaneImagesAsync(context,
                needOverview: context.Lane?.UseOverviewCam ?? true,
                needPlate: context.Lane?.UsePlateCam ?? true,
                needFace: needFace);

            var (plateSuccess, exitPlate, lprResult) = await _hardwareOrchestrator.RecognizePlateAsync(
                context, images.Plate, string.Empty, null);

            bool isManualPlateOut = false;
            string rawOutPlateOcr = lprResult?.RawPlateText ?? (plateSuccess ? exitPlate : string.Empty);

            // 4. Đối soát biển số ra và thực thi chính sách Strict Exit Lockout
            if (requirePlateVerification)
            {
                bool plateMatchesInitially = matchedVehicle != null
                    ? matchedVehicle.MatchesPlate(exitPlate)
                    : PlateHelper.Matches(parking.PlateNumber, exitPlate);

                if (!plateMatchesInitially && onManualPlateInput != null)
                {
                    string? manual = await onManualPlateInput(context, rawOutPlateOcr);
                    if (!string.IsNullOrWhiteSpace(manual))
                    {
                        exitPlate = manual.Trim().ToUpper();
                        isManualPlateOut = true;
                        matchedVehicle = client.FindMatchingVehicle(exitPlate, clientVehicles)
                            ?? client.FindMatchingVehicle(parking.PlateNumber, clientVehicles)
                            ?? clientVehicles.FirstOrDefault();
                    }
                }

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
                        lprResult,
                        card: card);
                }

                bool finalMatches = matchedVehicle != null
                    ? matchedVehicle.MatchesPlate(exitPlate)
                    : PlateHelper.Matches(parking.PlateNumber, exitPlate);

                if (!finalMatches)
                {
                    return BuildResult(
                        ProcessStatus.PlateMismatch,
                        $"Biển số ra ({exitPlate}) không khớp với biển số vào ({parking.PlateNumber}).",
                        client,
                        matchedVehicle ?? client.FindMatchingVehicle(exitPlate, clientVehicles) ?? clientVehicles.FirstOrDefault(),
                        departmentName,
                        parking,
                        images,
                        lprResult,
                        card: card);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(exitPlate))
                {
                    matchedVehicle = client.FindMatchingVehicle(exitPlate, clientVehicles)
                        ?? client.FindMatchingVehicle(parking.PlateNumber, clientVehicles)
                        ?? clientVehicles.FirstOrDefault();
                }
                else
                {
                    matchedVehicle = client.FindMatchingVehicle(parking.PlateNumber, clientVehicles)
                        ?? clientVehicles.FirstOrDefault();
                }
            }

            // 5. Tính phí nếu thẻ tính phí
            // Chuẩn hóa thành mốc UTC trước khi ghi CSDL (InTime/OutTime là true-UTC) & tính phí (ADR 0024 §1.1)
            DateTime outTime = VietnamTime.ToUtcInstant(
                (trigger.TriggerTime != default && trigger.TriggerTime != DateTime.MinValue) ? trigger.TriggerTime : DateTime.Now);
            decimal feeAmount = parking.FeeAmount;
            bool isFeeCollected = parking.IsFeeCollected;
            string tariffName = string.Empty;

            if (card?.IsChargeable == true)
            {
                if (_feeCalculator == null || _tariffRepository == null)
                {
                    return BuildResult(
                        ProcessStatus.TariffError,
                        "Dịch vụ tính phí chưa sẵn sàng.",
                        client, matchedVehicle, departmentName, parking, images, lprResult,
                        card: card, hasTariffWarning: true);
                }

                string? effectiveTariffId = parking.TariffId ?? card.TariffId;
                if (string.IsNullOrEmpty(effectiveTariffId))
                {
                    return BuildResult(
                        ProcessStatus.TariffError,
                        "Thẻ tính phí nhưng không có thông tin biểu phí.",
                        client, matchedVehicle, departmentName, parking, images, lprResult,
                        card: card, hasTariffWarning: true);
                }

                var tariff = await _tariffCache.GetCachedTariffAsync(effectiveTariffId);
                if (tariff == null || !tariff.IsActive)
                {
                    return BuildResult(
                        ProcessStatus.TariffError,
                        "Không tìm thấy biểu phí hoặc biểu phí bị vô hiệu hóa.",
                        client, matchedVehicle, departmentName, parking, images, lprResult,
                        card: card, hasTariffWarning: true);
                }

                tariffName = tariff.Name;
                var feeResult = _feeCalculator.Calculate(card, tariff, parking.InTime ?? trigger.TriggerTime, outTime);
                if (feeResult.HasTariffWarning)
                {
                    return BuildResult(
                        ProcessStatus.TariffError,
                        feeResult.WarningMessage ?? "Lỗi cấu hình biểu phí.",
                        client, matchedVehicle, departmentName, parking, images, lprResult,
                        card: card, hasTariffWarning: true);
                }

                feeAmount = feeResult.FeeAmount;
            }
            else
            {
                feeAmount = 0m;
            }

            string zeroText = card?.TargetType == CardTargetType.VIP ? "VE VIP"
                            : (card != null ? "VE THANG" : "MIEN PHI");
            context.DisplayFee(feeAmount, zeroText);

            // 6. Bỏ qua xác nhận qua popup TaskDialog (Phương án 1: Tự động ghi nhận thu phí)
            if (feeAmount > 0)
            {
                isFeeCollected = true;
            }

            // 7. Save-First: Cập nhật hoàn tất phiên gửi xe vào CSDL trước
            parking.OutTime = outTime;
            parking.OutLaneName = context.Lane?.Name ?? string.Empty;
            parking.RawOutPlateOcr = rawOutPlateOcr;
            parking.IsManualPlateOut = isManualPlateOut;
            parking.FeeAmount = feeAmount;
            parking.IsFeeCollected = isFeeCollected || (feeAmount == 0m);
            parking.Status = ParkingSessionStatus.Completed;
            parking.CloseReason = SessionCloseReason.Normal;
            parking.UpdatedAt = DateTime.UtcNow;

            await _sessionRepository.UpdateAsync(parking);

            // 8. Mở Barrier
            bool shouldOpenBarrier = client.ShouldAutoOpenBarrier(LaneDirection.Out);
            if (shouldOpenBarrier && !_hardwareOrchestrator.TryOpenBarrier(context, onBarrierOpenFailed))
            {
                // Barrier thất bại -> Rollback về Active nhưng giữ nguyên FeeAmount và IsFeeCollected
                parking.Status = ParkingSessionStatus.Active;
                parking.OutTime = null;
                parking.OutLaneName = null;
                parking.CloseReason = null;
                parking.UpdatedAt = DateTime.UtcNow;

                try
                {
                    await _sessionRepository.UpdateAsync(parking);
                }
                catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
                {
                    _logger?.LogCritical(ex, "Rollback phiên {SessionId} về Active gặp DuplicateKey cho khách {ClientId}", parking.Id, client.Id);
                }

                return BuildResult(
                    ProcessStatus.BarrierFailed,
                    "Không thể mở barrier. Vui lòng kiểm tra thiết bị.",
                    client,
                    matchedVehicle ?? clientVehicles.FirstOrDefault(),
                    departmentName,
                    parking,
                    images,
                    lprResult,
                    card: card,
                    feeAmount: feeAmount,
                    isFeeCollected: feeAmount > 0);
            }

            _imageOrchestrator.SaveSessionImagesBackground(parking, images, isEntry: false, imageBasePath, client: client);

            string successMessage = cardExitWarning ?? string.Empty;
            return BuildResult(
                ProcessStatus.Success,
                successMessage,
                client,
                matchedVehicle,
                departmentName,
                parking,
                images,
                lprResult,
                card: card,
                feeAmount: feeAmount);
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
            LprResult? lprResult,
            Card? card = null,
            decimal feeAmount = 0m,
            bool hasTariffWarning = false,
            bool isFeeCollected = false)
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
                OverviewImage = overviewSnap,
                Card = card,
                FeeAmount = feeAmount,
                IsFeeCollected = isFeeCollected || (session?.IsFeeCollected ?? false),
                HasTariffWarning = hasTariffWarning
            };
        }
    }
}
