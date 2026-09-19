using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.Configuration;
using HPParking.Api.Services.Implementations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class LocalFileStorageServiceTests : IDisposable
    {
        private readonly string _testTempDir;
        private readonly IWebHostEnvironment _environment = Substitute.For<IWebHostEnvironment>();
        private readonly ILogger<LocalFileStorageService> _logger = Substitute.For<ILogger<LocalFileStorageService>>();
        private readonly IOptions<StorageSettings> _options;
        private readonly LocalFileStorageService _storageService;

        public LocalFileStorageServiceTests()
        {
            _testTempDir = Path.Combine(Path.GetTempPath(), "HPParking_StorageTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testTempDir);

            _environment.ContentRootPath.Returns(_testTempDir);

            _options = Options.Create(new StorageSettings
            {
                UploadPath = "Uploads/Avatar",
                RequestPath = "/uploads/avatar",
                MaxSizeBytes = 5 * 1024 * 1024, // 5MB
                AllowedExtensions = new() { ".jpg", ".jpeg", ".png", ".webp" }
            });

            _storageService = new LocalFileStorageService(_environment, _options, _logger);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testTempDir))
                {
                    Directory.Delete(_testTempDir, true);
                }
            }
            catch
            {
                // Ignore cleanup error in test
            }
        }

        private static IFormFile CreateMockFormFile(string fileName, byte[] content)
        {
            var stream = new MemoryStream(content);
            var file = Substitute.For<IFormFile>();
            file.FileName.Returns(fileName);
            file.Length.Returns(content.Length);
            file.OpenReadStream().Returns(stream);
            file.CopyToAsync(Arg.Any<Stream>()).Returns(callInfo =>
            {
                var targetStream = callInfo.Arg<Stream>();
                return stream.CopyToAsync(targetStream);
            });
            return file;
        }

        [Fact]
        public async Task SaveAvatarAsync_WithBaseFileName_SavesWithCustomName()
        {
            var imageContent = Encoding.UTF8.GetBytes("fake-jpeg-image-bytes");
            var mockFile = CreateMockFormFile("camera_upload.jpg", imageContent);

            var relativeUrl = await _storageService.SaveAvatarAsync(mockFile, baseFileName: "001200001234");

            relativeUrl.Should().Be("/uploads/avatar/001200001234.jpg");

            var readBytes = await _storageService.ReadFileBytesAsync(relativeUrl);
            readBytes.Should().Equal(imageContent);
        }

        [Fact]
        public async Task SaveAvatarAsync_WithoutBaseFileName_GeneratesGuidFileName()
        {
            var imageContent = Encoding.UTF8.GetBytes("fake-jpeg-image-bytes");
            var mockFile = CreateMockFormFile("photo.png", imageContent);

            var relativeUrl = await _storageService.SaveAvatarAsync(mockFile);

            relativeUrl.Should().StartWith("/uploads/avatar/");
            relativeUrl.Should().EndWith(".png");

            var readBytes = await _storageService.ReadFileBytesAsync(relativeUrl);
            readBytes.Should().Equal(imageContent);
        }

        [Fact]
        public async Task SaveAvatarAsync_EmptyFile_ThrowsBadRequestException()
        {
            var mockFile = CreateMockFormFile("empty.jpg", Array.Empty<byte>());
            mockFile.Length.Returns(0);

            var act = () => _storageService.SaveAvatarAsync(mockFile);

            await act.Should().ThrowAsync<BadRequestException>()
                .Where(e => e.ErrorCode == ErrorCodes.BAD_REQUEST);
        }

        [Fact]
        public async Task SaveAvatarAsync_FileSizeExceeded_ThrowsBadRequestException()
        {
            var mockFile = Substitute.For<IFormFile>();
            mockFile.FileName.Returns("big.jpg");
            mockFile.Length.Returns(10 * 1024 * 1024); // 10MB > 5MB

            var act = () => _storageService.SaveAvatarAsync(mockFile);

            await act.Should().ThrowAsync<BadRequestException>()
                .Where(e => e.ErrorCode == ErrorCodes.FILE_SIZE_EXCEEDED);
        }

        [Fact]
        public async Task SaveAvatarAsync_InvalidExtension_ThrowsBadRequestException()
        {
            var content = Encoding.UTF8.GetBytes("executable file");
            var mockFile = CreateMockFormFile("malware.exe", content);

            var act = () => _storageService.SaveAvatarAsync(mockFile);

            await act.Should().ThrowAsync<BadRequestException>()
                .Where(e => e.ErrorCode == ErrorCodes.FILE_INVALID_FORMAT);
        }

        [Fact]
        public async Task ReadFileBytesAsync_NonexistentFile_ThrowsNotFoundException()
        {
            var act = () => _storageService.ReadFileBytesAsync("/uploads/avatar/notfound.jpg");

            await act.Should().ThrowAsync<NotFoundException>()
                .Where(e => e.ErrorCode == ErrorCodes.NOT_FOUND);
        }

        [Fact]
        public async Task DeleteFileAsync_ExistingFile_DeletesFile_AndReturnsTrue()
        {
            var imageContent = Encoding.UTF8.GetBytes("image-content-to-delete");
            var mockFile = CreateMockFormFile("test_delete.png", imageContent);

            var relativeUrl = await _storageService.SaveAvatarAsync(mockFile, baseFileName: "client_999");
            var deleteResult = await _storageService.DeleteFileAsync(relativeUrl);

            deleteResult.Should().BeTrue();

            var readAct = () => _storageService.ReadFileBytesAsync(relativeUrl);
            await readAct.Should().ThrowAsync<NotFoundException>();
        }
    }
}
