using HPParking.Helper;
using HPParking.Interfaces;
using HPParking.Models.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HPParking.Forms.ConfigManager
{
    public partial class UcLanCarManager : UserControl
    {
        private readonly ILaneRepository _laneRepository;
        private List<Lane>? _lanes;

        public UcLanCarManager(ILaneRepository laneRepository)
        {
            InitializeComponent();
            _laneRepository = laneRepository;
        }

        private async void UcLanCarManager_Load(object sender, System.EventArgs e)
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
        /// Đổ dữ liệu từ danh sách _lanes lên các TextBox trên UI (Làn Ô tô)
        /// </summary>
        private void BindDataToUI()
        {
            if (_lanes == null || !_lanes.Any()) return;

            foreach (var lane in _lanes)
            {
                // Làn vào Ô tô (Type lẻ, InputReader chẵn)
                if (lane.Type % 2 != 0 && lane.InputReader % 2 == 0)
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

                    txtInIpFaceId.Text = lane.FaceIdConfig?.IP ?? "";
                    txtInPortFaceId.Text = lane.FaceIdConfig?.Port.ToString() ?? "";
                    txtInUserFaceId.Text = lane.FaceIdConfig?.User ?? "";
                    txtInPassFaceId.Text = lane.FaceIdConfig?.Pass ?? "";

                    txtInReader.Text = lane.InputReader.ToString();
                    txtInRelay.Text = lane.OutputRelay.ToString();
                }

                // Làn ra Ô tô (Type chẵn, InputReader chẵn)
                if (lane.Type % 2 == 0 && lane.InputReader % 2 == 0)
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

                    txtOutIpFaceId.Text = lane.FaceIdConfig?.IP ?? "";
                    txtOutPortFaceId.Text = lane.FaceIdConfig?.Port.ToString() ?? "";
                    txtOutUserFaceid.Text = lane.FaceIdConfig?.User ?? "";
                    txtOutPassFaceId.Text = lane.FaceIdConfig?.Pass ?? "";

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

                // Hàm hỗ trợ đọc giá trị an toàn tránhKeyNotFoundException
                string GetValue(TextBox txt)
                {
                    if (validationResult.Values.TryGetValue(txt.Name, out string val))
                    {
                        return val;
                    }
                    return txt.Text.Trim();
                }

                // --- 1. XỬ LÝ LANE IN Ô TÔ ---
                // InputReader số chẵn là Ô tô
                var laneIn = _lanes?.FirstOrDefault(x => x.Type % 2 != 0 && x.InputReader % 2 == 0);

                var CameraLicensePlateIn = new
                {
                    IP = GetValue(txtInIpCameraLicenseplate),
                    Port = int.Parse(GetValue(txtInPortCameraLicenseplate)),
                    User = GetValue(txtInUserCameraLicenseplate),
                    Pass = GetValue(txtInPassCameraLicenseplate),
                };

                var CameraClientIn = new
                {
                    IP = GetValue(txtInIpCameraClient),
                    Port = int.Parse(GetValue(txtInPortCameraClient)),
                    User = GetValue(txtInUserCameraClient),
                    Pass = GetValue(txtInPassCameraClient),
                };

                var ControllerIn = new
                {
                    IP = GetValue(txtInIpController),
                    Port = int.Parse(GetValue(txtInPortController)),
                    User = GetValue(txtInUserController),
                    Pass = GetValue(txtInPassController),
                };

                var FaceIdIn = new
                {
                    IP = GetValue(txtInIpFaceId),
                    Port = int.Parse(GetValue(txtInPortFaceId)),
                    User = GetValue(txtInUserFaceId),
                    Pass = GetValue(txtInPassFaceId),
                };

                Lane laneInReq = new()
                {
                    Code = "LANEIN",
                    Name = "LÀN VÀO",
                    Type = 1,
                    CameraLicensePlate = JsonSerializer.Serialize(CameraLicensePlateIn),
                    CameraClient = JsonSerializer.Serialize(CameraClientIn),
                    Controller = JsonSerializer.Serialize(ControllerIn),
                    FaceId = JsonSerializer.Serialize(FaceIdIn),
                    InputReader = int.Parse(GetValue(txtInReader)),
                    OutputRelay = int.Parse(GetValue(txtInRelay))
                };

                if (laneIn != null)
                {
                    laneIn.Code = laneInReq.Code;
                    laneIn.Name = laneInReq.Name;
                    laneIn.Type = laneInReq.Type;
                    laneIn.CameraLicensePlate = laneInReq.CameraLicensePlate;
                    laneIn.CameraClient = laneInReq.CameraClient;
                    laneIn.Controller = laneInReq.Controller;
                    laneIn.FaceId = laneInReq.FaceId;
                    laneIn.InputReader = laneInReq.InputReader;
                    laneIn.OutputRelay = laneInReq.OutputRelay;

                    await _laneRepository.UpdateLaneAsync(laneIn.Id, laneIn);
                }
                else
                {
                    await _laneRepository.CreateLaneAsync(laneInReq);
                }

                // --- 2. XỬ LÝ LANE OUT Ô TÔ ---
                var laneOut = _lanes?.FirstOrDefault(x => x.Type % 2 == 0 && x.InputReader % 2 == 0);

                var CameraLicensePlateOut = new
                {
                    IP = GetValue(txtOutIpCameraLicenseplate),
                    Port = int.Parse(GetValue(txtOutPortCameraLicenseplate)),
                    User = GetValue(txtOutUserCameraLicenseplate),
                    Pass = GetValue(txtOutPassCameraLicenseplate),
                };

                var CameraClientOut = new
                {
                    IP = GetValue(txtOutIpCameraClient),
                    Port = int.Parse(GetValue(txtOutPortCameraClient)),
                    User = GetValue(txtOutUserCameraClient),
                    Pass = GetValue(txtOutPassCameraClient),
                };

                var ControllerOut = new
                {
                    IP = GetValue(txtOutIpController),
                    Port = int.Parse(GetValue(txtOutPortController)),
                    User = GetValue(txtOutUserController),
                    Pass = GetValue(txtOutPassController),
                };

                var FaceIdOut = new
                {
                    IP = GetValue(txtOutIpFaceId),
                    Port = int.Parse(GetValue(txtOutPortFaceId)),
                    User = GetValue(txtOutUserFaceid),
                    Pass = GetValue(txtOutPassFaceId),
                };

                Lane laneOutReq = new()
                {
                    Code = "LANEOUT",
                    Name = "LÀN RA",
                    Type = 2,
                    CameraLicensePlate = JsonSerializer.Serialize(CameraLicensePlateOut),
                    CameraClient = JsonSerializer.Serialize(CameraClientOut),
                    Controller = JsonSerializer.Serialize(ControllerOut),
                    InputReader = int.Parse(GetValue(txtOutReader)),
                    OutputRelay = int.Parse(GetValue(txtOutRelay))
                };

                if (laneOut != null)
                {
                    laneOut.Code = laneOutReq.Code;
                    laneOut.Name = laneOutReq.Name;
                    laneOut.Type = laneOutReq.Type;
                    laneOut.CameraLicensePlate = laneOutReq.CameraLicensePlate;
                    laneOut.CameraClient = laneOutReq.CameraClient;
                    laneOut.Controller = laneOutReq.Controller;
                    laneOut.FaceId = laneOutReq.FaceId;
                    laneOut.InputReader = laneOutReq.InputReader;
                    laneOut.OutputRelay = laneOutReq.OutputRelay;

                    await _laneRepository.UpdateLaneAsync(laneOut.Id, laneOut);
                }
                else
                {
                    await _laneRepository.CreateLaneAsync(laneOutReq);
                }

                // --- 3. TẢI LẠI DỮ LIỆU TỪ DB VÀ CẬP NHẬT TRỰC TIẾP LÊN UI ---
                await LoadAndBindDataAsync();

                MessageBox.Show("Lưu cấu hình làn Ô tô thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (System.FormatException fEx)
            {
                MessageBox.Show($"Lỗi định dạng số (Port/Reader/Relay): {fEx.Message}", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}