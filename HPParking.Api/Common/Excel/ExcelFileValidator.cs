using HPParking.Api.Common.Exceptions;

namespace HPParking.Api.Common.Excel
{
    /// <summary>
    /// Bộ xác thực định dạng và dung lượng tệp Excel (.xlsx) chuẩn hóa (ADR 0023)
    /// </summary>
    public static class ExcelFileValidator
    {
        /// <summary>
        /// Giới hạn dung lượng tệp Excel tối đa cho phép tải lên (10MB theo tiêu chuẩn Issue #23)
        /// </summary>
        public const long DefaultMaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

        /// <summary>
        /// Xác thực tính hợp lệ của IFormFile tải lên từ HTTP Request
        /// </summary>
        public static void Validate(IFormFile file, long maxBytes = DefaultMaxFileSizeBytes)
        {
            if (file == null || file.Length == 0)
            {
                throw new BadRequestException("File Excel tải lên rỗng hoặc không có dữ liệu.", ErrorCodes.EXCEL_EMPTY_FILE);
            }

            if (file.Length > maxBytes)
            {
                throw new BadRequestException($"Kích thước file Excel vượt quá giới hạn cho phép (tối đa {maxBytes / (1024 * 1024)}MB).", ErrorCodes.EXCEL_FILE_SIZE_EXCEEDED);
            }

            var ext = Path.GetExtension(file.FileName);
            if (!string.IsNullOrWhiteSpace(ext) && !string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException("Định dạng file không hợp lệ. Chỉ chấp nhận định dạng Excel (.xlsx).", ErrorCodes.EXCEL_INVALID_FILE_FORMAT);
            }

            using var stream = file.OpenReadStream();
            ValidateStreamHeader(stream);
        }

        /// <summary>
        /// Xác thực tính hợp lệ của luồng dữ liệu Stream
        /// </summary>
        public static void Validate(Stream stream, long maxBytes = DefaultMaxFileSizeBytes)
        {
            if (stream == null || stream.Length == 0)
            {
                throw new BadRequestException("File Excel tải lên rỗng hoặc không có dữ liệu.", ErrorCodes.EXCEL_EMPTY_FILE);
            }

            if (stream.Length > maxBytes)
            {
                throw new BadRequestException($"Kích thước file Excel vượt quá giới hạn cho phép (tối đa {maxBytes / (1024 * 1024)}MB).", ErrorCodes.EXCEL_FILE_SIZE_EXCEEDED);
            }

            ValidateStreamHeader(stream);
        }

        private static void ValidateStreamHeader(Stream stream)
        {
            var position = stream.CanSeek ? stream.Position : 0;
            var header = new byte[4];
            var bytesRead = stream.Read(header, 0, 4);

            if (stream.CanSeek)
            {
                stream.Position = position;
            }

            // Kiểm tra ZIP PK magic bytes (0x50, 0x4B, 0x03, 0x04)
            if (bytesRead < 4 || header[0] != 0x50 || header[1] != 0x4B || header[2] != 0x03 || header[3] != 0x04)
            {
                throw new BadRequestException("Định dạng file không hợp lệ. Chỉ chấp nhận định dạng Excel (.xlsx).", ErrorCodes.EXCEL_INVALID_FILE_FORMAT);
            }
        }
    }
}
