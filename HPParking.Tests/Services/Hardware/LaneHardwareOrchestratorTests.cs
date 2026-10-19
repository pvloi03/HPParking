using HPParking.Core.Models.Entities;
using HPParking.Interfaces;
using HPParking.Models;
using HPParking.Services.Hardware;
using HPParking.Services.LPR;
using NSubstitute;
using System.Drawing;
using Xunit;

namespace HPParking.Tests.Services.Hardware
{
    public class LaneHardwareOrchestratorTests
    {
        private readonly ILprService _mockLprService;
        private readonly LaneHardwareOrchestrator _orchestrator;

        public LaneHardwareOrchestratorTests()
        {
            _mockLprService = Substitute.For<ILprService>();
            _orchestrator = new LaneHardwareOrchestrator(_mockLprService);
        }

        [Theory]
        [InlineData("29A-123.45", "29A12345")]
        [InlineData(" 30b - 999.88 ", "30B99988")]
        [InlineData("51F 11122", "51F11122")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void NormalizePlate_ShouldNormalizeConsistently(string? input, string expected)
        {
            var result = _orchestrator.NormalizePlate(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task RecognizePlateAsync_WhenLprSucceeds_ShouldReturnLprPlate()
        {
            // Arrange
            using var bmp = new Bitmap(10, 10);
            _mockLprService.Recognize(Arg.Any<Bitmap>())
                .Returns(new LprResult { Success = true, Plate = "29A-12345" });

            var context = new LaneRuntimeContext(new Lane());

            // Act
            var (success, plate, lprResult) = await _orchestrator.RecognizePlateAsync(context, bmp, "FALLBACK");

            // Assert
            Assert.True(success);
            Assert.Equal("29A-12345", plate);
            Assert.NotNull(lprResult);
        }

        [Fact]
        public async Task RecognizePlateAsync_WhenLprFails_ShouldFallbackToManualInput()
        {
            // Arrange
            using var bmp = new Bitmap(10, 10);
            _mockLprService.Recognize(Arg.Any<Bitmap>())
                .Returns(new LprResult { Success = false });

            var context = new LaneRuntimeContext(new Lane());

            // Act
            var (success, plate, lprResult) = await _orchestrator.RecognizePlateAsync(
                context, bmp, "FALLBACK",
                onManualPlateInput: (ctx, def) => Task.FromResult<string?>("MANUAL99"));

            // Assert
            Assert.True(success);
            Assert.Equal("MANUAL99", plate);
            Assert.NotNull(lprResult);
            Assert.True(lprResult.Success);
        }

        [Fact]
        public async Task RecognizePlateAsync_WhenLprFailsAndNoManual_ShouldFallbackToDefaultPlate()
        {
            // Arrange
            using var bmp = new Bitmap(10, 10);
            _mockLprService.Recognize(Arg.Any<Bitmap>())
                .Returns(new LprResult { Success = false });

            var context = new LaneRuntimeContext(new Lane());

            // Act
            var (success, plate, lprResult) = await _orchestrator.RecognizePlateAsync(context, bmp, "DEFAULT_PLATE");

            // Assert
            Assert.True(success);
            Assert.Equal("DEFAULT_PLATE", plate);
            Assert.NotNull(lprResult);
        }

        [Fact]
        public void ExtractWorkflowImages_ShouldExtractAndDisposeImagesSafely()
        {
            // Arrange
            var images = new CapturedLaneImages
            {
                Plate = new Bitmap(5, 5),
                Overview = new Bitmap(5, 5),
                Face = new Bitmap(5, 5)
            };

            // Act
            var (smallPlate, faceSnap, overviewSnap) = _orchestrator.ExtractWorkflowImages(images, null);

            // Assert
            Assert.NotNull(smallPlate);
            Assert.NotNull(faceSnap);
            Assert.NotNull(overviewSnap);

            // Clean up cloned bitmaps
            smallPlate?.Dispose();
            faceSnap?.Dispose();
            overviewSnap?.Dispose();
        }
    }
}
