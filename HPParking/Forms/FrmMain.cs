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
using System.Diagnostics;
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

        private readonly DeviceOrchestrator _deviceOrchestrator = new();
        private readonly Random _random = new();

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

                // 4. Nhận diện Cổng phụ trách theo MachineCode
                string currentMachineCode = HardwareFingerprint.GetMachineCode();
                _currentGate = await _gateRepository.FindOneAsync(x => x.MachineCode == currentMachineCode && x.IsActive && !x.IsDeleted);

                if (_currentGate != null && !string.IsNullOrWhiteSpace(_currentGate.Id))
                {
                    var gateLanes = await _laneRepository.FindAsync(x => x.GateId == _currentGate.Id && x.IsActive);
                    _lanes = gateLanes?.OrderBy(x => x.InputReader).ToList() ?? [];
                    _laneContexts = _lanes.Select(l => new LaneRuntimeContext(l)).ToList();
                    Text = $"HPPARKING - {(_currentGate.Name ?? "CỔNG KIỂM SOÁT").ToUpperInvariant()} (MÃ: {_currentGate.Code})";
                }
                else
                {
                    _lanes = [];
                    _laneContexts = [];
                    Text = "HPPARKING - [CHẾ ĐỘ MÔ PHỎNG / TEST] CỔNG ĐA LÀN";
                }

                // 5. Liên kết Làn 1-1 và áp dụng Dynamic Layout Option B
                BindLanesToUiSlots();

                // 6. Khởi tạo Thiết bị Phần Cứng & LiveView Camera
                _devices = (await _deviceRepository.GetAllAsync())?.ToList() ?? [];
                await _deviceOrchestrator.InitializeDevicesAsync(_laneContexts, _devices, previewHandleResolver: ResolvePreviewHandles);

                // 7. Lắng nghe tín hiệu quẹt thẻ & cảm biến Radar Realtime
                _deviceOrchestrator.OnCardSwiped += OnCardSwiped;
                _deviceOrchestrator.OnRadarTriggered += OnRadarTriggered;
                _deviceOrchestrator.StartRealtimeLoop();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi Khởi Động HPParking", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region --- 2. DYNAMIC 1-1 LANE BINDING & OPTION B LAYOUT ---

        private void BindLanesToUiSlots()
        {
            if (_lanes.Count == 0)
            {
                // Nếu chưa gán cổng thật: hiển thị chế độ 4 làn mẫu
                SwitchLaneMode(true, true, true, true, "4 Làn Mô Phỏng", btnMode4Lanes);
                GenerateInitialMockImages();
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
            Button? activeBtn = activeCount switch
            {
                1 => btnMode1Lane,
                2 => btnMode2Vehicles,
                3 => btnMode3Lanes,
                _ => btnMode4Lanes
            };

            SwitchLaneMode(lane1Vis, lane2Vis, lane3Vis, lane4Vis, modeDesc, activeBtn);
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

        private LanePreviewHandles? ResolvePreviewHandles(Lane lane)
        {
            int slotIndex = _lanes.IndexOf(lane);
            if (slotIndex < 0) return null;

            return slotIndex switch
            {
                0 => new LanePreviewHandles
                {
                    OverviewHandle = pbLane1Overview.Handle,
                    FaceHandle = pbLane1Face.Handle,
                    PlateHandle = pbLane1Face.Handle
                },
                1 => new LanePreviewHandles
                {
                    OverviewHandle = pbLane2Overview.Handle,
                    FaceHandle = pbLane2Face.Handle,
                    PlateHandle = pbLane2Face.Handle
                },
                2 => new LanePreviewHandles
                {
                    OverviewHandle = pbLane3Overview.Handle,
                    PlateHandle = pbLane3Plate.Handle,
                    FaceHandle = pbLane3Face.Handle
                },
                3 => new LanePreviewHandles
                {
                    OverviewHandle = pbLane4Overview.Handle,
                    PlateHandle = pbLane4Plate.Handle
                },
                _ => null
            };
        }

        public void SwitchLaneMode(bool lane1Vis, bool lane2Vis, bool lane3Vis, bool lane4Vis, string modeName, Button? activeBtn)
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

                ResetModeButtonStyles();
                if (activeBtn != null)
                {
                    activeBtn.BackColor = Color.FromArgb(2, 132, 199);
                    activeBtn.ForeColor = Color.White;
                }

                lblModeNote.Text = $"✓ Đang chạy: {modeName} (Mỗi làn {percentPerActive:0.#}% màn hình, camera dàn đều chuẩn 16:9/4:3)";
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

            // 2. Làn 1 (2 Cam Người Vào)
            Apply2CamsLayout(tlpCamsLane1, pbLane1Overview, pbLane1Face, isWideMode);

            // 3. Làn 2 (2 Cam Người Ra)
            Apply2CamsLayout(tlpCamsLane2, pbLane2Overview, pbLane2Face, isWideMode);

            // 4. Làn 3 (3 Cam Xe Vào: Toàn cảnh, Biển số, FaceID)
            Apply3CamsLayout(tlpCamsLane3, pbLane3Overview, pbLane3Plate, pbLane3Face, isWideMode);

            // 5. Làn 4 (2 Cam Xe Ra: Toàn cảnh, Biển số)
            Apply2CamsLayout(tlpCamsLane4, pbLane4Overview, pbLane4Plate, isWideMode);
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

        private void ResetModeButtonStyles()
        {
            Color defaultBg = Color.FromArgb(241, 245, 249);
            Color defaultFg = Color.FromArgb(51, 65, 85);

            btnMode4Lanes.BackColor = defaultBg;
            btnMode4Lanes.ForeColor = defaultFg;
            btnMode2Vehicles.BackColor = defaultBg;
            btnMode2Vehicles.ForeColor = defaultFg;
            btnMode2Pedestrians.BackColor = defaultBg;
            btnMode2Pedestrians.ForeColor = defaultFg;
            btnMode3Lanes.BackColor = defaultBg;
            btnMode3Lanes.ForeColor = defaultFg;
            btnMode1Lane.BackColor = defaultBg;
            btnMode1Lane.ForeColor = defaultFg;
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

            if (context == null) return;

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
            }));
        }

        private void UpdateLaneSlotUI(int slotIndex, LaneRuntimeContext context, ProcessResult result)
        {
            string timeStr = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
            bool isEntry = context.Lane.Direction == LaneDirection.In;

            if (context.Lane.TargetType == LaneTargetType.Pedestrian || result.Client != null)
            {
                // Cập nhật giao diện Người đi bộ
                string name = result.Client?.Name ?? "Người dùng";
                string code = result.Client?.Code ?? "--";
                string role = result.DepartmentName ?? (result.Client?.Type.ToString() ?? "Khách");

                if (slotIndex == 0)
                {
                    lblLane1Name.Text = $"Họ và tên: {name}";
                    lblLane1Code.Text = $"CCCD/Mã: {code}";
                    lblLane1Role.Text = $"Đối tượng: {role}";
                    if (isEntry) lblLane1TimeIn.Text = $"Ngày vào: {timeStr}";
                    else lblLane1TimeOut.Text = $"Ngày ra: {timeStr}";

                    if (result.Client?.Avatar != null) ImageHelper.SetAvatar(pbLane1Avatar, result.Client.Avatar);
                    if (result.FaceImage != null) pbLane1FaceSnap.Image = (Bitmap)result.FaceImage.Clone();
                    FrmMainMockHelper.FlashLabel(lblLane1Name, Color.LightCyan);
                }
                else if (slotIndex == 1)
                {
                    lblLane2Name.Text = $"Họ và tên: {name}";
                    lblLane2Code.Text = $"CCCD/Mã: {code}";
                    lblLane2Role.Text = $"Đối tượng: {role}";
                    if (isEntry) lblLane2TimeIn.Text = $"Ngày vào: {timeStr}";
                    else lblLane2TimeOut.Text = $"Ngày ra: {timeStr}";

                    if (result.Client?.Avatar != null) ImageHelper.SetAvatar(pbLane2Avatar, result.Client.Avatar);
                    if (result.FaceImage != null) pbLane2FaceSnap.Image = (Bitmap)result.FaceImage.Clone();
                    FrmMainMockHelper.FlashLabel(lblLane2Name, Color.LemonChiffon);
                }
            }
            else
            {
                // Cập nhật giao diện Xe cơ giới
                string plate = result.LprResult?.Plate ?? result.RegisteredPlate ?? "--";
                string driver = result.Client?.Name ?? result.Vehicle?.PlateNumber ?? "Tài xế";
                string dept = result.DepartmentName ?? "Điều vận / Nội bộ";

                if (slotIndex == 2 || slotIndex == 0)
                {
                    lblLane3Driver.Text = $"Tài xế/Chủ xe: {driver}";
                    lblLane3Dept.Text = $"Phòng ban: {dept}";
                    lblLane3PlateReg.Text = $"Biển số đăng ký: {result.RegisteredPlate ?? plate}";
                    lblLane3PlateDet.Text = $"Biển số nhận diện: {plate}";
                    lblLane3PlateDet.ForeColor = result.Status == ProcessStatus.PlateMismatch ? Color.Crimson : Color.DarkGreen;
                    if (isEntry) lblLane3TimeIn.Text = $"Ngày vào: {timeStr}";
                    else lblLane3TimeOut.Text = $"Ngày ra: {timeStr}";

                    if (result.PlateImage != null) pbLane3PlateCrop.Image = (Bitmap)result.PlateImage.Clone();
                    FrmMainMockHelper.FlashLabel(lblLane3PlateDet, Color.Honeydew);
                }
                else
                {
                    lblLane4Driver.Text = $"Tài xế/Chủ xe: {driver}";
                    lblLane4Dept.Text = $"Phòng ban: {dept}";
                    lblLane4PlateReg.Text = $"Biển số đăng ký: {result.RegisteredPlate ?? plate}";
                    lblLane4PlateDet.Text = $"Biển số nhận diện: {plate}";
                    lblLane4PlateDet.ForeColor = result.Status == ProcessStatus.PlateMismatch ? Color.Crimson : Color.DarkGreen;
                    if (isEntry) lblLane4TimeIn.Text = $"Ngày vào: {timeStr}";
                    else lblLane4TimeOut.Text = $"Ngày ra: {timeStr}";

                    if (result.PlateImage != null) pbLane4PlateCrop.Image = (Bitmap)result.PlateImage.Clone();
                    FrmMainMockHelper.FlashLabel(lblLane4PlateDet, Color.Honeydew);
                }
            }

            result.LprResult?.Dispose();
            result.OverviewImage?.Dispose();
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

        #region --- 5. UI ACTIONS & MANUAL BARRIER TRIGGERS ---

        private void BtnLane1Test_Click(object? sender, EventArgs e)
        {
            if (_laneContexts.Count > 0 && _laneContexts[0].Controller != null)
            {
                _laneContexts[0].OpenBarrier();
            }
            else
            {
                SimulateLane1();
            }
        }

        private void BtnLane2Test_Click(object? sender, EventArgs e)
        {
            if (_laneContexts.Count > 1 && _laneContexts[1].Controller != null)
            {
                _laneContexts[1].OpenBarrier();
            }
            else
            {
                SimulateLane2();
            }
        }

        private void BtnLane3Test_Click(object? sender, EventArgs e)
        {
            if (_laneContexts.Count > 2 && _laneContexts[2].Controller != null)
            {
                _laneContexts[2].OpenBarrier();
            }
            else
            {
                SimulateLane3();
            }
        }

        private void BtnLane4Test_Click(object? sender, EventArgs e)
        {
            if (_laneContexts.Count > 3 && _laneContexts[3].Controller != null)
            {
                _laneContexts[3].OpenBarrier();
            }
            else
            {
                SimulateLane4();
            }
        }

        private void BtnSimulateAll_Click(object? sender, EventArgs e)
        {
            SimulateLane1();
            SimulateLane2();
            SimulateLane3();
            SimulateLane4();
        }

        private void BtnMode4Lanes_Click(object? sender, EventArgs e) => SwitchLaneMode(true, true, true, true, "4 Làn (2 Người + 2 Xe)", btnMode4Lanes);
        private void BtnMode2Vehicles_Click(object? sender, EventArgs e) => SwitchLaneMode(false, false, true, true, "2 Làn Xe (1 Vào + 1 Ra)", btnMode2Vehicles);
        private void BtnMode2Pedestrians_Click(object? sender, EventArgs e) => SwitchLaneMode(true, true, false, false, "2 Làn Người (1 Vào + 1 Ra)", btnMode2Pedestrians);
        private void BtnMode3Lanes_Click(object? sender, EventArgs e) => SwitchLaneMode(true, false, true, true, "3 Làn (1 Người Vào + 2 Xe)", btnMode3Lanes);
        private void BtnMode1Lane_Click(object? sender, EventArgs e) => SwitchLaneMode(false, false, true, false, "1 Làn Xe Đơn", btnMode1Lane);

        #endregion

        #region --- 6. SIMULATION & MOCK DATA GENERATOR ---

        private void GenerateInitialMockImages()
        {
            try
            {
                pbLane1Avatar.Image = FrmMainMockHelper.CreateMockAvatar("Trần Văn Mạnh", Color.FromArgb(2, 132, 199));
                pbLane1FaceSnap.Image = FrmMainMockHelper.CreateMockFaceSnapshot("08:15:22", 98.7);

                pbLane2Avatar.Image = FrmMainMockHelper.CreateMockAvatar("Lê Thị Thu", Color.FromArgb(217, 119, 6));
                pbLane2FaceSnap.Image = FrmMainMockHelper.CreateMockFaceSnapshot("11:45:00", 99.2);

                pbLane3PlateCrop.Image = FrmMainMockHelper.CreateMockPlateCrop("29C-888.68");
                pbLane3DriverAvatar.Image = FrmMainMockHelper.CreateMockAvatar("Phạm Q. Hùng", Color.FromArgb(22, 163, 74));

                pbLane4PlateCrop.Image = FrmMainMockHelper.CreateMockPlateCrop("15A-678.90");
                pbLane4EntrySnap.Image = FrmMainMockHelper.CreateMockVehicleSnapshot("15A-678.90", "VÀO: 09:05:40");
            }
            catch { }
        }

        private void SimulateLane1()
        {
            int code = _random.Next(1000, 9999);
            string time = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
            lblLane1Name.Text = $"Họ và tên: NV Test {code % 100}";
            lblLane1TimeIn.Text = $"Ngày vào: {time}";
            lblLane1TimeOut.Text = "Ngày ra: --";
            lblLane1Code.Text = $"CCCD/Mã NV: 03109{code}12";
            lblLane1Role.Text = "Đối tượng: Nhân viên";

            pbLane1Avatar.Image?.Dispose();
            pbLane1FaceSnap.Image?.Dispose();
            pbLane1Avatar.Image = FrmMainMockHelper.CreateMockAvatar($"NV {code % 100}", Color.FromArgb(2, 132, 199));
            pbLane1FaceSnap.Image = FrmMainMockHelper.CreateMockFaceSnapshot(DateTime.Now.ToString("HH:mm:ss"), 98.5 + (_random.NextDouble() * 1.4));

            FrmMainMockHelper.FlashLabel(lblLane1Name, Color.LightCyan);
        }

        private void SimulateLane2()
        {
            int code = _random.Next(1000, 9999);
            string time = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
            lblLane2Name.Text = $"Họ và tên: Nhà thầu {code % 50}";
            lblLane2TimeOut.Text = $"Ngày ra: {time}";
            lblLane2Code.Text = $"CCCD: 03618{code}93";
            lblLane2Role.Text = "Đối tượng: Nhà thầu";

            pbLane2Avatar.Image?.Dispose();
            pbLane2FaceSnap.Image?.Dispose();
            pbLane2Avatar.Image = FrmMainMockHelper.CreateMockAvatar($"NT {code % 50}", Color.FromArgb(217, 119, 6));
            pbLane2FaceSnap.Image = FrmMainMockHelper.CreateMockFaceSnapshot(DateTime.Now.ToString("HH:mm:ss"), 97.8 + (_random.NextDouble() * 2.0));

            FrmMainMockHelper.FlashLabel(lblLane2Name, Color.LemonChiffon);
        }

        private void SimulateLane3()
        {
            int num = _random.Next(100, 999);
            string plate = $"29C-{num}.{_random.Next(10, 99)}";
            string time = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");

            lblLane3Driver.Text = $"Tài xế/Chủ xe: Lái xe {num % 30}";
            lblLane3TimeIn.Text = $"Ngày vào: {time}";
            lblLane3TimeOut.Text = "Ngày ra: --";
            lblLane3PlateReg.Text = $"Biển số đăng ký: {plate}";
            lblLane3PlateDet.Text = $"Biển số nhận diện: {plate}";
            lblLane3PlateDet.ForeColor = Color.DarkGreen;

            pbLane3PlateCrop.Image?.Dispose();
            pbLane3DriverAvatar.Image?.Dispose();
            pbLane3PlateCrop.Image = FrmMainMockHelper.CreateMockPlateCrop(plate);
            pbLane3DriverAvatar.Image = FrmMainMockHelper.CreateMockAvatar($"TX {num % 30}", Color.FromArgb(22, 163, 74));

            FrmMainMockHelper.FlashLabel(lblLane3PlateDet, Color.Honeydew);
        }

        private void SimulateLane4()
        {
            int num = _random.Next(100, 999);
            string plate = $"15A-{num}.{_random.Next(10, 99)}";
            string time = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");

            lblLane4Driver.Text = $"Tài xế/Khách: Khách vãng lai {num % 20}";
            lblLane4TimeOut.Text = $"Ngày ra: {time}";
            lblLane4PlateReg.Text = $"Biển số đăng ký: {plate}";
            lblLane4PlateDet.Text = $"Biển số nhận diện: {plate}";
            lblLane4PlateDet.ForeColor = Color.DarkGreen;

            pbLane4PlateCrop.Image?.Dispose();
            pbLane4EntrySnap.Image?.Dispose();
            pbLane4PlateCrop.Image = FrmMainMockHelper.CreateMockPlateCrop(plate);
            pbLane4EntrySnap.Image = FrmMainMockHelper.CreateMockVehicleSnapshot(plate, "LƯỢT RA HỢP LỆ");

            FrmMainMockHelper.FlashLabel(lblLane4PlateDet, Color.Honeydew);
        }

        #endregion

        #region --- 7. CLEANUP & FORM CLOSING ---

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
    /// Helper độc lập sinh dữ liệu đồ họa mô phỏng (Mock GDI+) phục vụ kiểm thử giao diện trạm khi không gắn phần cứng
    /// </summary>
    internal static class FrmMainMockHelper
    {
        public static Bitmap CreateMockAvatar(string name, Color badgeColor)
        {
            Bitmap bmp = new(160, 200);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.FromArgb(248, 250, 252));
            using var badgeBrush = new SolidBrush(badgeColor);
            g.FillRectangle(badgeBrush, 0, 0, 160, 40);
            using var textBrush = new SolidBrush(Color.White);
            using var font = new Font("Segoe UI", 9F, FontStyle.Bold);
            g.DrawString(name, font, textBrush, new RectangleF(0, 8, 160, 25), new StringFormat { Alignment = StringAlignment.Center });
            using var headBrush = new SolidBrush(Color.FromArgb(203, 213, 225));
            g.FillEllipse(headBrush, 45, 55, 70, 70);
            g.FillPie(headBrush, 25, 120, 110, 100, 180, 180);
            using var borderPen = new Pen(Color.FromArgb(226, 232, 240), 2);
            g.DrawRectangle(borderPen, 0, 0, 159, 199);
            return bmp;
        }

        public static Bitmap CreateMockFaceSnapshot(string time, double score)
        {
            Bitmap bmp = new(160, 200);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.FromArgb(15, 23, 42));
            using var boxPen = new Pen(Color.FromArgb(34, 197, 94), 2);
            g.DrawRectangle(boxPen, 30, 40, 100, 110);
            using var textBrush = new SolidBrush(Color.FromArgb(34, 197, 94));
            using var font = new Font("Segoe UI", 8F, FontStyle.Bold);
            g.DrawString($"MATCH: {score:0.0}%", font, textBrush, 32, 20);
            using var timeBrush = new SolidBrush(Color.White);
            g.DrawString($"SNAP: {time}", font, timeBrush, 10, 160);
            return bmp;
        }

        public static Bitmap CreateMockPlateCrop(string plate)
        {
            Bitmap bmp = new(200, 70);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.White);
            using var borderPen = new Pen(Color.Black, 3);
            g.DrawRectangle(borderPen, 2, 2, 195, 65);
            using var brush = new SolidBrush(Color.Black);
            using var font = new Font("Segoe UI", 16F, FontStyle.Bold);
            g.DrawString(plate, font, brush, new RectangleF(0, 14, 200, 45), new StringFormat { Alignment = StringAlignment.Center });
            return bmp;
        }

        public static Bitmap CreateMockVehicleSnapshot(string plate, string subtitle)
        {
            Bitmap bmp = new(200, 130);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.FromArgb(30, 41, 59));
            using var brush = new SolidBrush(Color.White);
            using var font = new Font("Segoe UI", 9F, FontStyle.Bold);
            g.DrawString("ẢNH LƯỢT VÀO", font, brush, new RectangleF(0, 15, 200, 25), new StringFormat { Alignment = StringAlignment.Center });
            using var plateBrush = new SolidBrush(Color.Gold);
            using var plateFont = new Font("Segoe UI", 12F, FontStyle.Bold);
            g.DrawString(plate, plateFont, plateBrush, new RectangleF(0, 45, 200, 30), new StringFormat { Alignment = StringAlignment.Center });
            using var subBrush = new SolidBrush(Color.LightGreen);
            using var subFont = new Font("Segoe UI", 8F, FontStyle.Regular);
            g.DrawString(subtitle, subFont, subBrush, new RectangleF(0, 85, 200, 25), new StringFormat { Alignment = StringAlignment.Center });
            return bmp;
        }

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
