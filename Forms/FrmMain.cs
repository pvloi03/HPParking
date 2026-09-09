using HPParking.Interfaces;
using HPParking.LicenseKey;
using HPParking.Models.Entities;
using HPParking.Services.CCCDReader;
using HPParking.Services.Controller;
using HPParking.Services.Devices;
using HPParking.Services.Parking;
using HPParking.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HPParking.Forms
{
    public partial class FrmMain : Form
    {
        private readonly ILaneRepository _laneRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IClientRepository _clientRepository;

        private readonly CccdReaderManager _readerManager;
        private FrmRegisterClient? _activeFrmRegisterClient;
        private readonly DeviceOrchestrator _deviceOrchestrator = new();
        private readonly IParkingWorkflowService _workflowService;

        private List<Lane> _lanes = [];
        private Company? _company;
        private Timer? _clockTimer;

        public FrmMain(
            ILaneRepository laneRepository,
            ICompanyRepository companyRepository,
            IClientRepository clientRepository,
            IParkingWorkflowService workflowService)
        {
            InitializeComponent();

            // Đăng ký nhận phím tắt phím F1
            KeyPreview = true;
            KeyDown += FrmMain_KeyDown;

            _laneRepository = laneRepository;
            _companyRepository = companyRepository;
            _clientRepository = clientRepository;
            _workflowService = workflowService;

            // Khởi tạo Manager kết nối Service piper
            _readerManager = new CccdReaderManager("http://localhost:5000/cardhub");

            // Đăng ký nhận sự kiện
            _readerManager.StatusUpdated += OnStatusUpdated;
            _deviceOrchestrator.OnControllerStatusChanged += Controller_OnStatusChanged;
        }

        private void FrmMain_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                e.Handled = true;
                using FrmLogin loginForm = new();
                loginForm.ShowDialog(this);
            }
        }

        private async void FrmMain_Load(object sender, EventArgs e)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;

                // Chạy kết nối ngầm khi mở App
                await _readerManager.StartAsync();

                // 2. Lấy dữ liệu Công ty (Company)
                _company = await _companyRepository.GetFirstCompanyAsync();
                if (_company == null || string.IsNullOrEmpty(_company.Lisen))
                {
                    Hide();
                    using (FrmLogin login = new())
                    {
                        login.ShowDialog();
                    }
                    _company = await _companyRepository.GetFirstCompanyAsync();
                    if (_company == null || string.IsNullOrEmpty(_company.Lisen))
                    {
                        Application.Exit();
                        return;
                    }
                    Show();
                }

                // 3. KHÔI PHỤC: KIỂM TRA LICENSE KEY & BẬT ĐỒNG HỒ REALTIME
                string licenseKey = _company.Lisen;
                if (!LicenseValidator.ValidateLicense(licenseKey, out string error, out int dayExpiryDate))
                {
                    MessageBox.Show(string.IsNullOrEmpty(error) ? "Hết hạn sử dụng phần mềm !" : error, "Thông báo License", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Hide();
                    using (FrmLogin login = new())
                    {
                        login.ShowDialog();
                    }
                    _company = await _companyRepository.GetFirstCompanyAsync();
                    if (_company == null || string.IsNullOrEmpty(_company.Lisen) || !LicenseValidator.ValidateLicense(_company.Lisen, out _, out dayExpiryDate))
                    {
                        Application.Exit();
                        return;
                    }
                    Show();
                }

                lbdayExpiryDate.Text = $"THỜI HẠN: {dayExpiryDate + 1} ngày";

                int tickCount = 0;
                _clockTimer = new Timer { Interval = 1000 };
                _clockTimer.Tick += (s, args) =>
                {
                    lbRealTime.Text = $"HÔM NAY: {DateTime.Now:HH:mm:ss dd/MM/yyyy}";
                    tickCount++;
                    // Mỗi 60 giây tự động cập nhật mốc thời gian chạy mới nhất
                    if (tickCount % 60 == 0)
                    {
                        LicenseValidator.UpdateLastRunTime();
                    }
                };
                _clockTimer.Start();

                // 4. Lấy dữ liệu Làn & Cấu hình UI
                _lanes = await _laneRepository.GetAllAsync() ?? [];
                BindLaneUI();

                // 5. Khởi tạo Thiết bị & LiveView trên PictureBox qua Explicit Semantic Mapping
                await _deviceOrchestrator.InitializeDevicesAsync(_lanes, previewHandleResolver: ResolvePreviewHandles);

                // 6. Lắng nghe tín hiệu quẹt thẻ Realtime
                _deviceOrchestrator.OnCardSwiped += OnCardSwiped;
                _deviceOrchestrator.StartRealtimeLoop();

                Cursor.Current = Cursors.Default;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi Khởi Động", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnStatusUpdated(DeviceStatusDto status)
        {
            if (status == null || IsDisposed) return;
            BeginInvoke(new Action(async () =>
            {
                string readerStatus = status.IsReaderConnected
                    ? $"DẦU ĐỌC CCCD: ĐÃ CẮM ({status.ReaderSerialNumber})"
                    : "DẦU ĐỌC CCCD: CHƯA CẮM";

                lblServerStatus.Text = status.IsServerReady ? $"SERVER ĐẦU ĐỌC CCCD: ONLINE - {readerStatus}" : $"SERVER ĐẦU ĐỌC CCCD: OFFLINE{readerStatus}";
                lblServerStatus.ForeColor = status.IsServerReady && status.IsReaderConnected ? Color.Green : Color.Red;


                if (status.IsCardPresent && (_activeFrmRegisterClient == null || _activeFrmRegisterClient.IsDisposed))
                {
                    _activeFrmRegisterClient = new FrmRegisterClient(_readerManager, _clientRepository, _companyRepository, _laneRepository);

                    _activeFrmRegisterClient.UpdateStatus("Đọc thẻ CCCD thất bại, vui lòng thử lại!", Color.Red);
                    _activeFrmRegisterClient.Show(this);
                }
            }));
        }

        private void Controller_OnStatusChanged(string controllerIp, bool isConnected, string message)
        {
            Debug.WriteLine($"{controllerIp}: {isConnected}");
        }

        private void OnCardSwiped(RealtimeLog data)
        {
            if (_lanes == null || _company == null) return;

            Lane? lane = _lanes.FirstOrDefault(x =>
                x.InputReader == data.DoorId &&
                x.ControllerConfig?.IP == data.ControllerIp);

            if (lane == null) return;

            string pathImage = _company.PathImage ?? string.Empty;

            BeginInvoke(new Action(async () =>
            {
                ProcessResult result = (lane.Type % 2 != 0)
                    ? await _workflowService.ProcessEntryAsync(lane, data, pathImage, HandleBarrierOpenFailed)
                    : await _workflowService.ProcessExitAsync(lane, data, pathImage, HandleBarrierOpenFailed);

                if (result.Status == ProcessStatus.Success)
                {
                    UpdateUI(lane, result);
                }
                else if (!string.IsNullOrEmpty(result.Message))
                {
                    MessageBox.Show(result.Message, "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }));
        }

        private static void UpdateUI(Lane lane, ProcessResult result)
        {
            if (lane.UI == null) return;

            if (result.Client != null)
            {
                lane.UI.LblFullName.Text = $"Họ và tên: {result.Client.Name}";
                lane.UI.LblDepartment.Text = $"Phòng ban: {result.Client.Department_Code}";
                lane.UI.LblPlateRegistered.Text = $"Biển số đăng ký: {result.Client.LicensePlate}";
                lane.UI.LblCardId.Text = $"Số CCCD: {result.Client.ID_Code}";
            }

            if (result.LprResult != null)
            {
                lane.UI.LblPlateDetected.Text = $"Biển số phát hiện: {result.LprResult.Plate}";
            }

            if (result.EventParking != null)
            {
                lane.UI.LblTimeIn.Text = $"Thời gian vào: {result.EventParking.TimeIn:HH:mm:ss dd/MM/yyyy}";
                if (lane.Type % 2 == 0)
                {
                    lane.UI.LblTimeOut.Text = $"Thời gian ra: {result.EventParking.TimeOut:HH:mm:ss dd/MM/yyyy}";
                }
            }

            if (result.LprResult?.PlateImage != null)
            {
                lane.UI.PicPlateIn.Image?.Dispose();
                lane.UI.PicPlateIn.Image = new Bitmap(result.LprResult.PlateImage);

                if (lane.Type % 2 == 0)
                {
                    lane.UI.PicPlateOut.Image?.Dispose();
                    lane.UI.PicPlateOut.Image = new Bitmap(result.LprResult.PlateImage);
                }
            }

            // Giải phóng các Bitmap tạm thời trong LprResult và ProcessResult sau khi UI đã copy/hiển thị
            result.LprResult?.Dispose();
            result.OverviewImage?.Dispose();
        }

        private void BindLaneUI()
        {
            InfoUI laneUI = new()
            {
                Car = new VehicleUI
                {
                    LblFullName = lblCarFullName,
                    LblCardId = lblCarCardId,
                    LblTimeIn = lblCarTimeIn,
                    LblTimeOut = lblCarTimeOut,
                    LblPlateRegistered = lblCarPlateRegistered,
                    LblPlateDetected = lblCarPlateDetected,
                    LblDepartment = lblCarDepartment,
                    PicPlateIn = pbCarPlateInImg,
                    PicPlateOut = pbCarPlateOutImg,
                },
                Moto = new VehicleUI
                {
                    LblFullName = lblMotoFullName,
                    LblCardId = lblMotoCardId,
                    LblTimeIn = lblMotoTimeIn,
                    LblTimeOut = lblMotoTimeOut,
                    LblPlateRegistered = lblMotoPlateRegistered,
                    LblPlateDetected = lblMotoPlateDetected,
                    LblDepartment = lblMotoDepartment,
                    PicPlateIn = pbMotoPlateInImg,
                    PicPlateOut = pbMotoPlateOutImg
                }
            };

            foreach (Lane lane in _lanes)
            {
                lane.UI = lane.InputReader % 2 != 0 ? laneUI.Moto : laneUI.Car;
            }
        }

        private LanePreviewHandles? ResolvePreviewHandles(Lane lane)
        {
            bool isMoto = (lane.InputReader % 2 != 0);
            bool isEntry = (lane.Type % 2 != 0);

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

        private bool HandleBarrierOpenFailed(Lane lane)
        {
            if (InvokeRequired)
            {
                return (bool)Invoke(new Func<bool>(() => HandleBarrierOpenFailed(lane)));
            }

            string laneDesc = $"{(lane.InputReader % 2 != 0 ? "Xe máy" : "Ô tô")} (Cổng {(lane.Type % 2 != 0 ? "VÀO" : "RA")} - Đầu đọc {lane.InputReader})";

            while (true)
            {
                var dialog = new TaskDialogPage
                {
                    Caption = "Lỗi Thiết Bị Barrier",
                    Heading = $"Không thể kích hoạt mở Barrier làn {laneDesc}!",
                    Text = "Tín hiệu kích xung tới rơle điều khiển barrier thất bại hoặc mất kết nối thiết bị controller.",
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
                    int relayPort = lane.OutputRelay > 0 ? lane.OutputRelay : lane.InputReader;
                    if (lane.Ctrl != null && lane.Ctrl.OpenBarrier(relayPort, 1))
                    {
                        return true;
                    }
                    continue;
                }

                var confirmResult = MessageBox.Show(
                    this,
                    "Bảo vệ đã mở barrier thủ công và xe đã qua chưa?\n\n- Chọn YES nếu đã mở thủ công để hệ thống ghi nhận lượt xe.\n- Chọn NO để hủy bỏ lượt xe này.",
                    "Xác nhận mở barrier thủ công",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                return confirmResult == DialogResult.Yes;
            }
        }

        private async void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            KeyDown -= FrmMain_KeyDown;
            _readerManager.StatusUpdated -= OnStatusUpdated;
            _deviceOrchestrator.OnControllerStatusChanged -= Controller_OnStatusChanged;
            _deviceOrchestrator.OnCardSwiped -= OnCardSwiped;
            _clockTimer?.Stop();
            _clockTimer?.Dispose();
            await _readerManager.StopAsync();
            _deviceOrchestrator.Dispose();
        }
    }
}