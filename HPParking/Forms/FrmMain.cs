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
using HPParking.Services.Printing;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace HPParking.Forms
{
    public partial class FrmMain : Form
    {
        private readonly IRepository<Lane> _laneRepository;
        private readonly IRepository<Gate> _gateRepository;
        private readonly IRepository<Device> _deviceRepository;
        private readonly LicenseManager _licenseManager;
        private readonly IParkingWorkflowService _workflowService;
        private readonly IServerHealthService _serverHealthService;
        private readonly IRepository<AuditLog>? _auditLogRepository;
        private readonly IParkingReceiptPrinterService _receiptPrinterService;

        internal Task? LastAuditTask { get; private set; }

        private DeviceOrchestrator _deviceOrchestrator = new();

        private List<Lane> _lanes = [];
        private List<LaneRuntimeContext> _laneContexts = [];
        private List<Device> _devices = [];
        private Gate? _currentGate;
        private Timer? _clockTimer;
        private ServerHealthReport? _lastServerHealthReport;
        private CancellationTokenSource? _healthCheckCts;
        private readonly ProcessResult?[] _lastSlotResults = new ProcessResult?[4];
        private CoreWebView2Environment? _webViewEnvironment;
        private readonly bool[] _slotWebViewReady = new bool[4];
        private readonly string?[] _pendingSlotJsonPayloads = new string?[4];
        private readonly LaneCardPayload?[] _lastSlotPayloads = new LaneCardPayload?[4];
        private static readonly JsonSerializerOptions _webViewJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        private static readonly CultureInfo _viCulture = CultureInfo.GetCultureInfo("vi-VN");

        public FrmMain(
            IRepository<Lane> laneRepository,
            IRepository<Gate> gateRepository,
            IRepository<Device> deviceRepository,
            LicenseManager licenseManager,
            IParkingWorkflowService workflowService,
            IServerHealthService serverHealthService,
            IRepository<AuditLog>? auditLogRepository = null,
            IParkingReceiptPrinterService? receiptPrinterService = null)
        {
            InitializeComponent();
            foreach (var wv in new[] { wvInfoLane1, wvInfoLane2, wvInfoLane3, wvInfoLane4 })
            {
                wv.TabStop = false;
            }

            _laneRepository = laneRepository;
            _gateRepository = gateRepository;
            _deviceRepository = deviceRepository;
            _licenseManager = licenseManager;
            _workflowService = workflowService;
            _serverHealthService = serverHealthService;
            _auditLogRepository = auditLogRepository;
            _receiptPrinterService = receiptPrinterService ?? new ParkingReceiptPrinterService();

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

                // 4. Khởi tạo các khung hiển thị thông tin WebView2 (HTML + Tailwind Offline)
                await InitializeLaneWebViewsAsync();

                // 5. Nạp Cổng, Làn và kết nối toàn bộ thiết bị phần cứng thực tế
                await LoadHardwareAndLanesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi Khởi Động HPParking", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task<bool> LoadHardwareAndLanesAsync()
        {
            // Nhận diện Cổng phụ trách theo MachineCode máy trạm
            string currentMachineCode = HardwareFingerprint.GetMachineCode();
            _currentGate = await _gateRepository.FindOneAsync(x => x.MachineCode == currentMachineCode && x.IsActive && !x.IsDeleted);

            // Không fallback — nếu không khớp MachineCode thì báo lỗi cấu hình rõ ràng
            if (_currentGate == null)
            {
                ShowNoGateConfiguredWarning(currentMachineCode);
                SetFormConfigurationLocked(true);
                return false;
            }

            var gateLanes = await _laneRepository.FindAsync(x => x.GateId == _currentGate.Id && x.IsActive && !x.IsDeleted);
            _lanes = gateLanes?.OrderBy(x => x.InputReader).ToList() ?? [];


            Text = $"HPPARKING - {(_currentGate.Name ?? "CỔNG KIỂM SOÁT").ToUpperInvariant()} (MÃ: {_currentGate.Code})";
            lblGateInfo.Text = $"CỔNG: {(_currentGate.Name ?? "CỔNG KIỂM SOÁT").ToUpperInvariant()} (MÃ: {_currentGate.Code}) - SỐ LÀN KẾT NỐI: {_lanes.Count}";

            if (_lanes.Count == 0)
            {
                ShowNoLaneConfiguredWarning(_currentGate.Name ?? _currentGate.Code);
                SetFormConfigurationLocked(true);
                return false;
            }

            // Cấu hình hợp lệ — mở khóa form và khởi tạo
            SetFormConfigurationLocked(false);

            // Liên kết Làn 1-1 và áp dụng Dynamic Layout
            ApplyLaneContexts([.. _lanes.Select(l => new LaneRuntimeContext(l))]);

            // Khởi tạo Thiết bị Phần Cứng & LiveView Camera
            _devices = (await _deviceRepository.FindAsync(x => x.IsActive && !x.IsDeleted))?.ToList() ?? [];
            await _deviceOrchestrator.InitializeDevicesAsync(_laneContexts, _devices, previewHandleResolver: ResolvePreviewHandles);

            // Lắng nghe tín hiệu quẹt thẻ & cảm biến Radar Realtime
            _deviceOrchestrator.OnCardSwiped -= OnCardSwiped;
            _deviceOrchestrator.OnCardSwiped += OnCardSwiped;
            _deviceOrchestrator.OnRadarTriggered -= OnRadarTriggered;
            _deviceOrchestrator.OnRadarTriggered += OnRadarTriggered;
            _deviceOrchestrator.StartRealtimeLoop();
            return true;
        }

        #endregion

        #region --- 2. DYNAMIC 1-1 LANE BINDING & OPTION B LAYOUT ---

        internal void ApplyLaneContexts(List<LaneRuntimeContext> contexts)
        {
            Array.Clear(_lastSlotResults, 0, _lastSlotResults.Length);
            _laneContexts = contexts ?? [];
            _lanes = [.. _laneContexts.Select(c => c.Lane)];
            BindLanesToUiSlots();
        }

        internal string GetSlotTitleText(int slotIndex) => GetSlotTitleLabel(slotIndex).Text;

        private Label GetSlotTitleLabel(int slotIndex) => slotIndex switch
        {
            0 => lblTitleLane1,
            1 => lblTitleLane2,
            2 => lblTitleLane3,
            3 => lblTitleLane4,
            _ => throw new ArgumentOutOfRangeException(nameof(slotIndex))
        };

        private void BindLanesToUiSlots()
        {
            if (_lanes.Count == 0)
            {
                SwitchLaneMode(true, false, false, false, "Chưa Cấu Hình Làn");
                lblTitleLane1.Text = "⚠️ CHƯA CÓ LÀN HOẠT ĐỘNG";
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
            GetSlotTitleLabel(slotIndex).Text = $"{lane.Name.ToUpperInvariant()} [F{slotIndex + 1}]";
            ResetLaneSlotCardToIdle(slotIndex, lane);
        }

        internal WebView2 GetLaneInfoWebView(int slotIndex) => slotIndex switch
        {
            0 => wvInfoLane1,
            1 => wvInfoLane2,
            2 => wvInfoLane3,
            3 => wvInfoLane4,
            _ => throw new ArgumentOutOfRangeException(nameof(slotIndex))
        };

        internal LaneCardPayload? GetLastSlotCardPayload(int slotIndex) =>
            (slotIndex >= 0 && slotIndex < 4) ? _lastSlotPayloads[slotIndex] : null;

        internal (PictureBox Overview, PictureBox Plate, PictureBox Face) GetLaneCamBoxes(int slotIndex)
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

        internal TableLayoutPanel GetLaneCamsContainer(int slotIndex)
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
                tlpLane.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F)); // Tiêu đề làn nhỏ gọn
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
                tlpCams.SetColumnSpan(cam1, 1);
                tlpCams.SetColumnSpan(cam2, 1);

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
                    tlpCams.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                    tlpCams.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

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

            // Đóng ngay cảnh báo/form nhập tay của lượt trước nếu có xe mới quẹt thẻ
            ParkingTaskDialog.DismissActive();
            FrmManualPlateInput.DismissActive();

            LaneRuntimeContext? context = _laneContexts.FirstOrDefault(x =>
                x.Lane.InputReader == data.DoorId &&
                (x.Controller?.Config?.IP == data.ControllerIp ||
                 _devices.Any(d => d.Id == x.Lane.ControllerDeviceId && d.IpAddress == data.ControllerIp)));

            if (context == null)
            {
                BeginInvoke(new Action(() =>
                {
                    ParkingTaskDialog.ShowWarning(
                        this,
                        heading: "ĐẦU ĐỌC CHƯA GÁN LÀN",
                        text: $"Đầu đọc {data.DoorId} (Bộ điều khiển {data.ControllerIp}) chưa được gán vào làn hoạt động nào!",
                        details: $"• Mã thẻ: {data.CardNo}\n• Thời gian: {DateTime.Now:HH:mm:ss dd/MM/yyyy}\n\nVui lòng kiểm tra danh sách Làn trên WebAdmin và bật làn tương ứng.");
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
                    HandleManualPlateInputAsync,
                    HandleFeeConfirmationAsync);

                UpdateLaneSlotUI(slotIndex, context, result);

                if (result.Status == ProcessStatus.TariffError)
                {
                    ParkingTaskDialog.ShowError(
                        this,
                        heading: "LỖI TÍNH PHÍ ĐỖ XE",
                        text: result.Message,
                        details: $"• Làn: {context.Lane.Name} (Đầu đọc {data.DoorId})\n• Mã thẻ: {trigger.RawCardNo}\n• Cảnh báo biểu phí: {result.HasTariffWarning}\n• Trạng thái: {result.Status}");
                }
                else if (result.Status == ProcessStatus.FeeConfirmationRequired)
                {
                    ParkingTaskDialog.ShowWarning(
                        this,
                        heading: "CHƯA XÁC NHẬN THU PHÍ",
                        text: result.Message,
                        details: $"• Làn: {context.Lane.Name} (Đầu đọc {data.DoorId})\n• Mã thẻ: {trigger.RawCardNo}\n• Số tiền chưa thu: {result.FeeAmount:N0} VNĐ");
                }
                else if (result.Status == ProcessStatus.PlateMismatch)
                {
                    ParkingTaskDialog.ShowError(
                        this,
                        heading: "BIỂN SỐ KHÔNG KHỚP",
                        text: result.Message,
                        details: $"• Làn: {context.Lane.Name} (Đầu đọc {data.DoorId})\n• Mã thẻ: {trigger.RawCardNo}\n• Biển số xe: {result.Vehicle?.PlateNumber ?? result.ParkingSession?.PlateNumber ?? "Chưa rõ"}\n• Trạng thái: {result.Status}");
                }
                else if (result.Status == ProcessStatus.ConfirmRequired)
                {
                    string title = (result.Message.Contains("SAI TUYẾN") || result.Message.Contains("LẠC TUYẾN"))
                        ? "CẢNH BÁO XE ĐI SAI TUYẾN"
                        : "CẢNH BÁO VI PHẠM";
                    ParkingTaskDialog.ShowWarning(
                        this,
                        heading: title,
                        text: result.Message,
                        details: $"• Làn: {context.Lane.Name} (Đầu đọc {data.DoorId})\n• Mã thẻ: {trigger.RawCardNo}\n• Trạng thái: {result.Status}");
                }
                else if (result.Status != ProcessStatus.Success)
                {
                    ParkingTaskDialog.ShowError(
                        this,
                        heading: "TỪ CHỐI QUA CỔNG",
                        text: result.Message,
                        details: $"• Lý do: {result.Message}\n• Trạng thái: {result.Status}\n• Mã thẻ: {trigger.RawCardNo}\n• Làn: {context.Lane.Name} (Đầu đọc {data.DoorId})");
                }
            }));
        }

        private void OnRadarTriggered(RealtimeLog data)
        {
            if (_laneContexts == null || _laneContexts.Count == 0) return;

            // Đóng ngay cảnh báo/form nhập tay của lượt trước nếu có xe tiếp cận
            ParkingTaskDialog.DismissActive();
            FrmManualPlateInput.DismissActive();

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
                    HandleManualPlateInputAsync,
                    HandleFeeConfirmationAsync);

                UpdateLaneSlotUI(slotIndex, context, result);

                if (result.Status != ProcessStatus.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"[Radar Làn {context.Lane.Name}]: {result.Status} - {result.Message}");
                }
            }));
        }

        private static string FormatClientTypeVietnamese(ClientType type) => type switch
        {
            ClientType.Employee => "Nhân viên",
            ClientType.Contractor => "Nhà thầu",
            ClientType.Visitor => "Khách vãng lai",
            ClientType.VIP => "Khách VIP",
            ClientType.Guest => "Khách",
            _ => type.ToString()
        };

        private static (string CardCodeText, string? FeeHighlight) FormatCardDisplayParts(ProcessResult result)
        {
            if (result.HasTariffWarning)
            {
                return ("CẢNH BÁO BIỂU PHÍ (Lỗi tính phí)", null);
            }

            string cardNumber = result.Card?.CardNumber
                ?? result.ParkingSession?.CardNumber
                ?? result.Client?.CardCode
                ?? string.Empty;

            if (result.Card == null && string.IsNullOrWhiteSpace(cardNumber) && result.FeeAmount <= 0)
            {
                return ("---", null);
            }

            string codePart = !string.IsNullOrWhiteSpace(cardNumber) ? cardNumber : "---";
            string feePart = result.FeeAmount > 0
                ? $"{result.FeeAmount.ToString("N0", _viCulture)} VNĐ"
                : "Miễn phí";

            return (codePart, feePart);
        }

        private static string FormatProcessStatusBadgeText(ProcessStatus status) => status switch
        {
            ProcessStatus.Success => "HỢP LỆ",
            ProcessStatus.PlateMismatch => "SAI BIỂN SỐ",
            ProcessStatus.TariffError => "LỖI BIỂU PHÍ",
            ProcessStatus.FeeConfirmationRequired => "CHỜ THU PHÍ",
            ProcessStatus.ConfirmRequired => "CẢNH BÁO",
            _ => "TỪ CHỐI"
        };

        private static string FormatLaneTargetBadge(Lane? lane)
        {
            if (lane == null) return "LÀN KIỂM SOÁT";
            string dirText = lane.Direction == LaneDirection.In ? "LÀN VÀO" : "LÀN RA";
            string targetText = lane.TargetType == LaneTargetType.Pedestrian ? "NGƯỜI ĐI BỘ" : "XE CƠ GIỚI";
            return $"{targetText} • {dirText}";
        }

        private async Task InitializeLaneWebViewsAsync()
        {
            try
            {
                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "HPParking",
                    "WebView2");

                _webViewEnvironment ??= await CoreWebView2Environment.CreateAsync(null, userDataFolder);

                string htmlFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UI");

                for (int i = 0; i < 4; i++)
                {
                    int slotIndex = i;
                    WebView2 webView = GetLaneInfoWebView(slotIndex);
                    webView.TabStop = false;

                    await webView.EnsureCoreWebView2Async(_webViewEnvironment);

                    var settings = webView.CoreWebView2.Settings;
                    settings.AreDefaultContextMenusEnabled = false;
                    settings.AreDevToolsEnabled = false;
                    settings.AreBrowserAcceleratorKeysEnabled = false;
                    settings.IsStatusBarEnabled = false;
                    settings.IsZoomControlEnabled = false;

                    webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "laneui.local",
                        htmlFolder,
                        CoreWebView2HostResourceAccessKind.Allow);

                    webView.CoreWebView2.WebMessageReceived += async (s, args) =>
                    {
                        string? message = null;
                        try
                        {
                            message = args.TryGetWebMessageAsString();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[WebView2 Message Error]: {ex.Message}");
                        }

                        await HandleLaneWebViewMessageAsync(slotIndex, message, webView);
                    };

                    webView.CoreWebView2.NavigationCompleted += (s, args) =>
                    {
                        if (!args.IsSuccess) return;
                        FlushPendingSlotPayload(slotIndex, webView);
                    };

                    webView.CoreWebView2.Navigate($"https://laneui.local/lane-card.html?v={DateTime.UtcNow.Ticks}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WebView2 Init Error]: {ex.Message}");
            }
        }

        internal async Task<bool> HandleLaneWebViewMessageAsync(
            int slotIndex,
            string? message,
            WebView2? webView = null)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                if (string.Equals(message, "PRINT_RECEIPT", StringComparison.OrdinalIgnoreCase))
                {
                    if (IsHandleCreated && !IsDisposed && !Disposing)
                    {
                        ActiveControl = null;
                    }

                    ParkingReceiptPrintResult printResult = await HandlePrintReceiptForSlotAsync(slotIndex);
                    return printResult.IsSuccess;
                }

                if (!string.Equals(message, "READY", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (webView != null)
            {
                FlushPendingSlotPayload(slotIndex, webView);
                return true;
            }

            return false;
        }

        private void FlushPendingSlotPayload(int slotIndex, WebView2 webView)
        {
            _slotWebViewReady[slotIndex] = true;
            string? pendingJson = _pendingSlotJsonPayloads[slotIndex];
            _pendingSlotJsonPayloads[slotIndex] = null;
            if (!string.IsNullOrEmpty(pendingJson) && !webView.IsDisposed && webView.CoreWebView2 != null)
            {
                webView.CoreWebView2.PostWebMessageAsJson(pendingJson);
            }
        }

        internal async Task<ParkingReceiptPrintResult> HandlePrintReceiptForSlotAsync(int slotIndex)
        {
            if (!tlpLanes.Enabled || _laneContexts == null || slotIndex < 0 || slotIndex >= _laneContexts.Count || slotIndex >= 4)
            {
                return ParkingReceiptPrintResult.Failure("Chỉ số làn không hợp lệ hoặc làn đang bị khóa cấu hình.");
            }

            ProcessResult? lastResult = _lastSlotResults[slotIndex];
            LaneRuntimeContext context = _laneContexts[slotIndex];

            ParkingReceiptData? receiptData = _receiptPrinterService.TryCreateReceiptData(lastResult, context, _currentGate);
            if (receiptData == null)
            {
                string invalidMsg = "Lượt xe hiện tại tại làn này không có dữ liệu thu phí hợp lệ để in biên lai.";
                ShowPrintWarningSafe("KHÔNG THỂ IN BIÊN LAI", invalidMsg, null);
                return ParkingReceiptPrintResult.Failure(invalidMsg);
            }

            ParkingReceiptPrintResult printResult = await _receiptPrinterService.PrintReceiptAsync(receiptData);
            if (!printResult.IsSuccess)
            {
                string heading = "LỖI MÁY IN BIÊN LAI NHIỆT (K80)";
                string text = printResult.ErrorMessage ?? "Không thể gửi lệnh in biên lai thu phí xuống máy in.";
                string details =
                    $"• Làn kiểm soát: {context?.Lane?.Name ?? receiptData.OutLaneName}\n" +
                    $"• Biển số xe: {receiptData.PlateNumber}\n" +
                    $"• Số tiền thu: {receiptData.FeeAmount.ToString("N0", _viCulture)} VNĐ\n" +
                    $"• Máy in cấu hình: {(!string.IsNullOrWhiteSpace(printResult.PrinterName) ? printResult.PrinterName : "Default Printer")}";

                ShowPrintWarningSafe(heading, text, details);
            }

            return printResult;
        }

        private void ShowPrintWarningSafe(
            string heading,
            string text,
            string? details)
        {
            if (!IsHandleCreated || IsDisposed || Disposing)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ShowPrintWarningSafe(heading, text, details)));
                return;
            }

            ParkingTaskDialog.ShowWarning(
                this,
                heading: heading,
                text: text,
                details: details);
        }

        private void ResetLaneSlotCardToIdle(int slotIndex, Lane lane)
        {
            if (slotIndex >= 0 && slotIndex < 4)
            {
                _lastSlotResults[slotIndex] = null;
            }

            bool isPedestrian = lane.TargetType == LaneTargetType.Pedestrian;
            bool isEntry = lane.Direction == LaneDirection.In;
            var idlePayload = new LaneCardPayload
            {
                IsIdle = true,
                IsEntry = isEntry,
                IsSuccess = true,
                CanPrintReceipt = false,
                StatusText = "CHỜ QUẸT THẺ",
                TargetType = FormatLaneTargetBadge(lane),
                CurrentTime = "--:--:--",
                Row1Title = isPedestrian ? "Họ và tên:" : string.Empty,
                Row1Value = "---",
                Row2Title = isPedestrian ? "CCCD / Mã:" : "Biển số nhận diện:",
                Row2Value = "---",
                IsPlateMismatch = false,
                SubTitle1 = isPedestrian ? "Đối tượng:" : "Tài xế / Chủ xe:",
                SubVal1 = "---",
                SubTitle2 = "Mã thẻ:",
                CardCodeText = "---",
                FeeHighlight = null,
                TimeIn = "---",
                TimeOut = "---",
                Img1 = null,
                Img2 = null,
                ImgTitle1 = isPedestrian ? "Ảnh đăng ký (Avatar)" : "Ảnh biển số",
                ImgTitle2 = isPedestrian ? "Ảnh nhận diện (FaceID)" : "Ảnh toàn cảnh"
            };

            PushLaneCardPayload(slotIndex, idlePayload);
        }

        private void PushLaneCardPayload(int slotIndex, LaneCardPayload payload)
        {
            if (slotIndex < 0 || slotIndex >= 4) return;

            _lastSlotPayloads[slotIndex] = payload;
            string json = JsonSerializer.Serialize(payload, _webViewJsonOptions);
            _pendingSlotJsonPayloads[slotIndex] = json;

            if (_slotWebViewReady[slotIndex])
            {
                FlushPendingSlotPayload(slotIndex, GetLaneInfoWebView(slotIndex));
            }
        }

        internal void UpdateLaneSlotUI(int slotIndex, LaneRuntimeContext context, ProcessResult result)
        {
            if (slotIndex >= 0 && slotIndex < 4)
            {
                _lastSlotResults[slotIndex] = result;
            }

            bool isEntry = context.Lane.Direction == LaneDirection.In;
            bool isSuccess = result.Status == ProcessStatus.Success;
            bool canPrintReceipt = !isEntry && isSuccess && result.FeeAmount > 0m && result.ParkingSession != null;

            DateTime displayTime = isEntry
                ? (result.ParkingSession?.InTime is DateTime inTime && inTime != default
                    ? HPParking.Core.Helpers.VietnamTime.ToVietnamTime(inTime)
                    : DateTime.Now)
                : (result.ParkingSession?.OutTime is DateTime outTime && outTime != default
                    ? HPParking.Core.Helpers.VietnamTime.ToVietnamTime(outTime)
                    : DateTime.Now);
            string timeStr = displayTime.ToString("HH:mm:ss dd/MM/yyyy");

            string inTimeVal = isEntry ? timeStr : "---";
            string outTimeVal = isEntry ? "---" : timeStr;
            (string cardCodeText, string? feeHighlight) = FormatCardDisplayParts(result);

            LaneCardPayload payload;

            if (context.Lane.TargetType == LaneTargetType.Pedestrian)
            {
                // Cập nhật giao diện Người đi bộ
                string name = result.Client?.Name ?? string.Empty;
                string code = result.Client?.Code
                    ?? (!string.IsNullOrEmpty(result.Client?.CardCode) ? result.Client.CardCode : string.Empty);

                string roleVi = result.Client != null ? FormatClientTypeVietnamese(result.Client.Type) : "Vãng lai";

                payload = new LaneCardPayload
                {
                    IsIdle = false,
                    IsEntry = isEntry,
                    IsSuccess = isSuccess,
                    CanPrintReceipt = canPrintReceipt,
                    StatusText = FormatProcessStatusBadgeText(result.Status),
                    TargetType = FormatLaneTargetBadge(context.Lane),
                    CurrentTime = DateTime.Now.ToString("HH:mm:ss"),
                    Row1Title = "Họ và tên:",
                    Row1Value = !string.IsNullOrEmpty(name) ? name : "---",
                    Row2Title = "CCCD / Mã:",
                    Row2Value = !string.IsNullOrEmpty(code) ? code : "---",
                    IsPlateMismatch = false,
                    SubTitle1 = "Đối tượng:",
                    SubVal1 = roleVi,
                    SubTitle2 = "Mã thẻ:",
                    CardCodeText = cardCodeText,
                    FeeHighlight = feeHighlight,
                    HasTariffWarning = result.HasTariffWarning,
                    TimeIn = inTimeVal,
                    TimeOut = outTimeVal,
                    Img1 = result.Client?.Avatar != null ? ImageHelper.AvatarToDataUrl(result.Client.Avatar) : null,
                    Img2 = ImageHelper.BitmapToDataUrl(result.FaceImage),
                    ImgTitle1 = "Ảnh đăng ký (Avatar)",
                    ImgTitle2 = "Ảnh nhận diện (FaceID)"
                };
            }
            else
            {
                // Cập nhật giao diện Xe cơ giới
                string plate = result.LprResult?.Plate ?? string.Empty;
                string driver = result.Client?.Name ?? string.Empty;

                if (result.Card?.TargetType == CardTargetType.Visitor && string.IsNullOrWhiteSpace(driver))
                {
                    driver = "Khách vãng lai";
                }

                // 1. Ô đầu tiên hiện ảnh biển số nhỏ (cropped plate)
                Bitmap? smallPlate = result.PlateImage ?? result.LprResult?.PlateImage;

                // 2. Ô thứ 2 hiện ảnh toàn cảnh (OverviewImage)
                payload = new LaneCardPayload
                {
                    IsIdle = false,
                    IsEntry = isEntry,
                    IsSuccess = isSuccess,
                    CanPrintReceipt = canPrintReceipt,
                    StatusText = FormatProcessStatusBadgeText(result.Status),
                    TargetType = FormatLaneTargetBadge(context.Lane),
                    CurrentTime = DateTime.Now.ToString("HH:mm:ss"),
                    Row1Title = string.Empty,
                    Row1Value = string.Empty,
                    Row2Title = "Biển số nhận diện:",
                    Row2Value = !string.IsNullOrEmpty(plate) ? plate : "---",
                    IsPlateMismatch = result.Status == ProcessStatus.PlateMismatch,
                    SubTitle1 = "Tài xế / Chủ xe:",
                    SubVal1 = !string.IsNullOrEmpty(driver) ? driver : "---",
                    SubTitle2 = "Mã thẻ:",
                    CardCodeText = cardCodeText,
                    FeeHighlight = feeHighlight,
                    HasTariffWarning = result.HasTariffWarning,
                    TimeIn = inTimeVal,
                    TimeOut = outTimeVal,
                    Img1 = ImageHelper.BitmapToDataUrl(smallPlate),
                    Img2 = ImageHelper.BitmapToDataUrl(result.OverviewImage),
                    ImgTitle1 = "Ảnh biển số",
                    ImgTitle2 = "Ảnh toàn cảnh"
                };
            }

            PushLaneCardPayload(slotIndex, payload);

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

                var confirmResult = ParkingTaskDialog.ShowConfirmation(
                    this,
                    heading: "XÁC NHẬN MỞ BARRIER THỦ CÔNG",
                    text: "Mở barrier thủ công cho đối tượng đã qua?",
                    details: "• Chọn Đồng ý nếu đã mở thủ công cho xe qua.\n• Chọn Hủy bỏ để hủy bỏ lượt này.");

                return confirmResult;
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

        private Task<bool> HandleFeeConfirmationAsync(LaneRuntimeContext context, FeeConfirmationRequest req)
        {
            // Phương án 1: Bỏ hoàn toàn xác nhận popup TaskDialog khi thu phí
            return Task.FromResult(true);
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
            else if (validation.Payload != null)
            {
                lbdayExpiryDate.Text = $"BẢN QUYỀN: {validation.Payload.ExpiryDate:dd/MM/yyyy}";
                lbdayExpiryDate.BackColor = Color.DarkSlateGray;
            }
            else
            {
                lbdayExpiryDate.Text = "BẢN QUYỀN: HẾT HẠN";
                lbdayExpiryDate.BackColor = Color.OrangeRed;
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

        internal static int? ResolveHotkeySlot(Keys keyData) => keyData switch
        {
            Keys.F1 => 0,
            Keys.F2 => 1,
            Keys.F3 => 2,
            Keys.F4 => 3,
            _ => null
        };

        internal bool TryOpenBarrierByHotkey(int slotIndex, Func<LaneRuntimeContext, bool>? onBarrierOpenFailed = null)
        {
            if (!tlpLanes.Enabled || _laneContexts == null || slotIndex < 0 || slotIndex >= _laneContexts.Count || slotIndex >= 4)
            {
                return false;
            }

            LaneRuntimeContext context = _laneContexts[slotIndex];
            bool success = context.OpenBarrier();

            if (success)
            {
                return true;
            }

            int relayPort = context.Lane.OutputRelay > 0 ? context.Lane.OutputRelay : context.Lane.InputReader;
            string controllerIp = context.Controller?.Config?.IP ?? "N/A";
            string errorMsg = $"[Hotkey F{slotIndex + 1}] Mở barrier thất bại tại làn '{context.Lane.Name}' (Relay={relayPort}, Controller={controllerIp}).";
            System.Diagnostics.Debug.WriteLine(errorMsg);
            Console.WriteLine(errorMsg);

            var failHandler = onBarrierOpenFailed ?? HandleBarrierOpenFailed;
            return failHandler(context);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            int? slotIndex = ResolveHotkeySlot(keyData);
            if (slotIndex.HasValue)
            {
                TryOpenBarrierByHotkey(slotIndex.Value);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private async void BtnReloadHardware_Click(object? sender, EventArgs e)
        {
            try
            {
                using var waitScope = new WaitCursorScope(this);
                btnReloadHardware.Enabled = false;
                btnReloadHardware.Text = "ĐANG KHỞI ĐỘNG LẠI...";

                // Giải phóng kết nối phần cứng cũ và khởi tạo lại Orchestrator mới
                _deviceOrchestrator.Dispose();
                _deviceOrchestrator = new DeviceOrchestrator();
                _deviceOrchestrator.OnControllerStatusChanged += Controller_OnStatusChanged;

                // Nạp lại cấu hình và khởi tạo lại toàn bộ thiết bị
                bool isLoaded = await LoadHardwareAndLanesAsync();

                // Chỉ hiện "thành công" khi cấu hình hợp lệ
                if (isLoaded)
                    MessageBox.Show("Nạp lại cấu hình và kết nối thiết bị phần cứng thành công!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối lại thiết bị: {ex.Message}", "Lỗi Phần Cứng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnReloadHardware.Enabled = true;
                btnReloadHardware.Text = "KHỞI ĐỘNG LẠI";
            }
        }

        private void ShowConfigurationWarning(string laneTitle, string messageBoxTitle, string messageBoxContent, string? gateInfoText = null)
        {
            if (!string.IsNullOrEmpty(gateInfoText))
            {
                lblGateInfo.Text = gateInfoText;
            }
            lblTitleLane1.Text = laneTitle;

            MessageBox.Show(
                messageBoxContent,
                messageBoxTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Hiển thị cảnh báo khi máy trạm chưa được gán vào Cổng nào trong hệ thống.
        /// </summary>
        private void ShowNoGateConfiguredWarning(string machineCode)
        {
            ShowConfigurationWarning(
                laneTitle: "⚠️ CHƯA CÓ CỔNG",
                messageBoxTitle: "⚠️ Chưa Cấu Hình Cổng",
                messageBoxContent: $"Máy trạm này (MachineCode: {machineCode}) chưa được gán vào Cổng nào.\n\n" +
                    "Vui lòng vào WebAdmin → Cài đặt → Cổng và gán MachineCode cho máy trạm này, " +
                    "sau đó nhấn \"KHỞI ĐỘNG LẠI\".",
                gateInfoText: "⚠️ CHƯA CẤU HÌNH CỔNG — Vui lòng vào WebAdmin để thiết lập");
        }

        /// <summary>
        /// Hiển thị cảnh báo khi Cổng đã tìm thấy nhưng chưa có Làn hoạt động nào.
        /// </summary>
        private void ShowNoLaneConfiguredWarning(string gateName)
        {
            ShowConfigurationWarning(
                laneTitle: "⚠️ CHƯA CÓ LÀN",
                messageBoxTitle: "⚠️ Chưa Cấu Hình Làn",
                messageBoxContent: $"Cổng \"{gateName}\" chưa có Làn hoạt động nào.\n\n" +
                    "Vui lòng vào WebAdmin → Cài đặt → Làn, thêm hoặc kích hoạt Làn cho cổng này, " +
                    "sau đó nhấn \"KHỞI ĐỘNG LẠI\".");
        }

        /// <summary>
        /// Khóa/mở toàn bộ giao diện làn. Khi locked, chỉ btnReloadHardware hoạt động.
        /// Dùng khi chưa có Cổng hoặc Làn hợp lệ để ngăn vận hành nhầm.
        /// </summary>
        internal void SetFormConfigurationLocked(bool locked)
        {
            tlpLanes.Enabled = !locked;
            // btnReloadHardware nằm trong pnlFooter — luôn giữ enabled
            btnReloadHardware.Enabled = true;
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
    /// Dữ liệu JSON truyền từ C# sang thẻ thông tin WebView2 (HTML + Tailwind Offline)
    /// </summary>
    internal sealed class LaneCardPayload
    {
        public bool IsIdle { get; set; }
        public bool IsEntry { get; set; }
        public bool IsSuccess { get; set; }
        public bool CanPrintReceipt { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public string CurrentTime { get; set; } = string.Empty;
        public string Row1Title { get; set; } = string.Empty;
        public string Row1Value { get; set; } = string.Empty;
        public string Row2Title { get; set; } = string.Empty;
        public string Row2Value { get; set; } = string.Empty;
        public bool IsPlateMismatch { get; set; }
        public string SubTitle1 { get; set; } = string.Empty;
        public string SubVal1 { get; set; } = string.Empty;
        public string SubTitle2 { get; set; } = string.Empty;
        public string CardCodeText { get; set; } = string.Empty;
        public string? FeeHighlight { get; set; }
        public bool HasTariffWarning { get; set; }
        public string TimeIn { get; set; } = string.Empty;
        public string TimeOut { get; set; } = string.Empty;
        public string? Img1 { get; set; }
        public string? Img2 { get; set; }
        public string ImgTitle1 { get; set; } = string.Empty;
        public string ImgTitle2 { get; set; } = string.Empty;
    }
}
