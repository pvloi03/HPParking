using HPParking.Models;
using HPParking.Services.LPR;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace HPParking.Services.Hardware
{
    /// <summary>
    /// Bộ điều phối phần cứng làn xe: Chụp ảnh đa camera, nhận diện biển số LPR và kích hoạt mở barrier
    /// </summary>
    public interface ILaneHardwareOrchestrator
    {
        /// <summary>
        /// Chụp ảnh đồng thời đa camera (Overview, Plate, Face) an toàn với timeout
        /// </summary>
        Task<CapturedLaneImages> CaptureLaneImagesAsync(
            LaneRuntimeContext context,
            bool needOverview = true,
            bool needPlate = true,
            bool needFace = false,
            int timeoutMs = 2500);

        /// <summary>
        /// Trích xuất ảnh phục vụ hiển thị UI và giải phóng CapturedLaneImages an toàn
        /// </summary>
        (Bitmap? SmallPlate, Bitmap? FaceSnap, Bitmap? OverviewSnap) ExtractWorkflowImages(
            CapturedLaneImages images,
            LprResult? lprResult);

        /// <summary>
        /// Nhận diện biển số xe qua OCR/LPR kèm fallback nhập tay
        /// </summary>
        Task<(bool Success, string DetectedPlate, LprResult? LprResult)> RecognizePlateAsync(
            LaneRuntimeContext context,
            Bitmap? plateImage,
            string defaultPlate,
            Func<LaneRuntimeContext, string?, Task<string?>>? onManualPlateInput = null);

        /// <summary>
        /// Chuẩn hóa chuỗi biển số xe
        /// </summary>
        string NormalizePlate(string? plate);

        /// <summary>
        /// Kích hoạt lệnh mở barrier qua Controller
        /// </summary>
        bool TryOpenBarrier(LaneRuntimeContext context, Func<LaneRuntimeContext, bool>? onBarrierOpenFailed = null);
    }
}
