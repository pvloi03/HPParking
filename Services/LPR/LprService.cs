using SimpleLPR3;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace HPParking.Services.LPR
{
    /// <summary>
    /// Dịch vụ nhận dạng biển số xe sử dụng SimpleLPR3 SDK
    /// Xử lý việc phát hiện, trích xuất và nhận dạng biển số từ hình ảnh
    /// </summary>
    public class LprService
    {
        /// <summary>
        /// SimpleLPR engine instance cho xử lý biển số
        /// </summary>
        private ISimpleLPR _lpr;

        /// <summary>
        /// Processor instance được tạo từ LPR engine, dùng để phân tích ảnh
        /// </summary>
        private IProcessor _processor;

        /// <summary>
        /// Khởi tạo SimpleLPR engine với cấu hình CPU
        /// - Vô hiệu hóa GPU processing
        /// - Thiết lập trọng số cho Việt Nam (priority cao nhất)
        /// - Chuẩn bị engine để xử lý ảnh
        /// </summary>
        /// <returns>True nếu khởi tạo thành công, False nếu có lỗi</returns>
        public bool Initialize()
        {
            try
            {
                // Cấu hình engine: sử dụng CPU (-1 = CPU mode), tắt GPU
                EngineSetupParms setupP;
                setupP.cudaDeviceId = -1; // Sử dụng CPU
                setupP.enableImageProcessingWithGPU = false;
                setupP.enableClassificationWithGPU = false;
                setupP.maxConcurrentImageProcessingOps = 0;  // Dùng giá trị mặc định

                _lpr = SimpleLPR.Setup(setupP);

                // Khởi tạo trọng số quốc gia - quá trình này có thể mất thời gian
                Cursor.Current = Cursors.WaitCursor;
                _lpr.realizeCountryWeights();
                Cursor.Current = Cursors.Default;

                // Thiết lập ưu tiên nhận dạng cho các quốc gia
                // Ưu tiên cao nhất cho Việt Nam (1.0f), các quốc gia khác (0.0f)
                for (uint i = 0; i < _lpr.numSupportedCountries; i++)
                {
                    string country = _lpr.get_countryCode(i);

                    _lpr.set_countryWeight(
                        country,
                        country == "Vietnam" ? 1.0f : 0.0f);
                }

                // Cập nhật trọng số quốc gia vào engine
                _lpr.realizeCountryWeights();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tạo bộ xử lý (Processor) từ SimpleLPR engine
        /// Chỉ tạo một lần, lần gọi tiếp theo sẽ bỏ qua
        /// Thiết lập các tham số xử lý ảnh: phát hiện vùng biển số, cắt ảnh vùng biển số
        /// </summary>
        private void CreateProcessor()
        {
            // Nếu processor đã tạo, không tạo lại
            if (_processor != null)
                return;

            // Load khóa sản phẩm từ resource file
            _lpr.set_productKey(System.Text.Encoding.UTF8.GetBytes(XDocument.Parse(Properties.Resources.SimpleLPR3_key).Document.ToString()));

            // Tạo processor instance từ LPR engine
            _processor = _lpr.createProcessor();

            // Bật các tính năng xử lý ảnh
            _processor.plateRegionDetectionEnabled = true;  // Phát hiện vùng biển số
            _processor.cropToPlateRegionEnabled = true;     // Cắt ảnh theo vùng biển số
        }

        /// <summary>
        /// Lấy ứng viên biển số tốt nhất từ danh sách các ứng viên được phát hiện
        /// Chọn biển số có độ tin cậy cao nhất (confidence cao nhất)
        /// </summary>
        /// <param name="candidates">Danh sách các ứng viên biển số được phát hiện</param>
        /// <returns>Ứng viên tốt nhất hoặc null nếu không tìm thấy</returns>
        private Candidate? GetBestCandidate(List<Candidate> candidates)
        {
            // Lọc các ứng viên có kết quả nhận dạng
            // Sắp xếp theo độ tin cậy giảm dần
            // Trả về ứng viên đầu tiên (tốt nhất) hoặc null
            return candidates
                .Where(c => c.matches != null && c.matches.Count > 0)
                .OrderByDescending(c => c.matches[0].confidence)
                .Select(c => (Candidate?)c)
                .FirstOrDefault();
        }

        /// <summary>
        /// Cắt hình ảnh lấy vùng chứa biển số từ ảnh gốc
        /// Sử dụng bounding box của ứng viên biển số
        /// </summary>
        /// <param name="source">Ảnh gốc</param>
        /// <param name="candidate">Ứng viên biển số chứa thông tin vị trí</param>
        /// <returns>Ảnh được cắt chỉ chứa vùng biển số</returns>
        private Bitmap CropPlate(Bitmap source, Candidate candidate)
        {
            // Lấy tọa độ của vùng biển số từ bounding box
            Rectangle rect = new(
                candidate.bbox.Left,
                candidate.bbox.Top,
                candidate.bbox.Width,
                candidate.bbox.Height);

            // Tạo bitmap mới với kích thước bằng vùng biển số
            Bitmap plate = new(rect.Width, rect.Height);

            // Vẽ phần ảnh vùng biển số từ ảnh gốc vào bitmap mới
            using (Graphics g = Graphics.FromImage(plate))
            {
                g.DrawImage(
                    source,
                    new Rectangle(0, 0, rect.Width, rect.Height),
                    rect,
                    GraphicsUnit.Pixel);
            }

            return plate;
        }

        /// <summary>
        /// Thay đổi kích thước hình ảnh nếu vượt quá chiều rộng tối đa
        /// Giữ nguyên tỷ lệ khung hình
        /// </summary>
        /// <param name="source">Ảnh gốc</param>
        /// <param name="maxWidth">Chiều rộng tối đa cho phép</param>
        /// <returns>Ảnh được thay đổi kích thước hoặc ảnh gốc nếu nhỏ hơn maxWidth</returns>
        private Bitmap ResizeBitmap(Bitmap source, int maxWidth)
        {
            // Nếu ảnh nhỏ hơn maxWidth, trả về bản sao của ảnh gốc
            if (source.Width <= maxWidth)
                return new Bitmap(source);

            // Tính toán tỷ lệ thu nhỏ
            double scale = (double)maxWidth / source.Width;

            int newWidth = maxWidth;
            int newHeight = (int)(source.Height * scale);

            // Tạo bitmap mới với kích thước đã tính
            Bitmap result = new(newWidth, newHeight);

            // Vẽ ảnh gốc vào bitmap mới với chất lượng cao
            using (Graphics g = Graphics.FromImage(result))
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;

                g.DrawImage(source, 0, 0, newWidth, newHeight);
            }

            return result;
        }

        /// <summary>
        /// Nhận dạng biển số từ hình ảnh
        /// Sử dụng processor để phân tích ảnh, phát hiện và nhận dạng biển số
        /// </summary>
        /// <param name="bitmap">Hình ảnh đầu vào cần nhận dạng</param>
        /// <returns>LprResult chứa kết quả nhận dạng hoặc thất bại</returns>
        public LprResult Recognize(Bitmap bitmap)
        {
            // Tạo processor nếu chưa tồn tại
            CreateProcessor();

            // Thu nhỏ ảnh để tối ưu hóa performance (1960 là chiều rộng tiêu chuẩn)
            using Bitmap resized = ResizeBitmap(bitmap, 960);

            // Phân tích ảnh để phát hiện biển số
            List<Candidate> candidates = _processor.analyze(resized);

            // Lấy ứng viên tốt nhất từ danh sách
            Candidate? candidate = GetBestCandidate(candidates);

            // Nếu không tìm thấy biển số, trả về kết quả rỗng
            if (!candidate.HasValue)
                return new LprResult();

            // Lấy kết quả nhận dạng tốt nhất
            CountryMatch match = candidate.Value.matches[0];

            // Trả về kết quả nhận dạng đầy đủ
            return new LprResult
            {
                Success = true,
                // Làm sạch biển số: bỏ khoảng trắng, dấu gạch, dấu chấm
                Plate = match.text
                .Replace(" ", "")
                .Replace("-", "")
                .Replace(".", "")
                .ToUpperInvariant(),
                Confidence = match.confidence,
                PlateImage = CropPlate(resized, candidate.Value),
                FullImage = new Bitmap(resized),
                RecognizedTime = DateTime.Now
            };
        }
    }
}
