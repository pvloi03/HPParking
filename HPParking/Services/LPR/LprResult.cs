using System;
using System.Drawing;

namespace HPParking.Services.LPR
{
    public class LprResult : IDisposable
    {
        public bool Success { get; set; }
        public string? Plate { get; set; }
        public string? RawPlateText { get; set; }
        public bool IsManualOverride { get; set; } = false;

        public float Confidence { get; set; }

        public Bitmap? PlateImage { get; set; }
        public Bitmap? FullImage { get; set; }
        public DateTime RecognizedTime { get; set; }

        public void Dispose()
        {
            PlateImage?.Dispose();
            PlateImage = null;

            FullImage?.Dispose();
            FullImage = null;
        }
    }
}
