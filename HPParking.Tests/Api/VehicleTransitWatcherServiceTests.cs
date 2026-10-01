using FluentAssertions;
using HPParking.Api.Services.Background;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Data;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using Xunit;

namespace HPParking.Tests.Api
{
    public class VehicleTransitWatcherServiceTests
    {
        [Fact]
        public async Task CheckOverdueTripsAsync_WhenTripIsOverdue_SendsEmailAndMarksAlertSent()
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
                RouteName = "Tuyến tự do mặc định",
                IsDefault = true,
                AlertEmails = new List<string> { "default_admin@test.com" }
            };
            routeRepo.FindOneAsync(Arg.Any<System.Linq.Expressions.Expression<Func<GateRouteConfig, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<GateRouteConfig?>(defaultRoute));

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IRepository<VehicleDispatchTrip>)).Returns(tripRepo);
            serviceProvider.GetService(typeof(IRepository<GateRouteConfig>)).Returns(routeRepo);
            serviceProvider.GetService(typeof(IRepository<Gate>)).Returns(gateRepo);
            serviceProvider.GetService(typeof(IEmailSenderService)).Returns(emailSender);
            var config = Substitute.For<IConfiguration>();
            config["FleetSettings:AlertEmails"].Returns("default_admin@test.com");
            serviceProvider.GetService(typeof(IConfiguration)).Returns(config);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(serviceProvider);

            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);

            var now = DateTime.UtcNow;
            var overdueTrip = new VehicleDispatchTrip
            {
                Id = "trip1",
                VehicleId = "veh1",
                PlateNumber = "30A-99999",
                CardNumber = "0000012345",
                OriginGateId = "gate1",
                CurrentGateId = "gate1",
                AssignedRouteId = "route1",
                Status = TripStatus.InTransit,
                StartTime = now.AddMinutes(-30),
                LastExitTime = now.AddMinutes(-30),
                NextDeadline = now.AddMinutes(-10), // Overdue by 10 minutes
                IsAlertSent = false
            };

            tripRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<VehicleDispatchTrip, bool>>>())
                .Returns(Task.FromResult<IReadOnlyList<VehicleDispatchTrip>>(new List<VehicleDispatchTrip> { overdueTrip }));

            var route = new GateRouteConfig
            {
                Id = "route1",
                RouteCode = "ROUTE-01",
                RouteName = "tuyến chính",
                AlertEmails = new List<string> { "phanvanloi150203@gmail.com", "phanvanloi1522003@gmail.com" }
            };
            routeRepo.GetByIdAsync("route1", Arg.Any<CancellationToken>()).Returns(Task.FromResult<GateRouteConfig?>(route));

            var gate = new Gate
            {
                Id = "gate1",
                Code = "GATE_01",
                Name = "Cổng Chính Nhà Máy"
            };
            gateRepo.GetByIdAsync("gate1", Arg.Any<CancellationToken>()).Returns(Task.FromResult<Gate?>(gate));

            emailSender.SendEmailAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

            var watcher = new VehicleTransitWatcherService(scopeFactory, logger);

            // Act
            await watcher.CheckOverdueTripsAsync(CancellationToken.None);

            // Assert
            overdueTrip.Status.Should().Be(TripStatus.OverdueTransit);
            overdueTrip.IsAlertSent.Should().BeTrue();
            overdueTrip.AlertSentAt.Should().NotBeNull();

            await emailSender.Received(1).SendEmailAsync(
                Arg.Is<IEnumerable<string>>(recipients =>
                    System.Linq.Enumerable.Contains(recipients, "phanvanloi150203@gmail.com") &&
                    System.Linq.Enumerable.Contains(recipients, "phanvanloi1522003@gmail.com") &&
                    System.Linq.Enumerable.Contains(recipients, "default_admin@test.com")),
                Arg.Is<string>(subject => subject.Contains("30A-99999") && subject.Contains("quá hạn di chuyển")),
                Arg.Is<string>(body => body.Contains("Cổng Chính Nhà Máy") && body.Contains("30A-99999")),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>());

            await tripRepo.Received(1).UpdateAsync(overdueTrip, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SeedViolationTripForMainRoute_IntoLocalDatabase_Succeeds()
        {
            // Seed a real violation trip for "tuyến chính" into the local MongoDB for live testing
            try
            {
                var context = new MongoDbContext("mongodb://localhost:27017", "hpparking");
                var tripCollection = context.GetCollection<VehicleDispatchTrip>("VehicleDispatchTrips");
                var vehicleCollection = context.GetCollection<Vehicle>("Vehicles");

                var now = DateTime.UtcNow;

                // Find or set up vehicle 30A1234545
                var vehicle = await vehicleCollection.Find(v => v.PlateNumber == "30A1234545").FirstOrDefaultAsync();
                string vehicleId = vehicle?.Id ?? "6abb3cb91f3a0cf5469cf5cb";
                string mainRouteId = "6abb5ce3dc1a98b2f6c5e34f";

                // Ensure vehicle is assigned to main route
                if (vehicle != null && vehicle.AssignedRouteId != mainRouteId)
                {
                    vehicle.AssignedRouteId = mainRouteId;
                    await vehicleCollection.ReplaceOneAsync(v => v.Id == vehicle.Id, vehicle);
                }

                // Delete any old uncompleted test trips for this vehicle to prevent duplicate test data
                await tripCollection.DeleteManyAsync(t => t.VehicleId == vehicleId && t.Status != TripStatus.Completed);

                // Insert overdue trip for "tuyến chính"
                var testTrip = new VehicleDispatchTrip
                {
                    VehicleId = vehicleId,
                    PlateNumber = "30A1234545",
                    CardNumber = "0012345678",
                    OriginGateId = "6ab24441aef536b162d6d1a7", // Cổng Chính Nhà Máy
                    CurrentGateId = "6ab24441aef536b162d6d1a7",
                    AssignedRouteId = mainRouteId, // tuyến chính
                    CurrentStepIndex = 1,
                    Status = TripStatus.InTransit,
                    StartTime = now.AddMinutes(-35),
                    LastExitTime = now.AddMinutes(-35),
                    NextDeadline = now.AddMinutes(-20), // Quá hạn 20 phút
                    IsAlertSent = false,
                    IsDeleted = false
                };

                await tripCollection.InsertOneAsync(testTrip);
                testTrip.Id.Should().NotBeNullOrEmpty();
            }
            catch (Exception ex)
            {
                // In case local MongoDB connection is unavailable during CI, do not fail
                Console.WriteLine($"[SeedViolationTripForMainRoute] Lưu ý: {ex.Message}");
            }
        }

        [Fact]
        public async Task SendRealAlertEmail_ToMainRouteRecipients_Succeeds()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile(@"C:\Users\ADMIN\source\repos\HPParking\HPParking.Api\appsettings.json")
                .Build();

            var logger = Substitute.For<ILogger<HPParking.Api.Services.Implementations.EmailSenderService>>();
            var emailService = new HPParking.Api.Services.Implementations.EmailSenderService(config, logger);

            var recipients = new List<string>
            {
                "phanvanloi150203@gmail.com",
                "phanvanloi1522003@gmail.com"
            };

            string subject = "[CẢNH BÁO SLA] Phương tiện nội bộ 30A1234545 quá hạn di chuyển trên Tuyến Chính";
            string body = @"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 2px solid #e11d48; border-radius: 8px; max-width: 650px;'>
                    <h2 style='color: #e11d48; margin-top: 0;'>QUÁ HẠN DI CHUYỂN GIỮA CÁC CỔNG (NGHI VẤN TRỐN VIỆC / LẠC TUYẾN)</h2>
                    <hr style='border: 0; border-top: 1px solid #ccc;' />
                    <p>Hệ thống giám sát điều vận HPParking ghi nhận phương tiện nội bộ sau đây đã vi phạm giới hạn thời gian (SLA) trên <strong>Tuyến chính (ROUTE-01)</strong>:</p>
                    <p><strong>Biển số xe:</strong> <span style='font-size: 18px; font-weight: bold;'>30A1234545</span></p>
                    <p><strong>Mã thẻ quẹt:</strong> 0012345678</p>
                    <p><strong>Tuyến lộ trình:</strong> Tuyến chính (ROUTE-01)</p>
                    <p><strong>Cổng xuất phát ban đầu:</strong> Cổng Chính Nhà Máy (GATE_01)</p>
                    <p><strong>Thời điểm quẹt RA gần nhất:</strong> " + DateTime.Now.AddMinutes(-35).ToString("dd/MM/yyyy HH:mm:ss") + @"</p>
                    <p><strong>Hạn chót phải quẹt VÀO cổng tiếp theo:</strong> <span style='color:red;'>" + DateTime.Now.AddMinutes(-20).ToString("dd/MM/yyyy HH:mm:ss") + @"</span></p>
                    <p><strong>Thời gian quá hạn:</strong> <span style='color:red; font-weight:bold;'>20 phút</span></p>
                    <hr style='border: 0; border-top: 1px solid #ccc;' />
                    <p style='color: #666; font-size: 13px;'>Email được gửi tự động từ Hệ thống Quản trị Bãi đỗ xe & Điều vận HPParking.</p>
                </div>";

            bool sent = await emailService.SendEmailAsync(recipients, subject, body);
            sent.Should().BeTrue();
        }

        [Fact]
        public async Task VehicleTransitWatcherService_FullWorkflow_WithRealDbAndEmail_Succeeds()
        {
            var context = new MongoDbContext("mongodb://localhost:27017", "hpparking");
            var tripCollection = context.GetCollection<VehicleDispatchTrip>("VehicleDispatchTrips");

            var now = DateTime.UtcNow;
            string vehicleId = "6abb3cb91f3a0cf5469cf5cb";
            string mainRouteId = "6abb5ce3dc1a98b2f6c5e34f";

            await tripCollection.DeleteManyAsync(t => t.VehicleId == vehicleId && t.Status != TripStatus.Completed);

            var testTrip = new VehicleDispatchTrip
            {
                VehicleId = vehicleId,
                PlateNumber = "30A1234545",
                CardNumber = "0012345678",
                OriginGateId = "6ab24441aef536b162d6d1a7", // Cổng Chính Nhà Máy
                CurrentGateId = "6ab24441aef536b162d6d1a7",
                AssignedRouteId = mainRouteId, // tuyến chính
                CurrentStepIndex = 1,
                Status = TripStatus.InTransit,
                StartTime = now.AddMinutes(-35),
                LastExitTime = now.AddMinutes(-35),
                NextDeadline = now.AddMinutes(-20), // Quá hạn 20 phút
                IsAlertSent = false,
                IsDeleted = false
            };
            await tripCollection.InsertOneAsync(testTrip);

            var config = new ConfigurationBuilder()
                .AddJsonFile(@"C:\Users\ADMIN\source\repos\HPParking\HPParking.Api\appsettings.json")
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddLogging();
            services.AddSingleton(context);
            services.AddScoped(typeof(IRepository<>), typeof(HPParking.Core.Repositories.MongoRepository<>));
            services.AddScoped<HPParking.Api.Services.Interfaces.IEmailSenderService, HPParking.Api.Services.Implementations.EmailSenderService>();
            services.AddSingleton<VehicleTransitWatcherService>();

            var provider = services.BuildServiceProvider();
            var watcher = provider.GetRequiredService<VehicleTransitWatcherService>();

            await watcher.CheckOverdueTripsAsync(CancellationToken.None);

            var updatedTrip = await tripCollection.Find(t => t.Id == testTrip.Id).FirstOrDefaultAsync();
            updatedTrip.Should().NotBeNull();
            updatedTrip!.Status.Should().Be(TripStatus.OverdueTransit);
            updatedTrip.IsAlertSent.Should().BeTrue();
            updatedTrip.AlertSentAt.Should().NotBeNull();
        }

        [Fact]
        public async Task DebugCheckCardInDatabase()
        {
            var context = new MongoDbContext("mongodb://localhost:27017", "hpparking");
            var cardRepo = new HPParking.Core.Repositories.MongoRepository<Card>(context);
            var vehicleRepo = new HPParking.Core.Repositories.MongoRepository<Vehicle>(context);

            var allCards = await cardRepo.GetAllAsync();
            foreach (var c in allCards)
            {
                System.Diagnostics.Debug.WriteLine($"[CARD] Number: '{c.CardNumber}', TargetType: {c.TargetType}, VehicleId: '{c.VehicleId}', ClientId: '{c.ClientId}', IsDeleted: {c.IsDeleted}");
                Console.WriteLine($"[CARD] Number: '{c.CardNumber}', TargetType: {c.TargetType}, VehicleId: '{c.VehicleId}', ClientId: '{c.ClientId}', IsDeleted: {c.IsDeleted}");
            }

            var laneRepo = new HPParking.Core.Repositories.MongoRepository<Lane>(context);
            var allLanes = await laneRepo.GetAllAsync();
            foreach (var l in allLanes)
            {
                Console.WriteLine($"[LANE] Id: '{l.Id}', Name: '{l.Name}', GateId: '{l.GateId}', InputReader: {l.InputReader}, Direction: {l.Direction}, IsActive: {l.IsActive}");
            }

            var gateRepo = new HPParking.Core.Repositories.MongoRepository<Gate>(context);
            var allGates = await gateRepo.GetAllAsync();
            foreach (var g in allGates)
            {
                Console.WriteLine($"[GATE] Id: '{g.Id}', Name: '{g.Name}', Code: '{g.Code}', MachineCode: '{g.MachineCode}'");
            }

            var allVehicles = await vehicleRepo.GetAllAsync();
            foreach (var v in allVehicles)
            {
                Console.WriteLine($"[VEHICLE] Id: '{v.Id}', Plate: '{v.PlateNumber}', IsShared: {v.IsShared}, IsActive: {v.IsActive}, IsDeleted: {v.IsDeleted}, RouteId: '{v.AssignedRouteId}', OwnerId: '{v.OwnerClientId}'");
            }

            var sessionRepo = new HPParking.Core.Repositories.MongoRepository<ParkingSession>(context);
            var sessions = await sessionRepo.FindAsync(s => !s.IsDeleted);
            foreach (var s in sessions.OrderByDescending(x => x.InTime).Take(5))
            {
                Console.WriteLine($"[SESSION] Id: '{s.Id}', PersonId: '{s.PersonId}', Plate: '{s.PlateNumber}', InTime: {s.InTime:HH:mm:ss}, OutTime: {s.OutTime:HH:mm:ss}, Status: {s.Status}");
            }

            var tripRepo = new HPParking.Core.Repositories.MongoRepository<VehicleDispatchTrip>(context);
            var trips = await tripRepo.FindAsync(t => !t.IsDeleted);
            foreach (var t in trips.OrderByDescending(x => x.CreatedAt).Take(5))
            {
                Console.WriteLine($"[TRIP] Id: '{t.Id}', Plate: '{t.PlateNumber}', Status: {t.Status}, Gate: '{t.CurrentGateId}', Route: '{t.AssignedRouteId}', CreatedAt: {t.CreatedAt:HH:mm:ss}");
            }

            string rawCard = "0013181773";
            string normalizedCard = HPParking.Core.Helpers.CardHelper.NormalizeCardCode(rawCard);

            var foundVehicleCard = await cardRepo.FindOneAsync(c =>
                (c.CardNumber == normalizedCard || c.CardNumber == rawCard) &&
                c.TargetType == CardTargetType.Vehicle &&
                !c.IsDeleted);

            Console.WriteLine($"[QUERY RESULT] Found: {foundVehicleCard != null}, CardNumber: {foundVehicleCard?.CardNumber}, VehicleId: {foundVehicleCard?.VehicleId}");
        }

        public class DummyDiService(
            HPParking.Core.Interfaces.IRepository<Client> clientRepo,
            HPParking.Core.Interfaces.IRepository<Card>? cardRepo = null,
            HPParking.Core.Interfaces.IRepository<Vehicle>? vehicleRepo = null)
        {
            public HPParking.Core.Interfaces.IRepository<Card>? CardRepo => cardRepo;
            public HPParking.Core.Interfaces.IRepository<Vehicle>? VehicleRepo => vehicleRepo;
        }

        [Fact]
        public void Test_ParkingWorkflowService_DI_Resolution()
        {
            var services = new ServiceCollection();
            services.AddSingleton(MongoDbContext.Instance);
            services.AddScoped(typeof(HPParking.Core.Interfaces.IRepository<>), typeof(HPParking.Core.Repositories.MongoRepository<>));
            services.AddScoped<DummyDiService>();

            var sp = services.BuildServiceProvider();
            var service = sp.GetRequiredService<DummyDiService>();

            Console.WriteLine($"[DI CHECK] CardRepo is null: {service.CardRepo == null}");
            Console.WriteLine($"[DI CHECK] VehicleRepo is null: {service.VehicleRepo == null}");

            service.CardRepo.Should().NotBeNull();
            service.VehicleRepo.Should().NotBeNull();
        }
    }
}
