using HPParking.Interfaces;
using HPParking.Models.Entities;
using HPParking.Services.Controller;
using HPParking.Services.LPR;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HPParking.Services.Parking
{
    public class ParkingWorkflowService(
        IClientRepository clientRepository,
        IEventParkingRepository eventRepository,
        LprService lprService,
        IImageStorageService imageStorageService) : IParkingWorkflowService
    {
        private readonly IClientRepository _clientRepository = clientRepository;
        private readonly IEventParkingRepository _eventRepository = eventRepository;
        private readonly LprService _lprService = lprService;
        private readonly IImageStorageService _imageStorageService = imageStorageService;

        private bool BarrierOpen(Lane lane)
        {
            if (!lane.Ctrl.OpenBarrier(lane.InputReader, 1))
            {
                var confirmBarrierOpened = MessageBox.Show(
                    "Không thể mở barrier. Vui lòng kiểm tra kết nối hoặc mở thủ công và xác nhận (Yes) khi xe đã vào, (No) để hủy lượt ra vào hiện tại", "Lỗi",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (confirmBarrierOpened == DialogResult.No)
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsClientExpired(Client client)
        {
            if ((client.Expired.StartDay > DateTime.UtcNow)) return false;
            return client.Expired.EndDay.Value <= DateTime.UtcNow;
        }

        public async Task<ProcessResult> ProcessEntryAsync(Lane lane, RealtimeLog data, string imageBasePath)
        {
            var client = await _clientRepository.GetByCardCode($"0{data.CardNo}");
            if (client == null)
                return new ProcessResult { Status = ProcessStatus.ClientNotFound, Message = "Không tìm thấy người dùng." };

            if (IsClientExpired(client))
            {
                return new ProcessResult { Status = ProcessStatus.ConfirmRequired, Message = $"Người dùng chỉ được ra vào từ {client.Expired.StartDay:dd/MM/yyyy} - {client.Expired.EndDay:dd/MM/yyyy}" };
            }

            var parkingInProgress = await _eventRepository.GetParkingInProgress(data.CardNo);
            if (parkingInProgress != null)
                return new ProcessResult { Status = ProcessStatus.AlreadyInParking, Message = "Khách hàng này đang có xe trong bãi." };

            Bitmap plateImage, overviewImage;
            try
            {
                plateImage = await Task.Run(() => lane.Cameras.LicensePlateCamera.Capture());
                overviewImage = await Task.Run(() => lane.Cameras.OverviewCamera.Capture());
            }
            catch (Exception ex)
            {
                return new ProcessResult { Status = ProcessStatus.CaptureFailed, Message = $"Lỗi chụp ảnh: {ex.Message}" };
            }

            LprResult result = await Task.Run(() => _lprService.Recognize(plateImage));
            if (!result.Success)
            {
                plateImage.Dispose();
                overviewImage.Dispose();
                return new ProcessResult { Status = ProcessStatus.LprFailed, Message = "Nhận diện biển số thất bại." };
            }

            // Mở Barrier
            if (!BarrierOpen(lane))
            {
                plateImage.Dispose();
                overviewImage.Dispose();
                return new ProcessResult
                {
                    Status = ProcessStatus.BarrierFailed,
                };
            }

            // Lưu dữ liệu ngầm
            Bitmap plateSave = (Bitmap)plateImage.Clone();
            Bitmap overviewSave = (Bitmap)overviewImage.Clone();
            plateImage.Dispose();

            _ = Task.Run(async () =>
            {
                using (plateSave)
                using (overviewSave)
                {
                    string platePath = _imageStorageService.SaveImage(plateSave, "ImageIn", "BienSo", imageBasePath);
                    string overviewPath = _imageStorageService.SaveImage(overviewSave, "ImageIn", "ToanCanh", imageBasePath);

                    EventParking parking = new()
                    {
                        PhoneNumber = client.PhoneNumber,
                        ClientName = client.Name,
                        Card_Code = client.PhoneNumber,
                        Card_Category = client.CardCategory,
                        LicensePlate = client.LicensePlate != result.Plate ? "" : client.LicensePlate,
                        LicensePlateIn = result.Plate,
                        UrlImageLicensePlateIn = platePath,
                        UrlImageClientIn = overviewPath,
                        TimeIn = data.Time,
                    };

                    await _eventRepository.Insert(parking);
                }
            });

            return new ProcessResult
            {
                Status = ProcessStatus.Success,
                Client = client,
                LprResult = result,
                OverviewImage = overviewImage
            };
        }

        public async Task<ProcessResult> ProcessExitAsync(Lane lane, RealtimeLog data, string imageBasePath)
        {
            var client = await _clientRepository.GetByCardCode($"0{data.CardNo}");
            if (client == null)
                return new ProcessResult { Status = ProcessStatus.ClientNotFound, Message = "Không tìm thấy khách hàng." };

            if (IsClientExpired(client))
            {
                return new ProcessResult { Status = ProcessStatus.ConfirmRequired, Message = $"Người dùng chỉ được ra vào từ {client.Expired.StartDay:dd/MM/yyyy} - {client.Expired.EndDay:dd/MM/yyyy}" };
            }

            var parking = await _eventRepository.GetParkingInProgress($"0{data.CardNo}");
            if (parking == null)
                return new ProcessResult { Status = ProcessStatus.NotInParking, Message = "Khách hàng này không có xe trong bãi." };

            Bitmap plateImage, overviewImage;
            try
            {
                plateImage = await Task.Run(() => lane.Cameras.LicensePlateCamera.Capture());
                overviewImage = await Task.Run(() => lane.Cameras.OverviewCamera.Capture());
            }
            catch (Exception ex)
            {
                return new ProcessResult { Status = ProcessStatus.CaptureFailed, Message = $"Lỗi chụp ảnh: {ex.Message}" };
            }

            LprResult result = await Task.Run(() => _lprService.Recognize(plateImage));
            if (!result.Success)
            {
                plateImage.Dispose();
                overviewImage.Dispose();
                return new ProcessResult { Status = ProcessStatus.LprFailed, Message = "Nhận diện biển số thất bại." };
            }

            if (result.Plate != parking.LicensePlate)
            {
                plateImage.Dispose();
                overviewImage.Dispose();
                return new ProcessResult { Status = ProcessStatus.PlateMismatch, Message = "Biển số không khớp với biển số xe đã gửi." };
            }

            if (!BarrierOpen(lane))
            {
                plateImage.Dispose();
                overviewImage.Dispose();
                return new ProcessResult { Status = ProcessStatus.BarrierFailed };
            }

            Bitmap plateSave = (Bitmap)plateImage.Clone();
            Bitmap overviewSave = (Bitmap)overviewImage.Clone();
            plateImage.Dispose();

            _ = Task.Run(async () =>
            {
                using (plateSave)
                using (overviewSave)
                {
                    string platePath = _imageStorageService.SaveImage(plateSave, "ImageOut", "BienSo", imageBasePath);
                    string overviewPath = _imageStorageService.SaveImage(overviewSave, "ImageOut", "ToanCanh", imageBasePath);

                    parking.LicensePlateOut = result.Plate;
                    parking.UrlImageLicensePlateOut = platePath;
                    parking.UrlImageClientOut = overviewPath;
                    parking.Status = "OUT";
                    parking.StatusInOut = true;
                    parking.TimeOut = data.Time;

                    await _eventRepository.Update(parking.Id, parking);
                }
            });

            return new ProcessResult
            {
                Status = ProcessStatus.Success,
                Client = client,
                EventParking = parking,
                LprResult = result,
                OverviewImage = overviewImage
            };
        }
    }
}