using System.Windows.Forms;

namespace HPParking.UI
{
    public class VehicleUI
    {
        public Label LblFullName { get; set; } = new();
        public Label LblIdentityCard { get; set; } = new();
        public Label LblCardId
        {
            get => LblIdentityCard;
            set => LblIdentityCard = value;
        }
        public Label LblTimeIn { get; set; } = new();
        public Label LblTimeOut { get; set; } = new();
        public Label LblPlateRegistered { get; set; } = new();
        public Label LblPlateDetected { get; set; } = new();
        public Label LblDepartment { get; set; } = new();

        // PictureBox
        public PictureBox PicPlate { get; set; } = new();
        public PictureBox PicAvatar { get; set; } = new();

        public PictureBox PicPlateIn
        {
            get => PicPlate;
            set => PicPlate = value;
        }

        public PictureBox PicPlateOut
        {
            get => PicAvatar;
            set => PicAvatar = value;
        }
    }

    public class InfoUI
    {
        public VehicleUI Moto { get; set; } = new();

        public VehicleUI Car { get; set; } = new();
    }
}