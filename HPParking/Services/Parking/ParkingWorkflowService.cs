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
using HPParking.Services.Parking.Handlers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace HPParking.Services.Parking
{
    public class ParkingWorkflowService : IParkingWorkflowService
    {
        private readonly IRepository<Client> _clientRepository;
        private readonly IRepository<ParkingSession> _sessionRepository;
        private readonly ILprService _lprService;
        private readonly IRepository<Department> _departmentRepository;
        private readonly IRepository<Contractor> _contractorRepository;
        private readonly IRepository<Company> _companyRepository;
        private readonly IRepository<Vehicle> _vehicleRepository;
        private readonly IRepository<Card> _cardRepository;
        private readonly ISharedVehicleWorkflowHandler _sharedVehicleHandler;
        private readonly IClientVehicleWorkflowHandler _clientVehicleHandler;
        private readonly IWorkflowImageStorageOrchestrator _imageOrchestrator;

        /// <summary>
        /// Primary DI Constructor: Bộ điều phối gọn nhẹ (Lightweight Dispatcher) nhận trực tiếp các Handler chuyên trách.
        /// </summary>
        [ActivatorUtilitiesConstructor]
        public ParkingWorkflowService(
            IRepository<Client> clientRepository,
            IRepository<ParkingSession> sessionRepository,
            ILprService lprService,
            IRepository<Department> departmentRepository,
            IRepository<Contractor> contractorRepository,
            IRepository<Company> companyRepository,
            IRepository<Vehicle> vehicleRepository,
            IRepository<Card> cardRepository,
            ISharedVehicleWorkflowHandler sharedVehicleHandler,
            IClientVehicleWorkflowHandler clientVehicleHandler,
            IWorkflowImageStorageOrchestrator imageOrchestrator)
        {
            _clientRepository = clientRepository;
            _sessionRepository = sessionRepository;
            _lprService = lprService;
            _departmentRepository = departmentRepository;
            _contractorRepository = contractorRepository;
            _companyRepository = companyRepository;
            _vehicleRepository = vehicleRepository;
            _cardRepository = cardRepository;
            _sharedVehicleHandler = sharedVehicleHandler;
            _clientVehicleHandler = clientVehicleHandler;
            _imageOrchestrator = imageOrchestrator;
        }

        /// <summary>
        /// Fallback Constructor: Duy trì tương thích ngược 100% cho caller và bộ kiểm thử cũ, tự khởi tạo các handler mặc định.
        /// </summary>
        public ParkingWorkflowService(
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
            IRepository<Gate> gateRepository,
            ISharedVehicleWorkflowHandler? sharedVehicleHandler = null,
            IClientVehicleWorkflowHandler? clientVehicleHandler = null,
            IWorkflowImageStorageOrchestrator? imageOrchestrator = null)
            : this(
                clientRepository,
                sessionRepository,
                lprService,
                departmentRepository,
                contractorRepository,
                companyRepository,
                vehicleRepository,
                cardRepository,
                sharedVehicleHandler ?? new SharedVehicleWorkflowHandler(
                    tripRepository, vehicleRepository, gateRouteRepository, gateRepository,
                    imageStorageService, new LaneHardwareOrchestrator(lprService),
                    imageOrchestrator: imageOrchestrator ?? new WorkflowImageStorageOrchestrator(imageStorageService, sessionRepository)),
                clientVehicleHandler ?? new ClientVehicleWorkflowHandler(
                    vehicleRepository, sessionRepository, imageStorageService, new LaneHardwareOrchestrator(lprService),
                    imageOrchestrator: imageOrchestrator ?? new WorkflowImageStorageOrchestrator(imageStorageService, sessionRepository)),
                imageOrchestrator ?? new WorkflowImageStorageOrchestrator(imageStorageService, sessionRepository))
        {
        }

        #region --- 1. MASTER WORKFLOW DISPATCHER (TUPLE PATTERN MATCHING) ---

        public async Task<ProcessResult> ProcessWorkflowAsync(
            LaneRuntimeContext context,
            WorkflowTriggerEvent trigger,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed = null,
            Func<LaneRuntimeContext, string?, Task<string?>>? onManualPlateInput = null)
        {
            Card? cardEntity = null;
            Client? client = null;
            string? departmentName = null;

            // Kiểm tra thẻ hợp lệ và tra cứu danh tính nếu nguồn kích hoạt là quẹt thẻ
            if (trigger.Source == TriggerSource.CardSwipe)
            {
                string raw = trigger.RawCardNo?.Trim() ?? string.Empty;
                string norm = CardHelper.NormalizeCardCode(raw);
                if (!CardHelper.IsValidCardCode(raw) && !CardHelper.IsValidCardCode(norm))
                {
                    return new ProcessResult { Status = ProcessStatus.ClientNotFound, Message = string.Empty };
                }

                (cardEntity, _, client) = await ResolveIdentityAsync(raw);
                if (client != null)
                {
                    departmentName = await GetDepartmentNameAsync(client);
                }
            }

            return (context.Lane.TargetType, trigger.Source, context.Lane.Direction) switch
            {
                // 1. NGƯỜI ĐI BỘ (TURNSTILE / FLAP BARRIER)
                (LaneTargetType.Pedestrian, TriggerSource.CardSwipe, LaneDirection.In)
                    => await ProcessPedestrianEntryAsync(context, trigger, imageBasePath, onBarrierOpenFailed),
                (LaneTargetType.Pedestrian, TriggerSource.CardSwipe, LaneDirection.Out)
                    => await ProcessPedestrianExitAsync(context, trigger, imageBasePath, onBarrierOpenFailed),
                (LaneTargetType.Pedestrian, TriggerSource.FaceTerminal, _)
                    => await ProcessPedestrianFacePassAsync(context, trigger, imageBasePath, onBarrierOpenFailed),

                // 2. XE CƠ GIỚI - QUẸT THẺ THỦ CÔNG (ADR 0041 §4: Dispatch trực tiếp tới Handler)
                (LaneTargetType.Vehicle, TriggerSource.CardSwipe, _) when trigger.IsSharedVehicle || (cardEntity != null && cardEntity.TargetType == CardTargetType.Vehicle && !string.IsNullOrEmpty(cardEntity.VehicleId))
                    => await ProcessSharedVehicleTripFromTriggerAsync(context, trigger, imageBasePath, onBarrierOpenFailed, onManualPlateInput),

                (LaneTargetType.Vehicle, TriggerSource.CardSwipe, _) when client == null
                    => new ProcessResult { Status = ProcessStatus.ClientNotFound, Message = "Không tìm thấy người dùng." },

                (LaneTargetType.Vehicle, TriggerSource.CardSwipe, LaneDirection.In)
                    => await _clientVehicleHandler.ProcessEntryAsync(new ClientVehicleExecutionContext(
                        context, trigger, client!, imageBasePath, onBarrierOpenFailed, onManualPlateInput, departmentName)),

                (LaneTargetType.Vehicle, TriggerSource.CardSwipe, LaneDirection.Out)
                    => await _clientVehicleHandler.ProcessExitAsync(new ClientVehicleExecutionContext(
                        context, trigger, client!, imageBasePath, onBarrierOpenFailed, onManualPlateInput, departmentName)),

                // 3. XE CƠ GIỚI - CẢM BIẾN RADAR KÍCH HOẠT (FREE-FLOW)
                (LaneTargetType.Vehicle, TriggerSource.Radar, LaneDirection.In)
                    => await ProcessVehicleRadarEntryAsync(context, trigger, imageBasePath, onBarrierOpenFailed),
                (LaneTargetType.Vehicle, TriggerSource.Radar, LaneDirection.Out)
                    => await ProcessVehicleRadarExitAsync(context, trigger, imageBasePath, onBarrierOpenFailed),

                // 4. MỞ CƯỠNG BỨC THỦ CÔNG
                (_, TriggerSource.Manual, _)
                    => await ProcessManualOpenAsync(context, onBarrierOpenFailed),

                _ => new ProcessResult { Status = ProcessStatus.Error, Message = "Chế độ xử lý chưa được hỗ trợ." }
            };
        }

        #endregion

        #region --- 2. BACKWARD COMPATIBILITY ENTRY/EXIT WRAPPERS ---

        public async Task<ProcessResult> ProcessEntryAsync(
            LaneRuntimeContext context,
            RealtimeLog data,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed = null,
            Func<LaneRuntimeContext, string?, Task<string?>>? onManualPlateInput = null)
        {
            var trigger = new WorkflowTriggerEvent
            {
                Source = TriggerSource.CardSwipe,
                RawCardNo = data.CardNo ?? string.Empty,
                ReaderIndex = data.DoorId,
                DoorIndex = data.DoorId,
                TriggerTime = (data.Time != default && data.Time != DateTime.MinValue) ? data.Time : DateTime.Now
            };

            // Phân nhánh Thẻ Phương tiện nội bộ
            var (card, _, _) = await ResolveIdentityAsync(trigger.RawCardNo);
            if (card != null && card.TargetType == CardTargetType.Vehicle)
            {
                trigger.IsSharedVehicle = true;
            }

            return await ProcessWorkflowAsync(context, trigger, imageBasePath, onBarrierOpenFailed, onManualPlateInput);
        }

        public async Task<ProcessResult> ProcessExitAsync(
            LaneRuntimeContext context,
            RealtimeLog data,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed = null,
            Func<LaneRuntimeContext, string?, Task<string?>>? onManualPlateInput = null)
        {
            var trigger = new WorkflowTriggerEvent
            {
                Source = TriggerSource.CardSwipe,
                RawCardNo = data.CardNo ?? string.Empty,
                ReaderIndex = data.DoorId,
                DoorIndex = data.DoorId,
                TriggerTime = (data.Time != default && data.Time != DateTime.MinValue) ? data.Time : DateTime.Now
            };

            var (card, _, _) = await ResolveIdentityAsync(trigger.RawCardNo);
            if (card != null && card.TargetType == CardTargetType.Vehicle)
            {
                trigger.IsSharedVehicle = true;
            }

            return await ProcessWorkflowAsync(context, trigger, imageBasePath, onBarrierOpenFailed, onManualPlateInput);
        }

        #endregion

        #region --- 3. WORKFLOW HANDLERS (CHUYÊN BIỆT TỪNG NHÁNH NGHIỆP VỤ) ---

        // ======================== [A] NGƯỜI ĐI BỘ ========================

        private async Task<ProcessResult> ProcessPedestrianEntryAsync(
            LaneRuntimeContext context,
            WorkflowTriggerEvent trigger,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed)
        {
            var (_, _, client) = await ResolveIdentityAsync(trigger.RawCardNo);
            if (client == null)
            {
                return new ProcessResult { Status = ProcessStatus.ClientNotFound, Message = "Không tìm thấy thông tin người dùng trong hệ thống." };
            }

            string departmentName = await GetDepartmentNameAsync(client);
            if (!client.CanPassGate(trigger.TriggerTime, out string rejectReason))
            {
                return new ProcessResult
                {
                    Status = !client.IsActive ? ProcessStatus.AccessDenied : ProcessStatus.CardExpired,
                    Message = rejectReason,
                    Client = client,
                    DepartmentName = departmentName
                };
            }

            var activeSession = await _sessionRepository.FindOneAsync(x =>
                x.PersonId == client.Id &&
                x.TargetType == LaneTargetType.Pedestrian &&
                x.Status == ParkingSessionStatus.Active &&
                !x.IsDeleted);

            if (activeSession != null)
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.AlreadyInParking,
                    Message = "Người dùng này đang có lượt vào chưa hoàn tất.",
                    Client = client,
                    DepartmentName = departmentName,
                    ParkingSession = activeSession
                };
            }

            // Chụp camera toàn cảnh và FaceID (bỏ qua camera biển số)
            var images = await CaptureLaneImagesAsync(context,
                needOverview: context.Lane.UseOverviewCam,
                needPlate: false,
                needFace: context.Lane.UseFaceCam || !string.IsNullOrEmpty(context.Lane.FaceDeviceId) || context.Lane.TargetType == LaneTargetType.Pedestrian);

            // Mở Turnstile
            if (!TryOpenBarrier(context, onBarrierOpenFailed))
            {
                var (_, failFace, failOverview) = ExtractWorkflowImages(images, null);
                return new ProcessResult
                {
                    Status = ProcessStatus.BarrierFailed,
                    Message = "Không thể mở cửa Turnstile. Vui lòng kiểm tra kết nối thiết bị.",
                    Client = client,
                    DepartmentName = departmentName,
                    FaceImage = failFace,
                    OverviewImage = failOverview
                };
            }

            var session = new ParkingSession
            {
                TargetType = LaneTargetType.Pedestrian,
                PersonId = client.Id,
                VehicleType = null,
                InTime = trigger.TriggerTime,
                InLaneName = context.Lane.Name,
                Status = ParkingSessionStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            await _sessionRepository.AddAsync(session);

            _imageOrchestrator.SaveSessionImagesBackground(session, images, isEntry: true, imageBasePath, client: client);

            var (_, pedFace, pedOverview) = ExtractWorkflowImages(images, null);
            return new ProcessResult
            {
                Status = ProcessStatus.Success,
                Client = client,
                DepartmentName = departmentName,
                ParkingSession = session,
                OverviewImage = pedOverview,
                FaceImage = pedFace,
                Message = $"Xác thực người đi bộ vào thành công: {client.Name}"
            };
        }

        private async Task<ProcessResult> ProcessPedestrianExitAsync(
            LaneRuntimeContext context,
            WorkflowTriggerEvent trigger,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed)
        {
            var (_, _, client) = await ResolveIdentityAsync(trigger.RawCardNo);
            if (client == null)
            {
                return new ProcessResult { Status = ProcessStatus.ClientNotFound, Message = "Không tìm thấy thông tin người dùng trong hệ thống." };
            }

            string departmentName = await GetDepartmentNameAsync(client);
            if (!client.CanPassGate(trigger.TriggerTime, out string rejectReason))
            {
                return new ProcessResult
                {
                    Status = !client.IsActive ? ProcessStatus.AccessDenied : ProcessStatus.CardExpired,
                    Message = rejectReason,
                    Client = client,
                    DepartmentName = departmentName
                };
            }

            var activeSession = await _sessionRepository.FindOneAsync(x =>
                x.PersonId == client.Id &&
                x.TargetType == LaneTargetType.Pedestrian &&
                x.Status == ParkingSessionStatus.Active &&
                !x.IsDeleted);

            var images = await CaptureLaneImagesAsync(context,
                needOverview: context.Lane.UseOverviewCam,
                needPlate: false,
                needFace: context.Lane.UseFaceCam || !string.IsNullOrEmpty(context.Lane.FaceDeviceId) || context.Lane.TargetType == LaneTargetType.Pedestrian);

            if (!TryOpenBarrier(context, onBarrierOpenFailed))
            {
                var (_, failFace, failOverview) = ExtractWorkflowImages(images, null);
                return new ProcessResult
                {
                    Status = ProcessStatus.BarrierFailed,
                    Message = "Không thể mở cửa Turnstile ra. Vui lòng kiểm tra kết nối thiết bị.",
                    Client = client,
                    DepartmentName = departmentName,
                    FaceImage = failFace,
                    OverviewImage = failOverview
                };
            }

            if (activeSession != null)
            {
                activeSession.OutTime = trigger.TriggerTime;
                activeSession.OutLaneName = context.Lane.Name;
                activeSession.Status = ParkingSessionStatus.Completed;
                activeSession.UpdatedAt = DateTime.UtcNow;
                await _sessionRepository.UpdateAsync(activeSession);
                _imageOrchestrator.SaveSessionImagesBackground(activeSession, images, isEntry: false, imageBasePath, client: client);
            }
            else
            {
                var newSession = new ParkingSession
                {
                    TargetType = LaneTargetType.Pedestrian,
                    PersonId = client.Id,
                    VehicleType = null,
                    OutTime = trigger.TriggerTime,
                    OutLaneName = context.Lane.Name,
                    Status = ParkingSessionStatus.Completed,
                    CreatedAt = DateTime.UtcNow
                };
                await _sessionRepository.AddAsync(newSession);
                _imageOrchestrator.SaveSessionImagesBackground(newSession, images, isEntry: false, imageBasePath, client: client);
                activeSession = newSession;
            }

            var (_, exitFace, exitOverview) = ExtractWorkflowImages(images, null);
            return new ProcessResult
            {
                Status = ProcessStatus.Success,
                Client = client,
                DepartmentName = departmentName,
                ParkingSession = activeSession,
                OverviewImage = exitOverview,
                FaceImage = exitFace,
                Message = $"Xác thực người đi bộ ra thành công: {client.Name}"
            };
        }

        private async Task<ProcessResult> ProcessPedestrianFacePassAsync(
            LaneRuntimeContext context,
            WorkflowTriggerEvent trigger,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed)
        {
            if (context.Direction == LaneDirection.In)
            {
                return await ProcessPedestrianEntryAsync(context, trigger, imageBasePath, onBarrierOpenFailed);
            }
            return await ProcessPedestrianExitAsync(context, trigger, imageBasePath, onBarrierOpenFailed);
        }
        // ======================== [B] XE CƠ GIỚI - QUẸT THẺ (Đã chuyển giao trực tiếp sang IClientVehicleWorkflowHandler theo ADR 0041 §4) ========================

        // ======================== [C] XE CƠ GIỚI - CẢM BIẾN RADAR ========================

        private async Task<ProcessResult> ProcessVehicleRadarEntryAsync(
            LaneRuntimeContext context,
            WorkflowTriggerEvent trigger,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed)
        {
            var images = await CaptureLaneImagesAsync(context,
                needOverview: context.Lane.UseOverviewCam,
                needPlate: context.Lane.UsePlateCam,
                needFace: context.Lane.UseFaceCam || !string.IsNullOrEmpty(context.Lane.FaceDeviceId));
            var (lprSuccess, detectedPlate, lprResult) = await RecognizePlateAsync(context, images.Plate, defaultPlate: trigger.ManualPlateNumber ?? "", onManualPlateInput: null);

            if (!lprSuccess || string.IsNullOrEmpty(detectedPlate))
            {
                var (smallPlate, faceSnap, overviewSnap) = ExtractWorkflowImages(images, lprResult);
                return new ProcessResult
                {
                    Status = ProcessStatus.ConfirmRequired,
                    Message = "Cảm biến Radar phát hiện xe nhưng chưa đọc được biển số. Vui lòng quẹt thẻ.",
                    OverviewImage = overviewSnap,
                    PlateImage = smallPlate,
                    FaceImage = faceSnap,
                    LprResult = lprResult
                };
            }

            string cleanPlate = PlateHelper.Normalize(detectedPlate);

            var vehicle = await _vehicleRepository.FindOneAsync(v =>
                PlateHelper.Normalize(v.PlateNumber) == cleanPlate &&
                v.IsActive && !v.IsDeleted);

            if (vehicle == null)
            {
                var (smallPlate, faceSnap, overviewSnap) = ExtractWorkflowImages(images, lprResult);
                return new ProcessResult
                {
                    Status = ProcessStatus.ConfirmRequired,
                    Message = $"Phát hiện xe {detectedPlate}. Xe chưa đăng ký vé tháng/nội bộ, vui lòng quẹt thẻ.",
                    OverviewImage = overviewSnap,
                    PlateImage = smallPlate,
                    FaceImage = faceSnap,
                    LprResult = lprResult
                };
            }

            if (!TryOpenBarrier(context, onBarrierOpenFailed))
            {
                var (smallPlate, faceSnap, overviewSnap) = ExtractWorkflowImages(images, lprResult);
                return new ProcessResult
                {
                    Status = ProcessStatus.BarrierFailed,
                    Message = "Không thể mở Barie cho xe tự do. Vui lòng kiểm tra thiết bị.",
                    Vehicle = vehicle,
                    LprResult = lprResult,
                    OverviewImage = overviewSnap,
                    PlateImage = smallPlate,
                    FaceImage = faceSnap
                };
            }

            var session = new ParkingSession
            {
                PlateNumber = vehicle.PlateNumber,
                VehicleType = vehicle.Type,
                TargetType = LaneTargetType.Vehicle,
                InTime = trigger.TriggerTime,
                InLaneName = context.Lane.Name,
                Status = ParkingSessionStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            await _sessionRepository.AddAsync(session);
            _imageOrchestrator.SaveSessionImagesBackground(session, images, isEntry: true, imageBasePath);

            var (succSmallPlate, succFaceSnap, succOverviewSnap) = ExtractWorkflowImages(images, lprResult);
            return new ProcessResult
            {
                Status = ProcessStatus.Success,
                Vehicle = vehicle,
                ParkingSession = session,
                LprResult = lprResult,
                OverviewImage = succOverviewSnap,
                PlateImage = succSmallPlate,
                FaceImage = succFaceSnap,
                Message = $"Xe hợp lệ {vehicle.PlateNumber} qua cảm biến Radar thành công."
            };
        }

        private async Task<ProcessResult> ProcessVehicleRadarExitAsync(
            LaneRuntimeContext context,
            WorkflowTriggerEvent trigger,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed)
        {
            var images = await CaptureLaneImagesAsync(context,
                needOverview: context.Lane.UseOverviewCam,
                needPlate: context.Lane.UsePlateCam,
                needFace: context.Lane.UseFaceCam || !string.IsNullOrEmpty(context.Lane.FaceDeviceId));
            var (lprSuccess, detectedPlate, lprResult) = await RecognizePlateAsync(context, images.Plate, defaultPlate: trigger.ManualPlateNumber ?? "", onManualPlateInput: null);

            if (!lprSuccess || string.IsNullOrEmpty(detectedPlate))
            {
                var (smallPlate, faceSnap, overviewSnap) = ExtractWorkflowImages(images, lprResult);
                return new ProcessResult
                {
                    Status = ProcessStatus.ConfirmRequired,
                    Message = "Cảm biến Radar phát hiện xe ra nhưng chưa đọc được biển số. Vui lòng quẹt thẻ.",
                    OverviewImage = overviewSnap,
                    PlateImage = smallPlate,
                    FaceImage = faceSnap,
                    LprResult = lprResult
                };
            }

            string cleanPlate = PlateHelper.Normalize(detectedPlate);

            var vehicle = await _vehicleRepository.FindOneAsync(v =>
                PlateHelper.Normalize(v.PlateNumber) == cleanPlate &&
                v.IsActive && !v.IsDeleted);

            if (vehicle == null)
            {
                var (smallPlate, faceSnap, overviewSnap) = ExtractWorkflowImages(images, lprResult);
                return new ProcessResult
                {
                    Status = ProcessStatus.ConfirmRequired,
                    Message = $"Phát hiện xe {detectedPlate}. Xe chưa đăng ký vé tháng/nội bộ, vui lòng quẹt thẻ thanh toán.",
                    OverviewImage = overviewSnap,
                    PlateImage = smallPlate,
                    FaceImage = faceSnap,
                    LprResult = lprResult
                };
            }

            var activeSession = await _sessionRepository.FindOneAsync(s =>
                PlateHelper.Normalize(s.PlateNumber) == cleanPlate &&
                s.Status == ParkingSessionStatus.Active &&
                !s.IsDeleted);

            if (!TryOpenBarrier(context, onBarrierOpenFailed))
            {
                var (smallPlate, faceSnap, overviewSnap) = ExtractWorkflowImages(images, lprResult);
                return new ProcessResult
                {
                    Status = ProcessStatus.BarrierFailed,
                    Message = "Không thể mở Barie cho xe tự do. Vui lòng kiểm tra thiết bị.",
                    Vehicle = vehicle,
                    LprResult = lprResult,
                    OverviewImage = overviewSnap,
                    PlateImage = smallPlate,
                    FaceImage = faceSnap
                };
            }

            if (activeSession != null)
            {
                activeSession.OutTime = trigger.TriggerTime;
                activeSession.OutLaneName = context.Lane.Name;
                activeSession.Status = ParkingSessionStatus.Completed;
                activeSession.UpdatedAt = DateTime.UtcNow;
                await _sessionRepository.UpdateAsync(activeSession);
                _imageOrchestrator.SaveSessionImagesBackground(activeSession, images, isEntry: false, imageBasePath);
            }

            var (succSmallPlate, succFaceSnap, succOverviewSnap) = ExtractWorkflowImages(images, lprResult);
            return new ProcessResult
            {
                Status = ProcessStatus.Success,
                Vehicle = vehicle,
                ParkingSession = activeSession,
                LprResult = lprResult,
                OverviewImage = succOverviewSnap,
                PlateImage = succSmallPlate,
                FaceImage = succFaceSnap,
                Message = $"Xe hợp lệ {vehicle.PlateNumber} qua cảm biến Radar ra thành công."
            };
        }

        // ======================== [D] ĐIỀU VẬN XE DÙNG CHUNG ========================

        private async Task<ProcessResult> ProcessSharedVehicleTripFromTriggerAsync(
            LaneRuntimeContext context,
            WorkflowTriggerEvent trigger,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed,
            Func<LaneRuntimeContext, string?, Task<string?>>? onManualPlateInput)
        {
            var (cardEntity, _, _) = await ResolveIdentityAsync(trigger.RawCardNo);
            if (cardEntity == null || string.IsNullOrEmpty(cardEntity.VehicleId))
            {
                return new ProcessResult { Status = ProcessStatus.ClientNotFound, Message = "Thẻ xe dùng chung không hợp lệ." };
            }

            return await _sharedVehicleHandler.ProcessSharedVehicleTripAsync(
                context, trigger, cardEntity, imageBasePath, onBarrierOpenFailed, onManualPlateInput);
        }

        // ======================== [E] MỞ CƯỠNG BỨC THỦ CÔNG ========================

        private static Task<ProcessResult> ProcessManualOpenAsync(
            LaneRuntimeContext context,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed)
        {
            bool opened = TryOpenBarrier(context, onBarrierOpenFailed);
            return Task.FromResult(new ProcessResult
            {
                Status = opened ? ProcessStatus.Success : ProcessStatus.BarrierFailed,
                Message = opened ? "Đã gửi lệnh mở cổng thành công." : "Không thể gửi lệnh mở cổng tới Controller."
            });
        }

        #endregion

        #region --- 4. 6 PRIVATE SHARED HELPERS (KHỐI KỸ THUẬT DÙNG CHUNG) ---

        /// <summary>
        /// Trích xuất và nhân bản các ảnh phục vụ hiển thị UI:
        /// - SmallPlate: Ảnh crop biển số từ LprResult (ưu tiên) hoặc fallback ảnh camera biển số.
        /// - FaceSnap: Ảnh chụp khuôn mặt từ camera FaceID.
        /// - OverviewSnap: Ảnh chụp toàn cảnh làn xe.
        /// Đồng thời giải phóng bộ đệm ảnh gốc CapturedLaneImages an toàn.
        /// </summary>
        private static (Bitmap? SmallPlate, Bitmap? FaceSnap, Bitmap? OverviewSnap) ExtractWorkflowImages(
            CapturedLaneImages images,
            LprResult? lprResult)
        {
            Bitmap? smallPlate = lprResult?.PlateImage != null
                ? (Bitmap)lprResult.PlateImage.Clone()
                : (images.Plate != null ? (Bitmap)images.Plate.Clone() : null);
            Bitmap? faceSnap = images.Face != null ? (Bitmap)images.Face.Clone() : null;
            Bitmap? overviewSnap = images.Overview != null ? (Bitmap)images.Overview.Clone() : null;

            images.Dispose();
            return (smallPlate, faceSnap, overviewSnap);
        }

        /// <summary>
        /// Khối 1: Định danh thẻ & tra cứu đối tượng (Thẻ -> Xe hoặc Người)
        /// </summary>
        private async Task<(Card? Card, Vehicle? Vehicle, Client? Client)> ResolveIdentityAsync(string rawCard)
        {
            string normalizedCard = CardHelper.NormalizeCardCode(rawCard);
            var card = await _cardRepository.FindOneAsync(c =>
                (c.CardNumber == normalizedCard || c.CardNumber == rawCard) &&
                !c.IsDeleted);

            Vehicle? vehicle = null;
            Client? client = null;

            if (card != null)
            {
                if (card.TargetType == CardTargetType.Vehicle && !string.IsNullOrEmpty(card.VehicleId))
                {
                    vehicle = await _vehicleRepository.GetByIdAsync(card.VehicleId);
                }
                else if (!string.IsNullOrEmpty(card.ClientId))
                {
                    client = await _clientRepository.GetByIdAsync(card.ClientId);
                }
            }

            if (client == null)
            {
                client = await _clientRepository.FindOneAsync(c =>
                    (c.CardCode == normalizedCard || c.CardCode == rawCard ||
                     c.PhoneNumber == normalizedCard || c.PhoneNumber == rawCard) &&
                    !c.IsDeleted);
            }

            return (card, vehicle, client);
        }

        /// <summary>
        /// Khối 2: Chụp ảnh song song đa camera an toàn với timeout và quản lý bộ nhớ
        /// </summary>
        private static async Task<CapturedLaneImages> CaptureLaneImagesAsync(
            LaneRuntimeContext context,
            bool needOverview = true,
            bool needPlate = true,
            bool needFace = false,
            int timeoutMs = 2500)
        {
            var result = new CapturedLaneImages();
            if (context.Cameras == null) return result;

            var plateTask = (needPlate && context.Cameras.LicensePlateCamera != null)
                ? Task.Run(() => { try { return context.Cameras.LicensePlateCamera.Capture(); } catch { return null; } })
                : Task.FromResult<Bitmap?>(null);

            var overviewTask = (needOverview && context.Cameras.OverviewCamera != null)
                ? Task.Run(() => { try { return context.Cameras.OverviewCamera.Capture(); } catch { return null; } })
                : Task.FromResult<Bitmap?>(null);

            var faceTask = (needFace && context.Cameras.FaceCamera != null)
                ? Task.Run(() => { try { return context.Cameras.FaceCamera.Capture(); } catch { return null; } })
                : Task.FromResult<Bitmap?>(null);

            var allTasks = Task.WhenAll(plateTask, overviewTask, faceTask);
            var timeoutTask = Task.Delay(timeoutMs);

            await Task.WhenAny(allTasks, timeoutTask);

            if (plateTask.IsCompletedSuccessfully) result.Plate = plateTask.Result;
            else SafelyDisposeTaskResult(plateTask);

            if (overviewTask.IsCompletedSuccessfully) result.Overview = overviewTask.Result;
            else SafelyDisposeTaskResult(overviewTask);

            if (faceTask.IsCompletedSuccessfully) result.Face = faceTask.Result;
            else SafelyDisposeTaskResult(faceTask);

            return result;
        }

        private static void SafelyDisposeTaskResult(Task<Bitmap?> task)
        {
            _ = task.ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully && t.Result != null)
                {
                    t.Result.Dispose();
                }
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }


        /// <summary>
        /// Khối 3: OCR biển số phương tiện qua SimpleLPR3 kèm fallback nhập tay
        /// </summary>
        private async Task<(bool Success, string DetectedPlate, LprResult? LprResult)> RecognizePlateAsync(
            LaneRuntimeContext context,
            Bitmap? plateImage,
            string defaultPlate,
            Func<LaneRuntimeContext, string?, Task<string?>>? onManualPlateInput)
        {
            string recognizedPlate = string.Empty;
            LprResult? lprResult = null;

            if (plateImage != null)
            {
                lprResult = await Task.Run(() => _lprService.Recognize(plateImage));
                if (lprResult != null && lprResult.Success && !string.IsNullOrWhiteSpace(lprResult.Plate))
                {
                    recognizedPlate = lprResult.Plate.Trim().ToUpper();
                }
            }

            if (string.IsNullOrEmpty(recognizedPlate) && onManualPlateInput != null)
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
            }
            else if (string.IsNullOrEmpty(recognizedPlate) && !string.IsNullOrWhiteSpace(defaultPlate))
            {
                recognizedPlate = defaultPlate.Trim().ToUpper();
                lprResult = new LprResult
                {
                    Success = true,
                    Plate = recognizedPlate,
                    PlateImage = plateImage != null ? (Bitmap)plateImage.Clone() : null
                };
            }

            bool success = !string.IsNullOrEmpty(recognizedPlate);
            return (success, recognizedPlate, lprResult);
        }

        /// <summary>
        /// Kích hoạt rơ-le mở barie / turnstile và xử lý lỗi phần cứng
        /// </summary>
        private static bool TryOpenBarrier(LaneRuntimeContext context, Func<LaneRuntimeContext, bool>? onBarrierOpenFailed)
        {
            if (context.OpenBarrier()) return true;
            return onBarrierOpenFailed?.Invoke(context) ?? false;
        }

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

        #endregion
    }
}