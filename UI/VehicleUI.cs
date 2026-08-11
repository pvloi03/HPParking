using System.Windows.Forms;

namespace HPParking.UI
{
    public class VehicleUI
    {
        // Label
        public Label LblFullName { get; set; }
        public Label LblCardId { get; set; }
        public Label LblTimeIn { get; set; }
        public Label LblTimeOut { get; set; }
        public Label LblPlateRegistered { get; set; }
        public Label LblPlateDetected { get; set; }
        public Label LblDepartment { get; set; }

        // PictureBox
        public PictureBox PicPlateIn { get; set; }
        public PictureBox PicPlateOut { get; set; }
    }

    public class InfoUI
    {
        public VehicleUI Moto { get; set; }

        public VehicleUI Car { get; set; }
    }
}