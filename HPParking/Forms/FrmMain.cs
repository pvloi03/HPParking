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
using HPParking.Services.HN212;
using HPParking.Services.License;
using HPParking.Services.Parking;
using HPParking.UI;
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
        private readonly LicenseManager _licenseManager;

        private readonly IHn212Client _hn212Client;
        private readonly IRepository<Client> _clientRepository;
        private FrmRegisterClient? _activeFrmRegisterClient;
        private readonly DeviceOrchestrator _deviceOrchestrator = new();
        private readonly IParkingWorkflowService _workflowService;
        private readonly IServerHealthService _serverHealthService;

        private List<Lane> _lanes = [];
        private List<LaneRuntimeContext> _laneContexts = [];
        private List<Device> _devices = [];
        private Gate? _currentGate;
        private Timer? _clockTimer;
        private ServerHealthReport? _lastServerHealthReport;
        private CancellationTokenSource? _healthCheckCts;

        private readonly IRepository<Device> _deviceRepository;
        private readonly IRepository<Vehicle> _vehicleRepository;

        public FrmMain(
            IRepository<Lane> laneRepository,
            IRepository<Company> companyRepository,
            IRepository<Gate> gateRepository,
            IRepository<Client> clientRepository,
            IRepository<Device> deviceRepository,
            LicenseManager licenseManager,
            IParkingWorkflowService workflowService,
            IHn212Client hn212Client,
            IServerHealthService serverHealthService,
            IRepository<Vehicle> vehicleRepository)
        {
            InitializeComponent();

            // Đăng ký nhận phím tắt phím F1
            KeyPreview = true;
            KeyDown += FrmMain_KeyDown;

            _laneRepository = laneRepository;
            _companyRepository = companyRepository;
            _gateRepository = gateRepository;
            _clientRepository = clientRepository;
            _deviceRepository = deviceRepository;
            _licenseManager = licenseManager;
            _workflowService = workflowService;
            _hn212Client = hn212Client;
            _serverHealthService = serverHealthService;
            _vehicleRepository = vehicleRepository;

            // Đăng ký nhận sự kiện thẻ CCCD HN212
            _hn212Client.CardStatusChanged += OnCardStatusChanged;
            _hn212Client.CardScanned += OnCardScanned;
            _deviceOrchestrator.OnControllerStatusChanged += Controller_OnStatusChanged;
        }

        private void FrmMain_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                e.Handled = true;
                using FrmLogin loginForm = new(_currentGate?.Id, _currentGate?.Code);
                loginForm.ShowDialog(this);
            }
        }

        private async void FrmMain_Load(object sender, EventArgs e)
        {
            try
            {
                using var waitScope = new WaitCursorScope(this);

                // Chạy kết nối ngầm tới HN212Reader khi mở App
                await _hn212Client.StartAsync();

                // 3. KIỂM TRA LICENSE BẢN QUYỀN 
                lbdayExpiryDate.Cursor = Cursors.Hand;
                lbdayExpiryDate.DoubleClick -= LbdayExpiryDate_DoubleClick;
                lbdayExpiryDate.DoubleClick += LbdayExpiryDate_DoubleClick;

                // Cấu hình nhãn trạng thái máy chủ & khởi động giám sát định kỳ ngầm
                lbServer.Cursor = Cursors.Hand;
                lbServer.Click -= lbServer_Click;
                lbServer.Click += lbServer_Click;
                StartServerHealthMonitoring();

                bool licenseOk = await CheckAndEnforceLicenseAsync();
                if (!licenseOk)
                {
                    return;
                }

                _clockTimer = new Timer { Interval = 1000 };
                _clockTimer.Tick += (s, args) =>
                {
                    lbRealTime.Text = $"HÔM NAY: {DateTime.Now:HH:mm:ss dd/MM/yyyy}";
                };
                _clockTimer.Start();

                // 4. Nhận diện Cổng phụ trách theo MachineCode & Lấy dữ liệu 4 Làn thuộc Cổng đó
                string currentMachineCode = HardwareFingerprint.GetMachineCode();
                _currentGate = await _gateRepository.FindOneAsync(x => x.MachineCode == currentMachineCode && x.IsActive && !x.IsDeleted);

                if (_currentGate != null && !string.IsNullOrWhiteSpace(_currentGate.Id))
                {
                    var gateLanes = await _laneRepository.FindAsync(x => x.GateId == _currentGate.Id && x.IsActive);
                    _lanes = gateLanes?.ToList() ?? [];
                    _laneContexts = _lanes.Select(l => new LaneRuntimeContext(l)).ToList();
                    Text = $"HPPARKING - {(_currentGate.Name ?? "CỔNG KIỂM SOÁT").ToUpperInvariant()} (MÃ: {_currentGate.Code})";
                }
                else
                {
                    // Máy trạm chưa được cấu hình gán vào Cổng: Không nạp làn, giữ UI trống và thông báo cho kỹ thuật viên
                    _lanes = [];
                    _laneContexts = [];
                    Text = "HPPARKING - CHƯA LIÊN KẾT CỔNG KIỂM SOÁT";
                    MessageBox.Show(
                        this,
                        $"Máy trạm này chưa được cấu hình liên kết với Cổng kiểm soát nào trong hệ thống!",
                        "Cảnh Báo Chưa Liên Kết Cổng",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                BindLaneUI();

                // 5. Khởi tạo Thiết bị & LiveView trên PictureBox qua Explicit Semantic Mapping
                _devices = (await _deviceRepository.GetAllAsync())?.ToList() ?? [];
                await _deviceOrchestrator.InitializeDevicesAsync(_laneContexts, _devices, previewHandleResolver: ResolvePreviewHandles);

                // 6. Lắng nghe tín hiệu quẹt thẻ Realtime
                _deviceOrchestrator.OnCardSwiped += OnCardSwiped;
                _deviceOrchestrator.StartRealtimeLoop();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi Khởi Động", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnCardStatusChanged(string status, string message)
        {
            if (IsDisposed) return;
            if (status == "Present" || status == "Reading")
            {
                BeginInvoke(new Action(EnsureRegisterClientFormOpen));
            }
        }

        private void OnCardScanned(CardDataDto card)
        {
            if (IsDisposed) return;
            BeginInvoke(new Action(EnsureRegisterClientFormOpen));
        }

        private void EnsureRegisterClientFormOpen()
        {
            if (_activeFrmRegisterClient == null || _activeFrmRegisterClient.IsDisposed)
            {
                _activeFrmRegisterClient = new FrmRegisterClient(_hn212Client, _clientRepository, _laneRepository, _deviceRepository, _vehicleRepository);
                _activeFrmRegisterClient.Show(this);
            }
            else
            {
                if (!_activeFrmRegisterClient.Visible)
                {
                    _activeFrmRegisterClient.Show(this);
                }
                _activeFrmRegisterClient.BringToFront();
            }
        }

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
                lbStatusCtrl.Text = $"BỘ ĐIỀU KHIỂN: ONLINE";
                lbStatusCtrl.BackColor = Color.SeaGreen;
            }
            else
            {
                lbStatusCtrl.Text = $"BỘ ĐIỀU KHIỂN: OFFLINE";
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
                    lbServer.BackColor = Color.Gray;
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
                MessageBox.Show(
                    this,
                    "Đang tiến hành kiểm tra kết nối máy chủ lần đầu...",
                    "Tình Trạng Máy Chủ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var icon = _lastServerHealthReport.OverallStatus switch
            {
                ServerHealthStatus.Online => MessageBoxIcon.Information,
                ServerHealthStatus.Warning => MessageBoxIcon.Warning,
                _ => MessageBoxIcon.Error
            };

            MessageBox.Show(
                this,
                _lastServerHealthReport.GetDetailedSummary(),
                "Chi Tiết Tình Trạng Máy Chủ & Lưu Trữ",
                MessageBoxButtons.OK,
                icon);
        }

        private void OnCardSwiped(RealtimeLog data)
        {
            if (_laneContexts == null) return;

            LaneRuntimeContext? context = _laneContexts.FirstOrDefault(x =>
                x.Lane.InputReader == data.DoorId &&
                (x.Controller?.Config?.IP == data.ControllerIp ||
                 _devices.Any(d => d.Id == x.Lane.ControllerDeviceId && d.IpAddress == data.ControllerIp)));

            if (context == null) return;

            string pathImage = StorageConfigHelper.GetPathImage();

            BeginInvoke(new Action(async () =>
            {
                ProcessResult result = (context.Lane.Direction == LaneDirection.In)
                    ? await _workflowService.ProcessEntryAsync(context, data, pathImage, HandleBarrierOpenFailed, HandleManualPlateInputAsync)
                    : await _workflowService.ProcessExitAsync(context, data, pathImage, HandleBarrierOpenFailed, HandleManualPlateInputAsync);

                if (result.Status == ProcessStatus.Success)
                {
                    UpdateUI(context, result);
                }
                else if (result.Status == ProcessStatus.PlateMismatch)
                {
                    UpdateUI(context, result);
                    MessageBox.Show(
                        this, result.Message,
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                else if (!string.IsNullOrEmpty(result.Message))
                {
                    MessageBox.Show(this, result.Message, "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }));
        }

        private static void UpdateUI(LaneRuntimeContext context, ProcessResult result)
        {
            ToolTip tooltip = new();
            var ui = context.UI;
            if (ui == null) return;

            if (result.Client != null)
            {
                ui.LblFullName.Text = $"Họ và tên: {result.Client.Name}";
                string departmentText = !string.IsNullOrWhiteSpace(result.DepartmentName)
                    ? result.DepartmentName
                    : "";
                string labelPrefix = result.Client.Type switch
                {
                    ClientType.Contractor => "Nhà thầu/Đối tác",
                    ClientType.Visitor => "Đối tượng",
                    ClientType.VIP => "Chức vụ/Phòng ban",
                    _ => "Phòng ban"
                };

                string departmentFullText = string.IsNullOrWhiteSpace(departmentText)
                    ? $"{labelPrefix}: "
                    : $"{labelPrefix}: {departmentText}";
                tooltip.SetToolTip(ui.LblDepartment, departmentFullText);
                ui.LblDepartment.Text = departmentFullText;
                ui.LblPlateRegistered.Text = $"Biển số đăng ký: {result.RegisteredPlate}";
                ui.LblIdentityCard.Text = $"Số CCCD: {result.Client.Code}";
            }

            if (result.LprResult != null)
            {
                ui.LblPlateDetected.Text = $"Biển số phát hiện: {result.LprResult.Plate}";
                ui.LblPlateDetected.ForeColor = (result.Status == ProcessStatus.PlateMismatch)
                    ? Color.Crimson
                    : Color.Black;
            }
            else
            {
                ui.LblPlateDetected.Text = "Biển số phát hiện:";
                ui.LblPlateDetected.ForeColor = Color.Black;
            }

            if (result.ParkingSession != null)
            {
                ui.LblTimeIn.Text = $"Ngày vào: {result.ParkingSession.InTime:HH:mm:ss dd/MM/yyyy}";
                if (context.Direction == LaneDirection.Out)
                {
                    DateTime outTime = result.ParkingSession.OutTime ?? DateTime.Now;
                    ui.LblTimeOut.Text = $"Ngày ra: {outTime:HH:mm:ss dd/MM/yyyy}";
                }
                else
                {
                    ui.LblTimeOut.Text = "Ngày ra:";
                }
            }
            else if (context.Direction == LaneDirection.In)
            {
                ui.LblTimeIn.Text = $"Ngày vào: {DateTime.Now:HH:mm:ss dd/MM/yyyy}";
                ui.LblTimeOut.Text = "Ngày ra:";
            }

            try
            {
                // Cập nhật ảnh biển số: Vào hay Ra đều cập nhật ảnh biển số mới nhất vào ô PicPlate
                var oldPlate = ui.PicPlate.Image;
                if (result.LprResult?.PlateImage != null)
                {
                    ui.PicPlate.Image = (Bitmap)result.LprResult.PlateImage.Clone();
                }
                else
                {
                    ui.PicPlate.Image = null;
                }
                oldPlate?.Dispose();

                // Cập nhật ảnh Avatar: hiển thị ảnh avatar của Client nếu có, ngược lại xóa trắng để tránh dính ảnh lượt trước
                var oldAvatar = ui.PicAvatar.Image;
                if (result.Client != null && !string.IsNullOrWhiteSpace(result.Client.Avatar) && System.IO.File.Exists(result.Client.Avatar))
                {
                    ui.PicAvatar.Image = LoadBitmapWithoutLock(result.Client.Avatar);
                }
                else
                {
                    ui.PicAvatar.Image = null;
                }
                oldAvatar?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FrmMain UpdateUI] Lỗi hiển thị ảnh biển số/avatar: {ex.Message}");
            }

            // Giải phóng các Bitmap tạm thời trong LprResult và ProcessResult sau khi UI đã copy/hiển thị
            result.LprResult?.Dispose();
            result.OverviewImage?.Dispose();
        }

        private static Bitmap? LoadBitmapWithoutLock(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath)) return null;
            try
            {
                using var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
                using var original = Image.FromStream(stream);
                return new Bitmap(original);
            }
            catch
            {
                return null;
            }
        }

        private void BindLaneUI()
        {
            InfoUI laneUI = new()
            {
                Car = new VehicleUI
                {
                    LblFullName = lblCarFullName,
                    LblIdentityCard = lblCarIdentityCard,
                    LblTimeIn = lblCarTimeIn,
                    LblTimeOut = lblCarTimeOut,
                    LblPlateRegistered = lblCarPlateRegistered,
                    LblPlateDetected = lblCarPlateDetected,
                    LblDepartment = lblCarDepartment,
                    PicPlate = pbCarPlateImg,
                    PicAvatar = pbCarAvatarImg,
                },
                Moto = new VehicleUI
                {
                    LblFullName = lblMotoFullName,
                    LblIdentityCard = lblMotoIdentityCard,
                    LblTimeIn = lblMotoTimeIn,
                    LblTimeOut = lblMotoTimeOut,
                    LblPlateRegistered = lblMotoPlateRegistered,
                    LblPlateDetected = lblMotoPlateDetected,
                    LblDepartment = lblMotoDepartment,
                    PicPlate = pbMotoPlateImg,
                    PicAvatar = pbMotoAvatarImg
                }
            };

            foreach (LaneRuntimeContext context in _laneContexts)
            {
                context.UI = context.Lane.InputReader % 2 != 0 ? laneUI.Moto : laneUI.Car;
            }
        }

        private LanePreviewHandles? ResolvePreviewHandles(Lane lane)
        {
            bool isMoto = lane.InputReader % 2 != 0;
            bool isEntry = lane.Direction == LaneDirection.In;

            if (isMoto)
            {
                return isEntry
                    ? new LanePreviewHandles { PlateHandle = pbMotoEntryPlate.Handle, OverviewHandle = pbMotoEntryOverview.Handle }
                    : new LanePreviewHandles { PlateHandle = pbMotoExitPlate.Handle, OverviewHandle = pbMotoExitOverview.Handle };
            }
            else
            {
                return isEntry
                    ? new LanePreviewHandles { PlateHandle = pbCarEntryPlate.Handle, OverviewHandle = pbCarEntryOverview.Handle }
                    : new LanePreviewHandles { PlateHandle = pbCarExitPlate.Handle, OverviewHandle = pbCarExitOverview.Handle };
            }
        }

        private bool HandleBarrierOpenFailed(LaneRuntimeContext context)
        {
            if (InvokeRequired)
            {
                return (bool)Invoke(new Func<bool>(() => HandleBarrierOpenFailed(context)));
            }

            Lane lane = context.Lane;
            string laneDesc = $"{(lane.InputReader % 2 != 0 ? "Xe máy" : "Ô tô")} (Cổng {(lane.Direction == LaneDirection.In ? "VÀO" : "RA")} - Đầu đọc {lane.InputReader})";

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
                    if (context.OpenBarrier())
                    {
                        return true;
                    }
                    continue;
                }

                var confirmResult = MessageBox.Show(
                    this,
                    "Mở barrier thủ công cho xe đã qua?\n\n- Chọn YES nếu đã mở thủ công.\n- Chọn NO để hủy bỏ lượt xe này.",
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

                // Tự động mở form kích hoạt / hết hạn
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
                            MessageBox.Show(
                                $"Không thể lưu KEY bản quyền: {ex.Message}\n\nVui lòng kiểm tra lại kết nối mạng hoặc máy chủ cơ sở dữ liệu.",
                                "Lỗi Lưu KEY Bản Quyền",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                            );
                            Application.Exit();
                            return false;
                        }
                    }
                }
                else
                {
                    // Người dùng đóng form hoặc bấm Thoát ➔ Đóng ứng dụng ngay lập tức
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
            }
            else
            {
                lbdayExpiryDate.Text = "BẢN QUYỀN: HẾT HẠN";
                lbdayExpiryDate.BackColor = Color.OrangeRed;
            }
        }

        private async void LbdayExpiryDate_DoubleClick(object? sender, EventArgs e)
        {
            // Cho phép người dùng click đúp vào footer để nạp key gia hạn sớm
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

        private async void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _healthCheckCts?.Cancel();
            _healthCheckCts?.Dispose();
            lbServer.Click -= lbServer_Click;
            KeyDown -= FrmMain_KeyDown;
            _hn212Client.CardStatusChanged -= OnCardStatusChanged;
            _hn212Client.CardScanned -= OnCardScanned;
            _deviceOrchestrator.OnControllerStatusChanged -= Controller_OnStatusChanged;
            _deviceOrchestrator.OnCardSwiped -= OnCardSwiped;
            _clockTimer?.Stop();
            _clockTimer?.Dispose();
            await _hn212Client.StopAsync();
            _deviceOrchestrator.Dispose();
        }
    }
}
