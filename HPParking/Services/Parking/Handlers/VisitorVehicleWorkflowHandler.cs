using HPParking.Core.Helpers;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Core.Services.Pricing;
using HPParking.Interfaces;
using HPParking.Models;
using HPParking.Services.Hardware;
using HPParking.Services.LPR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace HPParking.Services.Parking.Handlers
{
    /// <summary>
    /// Trình xử lý chuyên trách quy trình kiểm soát phương tiện vãng lai (Visitor Vehicle)
    /// </summary>
    public class VisitorVehicleWorkflowHandler : IVisitorVehicleWorkflowHandler
    {
        private readonly IRepository<Card> _cardRepository;
        private readonly IRepository<ParkingSession> _sessionRepository;
        private readonly IRepository<Tariff> _tariffRepository;
        private readonly IParkingFeeCalculator _feeCalculator;
        private readonly ILaneHardwareOrchestrator _hardwareOrchestrator;
        private readonly ILogger<VisitorVehicleWorkflowHandler>? _logger;
        private readonly IWorkflowImageStorageOrchestrator _imageOrchestrator;
        private readonly ITariffCache _tariffCache;

        public VisitorVehicleWorkflowHandler(
            IRepository<Card> cardRepository,
            IRepository<ParkingSession> sessionRepository,
            IRepository<Tariff> tariffRepository,
            IParkingFeeCalculator feeCalculator,
            ILaneHardwareOrchestrator hardwareOrchestrator,
            IImageStorageService imageStorageService,
            ILogger<VisitorVehicleWorkflowHandler>? logger = null,
            IWorkflowImageStorageOrchestrator? imageOrchestrator = null,
            ITariffCache? tariffCache = null)
        {
            _cardRepository = cardRepository;
            _sessionRepository = sessionRepository;
            _tariffRepository = tariffRepository;
            _feeCalculator = feeCalculator;
            _hardwareOrchestrator = hardwareOrchestrator;
            _logger = logger;
            _imageOrchestrator = imageOrchestrator ?? new WorkflowImageStorageOrchestrator(imageStorageService, sessionRepository, logger);
            _tariffCache = tariffCache ?? new TariffCache(tariffRepository);
        }

        public async Task<ProcessResult> ProcessEntryAsync(VisitorVehicleExecutionContext request)
        {
            var (context, trigger, card, imageBasePath, onBarrierOpenFailed, onManualPlateInput, _, autoOpenBarrier) = request;

            if (card == null)
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.ClientNotFound,
                    Message = "Không tìm thấy thẻ vãng lai."
                };
            }

            // 1. Kiểm tra điều kiện qua cổng của thẻ
            if (!card.CanPassGate(trigger.TriggerTime, LaneDirection.In, out string reason))
            {
                return BuildResult(ProcessStatus.AccessDenied, reason, card);
            }

            // 2. Anti-passback: Kiểm tra phiên đỗ xe đang Active của thẻ này
            var activeSession = await _sessionRepository.FindOneAsync(x =>
                x.CardId == card.Id &&
                x.TargetType == LaneTargetType.Vehicle &&
                x.Status == ParkingSessionStatus.Active &&
                !x.IsDeleted);

            if (activeSession != null)
            {
                return BuildResult(ProcessStatus.AlreadyInParking, "Thẻ vãng lai này đang có xe trong bãi.", card, activeSession);
            }

            // 3. Kiểm tra Biểu phí nếu thẻ tính phí
            if (card.IsChargeable)
            {
                if (string.IsNullOrEmpty(card.TariffId))
                {
                    return BuildResult(ProcessStatus.TariffError, "Thẻ tính phí nhưng chưa được gán biểu phí.", card, hasTariffWarning: true);
                }

                var tariff = await _tariffCache.GetCachedTariffAsync(card.TariffId);
                if (tariff == null || !tariff.IsActive)
                {
                    return BuildResult(ProcessStatus.TariffError, "Biểu phí được gán cho thẻ không tồn tại hoặc đã bị vô hiệu hóa.", card, hasTariffWarning: true);
                }
            }

            // 4. Chụp ảnh làn và nhận diện biển số LPR
            var images = await _hardwareOrchestrator.CaptureLaneImagesAsync(context,
                needOverview: context.Lane?.UseOverviewCam ?? true,
                needPlate: context.Lane?.UsePlateCam ?? true,
                needFace: false);

            var (plateSuccess, recognizedPlate, lprResult) = await _hardwareOrchestrator.RecognizePlateAsync(
                context, images.Plate, string.Empty, onManualPlateInput);

            bool isManualPlateIn = lprResult?.IsManualOverride == true;
            if (!plateSuccess || string.IsNullOrEmpty(recognizedPlate))
            {
                bool capturedPlate = images.Plate != null;
                return BuildResult(
                    !capturedPlate ? ProcessStatus.CaptureFailed : ProcessStatus.LprFailed,
                    "Không nhận diện được biển số và không có biển số nhập tay.",
                    card,
                    null,
                    images,
                    lprResult);
            }

            // 5. Ghi nhận ParkingSession vào CSDL trước (Save-First)
            var inOrderNumber = await _sessionRepository.GetNextInOrderNumberAsync(LaneTargetType.Vehicle);
            var session = new ParkingSession
            {
                CardId = card.Id,
                CardNumber = card.CardNumber,
                TargetType = LaneTargetType.Vehicle,
                VehicleType = card.VehicleType ?? VehicleType.Motorbike,
                PlateNumber = recognizedPlate,
                RawInPlateOcr = lprResult?.RawPlateText ?? (plateSuccess ? recognizedPlate : string.Empty),
                IsManualPlateIn = isManualPlateIn,
                TariffId = card.IsChargeable ? card.TariffId : null,
                InOrderNumber = inOrderNumber,
                InTime = VietnamTime.ToUtcInstant(
                    (trigger.TriggerTime != default && trigger.TriggerTime != DateTime.MinValue) ? trigger.TriggerTime : DateTime.Now),
                InLaneName = context.Lane?.Name ?? string.Empty,
                Status = ParkingSessionStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _sessionRepository.AddAsync(session);
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                return BuildResult(ProcessStatus.AlreadyInParking, "Thẻ này đã có phiên đỗ xe đang hoạt động (DuplicateKey).", card, session);
            }

            // 6. Thao tác mở thanh chắn Barrier (Visitor mặc định AutoOpenBarrier là false cả 2 làn)
            bool shouldOpenBarrier = autoOpenBarrier?.OnEntry ?? false;
            if (shouldOpenBarrier && !_hardwareOrchestrator.TryOpenBarrier(context, onBarrierOpenFailed))
            {
                // Barrier thất bại -> Rollback xóa sạch phiên khỏi CSDL (Hard delete, xe chưa vào bãi)
                try
                {
                    await _sessionRepository.DeleteAsync(session.Id, softDelete: false);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Lỗi rollback phiên gửi xe {SessionId} sau khi mở barrier thất bại", session.Id);
                }

                return BuildResult(
                    ProcessStatus.BarrierFailed,
                    "Không thể mở barrier. Vui lòng kiểm tra thiết bị.",
                    card,
                    null,
                    images,
                    lprResult);
            }

            // 7. Mở barrier thành công -> Cập nhật trạng thái thẻ sang InUse
            card.Status = CardStatus.InUse;
            card.UpdatedAt = DateTime.UtcNow;
            await _cardRepository.UpdateAsync(card);

            _imageOrchestrator.SaveSessionImagesBackground(session, images, isEntry: true, imageBasePath);

            return BuildResult(
                ProcessStatus.Success,
                string.Empty,
                card,
                session,
                images,
                lprResult);
        }

        public async Task<ProcessResult> ProcessExitAsync(VisitorVehicleExecutionContext request)
        {
            var (context, trigger, card, imageBasePath, onBarrierOpenFailed, onManualPlateInput, _, autoOpenBarrier) = request;

            if (card == null)
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.ClientNotFound,
                    Message = "Không tìm thấy thẻ vãng lai."
                };
            }

            // 1. Kiểm tra điều kiện ra cổng
            if (!card.CanPassGate(trigger.TriggerTime, LaneDirection.Out, out string reason))
            {
                return BuildResult(ProcessStatus.AccessDenied, reason, card);
            }

            // 2. Tra cứu phiên đỗ xe đang Active của thẻ này
            var parking = await _sessionRepository.FindOneAsync(x =>
                (x.CardId == card.Id || x.CardNumber == card.CardNumber) &&
                x.TargetType == LaneTargetType.Vehicle &&
                x.Status == ParkingSessionStatus.Active &&
                !x.IsDeleted);

            if (parking == null)
            {
                return BuildResult(ProcessStatus.NotInParking, "Thẻ này không có xe trong bãi.", card);
            }

            // 3. Chụp ảnh làn xe ra
            var images = await _hardwareOrchestrator.CaptureLaneImagesAsync(context,
                needOverview: context.Lane?.UseOverviewCam ?? true,
                needPlate: context.Lane?.UsePlateCam ?? true,
                needFace: false);

            // 4. Strict Exit Lockout: Nhận diện biển số ra và kiểm tra khớp biển số vào
            var (plateSuccess, exitPlate, lprResult) = await _hardwareOrchestrator.RecognizePlateAsync(
                context, images.Plate, string.Empty, null);

            bool isManualPlateOut = false;
            string rawOutOcrPlate = lprResult?.RawPlateText ?? (plateSuccess ? exitPlate : string.Empty);

            if (string.IsNullOrEmpty(exitPlate) || !PlateHelper.Matches(parking.PlateNumber, exitPlate))
            {
                if (onManualPlateInput != null)
                {
                    string? manual = await onManualPlateInput(context, rawOutOcrPlate);
                    if (!string.IsNullOrWhiteSpace(manual))
                    {
                        exitPlate = manual.Trim().ToUpper();
                        isManualPlateOut = true;
                    }
                }
            }

            if (string.IsNullOrEmpty(PlateHelper.Normalize(exitPlate)))
            {
                bool capturedPlate = images.Plate != null;
                return BuildResult(
                    !capturedPlate ? ProcessStatus.CaptureFailed : ProcessStatus.LprFailed,
                    "Không nhận diện được biển số ra và không có biển số nhập tay.",
                    card,
                    parking,
                    images,
                    lprResult);
            }

            if (!PlateHelper.Matches(parking.PlateNumber, exitPlate))
            {
                return BuildResult(
                    ProcessStatus.PlateMismatch,
                    $"Biển số ra ({exitPlate}) không khớp với biển số vào ({parking.PlateNumber}).",
                    card,
                    parking,
                    images,
                    lprResult);
            }

            // 5. Tính phí đỗ xe
            // Chuẩn hóa thành mốc UTC trước khi ghi CSDL (InTime/OutTime là true-UTC) & tính phí (ADR 0024 §1.1)
            DateTime outTime = VietnamTime.ToUtcInstant(
                (trigger.TriggerTime != default && trigger.TriggerTime != DateTime.MinValue) ? trigger.TriggerTime : DateTime.Now);
            decimal feeAmount = parking.FeeAmount;
            bool isFeeCollected = parking.IsFeeCollected;
            string tariffName = string.Empty;

            if (card.IsChargeable)
            {
                string? effectiveTariffId = parking.TariffId ?? card.TariffId;
                if (string.IsNullOrEmpty(effectiveTariffId))
                {
                    return BuildResult(ProcessStatus.TariffError, "Thẻ tính phí nhưng không có thông tin biểu phí.", card, parking, images, lprResult, hasTariffWarning: true);
                }

                var tariff = await _tariffCache.GetCachedTariffAsync(effectiveTariffId);
                if (tariff == null || !tariff.IsActive)
                {
                    return BuildResult(ProcessStatus.TariffError, "Không tìm thấy biểu phí hoặc biểu phí bị vô hiệu hóa.", card, parking, images, lprResult, hasTariffWarning: true);
                }

                tariffName = tariff.Name;
                var feeResult = _feeCalculator.Calculate(card, tariff, parking.InTime ?? trigger.TriggerTime, outTime);
                if (feeResult.HasTariffWarning)
                {
                    return BuildResult(ProcessStatus.TariffError, feeResult.WarningMessage ?? "Lỗi cấu hình biểu phí.", card, parking, images, lprResult, hasTariffWarning: true);
                }

                feeAmount = feeResult.FeeAmount;
            }
            else
            {
                feeAmount = 0m;
            }

            context.DisplayFee(feeAmount, "MIEN PHI");

            // 6. Bỏ qua xác nhận qua popup TaskDialog (Phương án 1: Tự động ghi nhận thu phí)
            if (feeAmount > 0)
            {
                isFeeCollected = true;
            }

            // 7. Cập nhật hoàn tất phiên gửi xe vào CSDL trước (Save-First)
            parking.OutTime = outTime;
            parking.OutLaneName = context.Lane?.Name ?? string.Empty;
            parking.RawOutPlateOcr = rawOutOcrPlate;
            parking.IsManualPlateOut = isManualPlateOut;
            parking.FeeAmount = feeAmount;
            parking.IsFeeCollected = isFeeCollected || (feeAmount == 0m);
            parking.Status = ParkingSessionStatus.Completed;
            parking.CloseReason = SessionCloseReason.Normal;
            parking.UpdatedAt = DateTime.UtcNow;

            await _sessionRepository.UpdateAsync(parking);

            // 8. Thao tác mở thanh chắn Barrier (Visitor mặc định AutoOpenBarrier là false cả 2 làn)
            bool shouldOpenBarrier = autoOpenBarrier?.OnExit ?? false;
            if (shouldOpenBarrier && !_hardwareOrchestrator.TryOpenBarrier(context, onBarrierOpenFailed))
            {
                // Mở barrier thất bại -> Rollback trạng thái phiên về Active nhưng GIỮ NGUYÊN FeeAmount và IsFeeCollected
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
                    _logger?.LogCritical(ex, "Rollback phiên {SessionId} về Active gặp DuplicateKey cho thẻ {CardNumber}", parking.Id, card.CardNumber);
                }

                return BuildResult(
                    ProcessStatus.BarrierFailed,
                    "Không thể mở barrier. Vui lòng kiểm tra thiết bị.",
                    card,
                    parking,
                    images,
                    lprResult,
                    feeAmount: feeAmount);
            }

            // 9. Mở barrier thành công -> Cập nhật trạng thái thẻ sang Available
            card.Status = CardStatus.Available;
            card.UpdatedAt = DateTime.UtcNow;
            await _cardRepository.UpdateAsync(card);

            _imageOrchestrator.SaveSessionImagesBackground(parking, images, isEntry: false, imageBasePath);

            return BuildResult(
                ProcessStatus.Success,
                string.Empty,
                card,
                parking,
                images,
                lprResult,
                feeAmount: feeAmount);
        }


        private ProcessResult BuildResult(
            ProcessStatus status,
            string message,
            Card? card = null,
            ParkingSession? session = null,
            CapturedLaneImages? images = null,
            LprResult? lprResult = null,
            decimal feeAmount = 0m,
            bool hasTariffWarning = false,
            bool isFeeCollected = false)
        {
            Bitmap? smallPlate = null;
            Bitmap? faceSnap = null;
            Bitmap? overviewSnap = null;

            if (images != null)
            {
                (smallPlate, faceSnap, overviewSnap) = _hardwareOrchestrator.ExtractWorkflowImages(images, lprResult);
            }

            return new ProcessResult
            {
                Status = status,
                Message = message,
                Card = card,
                ParkingSession = session,
                PlateImage = smallPlate,
                FaceImage = faceSnap,
                OverviewImage = overviewSnap,
                LprResult = lprResult,
                FeeAmount = feeAmount,
                IsFeeCollected = isFeeCollected || (session?.IsFeeCollected ?? false),
                HasTariffWarning = hasTariffWarning
            };
        }
    }
}
