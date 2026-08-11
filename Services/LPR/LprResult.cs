using System;
using System.Drawing;

namespace HPParking.Services.LPR
{
    public class LprResult
    {
        public bool Success { get; set; }
        public string Plate { get; set; }

        public float Confidence { get; set; }

        public Bitmap PlateImage { get; set; }
        public Bitmap FullImage { get; set; }
        public DateTime RecognizedTime { get; set; }
    }
}
