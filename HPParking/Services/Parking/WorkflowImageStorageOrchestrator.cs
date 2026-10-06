using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Interfaces;
using HPParking.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;

namespace HPParking.Services.Parking
{
    /// <summary>
    /// Triển khai điều phối lưu trữ ảnh quy trình ngầm ra ổ đĩa và cập nhật phiên đỗ xe (Deep Module)
    /// </summary>
    public class WorkflowImageStorageOrchestrator : IWorkflowImageStorageOrchestrator
    {
        private readonly IImageStorageService _imageStorageService;
        private readonly IRepository<ParkingSession>? _sessionRepository;
        private readonly ILogger? _logger;

        public WorkflowImageStorageOrchestrator(
            IImageStorageService imageStorageService,
            IRepository<ParkingSession>? sessionRepository = null,
            ILogger? logger = null)
        {
            _imageStorageService = imageStorageService ?? throw new ArgumentNullException(nameof(imageStorageService));
            _sessionRepository = sessionRepository;
            _logger = logger;
        }

        public WorkflowImageStorageOrchestrator(
            IImageStorageService imageStorageService,
            IRepository<ParkingSession>? sessionRepository)
            : this(imageStorageService, sessionRepository, null)
        {
        }

        public void SaveSessionImagesBackground(
            ParkingSession? session,
            CapturedLaneImages? images,
            bool isEntry,
            string imageBasePath,
            Action<string, string, string>? onSaved = null,
            Client? client = null)
        {
            Bitmap? plateSave = SafeClone(images?.Plate);
            Bitmap? overviewSave = SafeClone(images?.Overview);
            Bitmap? faceSave = SafeClone(images?.Face);

            string folder = isEntry ? "ImageIn" : "ImageOut";

            _ = Task.Run(async () =>
            {
                try
                {
                    using (plateSave)
                    using (overviewSave)
                    using (faceSave)
                    {
                        string pPath = plateSave != null
                            ? _imageStorageService.SaveImage(plateSave, folder, "BienSo", imageBasePath)
                            : "";
                        string oPath = overviewSave != null
                            ? _imageStorageService.SaveImage(overviewSave, folder, "ToanCanh", imageBasePath)
                            : "";
                        string fPath = faceSave != null
                            ? _imageStorageService.SaveImage(faceSave, folder, "KhuonMat", imageBasePath)
                            : "";

                        if (string.IsNullOrEmpty(fPath) && client != null && !string.IsNullOrWhiteSpace(client.Avatar))
                        {
                            fPath = client.Avatar.StartsWith("Avatar", StringComparison.OrdinalIgnoreCase)
                                ? client.Avatar
                                : $"Avatar/{client.Avatar}".Replace('\\', '/');
                        }

                        if (session != null)
                        {
                            if (isEntry)
                            {
                                session.InPlateImagePath = pPath;
                                session.InOverviewImagePath = oPath;
                                session.InFaceImagePath = fPath;
                            }
                            else
                            {
                                session.OutPlateImagePath = pPath;
                                session.OutOverviewImagePath = oPath;
                                session.OutFaceImagePath = fPath;
                            }
                            session.UpdatedAt = DateTime.UtcNow;

                            if (!string.IsNullOrEmpty(session.Id) && _sessionRepository != null)
                            {
                                await _sessionRepository.UpdateAsync(session);
                            }
                        }

                        onSaved?.Invoke(pPath, oPath, fPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Lỗi khi lưu ảnh nền quy trình xe");
                    Debug.WriteLine($"[WorkflowImageStorageOrchestrator Error] {ex.Message}");
                }
            });
        }

        private static Bitmap? SafeClone(Bitmap? src)
        {
            if (src == null) return null;
            try
            {
                return (Bitmap)src.Clone();
            }
            catch
            {
                return null;
            }
        }
    }
}
