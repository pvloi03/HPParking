using FluentAssertions;
using HPParking.Api.Services.Background;
using HPParking.Api.Services.Implementations;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Data;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MimeKit;
using NSubstitute;
using System.IO;
using Xunit;

namespace HPParking.Tests.Api
{
    public class EmailAlertImageTests
    {
        [Fact]
        public async Task CheckOverdueTripsAsync_WhenTripHasCheckpointImage_SendsAttachmentWithCustomDisplayName()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "hpparking_test_" + Guid.NewGuid().ToString("N"));
            var relPath = "Captures/ImageIn/2026-09-30/ToanCanh/overview.jpg";
            var fullFilePath = Path.Combine(tempDir, relPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullFilePath)!);
            await File.WriteAllBytesAsync(fullFilePath, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

            try
            {
                var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
                var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
                var gateRepo = Substitute.For<IRepository<Gate>>();
                var emailSender = Substitute.For<IEmailSenderService>();
                var logger = Substitute.For<ILogger<VehicleTransitWatcherService>>();

                var defaultRoute = new GateRouteConfig
                {
                    Id = "defaultRouteId",
                    RouteCode = "DEFAULT",
                    RouteName = "Tuyến mặc định",
                    IsDefault = true,
                    AlertEmails = new List<string> { "operator@hpparking.local" }
                };
                routeRepo.FindOneAsync(Arg.Any<System.Linq.Expressions.Expression<Func<GateRouteConfig, bool>>>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult<GateRouteConfig?>(defaultRoute));

                var serviceProvider = Substitute.For<IServiceProvider>();
                serviceProvider.GetService(typeof(IRepository<VehicleDispatchTrip>)).Returns(tripRepo);
                serviceProvider.GetService(typeof(IRepository<GateRouteConfig>)).Returns(routeRepo);
                serviceProvider.GetService(typeof(IRepository<Gate>)).Returns(gateRepo);
                serviceProvider.GetService(typeof(IEmailSenderService)).Returns(emailSender);
                var config = Substitute.For<IConfiguration>();
                config["FleetSettings:AlertEmails"].Returns("operator@hpparking.local");
                config["StorageSettings:RootPath"].Returns(tempDir);
                serviceProvider.GetService(typeof(IConfiguration)).Returns(config);

                var scope = Substitute.For<IServiceScope>();
                scope.ServiceProvider.Returns(serviceProvider);
                var scopeFactory = Substitute.For<IServiceScopeFactory>();
                scopeFactory.CreateScope().Returns(scope);

                var now = DateTime.UtcNow;
                var overdueTrip = new VehicleDispatchTrip
                {
                    Id = "trip_img_1",
                    VehicleId = "veh_img_1",
                    PlateNumber = "29B-12345",
                    CardNumber = "CARD-001",
                    OriginGateId = "gate_1",
                    CurrentGateId = "gate_1",
                    Status = TripStatus.InTransit,
                    StartTime = now.AddMinutes(-40),
                    LastExitTime = now.AddMinutes(-40),
                    NextDeadline = now.AddMinutes(-10),
                    IsAlertSent = false,
                    Checkpoints = new List<TripCheckpoint>
                    {
                        new TripCheckpoint
                        {
                            GateId = "gate_1",
                            Timestamp = now.AddMinutes(-40),
                            OverviewImagePath = relPath
                        }
                    }
                };

                tripRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<VehicleDispatchTrip, bool>>>())
                    .Returns(Task.FromResult<IReadOnlyList<VehicleDispatchTrip>>(new List<VehicleDispatchTrip> { overdueTrip }));

                string? capturedAttachmentPath = null;
                string? capturedDisplayName = null;
                string capturedHtmlBody = string.Empty;

                await emailSender.SendEmailAsync(
                    Arg.Any<IEnumerable<string>>(),
                    Arg.Any<string>(),
                    Arg.Do<string>(body => capturedHtmlBody = body),
                    Arg.Do<string?>(att => capturedAttachmentPath = att),
                    Arg.Do<string?>(name => capturedDisplayName = name),
                    Arg.Any<CancellationToken>());

                var watcher = new VehicleTransitWatcherService(scopeFactory, logger);

                // Act
                await watcher.CheckOverdueTripsAsync(CancellationToken.None);

                // Assert
                capturedAttachmentPath.Should().NotBeNullOrWhiteSpace();
                File.Exists(capturedAttachmentPath).Should().BeTrue("Attachment path must be resolved to an existing physical file");
                capturedDisplayName.Should().Be("AnhGiamSat_29B12345.jpg", "Attachment must be renamed with vehicle plate number");
                capturedHtmlBody.Should().Contain("Ảnh giám sát:", "Notice should appear when attachment exists");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public async Task CheckOverdueTripsAsync_WhenOverviewMissingOnDisk_FallsBackToPlateImage()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "hpparking_test_" + Guid.NewGuid().ToString("N"));
            var plateRelPath = "Captures/ImageIn/2026-09-30/BienSo/plate.jpg";
            var fullPlatePath = Path.Combine(tempDir, plateRelPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPlatePath)!);
            await File.WriteAllBytesAsync(fullPlatePath, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

            try
            {
                var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
                var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
                var gateRepo = Substitute.For<IRepository<Gate>>();
                var emailSender = Substitute.For<IEmailSenderService>();
                var logger = Substitute.For<ILogger<VehicleTransitWatcherService>>();

                var defaultRoute = new GateRouteConfig
                {
                    Id = "defaultRouteId",
                    RouteCode = "DEFAULT",
                    RouteName = "Tuyến mặc định",
                    IsDefault = true,
                    AlertEmails = new List<string> { "operator@hpparking.local" }
                };
                routeRepo.FindOneAsync(Arg.Any<System.Linq.Expressions.Expression<Func<GateRouteConfig, bool>>>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult<GateRouteConfig?>(defaultRoute));

                var serviceProvider = Substitute.For<IServiceProvider>();
                serviceProvider.GetService(typeof(IRepository<VehicleDispatchTrip>)).Returns(tripRepo);
                serviceProvider.GetService(typeof(IRepository<GateRouteConfig>)).Returns(routeRepo);
                serviceProvider.GetService(typeof(IRepository<Gate>)).Returns(gateRepo);
                serviceProvider.GetService(typeof(IEmailSenderService)).Returns(emailSender);
                var config = Substitute.For<IConfiguration>();
                config["FleetSettings:AlertEmails"].Returns("operator@hpparking.local");
                config["StorageSettings:RootPath"].Returns(tempDir);
                serviceProvider.GetService(typeof(IConfiguration)).Returns(config);

                var scope = Substitute.For<IServiceScope>();
                scope.ServiceProvider.Returns(serviceProvider);
                var scopeFactory = Substitute.For<IServiceScopeFactory>();
                scopeFactory.CreateScope().Returns(scope);

                var now = DateTime.UtcNow;
                var overdueTrip = new VehicleDispatchTrip
                {
                    Id = "trip_img_2",
                    VehicleId = "veh_img_2",
                    PlateNumber = "30A-99999",
                    CardNumber = "CARD-002",
                    OriginGateId = "gate_1",
                    CurrentGateId = "gate_1",
                    Status = TripStatus.InTransit,
                    StartTime = now.AddMinutes(-40),
                    LastExitTime = now.AddMinutes(-40),
                    NextDeadline = now.AddMinutes(-10),
                    IsAlertSent = false,
                    Checkpoints = new List<TripCheckpoint>
                    {
                        new TripCheckpoint
                        {
                            GateId = "gate_1",
                            Timestamp = now.AddMinutes(-40),
                            OverviewImagePath = "Captures/ImageIn/2026-09-30/ToanCanh/non_existent.jpg", // Missing on disk
                            PlateImagePath = plateRelPath // Exists on disk
                        }
                    }
                };

                tripRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<VehicleDispatchTrip, bool>>>())
                    .Returns(Task.FromResult<IReadOnlyList<VehicleDispatchTrip>>(new List<VehicleDispatchTrip> { overdueTrip }));

                string? capturedAttachmentPath = null;
                string? capturedDisplayName = null;

                await emailSender.SendEmailAsync(
                    Arg.Any<IEnumerable<string>>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Do<string?>(att => capturedAttachmentPath = att),
                    Arg.Do<string?>(name => capturedDisplayName = name),
                    Arg.Any<CancellationToken>());

                var watcher = new VehicleTransitWatcherService(scopeFactory, logger);

                // Act
                await watcher.CheckOverdueTripsAsync(CancellationToken.None);

                // Assert
                capturedAttachmentPath.Should().NotBeNullOrWhiteSpace();
                capturedAttachmentPath.Should().Be(fullPlatePath, "Should fallback to PlateImagePath when Overview is missing on disk");
                capturedDisplayName.Should().Be("AnhGiamSat_30A99999.jpg");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public async Task CheckOverdueTripsAsync_WhenNoImageOnDisk_HidesNoticeAndDoesNotAttach()
        {
            // Arrange
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var emailSender = Substitute.For<IEmailSenderService>();
            var logger = Substitute.For<ILogger<VehicleTransitWatcherService>>();

            var defaultRoute = new GateRouteConfig
            {
                Id = "defaultRouteId",
                RouteCode = "DEFAULT",
                RouteName = "Tuyến mặc định",
                IsDefault = true,
                AlertEmails = new List<string> { "operator@hpparking.local" }
            };
            routeRepo.FindOneAsync(Arg.Any<System.Linq.Expressions.Expression<Func<GateRouteConfig, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<GateRouteConfig?>(defaultRoute));

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IRepository<VehicleDispatchTrip>)).Returns(tripRepo);
            serviceProvider.GetService(typeof(IRepository<GateRouteConfig>)).Returns(routeRepo);
            serviceProvider.GetService(typeof(IRepository<Gate>)).Returns(gateRepo);
            serviceProvider.GetService(typeof(IEmailSenderService)).Returns(emailSender);
            var config = Substitute.For<IConfiguration>();
            config["FleetSettings:AlertEmails"].Returns("operator@hpparking.local");
            config["StorageSettings:RootPath"].Returns(@"C:\empty_test_path_" + Guid.NewGuid().ToString("N"));
            serviceProvider.GetService(typeof(IConfiguration)).Returns(config);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(serviceProvider);
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);

            var now = DateTime.UtcNow;
            var overdueTrip = new VehicleDispatchTrip
            {
                Id = "trip_no_img",
                VehicleId = "veh_no_img",
                PlateNumber = "29C-55555",
                CardNumber = "CARD-003",
                OriginGateId = "gate_1",
                CurrentGateId = "gate_1",
                Status = TripStatus.InTransit,
                StartTime = now.AddMinutes(-40),
                LastExitTime = now.AddMinutes(-40),
                NextDeadline = now.AddMinutes(-10),
                IsAlertSent = false,
                Checkpoints = new List<TripCheckpoint>
                {
                    new TripCheckpoint
                    {
                        GateId = "gate_1",
                        Timestamp = now.AddMinutes(-40),
                        OverviewImagePath = "Captures/non_existent.jpg"
                    }
                }
            };

            tripRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<VehicleDispatchTrip, bool>>>())
                .Returns(Task.FromResult<IReadOnlyList<VehicleDispatchTrip>>(new List<VehicleDispatchTrip> { overdueTrip }));

            string? capturedAttachmentPath = null;
            string capturedHtmlBody = string.Empty;

            await emailSender.SendEmailAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string>(),
                Arg.Do<string>(body => capturedHtmlBody = body),
                Arg.Do<string?>(att => capturedAttachmentPath = att),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>());

            var watcher = new VehicleTransitWatcherService(scopeFactory, logger);

            // Act
            await watcher.CheckOverdueTripsAsync(CancellationToken.None);

            // Assert
            capturedAttachmentPath.Should().BeNull();
            capturedHtmlBody.Should().NotContain("Ảnh giám sát:", "Notice must be hidden when no image file exists on disk");
        }

        [Fact]
        public void EmailSenderService_CreateMimeMessage_ResolvesRelativePathAndSetsCustomDisplayName()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "hpparking_email_" + Guid.NewGuid().ToString("N"));
            var relPath = "Captures/ImageIn/2026-09-30/ToanCanh/sample.jpg";
            var fullFilePath = Path.Combine(tempDir, relPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullFilePath)!);
            File.WriteAllBytes(fullFilePath, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

            try
            {
                var settings = new Dictionary<string, string?>
                {
                    { "StorageSettings:RootPath", tempDir },
                    { "Smtp:FromEmail", "alerts@hpparking.local" },
                    { "Smtp:FromName", "HPParking Alert System" }
                };
                var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
                var logger = Substitute.For<ILogger<EmailSenderService>>();
                var service = new EmailSenderService(config, logger);

                var message = service.CreateMimeMessage(
                    new[] { "admin@hpparking.local" },
                    "[CẢNH BÁO SLA] Xe 30A-12345",
                    "<html><body><p>Nội dung cảnh báo</p></body></html>",
                    relPath,
                    "AnhGiamSat_30A12345.jpg");

                message.Should().NotBeNull();
                message.Subject.Should().Be("[CẢNH BÁO SLA] Xe 30A-12345");
                message.To.Mailboxes.Should().ContainSingle(m => m.Address == "admin@hpparking.local");

                var multipart = message.Body as Multipart;
                multipart.Should().NotBeNull();

                var attachmentPart = multipart!.OfType<MimePart>().FirstOrDefault(p => p.IsAttachment);
                attachmentPart.Should().NotBeNull("Attachment must be present in MimeMessage");
                attachmentPart!.FileName.Should().Be("AnhGiamSat_30A12345.jpg");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
    }
}
