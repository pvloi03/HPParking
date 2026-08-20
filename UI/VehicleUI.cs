using System.Windows.Forms;

namespace HPParking.UI
{
    public class VehicleUI
    {
        // Label
        public Label LblFullName { get; set; } = new();
        public Label LblCardId { get; set; } = new();
        public Label LblTimeIn { get; set; } = new();
        public Label LblTimeOut { get; set; } = new();
        public Label LblPlateRegistered { get; set; } = new();
        public Label LblPlateDetected { get; set; } = new();
        public Label LblDepartment { get; set; } = new();

        // PictureBox
        public PictureBox PicPlateIn { get; set; } = new();
        public PictureBox PicPlateOut { get; set; } = new();
    }

    public class InfoUI
    {
        public VehicleUI Moto { get; set; } = new();

        public VehicleUI Car { get; set; } = new();
    }
}