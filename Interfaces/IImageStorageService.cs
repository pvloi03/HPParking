using System.Drawing;

namespace HPParking.Interfaces
{
    public interface IImageStorageService
    {
        string SaveImage(Bitmap bitmap, string type, string folder, string basePath);
    }
}