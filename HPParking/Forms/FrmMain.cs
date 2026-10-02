using HPParking.Core.Interfaces;
using HPParking.Core.Licensing;
using HPParking.Core.Models.Common;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Helper;
using HPParking.Interfaces;
using HPParking.Models;
using HPParking.Services.Controller;
using HPParking.Services.Devices;
using HPParking.Services.License;
using HPParking.Services.Parking;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace HPParking.Forms
{
    public partial class FrmMain : Form
    {
        private readonly IRepository<Lane> _laneRepository;
        private readonly IRepository<Company> _companyRepository;
        private readonly IRepository<Gate> _gateRepository;
        private readonly IRepository<Device> _deviceRepository;
        private readonly LicenseManager _licenseManager;
        private readonly IParkingWorkflowService _workflowService;
        private readonly IServerHealthService _serverHealthService;

        private DeviceOrchestrator _deviceOrchestrator = new();

        private List<Lane> _lanes = [];
        private List<LaneRuntimeContext> _laneContexts = [];
        private List<Device> _devices = [];
        private Gate? _currentGate;
        private Timer? _clockTimer;
        private ServerHealthReport? _lastServerHealthReport;
        private CancellationTokenSource? _healthCheckCts;

        public FrmMain(
            IRepository<Lane> laneRepository,
            IRepository<Company> companyRepository,
            IRepository<Gate> gateRepository,
            IRepository<Device> deviceRepository,
            LicenseManager licenseManager,
            IParkingWorkflowService workflowService,
            IServerHealthService serverHealthService)
        {
            InitializeComponent();

            _laneRepository = laneRepository;
            _companyRepository = companyRepository;
            _gateRepository = gateRepository;
            _deviceRepository = deviceRepository;
            _licenseManager = licenseManager;
            _workflowService = workflowService;
            _serverHealthService = serverHealthService;

            _deviceOrchestrator.OnControllerStatusChanged += Controller_OnStatusChanged;
        }

        #region --- 1. FORM LOAD & INITIALIZATION ---

        private async void FrmMain_Load(object? sender, EventArgs e)
        {
            try
            {
                using var waitScope = new WaitCursorScope(this);

                // 1. Kiểm tra License bản quyền
                lbdayExpiryDate.Cursor = Cursors.Hand;
                lbdayExpiryDate.DoubleClick -= LbdayExpiryDate_DoubleClick;
                lbdayExpiryDate.DoubleClick += LbdayExpiryDate_DoubleClick;

                // 2. Giám sát Server Health
                lbServer.Cursor = Cursors.Hand;
                lbServer.Click -= lbServer_Click;
                lbServer.Click += lbServer_Click;
                StartServerHealthMonitoring();

                bool licenseOk = await CheckAndEnforceLicenseAsync();
                if (!licenseOk) return;

                // 3. Đồng hồ thời gian thực
                _clockTimer = new Timer { Interval = 1000 };
                _clockTimer.Tick += (s, args) =>
                {
                    lbRealTime.Text = $"HÔM NAY: {DateTime.Now:HH:mm:ss dd/MM/yyyy}";
                };
                _clockTimer.Start();

                // 4. Nạp Cổng, Làn và kết nối toàn bộ thiết bị phần cứng thực tế
                await LoadHardwareAndLanesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi Khởi Động HPParking", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadHardwareAndLanesAsync()
        {
            // Nhận diện Cổng phụ trách theo MachineCode máy trạm
            string currentMachineCode = HardwareFingerprint.GetMachineCode();
            _currentGate = await _gateRepository.FindOneAsync(x => x.MachineCode == currentMachineCode && x.IsActive && !x.IsDeleted);

            // Fallback nếu máy tính chưa gán MachineCode vào cổng cụ thể: lấy Cổng đang kích hoạt đầu tiên
            if (_currentGate == null)
            {
                var allActiveGates = await _gateRepository.FindAsync(x => x.IsActive && !x.IsDeleted);
                _currentGate = allActiveGates?.FirstOrDefault();
            }

            if (_currentGate != null && !string.IsNullOrWhiteSpace(_currentGate.Id))
            {
                var gateLanes = await _laneRepository.FindAsync(x => x.GateId == _currentGate.Id && x.IsActive && !x.IsDeleted);
                _lanes = gateLanes?.OrderBy(x => x.InputReader).ToList() ?? [];
                _laneContexts = _lanes.Select(l => new LaneRuntimeContext(l)).ToList();

                Text = $"HPPARKING - {(_currentGate.Name ?? "CỔNG KIỂM SOÁT").ToUpperInvariant()} (MÃ: {_currentGate.Code})";
                lblGateInfo.Text = $"CỔNG: {(_currentGate.Name ?? "CỔNG KIỂM SOÁT").ToUpperInvariant()} (MÃ: {_currentGate.Code}) - SỐ LÀN KẾT NỐI: {_lanes.Count}";
            }
            else
            {
                // Fallback nếu không tìm thấy cổng nào: nạp toàn bộ các làn đang kích hoạt
                var allActiveLanes = await _laneRepository.FindAsync(x => x.IsActive && !x.IsDeleted);
                _lanes = allActiveLanes?.OrderBy(x => x.InputReader).ToList() ?? [];
                _laneContexts = _lanes.Select(l => new LaneRuntimeContext(l)).ToList();

                Text = "HPPARKING - HỆ THỐNG KIỂM SOÁT VÀO RA";
                lblGateInfo.Text = $"HỆ THỐNG KIỂM SOÁT VÀO RA - TẤT CẢ LÀN HOẠT ĐỘNG ({_lanes.Count} LÀN)";
            }

            // Liên kết Làn 1-1 và áp dụng Dynamic Layout
            BindLanesToUiSlots();

            // Khởi tạo Thiết bị Phần Cứng & LiveView Camera
            _devices = (await _deviceRepository.FindAsync(x => x.IsActive && !x.IsDeleted))?.ToList() ?? [];
            await _deviceOrchestrator.InitializeDevicesAsync(_laneContexts, _devices, previewHandleResolver: ResolvePreviewHandles);

            // Lắng nghe tín hiệu quẹt thẻ & cảm biến Radar Realtime
            _deviceOrchestrator.OnCardSwiped -= OnCardSwiped;
            _deviceOrchestrator.OnCardSwiped += OnCardSwiped;
            _deviceOrchestrator.OnRadarTriggered -= OnRadarTriggered;
            _deviceOrchestrator.OnRadarTriggered += OnRadarTriggered;
            _deviceOrchestrator.StartRealtimeLoop();
        }

        #endregion

        #region --- 2. DYNAMIC 1-1 LANE BINDING & OPTION B LAYOUT ---

        private void BindLanesToUiSlots()
        {
            if (_lanes.Count == 0)
            {
                SwitchLaneMode(true, false, false, false, "Chưa Cấu Hình Làn");
                lblTitleLane1.Text = "⚠️ CHƯA CÓ LÀN HOẠT ĐỘNG";
                lblSubLane1.Text = "Vui lòng cấu hình cổng và làn trên WebAdmin";
                return;
            }

            int activeCount = Math.Min(_lanes.Count, 4);

            // Cấu hình tiêu đề và loại làn cho từng slot
            for (int i = 0; i < activeCount; i++)
            {
                var lane = _lanes[i];
                ConfigureSlotHeader(i, lane);
            }

            // Kích hoạt hiển thị tương ứng
            bool lane1Vis = activeCount >= 1;
            bool lane2Vis = activeCount >= 2;
            bool lane3Vis = activeCount >= 3;
            bool lane4Vis = activeCount >= 4;

            string modeDesc = $"{activeCount} Làn Hoạt Động";
            SwitchLaneMode(lane1Vis, lane2Vis, lane3Vis, lane4Vis, modeDesc);
        }

        private void ConfigureSlotHeader(int slotIndex, Lane lane)
        {
            string dirText = lane.Direction == LaneDirection.In ? "VÀO" : "RA";
            string typeIcon = lane.TargetType == LaneTargetType.Pedestrian ? "🚶" : "🚗";
            string typeText = lane.TargetType == LaneTargetType.Pedestrian ? "NGƯỜI ĐI BỘ" : "XE CƠ GIỚI";

            switch (slotIndex)
            {
                case 0:
                    lblTitleLane1.Text = $"{typeIcon} LÀN 1 ({dirText})";
                    lblSubLane1.Text = $"{lane.Name.ToUpperInvariant()} - {typeText}";
                    break;
                case 1:
                    lblTitleLane2.Text = $"{typeIcon} LÀN 2 ({dirText})";
                    lblSubLane2.Text = $"{lane.Name.ToUpperInvariant()} - {typeText}";
                    break;
                case 2:
                    lblTitleLane3.Text = $"{typeIcon} LÀN 3 ({dirText})";
                    lblSubLane3.Text = $"{lane.Name.ToUpperInvariant()} - {typeText}";
                    break;
                case 3:
                    lblTitleLane4.Text = $"{typeIcon} LÀN 4 ({dirText})";
                    lblSubLane4.Text = $"{lane.Name.ToUpperInvariant()} - {typeText}";
                    break;
            }
        }

        private (PictureBox Overview, PictureBox Plate, PictureBox Face) GetLaneCamBoxes(int slotIndex)
        {
            return slotIndex switch
            {
                0 => (pbLane1Overview, pbLane1Plate, pbLane1Face),
                1 => (pbLane2Overview, pbLane2Plate, pbLane2Face),
                2 => (pbLane3Overview, pbLane3Plate, pbLane3Face),
                3 => (pbLane4Overview, pbLane4Plate, pbLane4Face),
                _ => throw new ArgumentOutOfRangeException(nameof(slotIndex))
            };
        }

        private TableLayoutPanel GetLaneCamsContainer(int slotIndex)
        {
            return slotIndex switch
            {
                0 => tlpCamsLane1,
                1 => tlpCamsLane2,
                2 => tlpCamsLane3,
                3 => tlpCamsLane4,
                _ => throw new ArgumentOutOfRangeException(nameof(slotIndex))
            };
        }

        private LanePreviewHandles? ResolvePreviewHandles(Lane lane)
        {
            int slotIndex = _lanes.IndexOf(lane);
            if (slotIndex < 0 || slotIndex > 3) return null;

            var (pbOverview, pbPlate, pbFace) = GetLaneCamBoxes(slotIndex);

            bool isPedestrian = lane.TargetType == LaneTargetType.Pedestrian;
            bool hasFace = lane.UseFaceCam || (!string.IsNullOrEmpty(lane.FaceDeviceId)) || isPedestrian;
            bool hasPlate = !isPedestrian && (lane.UsePlateCam || !string.IsNullOrEmpty(lane.PlateCameraDeviceId));
            bool hasOverview = lane.UseOverviewCam || (!string.IsNullOrEmpty(lane.OverviewCameraDeviceId));

            return new LanePreviewHandles
            {
                OverviewHandle = hasOverview ? pbOverview.Handle : IntPtr.Zero,
                PlateHandle = hasPlate ? pbPlate.Handle : IntPtr.Zero,
                FaceHandle = hasFace ? pbFace.Handle : IntPtr.Zero
            };
        }

        public void SwitchLaneMode(bool lane1Vis, bool lane2Vis, bool lane3Vis, bool lane4Vis, string modeName)
        {
            tlpLanes.SuspendLayout();
            try
            {
                tlpLane1.Visible = lane1Vis;
                tlpLane2.Visible = lane2Vis;
                tlpLane3.Visible = lane3Vis;
                tlpLane4.Visible = lane4Vis;

                int activeCount = (lane1Vis ? 1 : 0) + (lane2Vis ? 1 : 0) + (lane3Vis ? 1 : 0) + (lane4Vis ? 1 : 0);
                if (activeCount == 0) activeCount = 1;
                float percentPerActive = 100f / activeCount;

                tlpLanes.ColumnStyles.Clear();
                tlpLanes.ColumnStyles.Add(lane1Vis ? new ColumnStyle(SizeType.Percent, percentPerActive) : new ColumnStyle(SizeType.Absolute, 0f));
                tlpLanes.ColumnStyles.Add(lane2Vis ? new ColumnStyle(SizeType.Percent, percentPerActive) : new ColumnStyle(SizeType.Absolute, 0f));
                tlpLanes.ColumnStyles.Add(lane3Vis ? new ColumnStyle(SizeType.Percent, percentPerActive) : new ColumnStyle(SizeType.Absolute, 0f));
                tlpLanes.ColumnStyles.Add(lane4Vis ? new ColumnStyle(SizeType.Percent, percentPerActive) : new ColumnStyle(SizeType.Absolute, 0f));

                // Áp dụng Phương án B: Tự động dàn đều camera trên 1 hàng ngang khi ở chế độ ít làn (<= 3 làn)
                ApplyOptionBCameraLayout(activeCount);
            }
            finally
            {
                tlpLanes.ResumeLayout(true);
            }
        }

        private void ApplyOptionBCameraLayout(int activeLaneCount)
        {
            bool isWideMode = activeLaneCount <= 3;

            // 1. Điều chỉnh tỷ lệ chiều cao của hàng camera trong mỗi cột
            UpdateLaneHeightRatio(tlpLane1, isWideMode);
            UpdateLaneHeightRatio(tlpLane2, isWideMode);
            UpdateLaneHeightRatio(tlpLane3, isWideMode);
            UpdateLaneHeightRatio(tlpLane4, isWideMode);

            // 2. Dàn layout camera linh hoạt cho từng làn dựa trên cấu hình và số camera kích hoạt
            for (int i = 0; i < 4; i++)
            {
                var tlpCams = GetLaneCamsContainer(i);
                var (pbOverview, pbPlate, pbFace) = GetLaneCamBoxes(i);
                var lane = i < _lanes.Count ? _lanes[i] : null;

                ApplyDynamicLaneCamLayout(tlpCams, pbOverview, pbPlate, pbFace, lane, isWideMode);
            }
        }

        private static void ApplyDynamicLaneCamLayout(
            TableLayoutPanel tlpCams,
            PictureBox pbOverview,
            PictureBox pbPlate,
            PictureBox pbFace,
            Lane? lane,
            bool isWideMode)
        {
            tlpCams.SuspendLayout();
            try
            {
                bool isPedestrian = lane?.TargetType == LaneTargetType.Pedestrian;
                bool hasFace = lane != null && (lane.UseFaceCam || !string.IsNullOrEmpty(lane.FaceDeviceId) || isPedestrian);
                bool hasPlate = lane != null && !isPedestrian && (lane.UsePlateCam || !string.IsNullOrEmpty(lane.PlateCameraDeviceId));
                bool hasOverview = lane == null || lane.UseOverviewCam || !string.IsNullOrEmpty(lane.OverviewCameraDeviceId);

                int camCount = (hasOverview ? 1 : 0) + (hasPlate ? 1 : 0) + (hasFace ? 1 : 0);

                if (camCount >= 3)
                {
                    pbOverview.Visible = true;
                    pbPlate.Visible = true;
                    pbFace.Visible = true;
                    Apply3CamsLayout(tlpCams, pbOverview, pbPlate, pbFace, isWideMode);
                }
                else if (camCount == 2)
                {
                    if (hasOverview && hasPlate)
                    {
                        pbOverview.Visible = true;
                        pbPlate.Visible = true;
                        pbFace.Visible = false;
                        Apply2CamsLayout(tlpCams, pbOverview, pbPlate, isWideMode);
                    }
                    else if (hasOverview && hasFace)
                    {
                        pbOverview.Visible = true;
                        pbFace.Visible = true;
                        pbPlate.Visible = false;
                        Apply2CamsLayout(tlpCams, pbOverview, pbFace, isWideMode);
                    }
                    else
                    {
                        pbPlate.Visible = true;
                        pbFace.Visible = true;
                        pbOverview.Visible = false;
                        Apply2CamsLayout(tlpCams, pbPlate, pbFace, isWideMode);
                    }
                }
                else if (camCount == 1)
                {
                    var activeCam = hasOverview ? pbOverview : (hasPlate ? pbPlate : pbFace);
                    pbOverview.Visible = (activeCam == pbOverview);
                    pbPlate.Visible = (activeCam == pbPlate);
                    pbFace.Visible = (activeCam == pbFace);

                    tlpCams.ColumnCount = 1;
                    tlpCams.RowCount = 1;
                    tlpCams.ColumnStyles.Clear();
                    tlpCams.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                    tlpCams.RowStyles.Clear();
                    tlpCams.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                    tlpCams.SetColumnSpan(activeCam, 1);
                    tlpCams.SetCellPosition(activeCam, new TableLayoutPanelCellPosition(0, 0));
                }
                else
                {
                    pbOverview.Visible = true;
                    pbPlate.Visible = true;
                    pbFace.Visible = false;
                    Apply2CamsLayout(tlpCams, pbOverview, pbPlate, isWideMode);
                }
            }
            finally
            {
                tlpCams.ResumeLayout(true);
            }
        }

        private static void UpdateLaneHeightRatio(TableLayoutPanel tlpLane, bool isWideMode)
        {
            tlpLane.SuspendLayout();
            try
            {
                tlpLane.RowStyles.Clear();
                tlpLane.RowStyles.Add(new RowStyle(SizeType.Absolute, 68F)); // Tiêu đề cổng
                if (isWideMode)
                {
                    // Chế độ rộng: 1 hàng camera cao 280px (chuẩn 16:9 / 4:3), phần dưới thông tin nhận toàn bộ không gian còn lại
                    tlpLane.RowStyles.Add(new RowStyle(SizeType.Absolute, 280F));
                    tlpLane.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                }
                else
                {
                    // Chế độ 4 làn: camera xếp 2 hàng chiếm 55%, thông tin 45%
                    tlpLane.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
                    tlpLane.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
                }
            }
            finally
            {
                tlpLane.ResumeLayout(true);
            }
        }

        private static void Apply2CamsLayout(TableLayoutPanel tlpCams, PictureBox cam1, PictureBox cam2, bool isWideMode)
        {
            tlpCams.SuspendLayout();
            try
            {
                if (isWideMode)
                {
                    // Phương án B: 2 camera dàn đều 1 hàng ngang (2 Cột 50% / 50%)
                    tlpCams.ColumnCount = 2;
                    tlpCams.RowCount = 1;
                    tlpCams.ColumnStyles.Clear();
                    tlpCams.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                    tlpCams.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                    tlpCams.RowStyles.Clear();
                    tlpCams.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                    tlpCams.SetCellPosition(cam1, new TableLayoutPanelCellPosition(0, 0));
                    tlpCams.SetCellPosition(cam2, new TableLayoutPanelCellPosition(1, 0));
                }
                else
                {
                    // Chế độ 4 làn: 2 camera xếp dọc (1 Cột, 2 Hàng 50% / 50%)
                    tlpCams.ColumnCount = 1;
                    tlpCams.RowCount = 2;
                    tlpCams.ColumnStyles.Clear();
                    tlpCams.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                    tlpCams.RowStyles.Clear();
                    tlpCams.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                    tlpCams.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

                    tlpCams.SetCellPosition(cam1, new TableLayoutPanelCellPosition(0, 0));
                    tlpCams.SetCellPosition(cam2, new TableLayoutPanelCellPosition(0, 1));
                }
            }
            finally
            {
                tlpCams.ResumeLayout(true);
            }
        }

        private static void Apply3CamsLayout(TableLayoutPanel tlpCams, PictureBox camOverview, PictureBox camPlate, PictureBox camFace, bool isWideMode)
        {
            tlpCams.SuspendLayout();
            try
            {
                if (isWideMode)
                {
                    // Phương án B: 3 camera dàn đều 1 hàng ngang (3 Cột 33.33% mỗi cột)
                    tlpCams.ColumnCount = 3;
                    tlpCams.RowCount = 1;
                    tlpCams.ColumnStyles.Clear();
                    tlpCams.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
                    tlpCams.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
                    tlpCams.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.334F));
                    tlpCams.RowStyles.Clear();
                    tlpCams.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                    tlpCams.SetColumnSpan(camOverview, 1);
                    tlpCams.SetCellPosition(camOverview, new TableLayoutPanelCellPosition(0, 0));
                    tlpCams.SetCellPosition(camPlate, new TableLayoutPanelCellPosition(1, 0));
                    tlpCams.SetCellPosition(camFace, new TableLayoutPanelCellPosition(2, 0));
                }
                else
                {
                    // Chế độ 4 làn: Hàng 1 là Toàn cảnh rộng 100%, Hàng 2 chia đôi Biển số & FaceID
                    tlpCams.ColumnCount = 2;
                    tlpCams.RowCount = 2;
                    tlpCams.ColumnStyles.Clear();
                    tlpCams.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                    tlpCams.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                    tlpCams.RowStyles.Clear();
                    tlpCams.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
                    tlpCams.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));

                    tlpCams.SetColumnSpan(camOverview, 2);
                    tlpCams.SetCellPosition(camOverview, new TableLayoutPanelCellPosition(0, 0));
                    tlpCams.SetColumnSpan(camPlate, 1);
                    tlpCams.SetCellPosition(camPlate, new TableLayoutPanelCellPosition(0, 1));
                    tlpCams.SetColumnSpan(camFace, 1);
                    tlpCams.SetCellPosition(camFace, new TableLayoutPanelCellPosition(1, 1));
                }
            }
            finally
            {
                tlpCams.ResumeLayout(true);
            }
        }

        #endregion

        #region --- 3. WORKFLOW EVENTS: CARD SWIPE & RADAR ---

        private void OnCardSwiped(RealtimeLog data)
        {
            if (_laneContexts == null || _laneContexts.Count == 0) return;

            LaneRuntimeContext? context = _laneContexts.FirstOrDefault(x =>
                x.Lane.InputReader == data.DoorId &&
                (x.Controller?.Config?.IP == data.ControllerIp ||
                 _devices.Any(d => d.Id == x.Lane.ControllerDeviceId && d.IpAddress == data.ControllerIp)));

            if (context == null)
            {
                BeginInvoke(new Action(() =>
                {
                    MessageBox.Show(
                        this,
                        $"Nhận tín hiệu thẻ [{data.CardNo}] tại Đầu đọc {data.DoorId} (Bộ điều khiển {data.ControllerIp}), nhưng Đầu đọc {data.DoorId} chưa được gán vào làn hoạt động nào!\n\nVui lòng kiểm tra danh sách Làn trên WebAdmin và bật làn tương ứng.",
                        "⚠️ Cảnh Báo Đầu Đọc Chưa Gán Làn",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }));
                return;
            }

            int slotIndex = _laneContexts.IndexOf(context);
            string pathImage = StorageConfigHelper.GetPathImage();

            BeginInvoke(new Action(async () =>
            {
                var trigger = new WorkflowTriggerEvent
                {
                    Source = TriggerSource.CardSwipe,
                    RawCardNo = data.CardNo ?? string.Empty,
                    ReaderIndex = data.DoorId,
                    DoorIndex = data.DoorId,
                    TriggerTime = (data.Time != default && data.Time != DateTime.MinValue) ? data.Time : DateTime.Now
                };

                ProcessResult result = await _workflowService.ProcessWorkflowAsync(
                    context,
                    trigger,
                    pathImage,
                    HandleBarrierOpenFailed,
                    HandleManualPlateInputAsync);

                UpdateLaneSlotUI(slotIndex, context, result);

                if (result.Status == ProcessStatus.PlateMismatch)
                {
                    MessageBox.Show(this, result.Message, "Biển Số Không Khớp", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (result.Status == ProcessStatus.ConfirmRequired)
                {
                    MessageBox.Show(this, result.Message, "Cảnh Báo Vi Phạm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else if (result.Status != ProcessStatus.Success)
                {
                    MessageBox.Show(
                        this,
                        $"XÁC THỰC THẤT BẠI - TỪ CHỐI QUA CỔNG!\n\n• Lý do: {result.Message}\n• Trạng thái: {result.Status}\n• Mã thẻ: {trigger.RawCardNo}\n• Làn: {context.Lane.Name} (Đầu đọc {data.DoorId})",
                        "Từ Chối Vào / Ra",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }));
        }

        private void OnRadarTriggered(RealtimeLog data)
        {
            if (_laneContexts == null || _laneContexts.Count == 0) return;

            LaneRuntimeContext? context = _laneContexts.FirstOrDefault(x =>
                x.Lane.InputReader == data.DoorId &&
                (x.Controller?.Config?.IP == data.ControllerIp ||
                 _devices.Any(d => d.Id == x.Lane.ControllerDeviceId && d.IpAddress == data.ControllerIp)));

            if (context == null) return;

            int slotIndex = _laneContexts.IndexOf(context);
            string pathImage = StorageConfigHelper.GetPathImage();

            BeginInvoke(new Action(async () =>
            {
                var trigger = new WorkflowTriggerEvent
                {
                    Source = TriggerSource.Radar,
                    ReaderIndex = data.DoorId,
                    DoorIndex = data.DoorId,
                    TriggerTime = (data.Time != default && data.Time != DateTime.MinValue) ? data.Time : DateTime.Now
                };

                ProcessResult result = await _workflowService.ProcessWorkflowAsync(
                    context,
                    trigger,
                    pathImage,
                    HandleBarrierOpenFailed,
                    HandleManualPlateInputAsync);

                UpdateLaneSlotUI(slotIndex, context, result);

                if (result.Status != ProcessStatus.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"[Radar Làn {context.Lane.Name}]: {result.Status} - {result.Message}");
                }
            }));
        }

        private (PictureBox Box1, PictureBox Box2) GetLaneInfoBoxes(int slotIndex) => slotIndex switch
        {
            0 => (pbLane1Avatar, pbLane1FaceSnap),
            1 => (pbLane2Avatar, pbLane2FaceSnap),
            2 => (pbLane3PlateCrop, pbLane3DriverAvatar),
            3 => (pbLane4PlateCrop, pbLane4EntrySnap),
            _ => throw new ArgumentOutOfRangeException(nameof(slotIndex))
        };

        private static void SetBoxImage(PictureBox box, Bitmap? newImage)
        {
            var old = box.Image;
            box.Image = newImage != null ? (Bitmap)newImage.Clone() : null;
            old?.Dispose();
        }

        private void UpdateLaneSlotUI(int slotIndex, LaneRuntimeContext context, ProcessResult result)
        {
            string timeStr = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
            bool isEntry = context.Lane.Direction == LaneDirection.In;
            bool isSuccess = result.Status == ProcessStatus.Success;

            // Cập nhật banner thông báo trạng thái trên tiêu đề Làn
            Label? subLabel = slotIndex switch
            {
                0 => lblSubLane1,
                1 => lblSubLane2,
                2 => lblSubLane3,
                3 => lblSubLane4,
                _ => null
            };

            if (subLabel != null)
            {
                if (isSuccess)
                {
                    subLabel.Text = $"✅ {result.Message}";
                    subLabel.BackColor = Color.ForestGreen;
                    subLabel.ForeColor = Color.White;
                }
                else
                {
                    subLabel.Text = $"❌ {result.Message}";
                    subLabel.BackColor = Color.Crimson;
                    subLabel.ForeColor = Color.White;
                }
            }

            if (context.Lane.TargetType == LaneTargetType.Pedestrian)
            {
                // Cập nhật giao diện Người đi bộ
                string name = result.Client?.Name ?? (isSuccess ? "Người dùng" : "KHÔNG XÁC THỰC");
                string code = result.Client?.Code ?? (string.IsNullOrEmpty(result.Client?.CardCode) ? "--" : result.Client.CardCode);
                string dept = isSuccess
                    ? (result.DepartmentName ?? "Nội bộ")
                    : $"TỪ CHỐI ({result.Status})";
                string role = isSuccess
                    ? (result.Client?.Type.ToString() ?? "Nhân viên / Khách")
                    : $"TỪ CHỐI ({result.Status})";

                Color flashColor = isSuccess ? Color.LightGreen : Color.LightCoral;

                if (slotIndex == 0)
                {
                    lblLane1Name.Text = $"Họ và tên: {name}";
                    lblLane1Dept.Text = $"Phòng ban: {dept}";
                    lblLane1Dept.ForeColor = isSuccess ? Color.Black : Color.Crimson;
                    lblLane1Code.Text = $"CCCD/Mã: {code}";
                    lblLane1Role.Text = $"Đối tượng: {role}";
                    lblLane1Role.ForeColor = isSuccess ? Color.Black : Color.Crimson;
                    if (isEntry) lblLane1TimeIn.Text = $"Ngày vào: {timeStr}";
                    else lblLane1TimeOut.Text = $"Ngày ra: {timeStr}";
                    FrmMainVisualHelper.FlashLabel(lblLane1Name, flashColor);
                }
                else if (slotIndex == 1)
                {
                    lblLane2Name.Text = $"Họ và tên: {name}";
                    lblLane2Dept.Text = $"Phòng ban: {dept}";
                    lblLane2Dept.ForeColor = isSuccess ? Color.Black : Color.Crimson;
                    lblLane2Code.Text = $"CCCD/Mã: {code}";
                    lblLane2Role.Text = $"Đối tượng: {role}";
                    lblLane2Role.ForeColor = isSuccess ? Color.Black : Color.Crimson;
                    if (isEntry) lblLane2TimeIn.Text = $"Ngày vào: {timeStr}";
                    else lblLane2TimeOut.Text = $"Ngày ra: {timeStr}";
                    FrmMainVisualHelper.FlashLabel(lblLane2Name, flashColor);
                }
                else if (slotIndex == 2)
                {
                    lblLane3Driver.Text = $"Họ và tên: {name}";
                    lblLane3Dept.Text = $"Phòng ban: {dept}";
                    lblLane3Dept.ForeColor = isSuccess ? Color.Black : Color.Crimson;
                    lblLane3PlateReg.Text = $"CCCD/Mã: {code}";
                    lblLane3PlateDet.Text = $"Đối tượng: {role}";
                    lblLane3PlateDet.ForeColor = isSuccess ? Color.Black : Color.Crimson;
                    if (isEntry) lblLane3TimeIn.Text = $"Ngày vào: {timeStr}";
                    else lblLane3TimeOut.Text = $"Ngày ra: {timeStr}";
                    FrmMainVisualHelper.FlashLabel(lblLane3Driver, flashColor);
                }
                else
                {
                    lblLane4Driver.Text = $"Họ và tên: {name}";
                    lblLane4Dept.Text = $"Phòng ban: {dept}";
                    lblLane4Dept.ForeColor = isSuccess ? Color.Black : Color.Crimson;
                    lblLane4PlateReg.Text = $"CCCD/Mã: {code}";
                    lblLane4PlateDet.Text = $"Đối tượng: {role}";
                    lblLane4PlateDet.ForeColor = isSuccess ? Color.Black : Color.Crimson;
                    if (isEntry) lblLane4TimeIn.Text = $"Ngày vào: {timeStr}";
                    else lblLane4TimeOut.Text = $"Ngày ra: {timeStr}";
                    FrmMainVisualHelper.FlashLabel(lblLane4Driver, flashColor);
                }

                var (box1, box2) = GetLaneInfoBoxes(slotIndex);
                if (result.Client?.Avatar != null)
                {
                    ImageHelper.SetAvatar(box1, result.Client.Avatar);
                }
                else
                {
                    SetBoxImage(box1, null);
                }
                SetBoxImage(box2, result.FaceImage);
            }
            else
            {
                // Cập nhật giao diện Xe cơ giới
                string plate = result.LprResult?.Plate ?? result.RegisteredPlate ?? "--";
                string driver = result.Client?.Name ?? result.Vehicle?.PlateNumber ?? "Tài xế";
                string dept = isSuccess
                    ? (result.DepartmentName ?? "Điều vận / Nội bộ")
                    : $"TỪ CHỐI ({result.Status})";

                Color flashColor = isSuccess ? Color.Honeydew : Color.LightCoral;

                if (slotIndex == 0)
                {
                    lblLane1Name.Text = $"Tài xế/Chủ xe: {driver}";
                    lblLane1Dept.Text = $"Phòng ban: {dept}";
                    lblLane1Dept.ForeColor = isSuccess ? Color.Black : Color.Crimson;
                    lblLane1Code.Text = $"Biển số đăng ký: {result.RegisteredPlate ?? plate}";
                    lblLane1Role.Text = $"Biển số nhận diện: {plate}";
                    lblLane1Role.ForeColor = result.Status == ProcessStatus.PlateMismatch ? Color.Crimson : (isSuccess ? Color.DarkGreen : Color.Crimson);
                    if (isEntry) lblLane1TimeIn.Text = $"Ngày vào: {timeStr}";
                    else lblLane1TimeOut.Text = $"Ngày ra: {timeStr}";
                    FrmMainVisualHelper.FlashLabel(lblLane1Role, flashColor);
                }
                else if (slotIndex == 1)
                {
                    lblLane2Name.Text = $"Tài xế/Chủ xe: {driver}";
                    lblLane2Dept.Text = $"Phòng ban: {dept}";
                    lblLane2Dept.ForeColor = isSuccess ? Color.Black : Color.Crimson;
                    lblLane2Code.Text = $"Biển số đăng ký: {result.RegisteredPlate ?? plate}";
                    lblLane2Role.Text = $"Biển số nhận diện: {plate}";
                    lblLane2Role.ForeColor = result.Status == ProcessStatus.PlateMismatch ? Color.Crimson : (isSuccess ? Color.DarkGreen : Color.Crimson);
                    if (isEntry) lblLane2TimeIn.Text = $"Ngày vào: {timeStr}";
                    else lblLane2TimeOut.Text = $"Ngày ra: {timeStr}";
                    FrmMainVisualHelper.FlashLabel(lblLane2Role, flashColor);
                }
                else if (slotIndex == 2)
                {
                    lblLane3Driver.Text = $"Tài xế/Chủ xe: {driver}";
                    lblLane3Dept.Text = $"Phòng ban: {dept}";
                    lblLane3Dept.ForeColor = isSuccess ? Color.Black : Color.Crimson;
                    lblLane3PlateReg.Text = $"Biển số đăng ký: {result.RegisteredPlate ?? plate}";
                    lblLane3PlateDet.Text = $"Biển số nhận diện: {plate}";
                    lblLane3PlateDet.ForeColor = result.Status == ProcessStatus.PlateMismatch ? Color.Crimson : (isSuccess ? Color.DarkGreen : Color.Crimson);
                    if (isEntry) lblLane3TimeIn.Text = $"Ngày vào: {timeStr}";
                    else lblLane3TimeOut.Text = $"Ngày ra: {timeStr}";
                    FrmMainVisualHelper.FlashLabel(lblLane3PlateDet, flashColor);
                }
                else
                {
                    lblLane4Driver.Text = $"Tài xế/Chủ xe: {driver}";
                    lblLane4Dept.Text = $"Phòng ban: {dept}";
                    lblLane4Dept.ForeColor = isSuccess ? Color.Black : Color.Crimson;
                    lblLane4PlateReg.Text = $"Biển số đăng ký: {result.RegisteredPlate ?? plate}";
                    lblLane4PlateDet.Text = $"Biển số nhận diện: {plate}";
                    lblLane4PlateDet.ForeColor = result.Status == ProcessStatus.PlateMismatch ? Color.Crimson : (isSuccess ? Color.DarkGreen : Color.Crimson);
                    if (isEntry) lblLane4TimeIn.Text = $"Ngày vào: {timeStr}";
                    else lblLane4TimeOut.Text = $"Ngày ra: {timeStr}";
                    FrmMainVisualHelper.FlashLabel(lblLane4PlateDet, flashColor);
                }

                var (box1, box2) = GetLaneInfoBoxes(slotIndex);

                // 1. Ô đầu tiên hiện ảnh biển số nhỏ (cropped plate)
                Bitmap? smallPlate = result.PlateImage ?? result.LprResult?.PlateImage;
                SetBoxImage(box1, smallPlate);

                // 2. Ô thứ 2 hiện ảnh chụp khuôn mặt từ faceid nếu làn đó có faceid, không thì cứ để trống
                bool hasFaceId = context.Lane != null && (context.Lane.UseFaceCam || !string.IsNullOrEmpty(context.Lane.FaceDeviceId));
                SetBoxImage(box2, (hasFaceId && result.FaceImage != null) ? result.FaceImage : null);
            }

            result.LprResult?.Dispose();
            result.OverviewImage?.Dispose();
            result.PlateImage?.Dispose();
            result.FaceImage?.Dispose();
        }

        #endregion

        #region --- 4. HARDWARE & STATUS HANDLERS ---

        private void Controller_OnStatusChanged(string controllerIp, bool isConnected, string message)
        {
            if (IsDisposed || Disposing) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => Controller_OnStatusChanged(controllerIp, isConnected, message)));
                return;
            }

            if (isConnected)
            {
                lbStatusCtrl.Text = "BỘ ĐIỀU KHIỂN: ONLINE";
                lbStatusCtrl.BackColor = Color.SeaGreen;
            }
            else
            {
                lbStatusCtrl.Text = "BỘ ĐIỀU KHIỂN: OFFLINE";
                lbStatusCtrl.BackColor = Color.Crimson;
            }
        }

        private void StartServerHealthMonitoring()
        {
            _healthCheckCts?.Cancel();
            _healthCheckCts?.Dispose();
            _healthCheckCts = new CancellationTokenSource();
            var token = _healthCheckCts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var report = await _serverHealthService.CheckHealthAsync(token);
                        if (token.IsCancellationRequested) break;
                        UpdateServerStatusUI(report);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception)
                    {
                        UpdateServerStatusUI(new ServerHealthReport
                        {
                            OverallStatus = ServerHealthStatus.Offline,
                            MongoMessage = "Lỗi kết nối",
                            StorageMessage = "Lỗi kiểm tra"
                        });
                    }

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }, token);
        }

        private void UpdateServerStatusUI(ServerHealthReport report)
        {
            if (IsDisposed || Disposing) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateServerStatusUI(report)));
                return;
            }

            _lastServerHealthReport = report;

            switch (report.OverallStatus)
            {
                case ServerHealthStatus.Online:
                    lbServer.Text = "MÁY CHỦ: ONLINE";
                    lbServer.BackColor = Color.Teal;
                    break;
                case ServerHealthStatus.Warning:
                    lbServer.Text = "MÁY CHỦ: WARNING";
                    lbServer.BackColor = Color.Peru;
                    break;
                case ServerHealthStatus.Offline:
                default:
                    lbServer.Text = "MÁY CHỦ: OFFLINE";
                    lbServer.BackColor = Color.Crimson;
                    break;
            }
        }

        private void lbServer_Click(object? sender, EventArgs e)
        {
            if (_lastServerHealthReport == null)
            {
                MessageBox.Show(this, "Đang kiểm tra kết nối...", "Tình Trạng Máy Chủ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var icon = _lastServerHealthReport.OverallStatus switch
            {
                ServerHealthStatus.Online => MessageBoxIcon.Information,
                ServerHealthStatus.Warning => MessageBoxIcon.Warning,
                _ => MessageBoxIcon.Error
            };

            MessageBox.Show(this, _lastServerHealthReport.GetDetailedSummary(), "Chi Tiết Máy Chủ & Cơ Sở Dữ Liệu", MessageBoxButtons.OK, icon);
        }

        private bool HandleBarrierOpenFailed(LaneRuntimeContext context)
        {
            if (InvokeRequired)
            {
                return (bool)Invoke(new Func<bool>(() => HandleBarrierOpenFailed(context)));
            }

            Lane lane = context.Lane;
            string laneDesc = $"{lane.Name} ({lane.Direction})";

            while (true)
            {
                var dialog = new TaskDialogPage
                {
                    Caption = "Lỗi Thiết Bị Barrier",
                    Heading = $"Không thể kích hoạt mở Barrier làn {laneDesc}!",
                    Text = "Rơle điều khiển barrier thất bại hoặc mất kết nối thiết bị controller.",
                    Icon = TaskDialogIcon.Error,
                    Buttons =
                    {
                        TaskDialogButton.Retry,
                        TaskDialogButton.Cancel
                    }
                };

                var result = TaskDialog.ShowDialog(this, dialog);

                if (result == TaskDialogButton.Retry)
                {
                    if (context.OpenBarrier()) return true;
                    continue;
                }

                var confirmResult = MessageBox.Show(
                    this,
                    "Mở barrier thủ công cho đối tượng đã qua?\n\n- Chọn YES nếu đã mở thủ công.\n- Chọn NO để hủy bỏ lượt này.",
                    "Xác nhận mở barrier thủ công",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                return confirmResult == DialogResult.Yes;
            }
        }

        private Task<string?> HandleManualPlateInputAsync(LaneRuntimeContext context, string? expectedPlate)
        {
            if (IsDisposed || Disposing) return Task.FromResult<string?>(null);

            if (InvokeRequired)
            {
                return Invoke(new Func<Task<string?>>(() => HandleManualPlateInputAsync(context, expectedPlate)));
            }

            Lane lane = context.Lane;
            string mismatchMessage = (lane.Direction == LaneDirection.In)
                ? "Biển số xe không đúng với biển số đăng ký."
                : "Biển số không khớp với biển số xe đã gửi.";

            string? plate = FrmManualPlateInput.Prompt(
                this,
                lane,
                expectedPlate: expectedPlate,
                mismatchMessage: mismatchMessage);

            return Task.FromResult(plate);
        }

        private async Task<bool> CheckAndEnforceLicenseAsync()
        {
            string? currentKey = await _licenseManager.GetCurrentLicenseKeyAsync();
            var validation = !string.IsNullOrWhiteSpace(currentKey)
                ? LicenseCrypto.ValidateLicense(currentKey!)
                : new LicenseValidationResult { IsValid = false, Message = "Chưa tìm thấy thông tin bản quyền trên máy trạm này." };

            if (!validation.IsValid)
            {
                lbdayExpiryDate.Text = "BẢN QUYỀN: HẾT HẠN";
                lbdayExpiryDate.BackColor = Color.OrangeRed;

                using var expiredForm = new LicenseExpiredForm(validation.Message);
                var dialogResult = expiredForm.ShowDialog(this);
                if (dialogResult == DialogResult.OK && expiredForm.IsActivatedSuccessfully)
                {
                    var newValidation = LicenseCrypto.ValidateLicense(expiredForm.ActivatedKey);
                    if (newValidation.Payload != null)
                    {
                        try
                        {
                            await _licenseManager.SaveLicenseKeyAsync(expiredForm.ActivatedKey, newValidation.Payload);
                            UpdateLicenseFooter(newValidation);
                            return true;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Không thể lưu KEY bản quyền: {ex.Message}", "Lỗi Bản Quyền", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Application.Exit();
                            return false;
                        }
                    }
                }
                else
                {
                    Application.Exit();
                    return false;
                }
            }

            UpdateLicenseFooter(validation);
            return true;
        }

        private void UpdateLicenseFooter(LicenseValidationResult validation)
        {
            if (validation.Payload?.IsPermanent == true)
            {
                lbdayExpiryDate.Text = "BẢN QUYỀN: VĨNH VIỄN";
                lbdayExpiryDate.BackColor = Color.DarkSlateGray;
            }
            else
            {
                lbdayExpiryDate.Text = "BẢN QUYỀN: HỢP LỆ";
                lbdayExpiryDate.BackColor = Color.DarkSlateGray;
            }
        }

        private async void LbdayExpiryDate_DoubleClick(object? sender, EventArgs e)
        {
            using var activateForm = new LicenseExpiredForm("Quản Lý & Gia Hạn Bản Quyền Phần Mềm");
            if (activateForm.ShowDialog(this) == DialogResult.OK && activateForm.IsActivatedSuccessfully)
            {
                var newValidation = LicenseCrypto.ValidateLicense(activateForm.ActivatedKey);
                if (newValidation.Payload != null)
                {
                    try
                    {
                        await _licenseManager.SaveLicenseKeyAsync(activateForm.ActivatedKey, newValidation.Payload);
                        UpdateLicenseFooter(newValidation);
                        MessageBox.Show("Đã cập nhật bản quyền mới thành công!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Không thể lưu KEY bản quyền: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        #endregion

        #region --- 5. UI ACTIONS & HARDWARE RELOAD ---

        private async void BtnReloadHardware_Click(object? sender, EventArgs e)
        {
            try
            {
                using var waitScope = new WaitCursorScope(this);
                btnReloadHardware.Enabled = false;
                btnReloadHardware.Text = "⏳ ĐANG KẾT NỐI LẠI...";

                // Giải phóng kết nối phần cứng cũ và khởi tạo lại Orchestrator mới
                _deviceOrchestrator.Dispose();
                _deviceOrchestrator = new DeviceOrchestrator();
                _deviceOrchestrator.OnControllerStatusChanged += Controller_OnStatusChanged;

                // Nạp lại cấu hình và khởi tạo lại toàn bộ thiết bị
                await LoadHardwareAndLanesAsync();

                MessageBox.Show("Nạp lại cấu hình và kết nối thiết bị phần cứng thành công!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối lại thiết bị: {ex.Message}", "Lỗi Phần Cứng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnReloadHardware.Enabled = true;
                btnReloadHardware.Text = "🔄 NẠP LẠI THIẾT BỊ";
            }
        }

        #endregion

        #region --- 6. CLEANUP & FORM CLOSING ---

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _healthCheckCts?.Cancel();
            _healthCheckCts?.Dispose();
            lbServer.Click -= lbServer_Click;
            _deviceOrchestrator.OnControllerStatusChanged -= Controller_OnStatusChanged;
            _deviceOrchestrator.OnCardSwiped -= OnCardSwiped;
            _deviceOrchestrator.OnRadarTriggered -= OnRadarTriggered;
            _clockTimer?.Stop();
            _clockTimer?.Dispose();
            _deviceOrchestrator.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// Helper hỗ trợ hiệu ứng đồ họa UI trạm kiểm soát
    /// </summary>
    internal static class FrmMainVisualHelper
    {
        public static void FlashLabel(Label lbl, Color flashColor)
        {
            Color orig = lbl.BackColor;
            lbl.BackColor = flashColor;
            var t = new Timer { Interval = 400 };
            t.Tick += (s, args) =>
            {
                lbl.BackColor = orig;
                t.Stop();
                t.Dispose();
            };
            t.Start();
        }
    }
}
