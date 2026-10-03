using System;
using System.Drawing;

namespace HPParking.Models
{
    /// <summary>
    /// Đóng gói ảnh đa camera thu thập từ làn tại một thời điểm
    /// </summary>
    public sealed class CapturedLaneImages : IDisposable
    {
        public Bitmap? Overview { get; set; }
        public Bitmap? Plate { get; set; }
        public Bitmap? Face { get; set; }

        public void Dispose()
        {
            Overview?.Dispose();
            Plate?.Dispose();
            Face?.Dispose();
            Overview = null;
            Plate = null;
            Face = null;
        }
    }
}
