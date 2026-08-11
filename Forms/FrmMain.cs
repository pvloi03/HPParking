using HPParking.Interfaces;
using HPParking.LicenseKey;
using HPParking.Models.Entities;
using HPParking.Services.Controller;
using HPParking.Services.Devices;
using HPParking.Services.LPR;
using HPParking.Services.Parking;
using HPParking.Services.Storage;
using HPParking.Services.Worker;
using HPParking.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HPParking.Forms
{
    public partial class FrmMain : Form
    {
        private readonly ILaneRepository _laneRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IEventParkingRepository _eventRepository;
        private readonly IClientRepository _clientRepository;

        private readonly CccdWorkerManager _cccdWorkerManager = new();
        private readonly DeviceOrchestrator _deviceOrchestrator = new();
        private IParkingWorkflowService _workflowService;

        private List<Lane> _lanes;
        private Company _company;
        private Timer _clockTimer;

        public FrmMain(
            ILaneRepository laneRepository,
            ICompanyRepository companyRepository,
            IEventParkingRepository eventRepository,
            IClientRepository clientRepository)
        {
            InitializeComponent();

            // Đăng ký nhận phím tắt phím F1
            this.KeyPreview = true;
            this.KeyDown += FrmMain_KeyDown;

            _laneRepository = laneRepository;
            _companyRepository = companyRepository;
            _eventRepository = eventRepository;
            _clientRepository = clientRepository;
        }

        private void FrmMain_KeyDown(object sender, KeyEventArgs e)
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

                // 1. Khởi tạo Workflow Service & CCCD Worker
                _workflowService = new ParkingWorkflowService(
                    _clientRepository,
                    _eventRepository,
                    new LprService(),
                    new ImageStorageService());

                _cccdWorkerManager.OnCccdDataReceived += OnCccdReceived;
                _cccdWorkerManager.OnError += msg => MessageBox.Show(msg, "Lỗi Worker");
                _cccdWorkerManager.Start();

                // 2. Lấy dữ liệu Công ty (Company)
                _company = await _companyRepository.GetFirstCompanyAsync();
                if (_company == null || string.IsNullOrEmpty(_company.Lisen))
                {
                    using FrmLogin login = new();
                    login.Show();
                    this.Hide();
                    return;
                }

                // 3. KHÔI PHỤC: KIỂM TRA LICENSE KEY & BẬT ĐỒNG HỒ REALTIME
                string licenseKey = _company.Lisen;
                if (LicenseValidator.ValidateLicense(licenseKey, out string error, out int dayExpiryDate))
                {
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
                }
                else
                {
                    MessageBox.Show(string.IsNullOrEmpty(error) ? "Hết hạn sử dụng phần mềm !" : error, "Thông báo License", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    using FrmLogin login = new();
                    login.Show();
                    this.Hide();
                    return;
                }

                // 4. Lấy dữ liệu Làn & Cấu hình UI
                _lanes = await _laneRepository.GetAllAsync();
                BindLaneUI();

                // 5. Khởi tạo Thiết bị & LiveView trên PictureBox
                List<PictureBox> previews = GetControls<PictureBox>([tlpPreviewMoto, tlpPreviewCar], "pbPreview");
                await _deviceOrchestrator.InitializeDevicesAsync(_lanes, previews);

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

        private void OnCardSwiped(RealtimeLog data)
        {
            Lane lane = _lanes.FirstOrDefault(x =>
            x.InputReader == data.DoorId &&
            x.ControllerConfig?.IP == data.ControllerIp);

            if (lane == null) return;

            BeginInvoke(new Action(async () =>
            {
                ProcessResult result = (lane.Type % 2 != 0)
                    ? await _workflowService.ProcessEntryAsync(lane, data, _company.PathImage)
                    : await _workflowService.ProcessExitAsync(lane, data, _company.PathImage);

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

        private void UpdateUI(Lane lane, ProcessResult result)
        {
            lane.UI.LblFullName.Text = $"Họ và tên: {result.Client.Name}";
            lane.UI.LblDepartment.Text = $"Phòng ban: {result.Client.Department_Code}";
            lane.UI.LblPlateRegistered.Text = $"Biển số đăng ký: {result.Client.LicensePlate}";
            lane.UI.LblPlateDetected.Text = $"Biển số phát hiện: {result.LprResult.Plate}";
            lane.UI.LblCardId.Text = $"Số CCCD: {result.Client.ID_Code}";
            lane.UI.LblTimeIn.Text = $"Thời gian vào: {result.EventParking.TimeIn:HH:mm:ss dd/MM/yyyy}";

            lane.UI.PicPlateIn.Image?.Dispose();
            lane.UI.PicPlateIn.Image = new Bitmap(result.LprResult.PlateImage);
            if (lane.Type % 2 == 0)
            {
                lane.UI.PicPlateOut.Image?.Dispose();
                lane.UI.PicPlateOut.Image = new Bitmap(result.LprResult.PlateImage);
                lane.UI.LblTimeOut.Text = $"Thời gian ra: {result.EventParking.TimeOut:HH:mm:ss dd/MM/yyyy}";
            }
        }

        private void OnCccdReceived(CccdModel cccdData)
        {
            BeginInvoke(new Action(() =>
            {
                using FrmRegisterClient registerClientForm = new(cccdData, _clientRepository, _company);
                registerClientForm.ShowDialog(this);
            }));
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
                    PicPlateOut = pbCarPlateOutImg
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

        private List<T> GetControls<T>(List<TableLayoutPanel> tlpPreview, string tag) where T : Control
        {
            return [.. tlpPreview.SelectMany(tlp => tlp.Controls.OfType<T>()).Where(c => c.Tag?.ToString() == tag)];
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _clockTimer?.Stop();
            _cccdWorkerManager.Stop();
            _deviceOrchestrator.Dispose();
        }
    }
}