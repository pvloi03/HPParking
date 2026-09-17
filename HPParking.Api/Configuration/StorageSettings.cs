using System.Collections.Generic;

namespace HPParking.Api.Configuration
{
    public class StorageSettings
    {
        public string UploadPath { get; set; } = "Uploads/Avatars";
        public string RequestPath { get; set; } = "/uploads/avatars";
        public long MaxSizeBytes { get; set; } = 5242880; // 5MB
        public List<string> AllowedExtensions { get; set; } = new() { ".jpg", ".jpeg", ".png", ".webp" };
    }
}
