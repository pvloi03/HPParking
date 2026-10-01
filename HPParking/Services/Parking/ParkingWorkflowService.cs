using HPParking.Core.Helpers;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Common;
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

        #region --- 1. MASTER WORKFLOW DISPATCHER (TUPLE PATTERN MATCHING) ---

        public async Task<ProcessResult> ProcessWorkflowAsync(
            LaneRuntimeContext context,
            WorkflowTriggerEvent trigger,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed = null,
            Func<LaneRuntimeContext, string?, Task<string?>>? onManualPlateInput = null)
        {
            // Kiểm tra thẻ hợp lệ nếu nguồn kích hoạt là quẹt thẻ
            if (trigger.Source == TriggerSource.CardSwipe)
            {
                string raw = trigger.RawCardNo?.Trim() ?? string.Empty;
                string norm = CardHelper.NormalizeCardCode(raw);
                if (!CardHelper.IsValidCardCode(raw) && !CardHelper.IsValidCardCode(norm))
                {
                    return new ProcessResult { Status = ProcessStatus.ClientNotFound, Message = string.Empty };
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

                // 2. XE CƠ GIỚI - QUẸT THẺ THỦ CÔNG
                (LaneTargetType.Vehicle, TriggerSource.CardSwipe, _) when trigger.IsSharedVehicle
                    => await ProcessSharedVehicleTripFromTriggerAsync(context, trigger, imageBasePath, onBarrierOpenFailed, onManualPlateInput),
                (LaneTargetType.Vehicle, TriggerSource.CardSwipe, LaneDirection.In)
                    => await ProcessVehicleCardEntryAsync(context, trigger, imageBasePath, onBarrierOpenFailed, onManualPlateInput),
                (LaneTargetType.Vehicle, TriggerSource.CardSwipe, LaneDirection.Out)
                    => await ProcessVehicleCardExitAsync(context, trigger, imageBasePath, onBarrierOpenFailed, onManualPlateInput),

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
            var (isExpired, expiryMsg) = ValidateClientExpiry(client);
            if (isExpired)
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.CardExpired,
                    Message = expiryMsg,
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
            var images = await CaptureLaneImagesAsync(context, needOverview: context.Lane.UseOverviewCam, needPlate: false, needFace: context.Lane.UseFaceCam);

            // Mở Turnstile
            if (!TryOpenBarrier(context, onBarrierOpenFailed))
            {
                images.Dispose();
                return new ProcessResult
                {
                    Status = ProcessStatus.BarrierFailed,
                    Message = "Không thể mở cửa Turnstile. Vui lòng kiểm tra kết nối thiết bị.",
                    Client = client,
                    DepartmentName = departmentName
                };
            }

            var session = new ParkingSession
            {
                TargetType = LaneTargetType.Pedestrian,
                PersonId = client.Id,
                InTime = trigger.TriggerTime,
                InLaneName = context.Lane.Name,
                Status = ParkingSessionStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            await _sessionRepository.AddAsync(session);

            SaveImagesBackground(session, images, isEntry: true, imageBasePath);

            return new ProcessResult
            {
                Status = ProcessStatus.Success,
                Client = client,
                DepartmentName = departmentName,
                ParkingSession = session,
                OverviewImage = images.Overview,
                FaceImage = images.Face,
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
            var (isExpired, expiryMsg) = ValidateClientExpiry(client);
            if (isExpired)
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.CardExpired,
                    Message = expiryMsg,
                    Client = client,
                    DepartmentName = departmentName
                };
            }

            var activeSession = await _sessionRepository.FindOneAsync(x =>
                x.PersonId == client.Id &&
                x.TargetType == LaneTargetType.Pedestrian &&
                x.Status == ParkingSessionStatus.Active &&
                !x.IsDeleted);

            var images = await CaptureLaneImagesAsync(context, needOverview: context.Lane.UseOverviewCam, needPlate: false, needFace: context.Lane.UseFaceCam);

            if (!TryOpenBarrier(context, onBarrierOpenFailed))
            {
                images.Dispose();
                return new ProcessResult
                {
                    Status = ProcessStatus.BarrierFailed,
                    Message = "Không thể mở cửa Turnstile. Vui lòng kiểm tra kết nối thiết bị.",
                    Client = client,
                    DepartmentName = departmentName
                };
            }

            if (activeSession != null)
            {
                activeSession.OutTime = trigger.TriggerTime;
                activeSession.OutLaneName = context.Lane.Name;
                activeSession.Status = ParkingSessionStatus.Completed;
                activeSession.UpdatedAt = DateTime.UtcNow;
                await _sessionRepository.UpdateAsync(activeSession);
                SaveImagesBackground(activeSession, images, isEntry: false, imageBasePath);
            }
            else
            {
                var newSession = new ParkingSession
                {
                    TargetType = LaneTargetType.Pedestrian,
                    PersonId = client.Id,
                    OutTime = trigger.TriggerTime,
                    OutLaneName = context.Lane.Name,
                    Status = ParkingSessionStatus.Completed,
                    CreatedAt = DateTime.UtcNow
                };
                await _sessionRepository.AddAsync(newSession);
                SaveImagesBackground(newSession, images, isEntry: false, imageBasePath);
                activeSession = newSession;
            }

            return new ProcessResult
            {
                Status = ProcessStatus.Success,
                Client = client,
                DepartmentName = departmentName,
                ParkingSession = activeSession,
                OverviewImage = images.Overview,
                FaceImage = images.Face,
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

        // ======================== [B] XE CƠ GIỚI - QUẸT THẺ ========================

        private async Task<ProcessResult> ProcessVehicleCardEntryAsync(
            LaneRuntimeContext context,
            WorkflowTriggerEvent trigger,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed,
            Func<LaneRuntimeContext, string?, Task<string?>>? onManualPlateInput)
        {
            var (cardEntity, _, client) = await ResolveIdentityAsync(trigger.RawCardNo);

            if (cardEntity != null && cardEntity.TargetType == CardTargetType.Vehicle && !string.IsNullOrEmpty(cardEntity.VehicleId))
            {
                return await ProcessSharedVehicleTripFromTriggerAsync(context, trigger, imageBasePath, onBarrierOpenFailed, onManualPlateInput);
            }

            if (client == null)
            {
                return new ProcessResult { Status = ProcessStatus.ClientNotFound, Message = "Không tìm thấy người dùng." };
            }

            string departmentName = await GetDepartmentNameAsync(client);
            var (isExpired, expiryMsg) = ValidateClientExpiry(client);
            if (isExpired)
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.ConfirmRequired,
                    Message = expiryMsg,
                    Client = client,
                    DepartmentName = departmentName
                };
            }

            var parkingInProgress = await _sessionRepository.FindOneAsync(x => x.PersonId == client.Id && x.Status == ParkingSessionStatus.Active && !x.IsDeleted);
            if (parkingInProgress != null)
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.AlreadyInParking,
                    Message = "Khách hàng này đang có xe trong bãi.",
                    Client = client,
                    DepartmentName = departmentName
                };
            }

            List<Vehicle> clientVehicles = [];
            if (_vehicleRepository != null && !string.IsNullOrEmpty(client.Id))
            {
                var vehicles = await _vehicleRepository.FindAsync(v => v.OwnerClientId == client.Id && v.IsActive && !v.IsDeleted);
                clientVehicles = vehicles?.ToList() ?? [];
            }

            bool requirePlateVerification = client.VerifyVehiclePlate;
            if (requirePlateVerification && clientVehicles.Count == 0)
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

            var images = await CaptureLaneImagesAsync(context, needOverview: context.Lane.UseOverviewCam, needPlate: context.Lane.UsePlateCam, needFace: context.Lane.UseFaceCam);
            var (plateSuccess, recognizedPlate, lprResult) = await RecognizePlateAsync(context, images.Plate, defaultPlate, onManualPlateInput);

            Vehicle? matchedVehicle = null;

            if (!requirePlateVerification)
            {
                if (string.IsNullOrEmpty(recognizedPlate)) recognizedPlate = defaultPlate;
            }
            else
            {
                if (!plateSuccess || string.IsNullOrEmpty(recognizedPlate))
                {
                    images.Dispose();
                    return new ProcessResult
                    {
                        Status = images.Plate == null ? ProcessStatus.CaptureFailed : ProcessStatus.LprFailed,
                        Message = "Không nhận diện được biển số và không có biển số nhập tay.",
                        Client = client,
                        Vehicle = clientVehicles.FirstOrDefault(),
                        DepartmentName = departmentName
                    };
                }

                string actualPlate = NormalizePlate(recognizedPlate);
                matchedVehicle = clientVehicles.FirstOrDefault(v =>
                    NormalizePlate(v.PlateNumber) == actualPlate);

                if (matchedVehicle == null && clientVehicles.Count > 0)
                {
                    images.Dispose();
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

            if (!TryOpenBarrier(context, onBarrierOpenFailed))
            {
                images.Dispose();
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

            var parking = new ParkingSession
            {
                PersonId = client.Id,
                PlateNumber = !requirePlateVerification ? defaultPlate : (matchedVehicle?.PlateNumber ?? recognizedPlate ?? ""),
                VehicleType = matchedVehicle?.Type ?? VehicleType.Car,
                TargetType = LaneTargetType.Vehicle,
                InTime = trigger.TriggerTime,
                InLaneName = context.Lane.Name,
                Status = ParkingSessionStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            await _sessionRepository.AddAsync(parking);
            SaveImagesBackground(parking, images, isEntry: true, imageBasePath);

            return new ProcessResult
            {
                Status = ProcessStatus.Success,
                Client = client,
                Vehicle = matchedVehicle ?? clientVehicles.FirstOrDefault(),
                DepartmentName = departmentName,
                LprResult = lprResult,
                ParkingSession = parking,
                OverviewImage = images.Overview,
                PlateImage = images.Plate
            };
        }

        private async Task<ProcessResult> ProcessVehicleCardExitAsync(
            LaneRuntimeContext context,
            WorkflowTriggerEvent trigger,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed,
            Func<LaneRuntimeContext, string?, Task<string?>>? onManualPlateInput)
        {
            var (cardEntity, _, client) = await ResolveIdentityAsync(trigger.RawCardNo);

            if (cardEntity != null && cardEntity.TargetType == CardTargetType.Vehicle && !string.IsNullOrEmpty(cardEntity.VehicleId))
            {
                return await ProcessSharedVehicleTripFromTriggerAsync(context, trigger, imageBasePath, onBarrierOpenFailed, onManualPlateInput);
            }

            if (client == null)
            {
                return new ProcessResult { Status = ProcessStatus.ClientNotFound, Message = "Không tìm thấy người dùng." };
            }

            string departmentName = await GetDepartmentNameAsync(client);
            var (isExpired, expiryMsg) = ValidateClientExpiry(client);
            if (isExpired)
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.ConfirmRequired,
                    Message = expiryMsg,
                    Client = client,
                    DepartmentName = departmentName
                };
            }

            var parking = await _sessionRepository.FindOneAsync(x => x.PersonId == client.Id && x.Status == ParkingSessionStatus.Active && !x.IsDeleted);
            if (parking == null)
            {
                return new ProcessResult
                {
                    Status = ProcessStatus.NotInParking,
                    Message = "Khách hàng này không có xe trong bãi.",
                    Client = client,
                    DepartmentName = departmentName
                };
            }

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

            bool requirePlateVerification = client.VerifyVehiclePlate;
            var images = await CaptureLaneImagesAsync(context, needOverview: context.Lane.UseOverviewCam, needPlate: context.Lane.UsePlateCam, needFace: context.Lane.UseFaceCam);
            var (plateSuccess, exitPlate, lprResult) = await RecognizePlateAsync(context, images.Plate, parking.PlateNumber ?? "", onManualPlateInput);

            if (!requirePlateVerification)
            {
                if (string.IsNullOrEmpty(exitPlate)) exitPlate = parking.PlateNumber ?? "";
            }
            else
            {
                string cleanInPlate = NormalizePlate(parking.PlateNumber);
                string cleanExitPlate = NormalizePlate(exitPlate);

                if (string.IsNullOrEmpty(cleanExitPlate))
                {
                    images.Dispose();
                    return new ProcessResult
                    {
                        Status = images.Plate == null ? ProcessStatus.CaptureFailed : ProcessStatus.LprFailed,
                        Message = "Không nhận diện được biển số ra và không có biển số nhập tay.",
                        Client = client,
                        Vehicle = matchedVehicle,
                        DepartmentName = departmentName,
                        ParkingSession = parking
                    };
                }

                if (cleanExitPlate != cleanInPlate)
                {
                    images.Dispose();
                    return new ProcessResult
                    {
                        Status = ProcessStatus.PlateMismatch,
                        Message = $"Biển số ra ({exitPlate}) không khớp với biển số vào ({parking.PlateNumber}).",
                        Client = client,
                        Vehicle = matchedVehicle,
                        DepartmentName = departmentName,
                        ParkingSession = parking,
                        LprResult = lprResult
                    };
                }
            }

            if (!TryOpenBarrier(context, onBarrierOpenFailed))
            {
                images.Dispose();
                return new ProcessResult
                {
                    Status = ProcessStatus.BarrierFailed,
                    Message = "Không thể mở barrier. Vui lòng kiểm tra thiết bị.",
                    Client = client,
                    Vehicle = matchedVehicle,
                    DepartmentName = departmentName,
                    ParkingSession = parking,
                    LprResult = lprResult
                };
            }

            parking.OutTime = trigger.TriggerTime;
            parking.OutLaneName = context.Lane.Name;
            parking.Status = ParkingSessionStatus.Completed;
            parking.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(parking);
            SaveImagesBackground(parking, images, isEntry: false, imageBasePath);

            return new ProcessResult
            {
                Status = ProcessStatus.Success,
                Client = client,
                Vehicle = matchedVehicle,
                DepartmentName = departmentName,
                ParkingSession = parking,
                LprResult = lprResult,
                OverviewImage = images.Overview,
                PlateImage = images.Plate
            };
        }

        // ======================== [C] XE CƠ GIỚI - CẢM BIẾN RADAR ========================

        private async Task<ProcessResult> ProcessVehicleRadarEntryAsync(
            LaneRuntimeContext context,
            WorkflowTriggerEvent trigger,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed)
        {
            var images = await CaptureLaneImagesAsync(context, needOverview: context.Lane.UseOverviewCam, needPlate: context.Lane.UsePlateCam, needFace: false);
            var (lprSuccess, detectedPlate, lprResult) = await RecognizePlateAsync(context, images.Plate, defaultPlate: trigger.ManualPlateNumber ?? "", onManualPlateInput: null);

            if (!lprSuccess || string.IsNullOrEmpty(detectedPlate))
            {
                images.Dispose();
                return new ProcessResult
                {
                    Status = ProcessStatus.ConfirmRequired,
                    Message = "Cảm biến Radar phát hiện xe nhưng chưa đọc được biển số. Vui lòng quẹt thẻ.",
                    OverviewImage = images.Overview,
                    PlateImage = images.Plate,
                    LprResult = lprResult
                };
            }

            string cleanPlate = NormalizePlate(detectedPlate);

            var vehicle = await _vehicleRepository.FindOneAsync(v =>
                NormalizePlate(v.PlateNumber) == cleanPlate &&
                v.IsActive && !v.IsDeleted);

            if (vehicle == null)
            {
                images.Dispose();
                return new ProcessResult
                {
                    Status = ProcessStatus.ConfirmRequired,
                    Message = $"Phát hiện xe {detectedPlate}. Xe chưa đăng ký vé tháng/nội bộ, vui lòng quẹt thẻ.",
                    OverviewImage = images.Overview,
                    PlateImage = images.Plate,
                    LprResult = lprResult
                };
            }

            if (!TryOpenBarrier(context, onBarrierOpenFailed))
            {
                images.Dispose();
                return new ProcessResult
                {
                    Status = ProcessStatus.BarrierFailed,
                    Message = "Không thể mở Barie cho xe tự do. Vui lòng kiểm tra thiết bị.",
                    Vehicle = vehicle,
                    LprResult = lprResult
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
            SaveImagesBackground(session, images, isEntry: true, imageBasePath);

            return new ProcessResult
            {
                Status = ProcessStatus.Success,
                Vehicle = vehicle,
                ParkingSession = session,
                LprResult = lprResult,
                OverviewImage = images.Overview,
                PlateImage = images.Plate,
                Message = $"Xe hợp lệ {vehicle.PlateNumber} qua cảm biến Radar thành công."
            };
        }

        private async Task<ProcessResult> ProcessVehicleRadarExitAsync(
            LaneRuntimeContext context,
            WorkflowTriggerEvent trigger,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed)
        {
            var images = await CaptureLaneImagesAsync(context, needOverview: context.Lane.UseOverviewCam, needPlate: context.Lane.UsePlateCam, needFace: false);
            var (lprSuccess, detectedPlate, lprResult) = await RecognizePlateAsync(context, images.Plate, defaultPlate: trigger.ManualPlateNumber ?? "", onManualPlateInput: null);

            if (!lprSuccess || string.IsNullOrEmpty(detectedPlate))
            {
                images.Dispose();
                return new ProcessResult
                {
                    Status = ProcessStatus.ConfirmRequired,
                    Message = "Cảm biến Radar phát hiện xe ra nhưng chưa đọc được biển số. Vui lòng quẹt thẻ.",
                    OverviewImage = images.Overview,
                    PlateImage = images.Plate,
                    LprResult = lprResult
                };
            }

            string cleanPlate = NormalizePlate(detectedPlate);

            var vehicle = await _vehicleRepository.FindOneAsync(v =>
                NormalizePlate(v.PlateNumber) == cleanPlate &&
                v.IsActive && !v.IsDeleted);

            if (vehicle == null)
            {
                images.Dispose();
                return new ProcessResult
                {
                    Status = ProcessStatus.ConfirmRequired,
                    Message = $"Phát hiện xe {detectedPlate}. Xe chưa đăng ký vé tháng/nội bộ, vui lòng quẹt thẻ thanh toán.",
                    OverviewImage = images.Overview,
                    PlateImage = images.Plate,
                    LprResult = lprResult
                };
            }

            var activeSession = await _sessionRepository.FindOneAsync(s =>
                NormalizePlate(s.PlateNumber) == cleanPlate &&
                s.Status == ParkingSessionStatus.Active &&
                !s.IsDeleted);

            if (!TryOpenBarrier(context, onBarrierOpenFailed))
            {
                images.Dispose();
                return new ProcessResult
                {
                    Status = ProcessStatus.BarrierFailed,
                    Message = "Không thể mở Barie cho xe tự do. Vui lòng kiểm tra thiết bị.",
                    Vehicle = vehicle,
                    LprResult = lprResult
                };
            }

            if (activeSession != null)
            {
                activeSession.OutTime = trigger.TriggerTime;
                activeSession.OutLaneName = context.Lane.Name;
                activeSession.Status = ParkingSessionStatus.Completed;
                activeSession.UpdatedAt = DateTime.UtcNow;
                await _sessionRepository.UpdateAsync(activeSession);
                SaveImagesBackground(activeSession, images, isEntry: false, imageBasePath);
            }

            return new ProcessResult
            {
                Status = ProcessStatus.Success,
                Vehicle = vehicle,
                ParkingSession = activeSession,
                LprResult = lprResult,
                OverviewImage = images.Overview,
                PlateImage = images.Plate,
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

            var log = new RealtimeLog
            {
                CardNo = trigger.RawCardNo,
                DoorId = trigger.DoorIndex,
                Time = trigger.TriggerTime
            };

            return await ProcessSharedVehicleTripAsync(context, cardEntity, log, imageBasePath, onBarrierOpenFailed, onManualPlateInput);
        }

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
                    Message = "Phương tiện nội bộ gắn với thẻ này không tồn tại hoặc đã bị khóa."
                };
            }

            string currentGateId = context.Lane?.GateId ?? "";
            bool isEntry = context.Direction == LaneDirection.In;

            Gate? currentGate = !string.IsNullOrEmpty(currentGateId) ? await _gateRepository.GetByIdAsync(currentGateId) : null;
            string currentGateName = currentGate?.Name ?? (string.IsNullOrEmpty(currentGateId) ? "Cổng không xác định" : currentGateId);

            VehicleDispatchTrip? activeTrip = await _tripRepository.FindOneAsync(t =>
                t.VehicleId == vehicle.Id &&
                t.Status != TripStatus.Completed &&
                !t.IsDeleted);

            var images = await CaptureLaneImagesAsync(context, needOverview: context.Lane?.UseOverviewCam ?? true, needPlate: context.Lane?.UsePlateCam ?? true, needFace: false);
            var (plateSuccess, detectedPlate, lprResult) = await RecognizePlateAsync(context, images.Plate, vehicle.PlateNumber ?? "", onManualPlateInput);

            string registeredPlateNorm = NormalizePlate(vehicle.PlateNumber);
            string detectedPlateNorm = NormalizePlate(detectedPlate);

            if (string.IsNullOrEmpty(detectedPlateNorm))
            {
                images.Dispose();
                return new ProcessResult
                {
                    Status = images.Plate == null ? ProcessStatus.CaptureFailed : ProcessStatus.LprFailed,
                    Message = $"Không nhận diện được biển số phương tiện {vehicle.PlateNumber} và không có biển số nhập tay.",
                    Vehicle = vehicle,
                    DepartmentName = "Không nhận diện được biển số",
                    LprResult = lprResult,
                    DispatchTrip = activeTrip
                };
            }

            if (detectedPlateNorm != registeredPlateNorm)
            {
                images.Dispose();
                return new ProcessResult
                {
                    Status = ProcessStatus.PlateMismatch,
                    Message = "BIỂN SỐ KHÔNG ĐÚNG VỚI BIỂN SỐ ĐÃ ĐĂNG KÝ!",
                    Vehicle = vehicle,
                    DepartmentName = "Cảnh báo sai biển số phương tiện",
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

            bool isCheckpointOverdue = false;
            double checkpointOverdueSeconds = 0;
            if (activeTrip != null && activeTrip.NextDeadline.HasValue)
            {
                if (now > activeTrip.NextDeadline.Value)
                {
                    isCheckpointOverdue = true;
                    checkpointOverdueSeconds = (now - activeTrip.NextDeadline.Value).TotalSeconds;
                }
            }

            int defaultStay = assignedRoute?.DefaultStayMinutes ?? 60;
            int defaultTravel = assignedRoute?.DefaultTravelMinutes ?? 30;

            if (activeTrip == null)
            {
                int firstDeadlineMinutes = isEntry ? defaultStay : defaultTravel;
                if (assignedRoute != null && assignedRoute.GateSteps.Count > 0)
                {
                    var firstStep = assignedRoute.GateSteps.FirstOrDefault(s => s.StepIndex == 1);
                    if (firstStep != null)
                    {
                        firstDeadlineMinutes = isEntry ? firstStep.MaxStayMinutes : firstStep.MaxTravelMinutes;
                    }
                }

                activeTrip = new VehicleDispatchTrip
                {
                    VehicleId = vehicle.Id,
                    PlateNumber = vehicle.PlateNumber ?? "",
                    CardId = vehicleCard.Id,
                    CardNumber = vehicleCard.CardNumber,
                    OriginGateId = currentGateId,
                    CurrentGateId = currentGateId,
                    AssignedRouteId = assignedRoute?.Id,
                    CurrentStepIndex = 1,
                    Status = isEntry ? TripStatus.WorkingAtGate : TripStatus.InTransit,
                    StartTime = now,
                    LastEntryTime = isEntry ? now : null,
                    LastExitTime = isEntry ? null : now,
                    NextDeadline = now.AddMinutes(firstDeadlineMinutes),
                    IsAlertSent = false,
                    Checkpoints = []
                };
                await _tripRepository.AddAsync(activeTrip);
            }
            else
            {
                if (isEntry)
                {
                    if (activeTrip.CurrentStepIndex >= (assignedRoute?.GateSteps.Count ?? 1))
                    {
                        activeTrip.Status = TripStatus.Completed;
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

            bool isRouteCompliant = true;
            if (assignedRoute != null && assignedRoute.GateSteps.Count > 0 && !assignedRoute.IsDefault)
            {
                var expectedStep = assignedRoute.GateSteps.FirstOrDefault(s => s.StepIndex == activeTrip.CurrentStepIndex);
                if (expectedStep != null && !string.IsNullOrEmpty(expectedStep.GateId))
                {
                    if (expectedStep.GateId != currentGateId)
                    {
                        isRouteCompliant = false;
                    }
                }
            }

            var currentCheckpoint = new TripCheckpoint
            {
                StepIndex = activeTrip.CurrentStepIndex,
                GateId = currentGateId,
                GateName = currentGateName,
                Direction = context.Direction,
                Timestamp = now,
                OverviewImagePath = "",
                PlateImagePath = "",
                PlateDetected = lprResult?.Plate ?? vehicle.PlateNumber,
                IsRouteCompliant = isRouteCompliant,
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

            if (!isRouteCompliant)
            {
                SaveImagesBackground(null, images, isEntry, imageBasePath, onSaved: (pPath, oPath, fPath) =>
                {
                    _ = Task.Run(async () =>
                    {
                        if (activeTrip != null && (!string.IsNullOrEmpty(oPath) || !string.IsNullOrEmpty(pPath)))
                        {
                            currentCheckpoint.OverviewImagePath = oPath;
                            currentCheckpoint.PlateImagePath = pPath;
                            await _tripRepository.UpdateAsync(activeTrip);
                        }
                    });
                });

                return new ProcessResult
                {
                    Status = ProcessStatus.ConfirmRequired,
                    Vehicle = vehicle,
                    DepartmentName = $"Tuyến: {assignedRoute?.RouteName ?? "Lạc tuyến"}",
                    LprResult = lprResult,
                    OverviewImage = images.Overview,
                    PlateImage = images.Plate,
                    Message = $"CẢNH BÁO LẠC TUYẾN: Xe {vehicle.PlateNumber} quẹt tại {currentGateName} không đúng lộ trình tuyến {assignedRoute?.RouteName}!",
                    DispatchTrip = activeTrip
                };
            }

            if (!TryOpenBarrier(context, onBarrierOpenFailed))
            {
                images.Dispose();
                return new ProcessResult
                {
                    Status = ProcessStatus.BarrierFailed,
                    Message = "Không thể mở barrier cho phương tiện. Vui lòng kiểm tra thiết bị.",
                    Vehicle = vehicle,
                    DepartmentName = assignedRoute != null ? $"Tuyến: {assignedRoute.RouteName}" : "Phương tiện nội bộ / Điều vận",
                    LprResult = lprResult
                };
            }

            SaveImagesBackground(null, images, isEntry, imageBasePath, onSaved: (pPath, oPath, fPath) =>
            {
                _ = Task.Run(async () =>
                {
                    if (activeTrip != null && (!string.IsNullOrEmpty(oPath) || !string.IsNullOrEmpty(pPath)))
                    {
                        currentCheckpoint.OverviewImagePath = oPath;
                        currentCheckpoint.PlateImagePath = pPath;
                        await _tripRepository.UpdateAsync(activeTrip);
                    }
                });
            });

            string routeDesc = assignedRoute != null ? assignedRoute.RouteName : "Tuyến tự do";
            int totalSteps = assignedRoute?.GateSteps.Count ?? 1;
            return new ProcessResult
            {
                Status = ProcessStatus.Success,
                Vehicle = vehicle,
                DepartmentName = $"Tuyến: {routeDesc} (Chặng {activeTrip.CurrentStepIndex}/{totalSteps})",
                LprResult = lprResult,
                OverviewImage = images.Overview,
                PlateImage = images.Plate,
                Message = $"Phương tiện nội bộ {vehicle.PlateNumber} - Chặng {activeTrip.CurrentStepIndex}/{totalSteps} ({activeTrip.Status})",
                DispatchTrip = activeTrip
            };
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
        /// Chuẩn hóa chuỗi biển số: loại bỏ dấu cách, dấu gạch nối, dấu chấm và in hoa
        /// </summary>
        private static string NormalizePlate(string? plate)
        {
            if (string.IsNullOrWhiteSpace(plate)) return string.Empty;
            return plate.Replace(" ", "").Replace("-", "").Replace(".", "").ToUpperInvariant();
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
        /// Khối 4: Kiểm tra ngày hết hạn đối tượng / khách
        /// </summary>
        private static (bool IsExpired, string Message) ValidateClientExpiry(Client client)
        {
            if (client.Expired.Enable)
            {
                DateTime now = DateTime.Now;
                if (client.Expired.StartDay.Date > now.Date || client.Expired.EndDay.Date < now.Date)
                {
                    return (true, $"Người dùng chỉ được ra vào từ {client.Expired.StartDay:dd/MM/yyyy} - {client.Expired.EndDay:dd/MM/yyyy}");
                }
            }
            return (false, string.Empty);
        }

        /// <summary>
        /// Khối 5: Kích hoạt rơ-le mở barie / turnstile và xử lý lỗi phần cứng
        /// </summary>
        private static bool TryOpenBarrier(LaneRuntimeContext context, Func<LaneRuntimeContext, bool>? onBarrierOpenFailed)
        {
            if (context.OpenBarrier()) return true;
            return onBarrierOpenFailed?.Invoke(context) ?? false;
        }

        /// <summary>
        /// Khối 6: Lưu ảnh ngầm ra ổ cứng mà không chặn luồng giao diện chính
        /// </summary>
        private void SaveImagesBackground(
            ParkingSession? session,
            CapturedLaneImages images,
            bool isEntry,
            string imageBasePath,
            Action<string, string, string>? onSaved = null)
        {
            Bitmap? plateSave = images.Plate != null ? (Bitmap)images.Plate.Clone() : null;
            Bitmap? overviewSave = images.Overview != null ? (Bitmap)images.Overview.Clone() : null;
            Bitmap? faceSave = images.Face != null ? (Bitmap)images.Face.Clone() : null;

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
                    Debug.WriteLine($"[SaveImagesBackground Error] {ex.Message}");
                }
            });
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

    /// <summary>
    /// Đóng gói ảnh đa camera thu thập từ làn tại một thời điểm
    /// </summary>
    public sealed class CapturedLaneImages : IDisposable
    {
        public Bitmap? Overview { get; set; }
        public Bitmap? Plate { get; set; }
        public Bitmap? Face { get; set; }

        public void Dispose()
        {
            Overview?.Dispose();
            Plate?.Dispose();
            Face?.Dispose();
            Overview = null;
            Plate = null;
            Face = null;
        }
    }
}