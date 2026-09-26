using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HPParking.Forms.ConfigManager
{
    public partial class UcLanMotoManager : UserControl
    {
        private readonly IRepository<Lane> _laneRepository;
        private readonly IRepository<Device> _deviceRepository;
        private List<Lane>? _lanes;
        private List<Device>? _devices;
        private string? _gateId;

        public UcLanMotoManager(IRepository<Lane> laneRepository, IRepository<Device> deviceRepository)
        {
            InitializeComponent();
            _laneRepository = laneRepository;
            _deviceRepository = deviceRepository;
        }

        public void SetGateId(string? gateId)
        {
            _gateId = gateId;
            if (IsHandleCreated)
            {
                _ = LoadAndBindDataAsync();
            }
        }

        public void SetGateCode(string? gateCode) => SetGateId(gateCode);

        private async void UcLanMotoManager_Load(object sender, EventArgs e)
        {
            await LoadAndBindDataAsync();
        }

        /// <summary>
        /// Tải dữ liệu mới nhất từ Database và đổ lên giao diện (theo GateId nếu có)
        /// </summary>
        private async Task LoadAndBindDataAsync()
        {
            using var waitScope = new WaitCursorScope(this);
            var devList = await _deviceRepository.GetAllAsync();
            _devices = devList?.ToList() ?? [];

            if (!string.IsNullOrWhiteSpace(_gateId))
            {
                var gateLanes = await _laneRepository.FindAsync(x => x.GateId == _gateId && x.IsActive);
                _lanes = gateLanes?.ToList() ?? [];
            }
            else
            {
                var laneList = await _laneRepository.GetAllAsync();
                _lanes = laneList?.ToList() ?? [];
            }
            BindDataToUI();
        }

        /// <summary>
        /// Đổ dữ liệu từ danh sách _lanes lên các TextBox trên UI (Làn Xe Máy)
        /// </summary>
        private void BindDataToUI()
        {
            if (_lanes == null || !_lanes.Any()) return;

            var devMap = _devices?.ToDictionary(d => d.Id, d => d) ?? new Dictionary<string, Device>();

            foreach (var lane in _lanes)
            {
                // Làn vào Xe Máy (Direction In, InputReader lẻ)
                if (lane.Direction == LaneDirection.In && lane.InputReader % 2 != 0)
                {
                    // Camera Plate In
                    Device? plateDev = (!string.IsNullOrEmpty(lane.PlateCameraDeviceId) && devMap.TryGetValue(lane.PlateCameraDeviceId, out var pDev)) ? pDev : null;
                    txtInIpCameraLicenseplate.Text = plateDev?.IpAddress ?? "";
                    txtInPortCameraLicenseplate.Text = plateDev != null ? plateDev.Port.ToString() : "";
                    txtInUserCameraLicenseplate.Text = plateDev?.UserName ?? "";
                    txtInPassCameraLicenseplate.Text = plateDev?.Password ?? "";

                    // Camera Overview In
                    Device? overviewDev = (!string.IsNullOrEmpty(lane.OverviewCameraDeviceId) && devMap.TryGetValue(lane.OverviewCameraDeviceId, out var oDev)) ? oDev : null;
                    txtInIpCameraClient.Text = overviewDev?.IpAddress ?? "";
                    txtInPortCameraClient.Text = overviewDev != null ? overviewDev.Port.ToString() : "";
                    txtInUserCameraClient.Text = overviewDev?.UserName ?? "";
                    txtInPassCameraClient.Text = overviewDev?.Password ?? "";

                    // Controller In
                    Device? ctrlDev = (!string.IsNullOrEmpty(lane.ControllerDeviceId) && devMap.TryGetValue(lane.ControllerDeviceId, out var cDev)) ? cDev : null;
                    txtInIpController.Text = ctrlDev?.IpAddress ?? "";
                    txtInPortController.Text = ctrlDev != null ? ctrlDev.Port.ToString() : "";
                    txtInUserController.Text = ctrlDev?.UserName ?? "";
                    txtInPassController.Text = ctrlDev?.Password ?? "";

                    // FaceId In
                    Device? faceDev = (!string.IsNullOrEmpty(lane.FaceDeviceId) && devMap.TryGetValue(lane.FaceDeviceId, out var fDev)) ? fDev : null;
                    txtInIpFaceId.Text = faceDev?.IpAddress ?? "";
                    txtInPortFaceId.Text = faceDev != null ? faceDev.Port.ToString() : "";
                    txtInUserFaceId.Text = faceDev?.UserName ?? "";
                    txtInPassFaceId.Text = faceDev?.Password ?? "";

                    txtInReader.Text = lane.InputReader.ToString();
                    txtInRelay.Text = lane.OutputRelay.ToString();
                }

                // Làn ra Xe Máy (Direction Out, InputReader lẻ)
                if (lane.Direction == LaneDirection.Out && lane.InputReader % 2 != 0)
                {
                    // Camera Plate Out
                    Device? plateDev = (!string.IsNullOrEmpty(lane.PlateCameraDeviceId) && devMap.TryGetValue(lane.PlateCameraDeviceId, out var pDev)) ? pDev : null;
                    txtOutIpCameraLicenseplate.Text = plateDev?.IpAddress ?? "";
                    txtOutPortCameraLicenseplate.Text = plateDev != null ? plateDev.Port.ToString() : "";
                    txtOutUserCameraLicenseplate.Text = plateDev?.UserName ?? "";
                    txtOutPassCameraLicenseplate.Text = plateDev?.Password ?? "";

                    // Camera Overview Out
                    Device? overviewDev = (!string.IsNullOrEmpty(lane.OverviewCameraDeviceId) && devMap.TryGetValue(lane.OverviewCameraDeviceId, out var oDev)) ? oDev : null;
                    txtOutIpCameraClient.Text = overviewDev?.IpAddress ?? "";
                    txtOutPortCameraClient.Text = overviewDev != null ? overviewDev.Port.ToString() : "";
                    txtOutUserCameraClient.Text = overviewDev?.UserName ?? "";
                    txtOutPassCameraClient.Text = overviewDev?.Password ?? "";

                    // Controller Out
                    Device? ctrlDev = (!string.IsNullOrEmpty(lane.ControllerDeviceId) && devMap.TryGetValue(lane.ControllerDeviceId, out var cDev)) ? cDev : null;
                    txtOutIpController.Text = ctrlDev?.IpAddress ?? "";
                    txtOutPortController.Text = ctrlDev != null ? ctrlDev.Port.ToString() : "";
                    txtOutUserController.Text = ctrlDev?.UserName ?? "";
                    txtOutPassController.Text = ctrlDev?.Password ?? "";

                    // FaceId Out
                    Device? faceDev = (!string.IsNullOrEmpty(lane.FaceDeviceId) && devMap.TryGetValue(lane.FaceDeviceId, out var fDev)) ? fDev : null;
                    txtOutIpFaceId.Text = faceDev?.IpAddress ?? "";
                    txtOutPortFaceId.Text = faceDev != null ? faceDev.Port.ToString() : "";
                    txtOutUserFaceid.Text = faceDev?.UserName ?? "";
                    txtOutPassFaceId.Text = faceDev?.Password ?? "";

                    txtOutReader.Text = lane.InputReader.ToString();
                    txtOutRelay.Text = lane.OutputRelay.ToString();
                }
            }
        }

        private async Task<string> UpsertDeviceAsync(string? existingDeviceId, string code, string name, DeviceType type, string ip, int port, string user, string pass)
        {
            Device? dev = !string.IsNullOrEmpty(existingDeviceId) ? await _deviceRepository.GetByIdAsync(existingDeviceId) : null;
            if (dev != null)
            {
                dev.Code = code;
                dev.Name = name;
                dev.Type = type;
                dev.IpAddress = ip;
                dev.Port = port;
                dev.UserName = user;
                dev.Password = pass;
                dev.IsActive = true;
                await _deviceRepository.UpdateAsync(dev);
                return dev.Id;
            }
            else
            {
                Device newDev = new()
                {
                    Code = code,
                    Name = name,
                    Type = type,
                    IpAddress = ip,
                    Port = port,
                    UserName = user,
                    Password = pass,
                    IsActive = true
                };
                await _deviceRepository.AddAsync(newDev);
                return newDev.Id;
            }
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            try
            {
                List<TextBox> textBoxes = FrmHelpers.GetControls<TextBox>([grbLaneIn, grbLaneOut]);

                var validationResult = ValidationHelper.CheckControlsNotEmpty(textBoxes);
                if (!validationResult.IsValid)
                {
                    return;
                }

                using var waitScope = new WaitCursorScope(this);

                string GetValue(TextBox txt)
                {
                    if (validationResult.Values.TryGetValue(txt.Name, out string? val))
                    {
                        return val!;
                    }
                    return txt.Text.Trim();
                }

                // Kiểm tra trùng cổng đọc Vào và Ra
                if (int.Parse(GetValue(txtInReader)) == int.Parse(GetValue(txtOutReader)))
                {
                    MessageBox.Show("Cổng đầu đọc Làn Vào và Làn Ra không được trùng nhau!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtOutReader.Focus();
                    return;
                }

                // --- 1. XỬ LÝ LANE IN XE MÁY ---
                var laneIn = _lanes?.FirstOrDefault(x => x.Direction == LaneDirection.In && x.InputReader % 2 != 0);

                string plateCamInId = await UpsertDeviceAsync(
                    laneIn?.PlateCameraDeviceId, "CAM-PLATE-MOTO-IN", "Camera Biển Số Xe Máy Vào", DeviceType.Camera,
                    GetValue(txtInIpCameraLicenseplate), int.Parse(GetValue(txtInPortCameraLicenseplate)),
                    GetValue(txtInUserCameraLicenseplate), GetValue(txtInPassCameraLicenseplate));

                string viewCamInId = await UpsertDeviceAsync(
                    laneIn?.OverviewCameraDeviceId, "CAM-VIEW-MOTO-IN", "Camera Toàn Cảnh Xe Máy Vào", DeviceType.Camera,
                    GetValue(txtInIpCameraClient), int.Parse(GetValue(txtInPortCameraClient)),
                    GetValue(txtInUserCameraClient), GetValue(txtInPassCameraClient));

                string ctrlInId = await UpsertDeviceAsync(
                    laneIn?.ControllerDeviceId, "CTRL-MOTO-IN", "Bộ Điều Khiển Xe Máy Vào", DeviceType.Controller,
                    GetValue(txtInIpController), int.Parse(GetValue(txtInPortController)),
                    GetValue(txtInUserController), GetValue(txtInPassController));

                string faceInId = await UpsertDeviceAsync(
                    laneIn?.FaceDeviceId, "FACE-MOTO-IN", "FaceID Xe Máy Vào", DeviceType.FaceId,
                    GetValue(txtInIpFaceId), int.Parse(GetValue(txtInPortFaceId)),
                    GetValue(txtInUserFaceId), GetValue(txtInPassFaceId));

                Lane laneInReq = new()
                {
                    Code = "LANEIN-MOTO",
                    Name = "LÀN VÀO XE MÁY",
                    GateId = string.IsNullOrWhiteSpace(_gateId) ? null : _gateId,
                    Direction = LaneDirection.In,
                    PlateCameraDeviceId = plateCamInId,
                    OverviewCameraDeviceId = viewCamInId,
                    ControllerDeviceId = ctrlInId,
                    FaceDeviceId = faceInId,
                    InputReader = int.Parse(GetValue(txtInReader)),
                    OutputRelay = int.Parse(GetValue(txtInRelay)),
                    IsActive = true
                };

                if (laneIn != null)
                {
                    laneIn.Code = laneInReq.Code;
                    laneIn.Name = laneInReq.Name;
                    if (!string.IsNullOrWhiteSpace(_gateId)) laneIn.GateId = _gateId;
                    laneIn.Direction = laneInReq.Direction;
                    laneIn.PlateCameraDeviceId = laneInReq.PlateCameraDeviceId;
                    laneIn.OverviewCameraDeviceId = laneInReq.OverviewCameraDeviceId;
                    laneIn.ControllerDeviceId = laneInReq.ControllerDeviceId;
                    laneIn.FaceDeviceId = laneInReq.FaceDeviceId;
                    laneIn.InputReader = laneInReq.InputReader;
                    laneIn.OutputRelay = laneInReq.OutputRelay;
                    laneIn.IsActive = true;

                    await _laneRepository.UpdateAsync(laneIn);
                }
                else
                {
                    await _laneRepository.AddAsync(laneInReq);
                }

                // --- 2. XỬ LÝ LANE OUT XE MÁY ---
                var laneOut = _lanes?.FirstOrDefault(x => x.Direction == LaneDirection.Out && x.InputReader % 2 != 0);

                string plateCamOutId = await UpsertDeviceAsync(
                    laneOut?.PlateCameraDeviceId, "CAM-PLATE-MOTO-OUT", "Camera Biển Số Xe Máy Ra", DeviceType.Camera,
                    GetValue(txtOutIpCameraLicenseplate), int.Parse(GetValue(txtOutPortCameraLicenseplate)),
                    GetValue(txtOutUserCameraLicenseplate), GetValue(txtOutPassCameraLicenseplate));

                string viewCamOutId = await UpsertDeviceAsync(
                    laneOut?.OverviewCameraDeviceId, "CAM-VIEW-MOTO-OUT", "Camera Toàn Cảnh Xe Máy Ra", DeviceType.Camera,
                    GetValue(txtOutIpCameraClient), int.Parse(GetValue(txtOutPortCameraClient)),
                    GetValue(txtOutUserCameraClient), GetValue(txtOutPassCameraClient));

                string ctrlOutId = await UpsertDeviceAsync(
                    laneOut?.ControllerDeviceId, "CTRL-MOTO-OUT", "Bộ Điều Khiển Xe Máy Ra", DeviceType.Controller,
                    GetValue(txtOutIpController), int.Parse(GetValue(txtOutPortController)),
                    GetValue(txtOutUserController), GetValue(txtOutPassController));

                string faceOutId = await UpsertDeviceAsync(
                    laneOut?.FaceDeviceId, "FACE-MOTO-OUT", "FaceID Xe Máy Ra", DeviceType.FaceId,
                    GetValue(txtOutIpFaceId), int.Parse(GetValue(txtOutPortFaceId)),
                    GetValue(txtOutUserFaceid), GetValue(txtOutPassFaceId));

                Lane laneOutReq = new()
                {
                    Code = "LANEOUT-MOTO",
                    Name = "LÀN RA XE MÁY",
                    GateId = string.IsNullOrWhiteSpace(_gateId) ? null : _gateId,
                    Direction = LaneDirection.Out,
                    PlateCameraDeviceId = plateCamOutId,
                    OverviewCameraDeviceId = viewCamOutId,
                    ControllerDeviceId = ctrlOutId,
                    FaceDeviceId = faceOutId,
                    InputReader = int.Parse(GetValue(txtOutReader)),
                    OutputRelay = int.Parse(GetValue(txtOutRelay)),
                    IsActive = true
                };

                if (laneOut != null)
                {
                    laneOut.Code = laneOutReq.Code;
                    laneOut.Name = laneOutReq.Name;
                    if (!string.IsNullOrWhiteSpace(_gateId)) laneOut.GateId = _gateId;
                    laneOut.Direction = laneOutReq.Direction;
                    laneOut.PlateCameraDeviceId = laneOutReq.PlateCameraDeviceId;
                    laneOut.OverviewCameraDeviceId = laneOutReq.OverviewCameraDeviceId;
                    laneOut.ControllerDeviceId = laneOutReq.ControllerDeviceId;
                    laneOut.FaceDeviceId = laneOutReq.FaceDeviceId;
                    laneOut.InputReader = laneOutReq.InputReader;
                    laneOut.OutputRelay = laneOutReq.OutputRelay;
                    laneOut.IsActive = true;

                    await _laneRepository.UpdateAsync(laneOut);
                }
                else
                {
                    await _laneRepository.AddAsync(laneOutReq);
                }

                // --- 3. TẢI LẠI DỮ LIỆU TỪ DB VÀ ĐỔ LÊN UI ---
                await LoadAndBindDataAsync();

                MessageBox.Show("Lưu cấu hình làn xe máy thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (FormatException fEx)
            {
                MessageBox.Show($"Lỗi định dạng số (Port/Reader/Relay): {fEx.Message}", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}