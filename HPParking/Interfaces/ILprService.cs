using HPParking.Services.LPR;
using System.Drawing;

namespace HPParking.Interfaces
{
    public interface ILprService
    {
        LprResult Recognize(Bitmap bitmap);
    }
}
