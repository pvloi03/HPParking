using HPParking.helper;
using HPParking.Interfaces;
using HPParking.Models.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HPParking.Forms.CofigManager
{
    public partial class UcLanMotoManager : UserControl
    {
        private readonly ILaneRepository _laneRepository;
        private List<Lane> _lanes;

        public UcLanMotoManager(ILaneRepository laneRepository)
        {
            InitializeComponent();
            _laneRepository = laneRepository;
        }

        private async void UcLanMotoManager_Load(object sender, System.EventArgs e)
        {
            await LoadAndBindDataAsync();
        }

        /// <summary>
        /// Tải dữ liệu mới nhất từ Database và đổ lên giao diện
        /// </summary>
        private async Task LoadAndBindDataAsync()
        {
            _lanes = await _laneRepository.GetAllAsync();
            BindDataToUI();
        }

        /// <summary>
        /// Đổ dữ liệu từ danh sách _lanes lên các TextBox trên UI
        /// </summary>
        private void BindDataToUI()
        {
            if (_lanes == null || !_lanes.Any()) return;

            foreach (var lane in _lanes)
            {
                // Làn vào Moto (Type lẻ, InputReader lẻ)
                if (lane.Type % 2 != 0 && lane.InputReader % 2 != 0)
                {
                    // Camera Plate In
                    txtInIpCameraLicenseplate.Text = lane.CameraLicensePlateConfig?.IP ?? "";
                    txtInPortCameraLicenseplate.Text = lane.CameraLicensePlateConfig?.Port.ToString() ?? "";
                    txtInUserCameraLicenseplate.Text = lane.CameraLicensePlateConfig?.User ?? "";
                    txtInPassCameraLicenseplate.Text = lane.CameraLicensePlateConfig?.Pass ?? "";

                    // Camera Overview In
                    txtInIpCameraClient.Text = lane.CameraClientConfig?.IP ?? "";
                    txtInPortCameraClient.Text = lane.CameraClientConfig?.Port.ToString() ?? "";
                    txtInUserCameraClient.Text = lane.CameraClientConfig?.User ?? "";
                    txtInPassCameraClient.Text = lane.CameraClientConfig?.Pass ?? "";

                    // Controller In
                    txtInIpController.Text = lane.ControllerConfig?.IP ?? "";
                    txtInPortController.Text = lane.ControllerConfig?.Port.ToString() ?? "";
                    txtInUserController.Text = lane.ControllerConfig?.User ?? "";
                    txtInPassController.Text = lane.ControllerConfig?.Pass ?? "";

                    txtInReader.Text = lane.InputReader.ToString();
                    txtInRelay.Text = lane.OutputRelay.ToString();
                }

                // Làn ra Moto (Type chẵn, InputReader lẻ)
                if (lane.Type % 2 == 0 && lane.InputReader % 2 != 0)
                {
                    // Camera Plate Out
                    txtOutIpCameraLicenseplate.Text = lane.CameraLicensePlateConfig?.IP ?? "";
                    txtOutPortCameraLicenseplate.Text = lane.CameraLicensePlateConfig?.Port.ToString() ?? "";
                    txtOutUserCameraLicenseplate.Text = lane.CameraLicensePlateConfig?.User ?? "";
                    txtOutPassCameraLicenseplate.Text = lane.CameraLicensePlateConfig?.Pass ?? "";

                    // Camera Overview Out
                    txtOutIpCameraClient.Text = lane.CameraClientConfig?.IP ?? "";
                    txtOutPortCameraClient.Text = lane.CameraClientConfig?.Port.ToString() ?? "";
                    txtOutUserCameraClient.Text = lane.CameraClientConfig?.User ?? "";
                    txtOutPassCameraClient.Text = lane.CameraClientConfig?.Pass ?? "";

                    // Controller Out
                    txtOutIpController.Text = lane.ControllerConfig?.IP ?? "";
                    txtOutPortController.Text = lane.ControllerConfig?.Port.ToString() ?? "";
                    txtOutUserController.Text = lane.ControllerConfig?.User ?? "";
                    txtOutPassController.Text = lane.ControllerConfig?.Pass ?? "";

                    txtOutReader.Text = lane.InputReader.ToString();
                    txtOutRelay.Text = lane.OutputRelay.ToString();
                }
            }
        }

        private async void button5_Click(object sender, System.EventArgs e)
        {
            try
            {
                List<TextBox> textBoxes = FrmHelpers.GetControls<TextBox>([grbLaneIn, grbLaneOut]);

                var validationResult = ValidationHelper.CheckControlsNotEmpty(textBoxes);
                if (!validationResult.IsValid)
                {
                    return;
                }

                // --- 1. XỬ LÝ LANE IN MOTO ---
                var laneIn = _lanes?.FirstOrDefault(x => x.Type % 2 != 0 && x.InputReader % 2 != 0);

                var CameraLicensePlateIn = new
                {
                    IP = validationResult.Values[txtInIpCameraLicenseplate.Name],
                    Port = int.Parse(validationResult.Values[txtInPortCameraLicenseplate.Name]),
                    User = validationResult.Values[txtInUserCameraLicenseplate.Name],
                    Pass = validationResult.Values[txtInPassCameraLicenseplate.Name],
                };

                var CameraClientIn = new
                {
                    IP = validationResult.Values[txtInIpCameraClient.Name],
                    Port = int.Parse(validationResult.Values[txtInPortCameraClient.Name]),
                    User = validationResult.Values[txtInUserCameraClient.Name],
                    Pass = validationResult.Values[txtInPassCameraClient.Name],
                };

                var ControllerIn = new
                {
                    IP = validationResult.Values[txtInIpController.Name],
                    Port = int.Parse(validationResult.Values[txtInPortController.Name]),
                    User = validationResult.Values[txtInUserController.Name],
                    Pass = validationResult.Values[txtInPassController.Name],
                };

                Lane laneInReq = new()
                {
                    Code = "LANEIN",
                    Name = "LÀN VÀO",
                    Type = 1,
                    CameraLicensePlate = JsonSerializer.Serialize(CameraLicensePlateIn),
                    CameraClient = JsonSerializer.Serialize(CameraClientIn),
                    Controller = JsonSerializer.Serialize(ControllerIn),
                    InputReader = int.Parse(validationResult.Values[txtInReader.Name]),
                    OutputRelay = int.Parse(validationResult.Values[txtInRelay.Name])
                };

                if (laneIn != null)
                {
                    laneIn.Code = laneInReq.Code;
                    laneIn.Name = laneInReq.Name;
                    laneIn.Type = laneInReq.Type;
                    laneIn.CameraLicensePlate = laneInReq.CameraLicensePlate;
                    laneIn.CameraClient = laneInReq.CameraClient;
                    laneIn.Controller = laneInReq.Controller;
                    laneIn.InputReader = laneInReq.InputReader;
                    laneIn.OutputRelay = laneInReq.OutputRelay;

                    await _laneRepository.UpdateLaneAsync(laneIn.Id, laneIn);
                }
                else
                {
                    await _laneRepository.CreateLaneAsync(laneInReq);
                }

                // --- 2. XỬ LÝ LANE OUT MOTO ---
                var laneOut = _lanes?.FirstOrDefault(x => x.Type % 2 == 0 && x.InputReader % 2 != 0);

                var CameraLicensePlateOut = new
                {
                    IP = validationResult.Values[txtOutIpCameraLicenseplate.Name],
                    Port = int.Parse(validationResult.Values[txtOutPortCameraLicenseplate.Name]),
                    User = validationResult.Values[txtOutUserCameraLicenseplate.Name],
                    Pass = validationResult.Values[txtOutPassCameraLicenseplate.Name],
                };

                var CameraClientOut = new
                {
                    IP = validationResult.Values[txtOutIpCameraClient.Name],
                    Port = int.Parse(validationResult.Values[txtOutPortCameraClient.Name]),
                    User = validationResult.Values[txtOutUserCameraClient.Name],
                    Pass = validationResult.Values[txtOutPassCameraClient.Name],
                };

                var ControllerOut = new
                {
                    IP = validationResult.Values[txtOutIpController.Name],
                    Port = int.Parse(validationResult.Values[txtOutPortController.Name]),
                    User = validationResult.Values[txtOutUserController.Name],
                    Pass = validationResult.Values[txtOutPassController.Name],
                };

                Lane laneOutReq = new()
                {
                    Code = "LANEOUT",
                    Name = "LÀN RA",
                    Type = 2,
                    CameraLicensePlate = JsonSerializer.Serialize(CameraLicensePlateOut),
                    CameraClient = JsonSerializer.Serialize(CameraClientOut),
                    Controller = JsonSerializer.Serialize(ControllerOut),
                    InputReader = int.Parse(validationResult.Values[txtOutReader.Name]),
                    OutputRelay = int.Parse(validationResult.Values[txtOutRelay.Name])
                };

                if (laneOut != null)
                {
                    laneOut.Code = laneOutReq.Code;
                    laneOut.Name = laneOutReq.Name;
                    laneOut.Type = laneOutReq.Type;
                    laneOut.CameraLicensePlate = laneOutReq.CameraLicensePlate;
                    laneOut.CameraClient = laneOutReq.CameraClient;
                    laneOut.Controller = laneOutReq.Controller;
                    laneOut.InputReader = laneOutReq.InputReader;
                    laneOut.OutputRelay = laneOutReq.OutputRelay;

                    await _laneRepository.UpdateLaneAsync(laneOut.Id, laneOut);
                }
                else
                {
                    await _laneRepository.CreateLaneAsync(laneOutReq);
                }

                // --- 3. TẢI LẠI DỮ LIỆU TỪ DB VÀ ĐỔ LÊN UI ---
                await LoadAndBindDataAsync();

                MessageBox.Show("Lưu cấu hình làn thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}