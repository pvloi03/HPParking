using HPParking.Core.Models.Entities;
using HPParking.Models;
using System;

namespace HPParking.Services.Parking
{
    /// <summary>
    /// Điều phối lưu trữ ảnh quy trình ngầm (Overview, Plate, Face) và cập nhật phiên đỗ xe
    /// </summary>
    public interface IWorkflowImageStorageOrchestrator
    {
        void SaveSessionImagesBackground(
            ParkingSession? session,
            CapturedLaneImages? images,
            bool isEntry,
            string imageBasePath,
            Action<string, string, string>? onSaved = null,
            Client? client = null);
    }
}
