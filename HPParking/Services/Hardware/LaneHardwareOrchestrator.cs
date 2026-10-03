using HPParking.Core.Helpers;
using HPParking.Interfaces;
using HPParking.Models;
using HPParking.Services.Controller;
using HPParking.Services.LPR;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace HPParking.Services.Hardware
{
    /// <summary>
    /// Triển khai điều phối thiết bị phần cứng làn xe: Camera, LPR OCR, Relay Barrier
    /// </summary>
    public class LaneHardwareOrchestrator(ILprService lprService) : ILaneHardwareOrchestrator
    {
        private readonly ILprService _lprService = lprService;

        public async Task<CapturedLaneImages> CaptureLaneImagesAsync(
            LaneRuntimeContext context,
            bool needOverview = true,
            bool needPlate = true,
            bool needFace = false,
            int timeoutMs = 2500)
        {
            var result = new CapturedLaneImages();
            if (context.Cameras == null) return result;

            var plateTask = (needPlate && context.Cameras.LicensePlateCamera != null)
                ? Task.Run(() => { try { return context.Cameras.LicensePlateCamera.Capture(); } catch { return null; } })
                : Task.FromResult<Bitmap?>(null);

            var overviewTask = (needOverview && context.Cameras.OverviewCamera != null)
                ? Task.Run(() => { try { return context.Cameras.OverviewCamera.Capture(); } catch { return null; } })
                : Task.FromResult<Bitmap?>(null);

            var faceTask = (needFace && context.Cameras.FaceCamera != null)
                ? Task.Run(() => { try { return context.Cameras.FaceCamera.Capture(); } catch { return null; } })
                : Task.FromResult<Bitmap?>(null);

            var allTasks = Task.WhenAll(plateTask, overviewTask, faceTask);
            var timeoutTask = Task.Delay(timeoutMs);

            await Task.WhenAny(allTasks, timeoutTask);

            if (plateTask.IsCompletedSuccessfully) result.Plate = plateTask.Result;
            else SafelyDisposeTaskResult(plateTask);

            if (overviewTask.IsCompletedSuccessfully) result.Overview = overviewTask.Result;
            else SafelyDisposeTaskResult(overviewTask);

            if (faceTask.IsCompletedSuccessfully) result.Face = faceTask.Result;
            else SafelyDisposeTaskResult(faceTask);

            return result;
        }

        public (Bitmap? SmallPlate, Bitmap? FaceSnap, Bitmap? OverviewSnap) ExtractWorkflowImages(
            CapturedLaneImages images,
            LprResult? lprResult)
        {
            Bitmap? smallPlate = lprResult?.PlateImage != null
                ? (Bitmap)lprResult.PlateImage.Clone()
                : (images.Plate != null ? (Bitmap)images.Plate.Clone() : null);
            Bitmap? faceSnap = images.Face != null ? (Bitmap)images.Face.Clone() : null;
            Bitmap? overviewSnap = images.Overview != null ? (Bitmap)images.Overview.Clone() : null;

            images.Dispose();
            return (smallPlate, faceSnap, overviewSnap);
        }

        public async Task<(bool Success, string DetectedPlate, LprResult? LprResult)> RecognizePlateAsync(
            LaneRuntimeContext context,
            Bitmap? plateImage,
            string defaultPlate,
            Func<LaneRuntimeContext, string?, Task<string?>>? onManualPlateInput = null)
        {
            string recognizedPlate = string.Empty;
            LprResult? lprResult = null;

            if (plateImage != null)
            {
                lprResult = await Task.Run(() => _lprService.Recognize(plateImage));
                if (lprResult != null && lprResult.Success && !string.IsNullOrWhiteSpace(lprResult.Plate))
                {
                    recognizedPlate = lprResult.Plate.Trim().ToUpper();
                }
            }

            if (string.IsNullOrEmpty(recognizedPlate) && onManualPlateInput != null)
            {
                string? manual = await onManualPlateInput(context, defaultPlate);
                if (!string.IsNullOrWhiteSpace(manual))
                {
                    recognizedPlate = manual.Trim().ToUpper();
                    lprResult = new LprResult
                    {
                        Success = true,
                        Plate = recognizedPlate,
                        PlateImage = plateImage != null ? (Bitmap)plateImage.Clone() : null
                    };
                }
            }
            else if (string.IsNullOrEmpty(recognizedPlate) && !string.IsNullOrWhiteSpace(defaultPlate))
            {
                recognizedPlate = defaultPlate.Trim().ToUpper();
                lprResult = new LprResult
                {
                    Success = true,
                    Plate = recognizedPlate,
                    PlateImage = plateImage != null ? (Bitmap)plateImage.Clone() : null
                };
            }

            bool success = !string.IsNullOrEmpty(recognizedPlate);
            return (success, recognizedPlate, lprResult);
        }

        public string NormalizePlate(string? plate)
        {
            return PlateHelper.Normalize(plate);
        }

        public bool TryOpenBarrier(LaneRuntimeContext context, Func<LaneRuntimeContext, bool>? onBarrierOpenFailed = null)
        {
            if (context.OpenBarrier()) return true;
            return onBarrierOpenFailed?.Invoke(context) ?? false;
        }

        private static void SafelyDisposeTaskResult(Task<Bitmap?> task)
        {
            _ = task.ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully && t.Result != null)
                {
                    t.Result.Dispose();
                }
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }
    }
}
