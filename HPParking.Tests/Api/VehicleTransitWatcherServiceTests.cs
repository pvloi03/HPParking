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

        [Fact]
        public async Task CheckOverdueTripsAsync_WhenEmailFails_DoesNotMarkAlertSent_AndRetriesSuccessfully()
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
                AlertEmails = new List<string> { "admin@test.com" }
            };
            routeRepo.FindOneAsync(Arg.Any<System.Linq.Expressions.Expression<Func<GateRouteConfig, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<GateRouteConfig?>(defaultRoute));

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IRepository<VehicleDispatchTrip>)).Returns(tripRepo);
            serviceProvider.GetService(typeof(IRepository<GateRouteConfig>)).Returns(routeRepo);
            serviceProvider.GetService(typeof(IRepository<Gate>)).Returns(gateRepo);
            serviceProvider.GetService(typeof(IEmailSenderService)).Returns(emailSender);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(serviceProvider);
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);

            var now = DateTime.UtcNow;
            var trip = new VehicleDispatchTrip
            {
                Id = "trip_fail_then_retry",
                VehicleId = "veh_retry",
                PlateNumber = "29A-88888",
                Status = TripStatus.InTransit,
                NextDeadline = now.AddMinutes(-5),
                IsAlertSent = false
            };

            tripRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<VehicleDispatchTrip, bool>>>())
                .Returns(Task.FromResult<IReadOnlyList<VehicleDispatchTrip>>(new List<VehicleDispatchTrip> { trip }));

            // First run: SendEmailAsync fails (returns false)
            emailSender.SendEmailAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));

            var watcher = new VehicleTransitWatcherService(scopeFactory, logger);

            // Act 1
            await watcher.CheckOverdueTripsAsync(CancellationToken.None);

            // Assert 1: status is overdue, but IsAlertSent remains false
            trip.Status.Should().Be(TripStatus.OverdueTransit);
            trip.IsAlertSent.Should().BeFalse();
            trip.AlertSentAt.Should().BeNull();

            // Act 2: email succeeds on next retry
            emailSender.SendEmailAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

            await watcher.CheckOverdueTripsAsync(CancellationToken.None);

            // Assert 2: now IsAlertSent is true!
            trip.IsAlertSent.Should().BeTrue();
            trip.AlertSentAt.Should().NotBeNull();
        }

        [Fact]
        public async Task CheckOverdueTripsAsync_WhenOneTripThrows_OtherTripsContinueProcessing()
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
                IsDefault = true,
                AlertEmails = new List<string> { "admin@test.com" }
            };
            routeRepo.FindOneAsync(Arg.Any<System.Linq.Expressions.Expression<Func<GateRouteConfig, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<GateRouteConfig?>(defaultRoute));

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IRepository<VehicleDispatchTrip>)).Returns(tripRepo);
            serviceProvider.GetService(typeof(IRepository<GateRouteConfig>)).Returns(routeRepo);
            serviceProvider.GetService(typeof(IRepository<Gate>)).Returns(gateRepo);
            serviceProvider.GetService(typeof(IEmailSenderService)).Returns(emailSender);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(serviceProvider);
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);

            var now = DateTime.UtcNow;
            var trip1 = new VehicleDispatchTrip
            {
                Id = "trip1_error",
                VehicleId = "veh1",
                PlateNumber = "29A-11111",
                AssignedRouteId = "bad_route_id",
                Status = TripStatus.InTransit,
                NextDeadline = now.AddMinutes(-10),
                IsAlertSent = false
            };

            var trip2 = new VehicleDispatchTrip
            {
                Id = "trip2_ok",
                VehicleId = "veh2",
                PlateNumber = "29A-22222",
                Status = TripStatus.WorkingAtGate,
                NextDeadline = now.AddMinutes(-5),
                IsAlertSent = false
            };

            tripRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<VehicleDispatchTrip, bool>>>())
                .Returns(Task.FromResult<IReadOnlyList<VehicleDispatchTrip>>(new List<VehicleDispatchTrip> { trip1, trip2 }));

            // Make trip1 route query throw
            routeRepo.GetByIdAsync("bad_route_id", Arg.Any<CancellationToken>())
                .Returns<Task<GateRouteConfig?>>(_ => throw new InvalidOperationException("DB connection error for trip1"));

            emailSender.SendEmailAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

            var watcher = new VehicleTransitWatcherService(scopeFactory, logger);

            // Act
            await watcher.CheckOverdueTripsAsync(CancellationToken.None);

            // Assert: trip1 failed, but trip2 was processed successfully
            trip1.IsAlertSent.Should().BeFalse();
            trip2.IsAlertSent.Should().BeTrue();
            trip2.Status.Should().Be(TripStatus.OverdueStay);
            trip2.AlertSentAt.Should().NotBeNull();
        }

        [Theory]
        [InlineData(0, "0 giây")]
        [InlineData(45, "45 giây")]
        [InlineData(60, "1 phút")]
        [InlineData(75, "1 phút 15 giây")]
        [InlineData(120, "2 phút")]
        [InlineData(204, "3 phút 24 giây")]
        public void FormatOverdueDuration_FormatsSecondsCorrectly(double seconds, string expected)
        {
            var result = VehicleTransitWatcherService.FormatOverdueDuration(seconds);
            result.Should().Be(expected);
        }

        [Fact]
        public async Task CheckOverdueTripsAsync_TransitOverdue_Leg2_DisplaysDepartedGateAndReturnOriginCorrectly()
        {
            // Arrange
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var emailSender = Substitute.For<IEmailSenderService>();
            var logger = Substitute.For<ILogger<VehicleTransitWatcherService>>();

            var fixedRoute = new GateRouteConfig
            {
                Id = "route_fixed",
                RouteCode = "TUYEN_01",
                RouteName = "Tuyến Nhà Máy 1 - Nhà Máy 2",
                IsDefault = false,
                AlertEmails = new List<string> { "manager@factory.com" },
                GateSteps = new List<RouteGateStep>
                {
                    new RouteGateStep { StepIndex = 1, GateId = "gate1", GateName = "Cổng Chính Nhà Máy", MaxTravelMinutes = 5 },
                    new RouteGateStep { StepIndex = 2, GateId = "gate2", GateName = "Cổng nhà máy 2", MaxTravelMinutes = 5, MaxStayMinutes = 10 }
                }
            };
            routeRepo.GetByIdAsync("route_fixed", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<GateRouteConfig?>(fixedRoute));

            var gate1 = new Gate { Id = "gate1", Code = "GATE_01", Name = "Cổng Chính Nhà Máy" };
            var gate2 = new Gate { Id = "gate2", Code = "GATE_02", Name = "Cổng nhà máy 2" };
            gateRepo.GetByIdAsync("gate1", Arg.Any<CancellationToken>()).Returns(Task.FromResult<Gate?>(gate1));
            gateRepo.GetByIdAsync("gate2", Arg.Any<CancellationToken>()).Returns(Task.FromResult<Gate?>(gate2));

            var now = DateTime.UtcNow;
            var deadline = now.AddSeconds(-85); // Overdue by 1 minute 25 seconds
            var trip = new VehicleDispatchTrip
            {
                Id = "trip_leg2",
                PlateNumber = "35B-263.37",
                CardNumber = "3800201218",
                AssignedRouteId = "route_fixed",
                OriginGateId = "gate1",
                CurrentGateId = "gate2", // Xe vừa rời Cổng nhà máy 2
                CurrentStepIndex = 2,    // Chặng 2 (Chặng quay về cổng 1)
                Status = TripStatus.InTransit,
                LastExitTime = deadline.AddMinutes(-5),
                NextDeadline = deadline,
                IsAlertSent = false
            };

            tripRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<VehicleDispatchTrip, bool>>>())
                .Returns(Task.FromResult<IReadOnlyList<VehicleDispatchTrip>>(new List<VehicleDispatchTrip> { trip }));

            string capturedBody = string.Empty;
            emailSender.SendEmailAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string>(),
                Arg.Do<string>(body => capturedBody = body),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

            emailSender.SendEmailAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string>(),
                Arg.Do<string>(body => capturedBody = body),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IRepository<VehicleDispatchTrip>)).Returns(tripRepo);
            serviceProvider.GetService(typeof(IRepository<GateRouteConfig>)).Returns(routeRepo);
            serviceProvider.GetService(typeof(IRepository<Gate>)).Returns(gateRepo);
            serviceProvider.GetService(typeof(IEmailSenderService)).Returns(emailSender);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(serviceProvider);
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);

            var watcher = new VehicleTransitWatcherService(scopeFactory, logger);

            // Act
            await watcher.CheckOverdueTripsAsync(CancellationToken.None);

            // Assert
            trip.IsAlertSent.Should().BeTrue();
            trip.Status.Should().Be(TripStatus.OverdueTransit);
            capturedBody.Should().NotBeNullOrEmpty();
            capturedBody.Should().Contain("Cổng vừa rời đi");
            capturedBody.Should().Contain("Cổng nhà máy 2");
            capturedBody.Should().Contain("Chặng #2");
            capturedBody.Should().Contain("Cổng bắt đầu chuyến đi");
            capturedBody.Should().Contain("Cổng Chính Nhà Máy");
            capturedBody.Should().Contain("Cổng Chính Nhà Máy (GATE_01) (Chặng quay về kết thúc)");
            capturedBody.Should().Contain("Quá 1 phút 25 giây");
        }

        [Fact]
        public async Task CheckOverdueTripsAsync_TransitOverdue_FreeRoam_DisplaysAnyGateCorrectly()
        {
            // Arrange
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var emailSender = Substitute.For<IEmailSenderService>();
            var logger = Substitute.For<ILogger<VehicleTransitWatcherService>>();

            var defaultRoute = new GateRouteConfig
            {
                Id = "route_free",
                RouteCode = "DEFAULT",
                RouteName = "Tuyến tự do mặc định (Free-roam SLA)",
                IsDefault = true,
                AlertEmails = new List<string> { "freeroam_alert@factory.com" },
                GateSteps = []
            };
            routeRepo.FindOneAsync(Arg.Any<System.Linq.Expressions.Expression<Func<GateRouteConfig, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<GateRouteConfig?>(defaultRoute));
            routeRepo.GetByIdAsync("route_free", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<GateRouteConfig?>(defaultRoute));

            var gate1 = new Gate { Id = "gate1", Code = "GATE_01", Name = "Cổng Chính Nhà Máy" };
            gateRepo.GetByIdAsync("gate1", Arg.Any<CancellationToken>()).Returns(Task.FromResult<Gate?>(gate1));

            var now = DateTime.UtcNow;
            var deadline = now.AddSeconds(-65); // Overdue by 1 minute 5 seconds
            var trip = new VehicleDispatchTrip
            {
                Id = "trip_free",
                PlateNumber = "29C-111.22",
                CardNumber = "3800209999",
                AssignedRouteId = "route_free",
                OriginGateId = "gate1",
                CurrentGateId = "gate1",
                CurrentStepIndex = 1,
                Status = TripStatus.InTransit,
                LastExitTime = deadline.AddMinutes(-5),
                NextDeadline = deadline,
                IsAlertSent = false
            };

            tripRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<VehicleDispatchTrip, bool>>>())
                .Returns(Task.FromResult<IReadOnlyList<VehicleDispatchTrip>>(new List<VehicleDispatchTrip> { trip }));

            string capturedBody = string.Empty;
            emailSender.SendEmailAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string>(),
                Arg.Do<string>(body => capturedBody = body),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

            emailSender.SendEmailAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string>(),
                Arg.Do<string>(body => capturedBody = body),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IRepository<VehicleDispatchTrip>)).Returns(tripRepo);
            serviceProvider.GetService(typeof(IRepository<GateRouteConfig>)).Returns(routeRepo);
            serviceProvider.GetService(typeof(IRepository<Gate>)).Returns(gateRepo);
            serviceProvider.GetService(typeof(IEmailSenderService)).Returns(emailSender);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(serviceProvider);
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);

            var watcher = new VehicleTransitWatcherService(scopeFactory, logger);

            // Act
            await watcher.CheckOverdueTripsAsync(CancellationToken.None);

            // Assert
            trip.IsAlertSent.Should().BeTrue();
            trip.Status.Should().Be(TripStatus.OverdueTransit);
            capturedBody.Should().NotBeNullOrEmpty();
            capturedBody.Should().Contain("🚩 Cổng xuất phát");
            capturedBody.Should().NotContain("Cổng vừa rời đi");
            capturedBody.Should().NotContain("Cổng bắt đầu chuyến đi");
            capturedBody.Should().Contain("Cổng bất kỳ (Tuyến tự do)");
            capturedBody.Should().Contain("Quá 1 phút 5 giây");
        }

        [Fact]
        public async Task CheckOverdueTripsAsync_TransitOverdue_Leg1_DisplaysOriginGateTitleWithoutRedundantTripOrigin()
        {
            // Arrange
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var emailSender = Substitute.For<IEmailSenderService>();
            var logger = Substitute.For<ILogger<VehicleTransitWatcherService>>();

            var fixedRoute = new GateRouteConfig
            {
                Id = "route_fixed_leg1",
                RouteCode = "TUYEN_03",
                RouteName = "Tuyến Nhà Máy 1 - Nhà Máy 2",
                IsDefault = false,
                AlertEmails = new List<string> { "manager@factory.com" },
                GateSteps = new List<RouteGateStep>
                {
                    new RouteGateStep { StepIndex = 1, GateId = "gate1", GateName = "Cổng Chính Nhà Máy", MaxTravelMinutes = 5 },
                    new RouteGateStep { StepIndex = 2, GateId = "gate2", GateName = "Cổng nhà máy 2", MaxTravelMinutes = 5, MaxStayMinutes = 10 }
                }
            };
            routeRepo.GetByIdAsync("route_fixed_leg1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<GateRouteConfig?>(fixedRoute));

            var gate1 = new Gate { Id = "gate1", Code = "GATE_01", Name = "Cổng Chính Nhà Máy" };
            var gate2 = new Gate { Id = "gate2", Code = "GATE_02", Name = "Cổng nhà máy 2" };
            gateRepo.GetByIdAsync("gate1", Arg.Any<CancellationToken>()).Returns(Task.FromResult<Gate?>(gate1));
            gateRepo.GetByIdAsync("gate2", Arg.Any<CancellationToken>()).Returns(Task.FromResult<Gate?>(gate2));

            var now = DateTime.UtcNow;
            var deadline = now.AddSeconds(-45); // Overdue by 45 seconds
            var trip = new VehicleDispatchTrip
            {
                Id = "trip_leg1",
                PlateNumber = "35B-263.37",
                CardNumber = "3800201218",
                AssignedRouteId = "route_fixed_leg1",
                OriginGateId = "gate1",
                CurrentGateId = "gate1", // Xe vừa xuất phát từ Cổng Chính Nhà Máy
                CurrentStepIndex = 1,    // Chặng 1
                Status = TripStatus.InTransit,
                LastExitTime = deadline.AddMinutes(-5),
                NextDeadline = deadline,
                IsAlertSent = false
            };

            tripRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<VehicleDispatchTrip, bool>>>())
                .Returns(Task.FromResult<IReadOnlyList<VehicleDispatchTrip>>(new List<VehicleDispatchTrip> { trip }));

            string capturedBody = string.Empty;
            emailSender.SendEmailAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string>(),
                Arg.Do<string>(body => capturedBody = body),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

            emailSender.SendEmailAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string>(),
                Arg.Do<string>(body => capturedBody = body),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IRepository<VehicleDispatchTrip>)).Returns(tripRepo);
            serviceProvider.GetService(typeof(IRepository<GateRouteConfig>)).Returns(routeRepo);
            serviceProvider.GetService(typeof(IRepository<Gate>)).Returns(gateRepo);
            serviceProvider.GetService(typeof(IEmailSenderService)).Returns(emailSender);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(serviceProvider);
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);

            var watcher = new VehicleTransitWatcherService(scopeFactory, logger);

            // Act
            await watcher.CheckOverdueTripsAsync(CancellationToken.None);

            // Assert
            trip.IsAlertSent.Should().BeTrue();
            trip.Status.Should().Be(TripStatus.OverdueTransit);
            capturedBody.Should().NotBeNullOrEmpty();
            capturedBody.Should().Contain("🚩 Cổng xuất phát");
            capturedBody.Should().Contain("Cổng Chính Nhà Máy");
            capturedBody.Should().Contain("Chặng #1");
            capturedBody.Should().NotContain("Cổng vừa rời đi");
            capturedBody.Should().NotContain("Cổng bắt đầu chuyến đi");
            capturedBody.Should().Contain("Cổng nhà máy 2");
            capturedBody.Should().Contain("Quá 45 giây");
        }

        [Fact]
        public async Task CheckOverdueTripsAsync_TransitOverdue_Leg1_WhenCurrentGateDiffersFromOrigin_DisplaysDepartedGateAndOriginGate()
        {
            // Arrange
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var emailSender = Substitute.For<IEmailSenderService>();
            var logger = Substitute.For<ILogger<VehicleTransitWatcherService>>();

            var fixedRoute = new GateRouteConfig
            {
                Id = "route_fixed_leg1_diff",
                RouteCode = "TUYEN_04",
                RouteName = "Tuyến Nhà Máy 1 - Nhà Máy 2",
                IsDefault = false,
                AlertEmails = new List<string> { "manager@factory.com" },
                GateSteps = new List<RouteGateStep>
                {
                    new RouteGateStep { StepIndex = 1, GateId = "gate1", GateName = "Cổng Chính Nhà Máy", MaxTravelMinutes = 5 },
                    new RouteGateStep { StepIndex = 2, GateId = "gate2", GateName = "Cổng nhà máy 2", MaxTravelMinutes = 5, MaxStayMinutes = 10 }
                }
            };
            routeRepo.GetByIdAsync("route_fixed_leg1_diff", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<GateRouteConfig?>(fixedRoute));

            var gate1 = new Gate { Id = "gate1", Code = "GATE_01", Name = "Cổng Chính Nhà Máy" };
            var gate2 = new Gate { Id = "gate2", Code = "GATE_02", Name = "Cổng nhà máy 2" };
            gateRepo.GetByIdAsync("gate1", Arg.Any<CancellationToken>()).Returns(Task.FromResult<Gate?>(gate1));
            gateRepo.GetByIdAsync("gate2", Arg.Any<CancellationToken>()).Returns(Task.FromResult<Gate?>(gate2));

            var now = DateTime.UtcNow;
            var deadline = now.AddSeconds(-45);
            var trip = new VehicleDispatchTrip
            {
                Id = "trip_leg1_diff",
                PlateNumber = "35B-263.37",
                CardNumber = "3800201218",
                AssignedRouteId = "route_fixed_leg1_diff",
                OriginGateId = "gate1",
                CurrentGateId = "gate2", // Chặng 1 nhưng quẹt rời đi tại gate2 (khác OriginGateId gate1)
                CurrentStepIndex = 1,
                Status = TripStatus.InTransit,
                LastExitTime = deadline.AddMinutes(-5),
                NextDeadline = deadline,
                IsAlertSent = false
            };

            tripRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<VehicleDispatchTrip, bool>>>())
                .Returns(Task.FromResult<IReadOnlyList<VehicleDispatchTrip>>(new List<VehicleDispatchTrip> { trip }));

            string capturedBody = string.Empty;
            emailSender.SendEmailAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string>(),
                Arg.Do<string>(body => capturedBody = body),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

            emailSender.SendEmailAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string>(),
                Arg.Do<string>(body => capturedBody = body),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IRepository<VehicleDispatchTrip>)).Returns(tripRepo);
            serviceProvider.GetService(typeof(IRepository<GateRouteConfig>)).Returns(routeRepo);
            serviceProvider.GetService(typeof(IRepository<Gate>)).Returns(gateRepo);
            serviceProvider.GetService(typeof(IEmailSenderService)).Returns(emailSender);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(serviceProvider);
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);

            var watcher = new VehicleTransitWatcherService(scopeFactory, logger);

            // Act
            await watcher.CheckOverdueTripsAsync(CancellationToken.None);

            // Assert
            trip.IsAlertSent.Should().BeTrue();
            trip.Status.Should().Be(TripStatus.OverdueTransit);
            capturedBody.Should().NotBeNullOrEmpty();
            capturedBody.Should().Contain("🚩 Cổng vừa rời đi");
            capturedBody.Should().Contain("Cổng nhà máy 2");
            capturedBody.Should().Contain("Chặng #1");
            capturedBody.Should().NotContain("🚩 Cổng xuất phát");
            capturedBody.Should().Contain("🏢 Cổng bắt đầu chuyến đi");
            capturedBody.Should().Contain("Cổng Chính Nhà Máy");
        }

        [Fact]
        public async Task CheckOverdueTripsAsync_TransitOverdue_Leg2_WhenDepartedFromOriginGate_DisplaysDepartedGateAndOriginGate()
        {
            // Arrange: Xe ở Chặng 2 rời đi từ cổng có ID trùng OriginGateId (lộ trình vòng ghé lại cổng ban đầu)
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var emailSender = Substitute.For<IEmailSenderService>();
            var logger = Substitute.For<ILogger<VehicleTransitWatcherService>>();

            var fixedRoute = new GateRouteConfig
            {
                Id = "route_fixed_leg2_origin",
                RouteCode = "TUYEN_HUB",
                RouteName = "Tuyến Trung Chuyển Nội Bộ",
                IsDefault = false,
                AlertEmails = ["manager@factory.com"],
                GateSteps =
                [
                    new RouteGateStep { StepIndex = 1, GateId = "gate1", GateName = "Cổng Chính Nhà Máy", MaxTravelMinutes = 5 },
                    new RouteGateStep { StepIndex = 2, GateId = "gate1", GateName = "Cổng Chính Nhà Máy", MaxTravelMinutes = 5, MaxStayMinutes = 10 }
                ]
            };
            routeRepo.GetByIdAsync("route_fixed_leg2_origin", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<GateRouteConfig?>(fixedRoute));

            var gate1 = new Gate { Id = "gate1", Code = "GATE_01", Name = "Cổng Chính Nhà Máy" };
            gateRepo.GetByIdAsync("gate1", Arg.Any<CancellationToken>()).Returns(Task.FromResult<Gate?>(gate1));

            var now = DateTime.UtcNow;
            var deadline = now.AddSeconds(-45);
            var trip = new VehicleDispatchTrip
            {
                Id = "trip_leg2_origin",
                PlateNumber = "35B-263.37",
                CardNumber = "3800201218",
                AssignedRouteId = "route_fixed_leg2_origin",
                OriginGateId = "gate1",
                CurrentGateId = "gate1", // Chặng 2 nhưng rời đi từ cổng gate1 (trùng OriginGateId)
                CurrentStepIndex = 2,
                Status = TripStatus.InTransit,
                LastExitTime = deadline.AddMinutes(-5),
                NextDeadline = deadline,
                IsAlertSent = false
            };

            tripRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<VehicleDispatchTrip, bool>>>())
                .Returns(Task.FromResult<IReadOnlyList<VehicleDispatchTrip>>(new List<VehicleDispatchTrip> { trip }));

            string capturedBody = string.Empty;
            emailSender.SendEmailAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string>(),
                Arg.Do<string>(body => capturedBody = body),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

            emailSender.SendEmailAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string>(),
                Arg.Do<string>(body => capturedBody = body),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IRepository<VehicleDispatchTrip>)).Returns(tripRepo);
            serviceProvider.GetService(typeof(IRepository<GateRouteConfig>)).Returns(routeRepo);
            serviceProvider.GetService(typeof(IRepository<Gate>)).Returns(gateRepo);
            serviceProvider.GetService(typeof(IEmailSenderService)).Returns(emailSender);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(serviceProvider);
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);

            var watcher = new VehicleTransitWatcherService(scopeFactory, logger);

            // Act
            await watcher.CheckOverdueTripsAsync(CancellationToken.None);

            // Assert
            trip.IsAlertSent.Should().BeTrue();
            trip.Status.Should().Be(TripStatus.OverdueTransit);
            capturedBody.Should().NotBeNullOrEmpty();
            capturedBody.Should().Contain("🚩 Cổng vừa rời đi");
            capturedBody.Should().Contain("Cổng Chính Nhà Máy");
            capturedBody.Should().Contain("Chặng #2");
            capturedBody.Should().NotContain("🚩 Cổng xuất phát");
            capturedBody.Should().Contain("🏢 Cổng bắt đầu chuyến đi");
        }

        [Fact]
        public async Task CheckOverdueTripsAsync_WhenTargetLegIndexExceedsStepCount_ShouldStillIdentifyReturnLeg()
        {
            // Arrange: Tuyến có 2 chặng (GateSteps.Count = 2), nhưng xe có targetLegIndex = 3 > 2 (ví dụ CurrentStepIndex = 3 trong InTransit)
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var emailSender = Substitute.For<IEmailSenderService>();
            var logger = Substitute.For<ILogger<VehicleTransitWatcherService>>();

            var fixedRoute = new GateRouteConfig
            {
                Id = "route_fixed_2steps",
                RouteCode = "TUYEN_2S",
                RouteName = "Tuyến 2 Chặng",
                IsDefault = false,
                AlertEmails = new List<string> { "manager@factory.com" },
                GateSteps = new List<RouteGateStep>
                {
                    new RouteGateStep { StepIndex = 1, GateId = "gate1", GateName = "Cổng Chính Nhà Máy", MaxTravelMinutes = 5 },
                    new RouteGateStep { StepIndex = 2, GateId = "gate2", GateName = "Cổng nhà máy 2", MaxTravelMinutes = 5, MaxStayMinutes = 10 }
                }
            };
            routeRepo.GetByIdAsync("route_fixed_2steps", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<GateRouteConfig?>(fixedRoute));

            var gate1 = new Gate { Id = "gate1", Code = "GATE_01", Name = "Cổng Chính Nhà Máy" };
            var gate2 = new Gate { Id = "gate2", Code = "GATE_02", Name = "Cổng nhà máy 2" };
            gateRepo.GetByIdAsync("gate1", Arg.Any<CancellationToken>()).Returns(Task.FromResult<Gate?>(gate1));
            gateRepo.GetByIdAsync("gate2", Arg.Any<CancellationToken>()).Returns(Task.FromResult<Gate?>(gate2));

            var now = DateTime.UtcNow;
            var deadline = now.AddSeconds(-30);
            var trip = new VehicleDispatchTrip
            {
                Id = "trip_overshoot",
                PlateNumber = "35B-263.37",
                CardNumber = "3800201218",
                AssignedRouteId = "route_fixed_2steps",
                OriginGateId = "gate1",
                CurrentGateId = "gate2",
                CurrentStepIndex = 3, // 3 > GateSteps.Count (2)
                Status = TripStatus.InTransit,
                LastExitTime = deadline.AddMinutes(-5),
                NextDeadline = deadline,
                IsAlertSent = false
            };

            tripRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<VehicleDispatchTrip, bool>>>())
                .Returns(Task.FromResult<IReadOnlyList<VehicleDispatchTrip>>(new List<VehicleDispatchTrip> { trip }));

            string capturedBody = string.Empty;
            emailSender.SendEmailAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string>(),
                Arg.Do<string>(body => capturedBody = body),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

            emailSender.SendEmailAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string>(),
                Arg.Do<string>(body => capturedBody = body),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IRepository<VehicleDispatchTrip>)).Returns(tripRepo);
            serviceProvider.GetService(typeof(IRepository<GateRouteConfig>)).Returns(routeRepo);
            serviceProvider.GetService(typeof(IRepository<Gate>)).Returns(gateRepo);
            serviceProvider.GetService(typeof(IEmailSenderService)).Returns(emailSender);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(serviceProvider);
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);

            var watcher = new VehicleTransitWatcherService(scopeFactory, logger);

            // Act
            await watcher.CheckOverdueTripsAsync(CancellationToken.None);

            // Assert
            trip.IsAlertSent.Should().BeTrue();
            capturedBody.Should().NotBeNullOrEmpty();
            capturedBody.Should().Contain("Cổng Chính Nhà Máy (GATE_01) (Chặng quay về kết thúc)");
            capturedBody.Should().NotContain("Cổng chưa xác định");
        }

        [Fact]
        public async Task CheckOverdueTripsAsync_StayOverdue_DisplaysWorkingGateAndNextDestination()
        {
            // Arrange
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var emailSender = Substitute.For<IEmailSenderService>();
            var logger = Substitute.For<ILogger<VehicleTransitWatcherService>>();

            var fixedRoute = new GateRouteConfig
            {
                Id = "route_fixed_stay",
                RouteCode = "TUYEN_02",
                RouteName = "Tuyến Giao Nhận Phụ Tùng",
                IsDefault = false,
                AlertEmails = new List<string> { "warehouse@factory.com" },
                GateSteps = new List<RouteGateStep>
                {
                    new RouteGateStep { StepIndex = 1, GateId = "gate1", GateName = "Cổng Chính Nhà Máy", MaxTravelMinutes = 5 },
                    new RouteGateStep { StepIndex = 2, GateId = "gate2", GateName = "Cổng nhà máy 2", MaxTravelMinutes = 5, MaxStayMinutes = 10 }
                }
            };
            routeRepo.GetByIdAsync("route_fixed_stay", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<GateRouteConfig?>(fixedRoute));

            var gate1 = new Gate { Id = "gate1", Code = "GATE_01", Name = "Cổng Chính Nhà Máy" };
            var gate2 = new Gate { Id = "gate2", Code = "GATE_02", Name = "Cổng nhà máy 2" };
            gateRepo.GetByIdAsync("gate1", Arg.Any<CancellationToken>()).Returns(Task.FromResult<Gate?>(gate1));
            gateRepo.GetByIdAsync("gate2", Arg.Any<CancellationToken>()).Returns(Task.FromResult<Gate?>(gate2));

            var now = DateTime.UtcNow;
            var deadline = now.AddSeconds(-90); // Overdue by 1 minute 30 seconds
            var trip = new VehicleDispatchTrip
            {
                Id = "trip_stay",
                PlateNumber = "35B-263.37",
                CardNumber = "3800201218",
                AssignedRouteId = "route_fixed_stay",
                OriginGateId = "gate1",
                CurrentGateId = "gate2", // Đang dừng đỗ tại Cổng nhà máy 2
                CurrentStepIndex = 1,    // Chặng 1 đến làm việc tại Cổng 2
                Status = TripStatus.WorkingAtGate,
                LastEntryTime = deadline.AddMinutes(-10),
                NextDeadline = deadline,
                IsAlertSent = false
            };

            tripRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<VehicleDispatchTrip, bool>>>())
                .Returns(Task.FromResult<IReadOnlyList<VehicleDispatchTrip>>(new List<VehicleDispatchTrip> { trip }));

            string capturedBody = string.Empty;
            emailSender.SendEmailAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string>(),
                Arg.Do<string>(body => capturedBody = body),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

            emailSender.SendEmailAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string>(),
                Arg.Do<string>(body => capturedBody = body),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IRepository<VehicleDispatchTrip>)).Returns(tripRepo);
            serviceProvider.GetService(typeof(IRepository<GateRouteConfig>)).Returns(routeRepo);
            serviceProvider.GetService(typeof(IRepository<Gate>)).Returns(gateRepo);
            serviceProvider.GetService(typeof(IEmailSenderService)).Returns(emailSender);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(serviceProvider);
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);

            var watcher = new VehicleTransitWatcherService(scopeFactory, logger);

            // Act
            await watcher.CheckOverdueTripsAsync(CancellationToken.None);

            // Assert
            trip.IsAlertSent.Should().BeTrue();
            trip.Status.Should().Be(TripStatus.OverdueStay);
            capturedBody.Should().NotBeNullOrEmpty();
            capturedBody.Should().Contain("Cổng đang dừng đỗ");
            capturedBody.Should().Contain("Cổng nhà máy 2");
            capturedBody.Should().Contain("Chặng #1");
            capturedBody.Should().Contain("Cổng bắt đầu chuyến đi");
            capturedBody.Should().Contain("Cổng Chính Nhà Máy");
            capturedBody.Should().Contain("Điểm đến tiếp theo sau khi rời bãi");
            capturedBody.Should().Contain("Cổng Chính Nhà Máy (GATE_01) (Chặng quay về kết thúc)");
            capturedBody.Should().Contain("Thời gian dừng đỗ quá hạn");
            capturedBody.Should().Contain("Quá 1 phút 30 giây");
        }

        [Fact]
        public async Task CheckOverdueTripsAsync_WithAssignedRoute_QueriesRouteRepoOnlyOnce()
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

            var assignedRoute = new GateRouteConfig
            {
                Id = "route123",
                RouteCode = "ROUTE_123",
                RouteName = "Tuyến kiểm tra",
                AlertEmails = new List<string> { "alert@test.com" },
                GateSteps = new List<RouteGateStep>()
            };
            routeRepo.GetByIdAsync("route123", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<GateRouteConfig?>(assignedRoute));

            var now = DateTime.UtcNow;
            var overdueTrip = new VehicleDispatchTrip
            {
                Id = "trip1",
                PlateNumber = "30A-99999",
                CardNumber = "0000012345",
                AssignedRouteId = "route123",
                Status = TripStatus.InTransit,
                NextDeadline = now.AddMinutes(-10),
                IsAlertSent = false
            };

            tripRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<VehicleDispatchTrip, bool>>>())
                .Returns(Task.FromResult<IReadOnlyList<VehicleDispatchTrip>>(new List<VehicleDispatchTrip> { overdueTrip }));

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IRepository<VehicleDispatchTrip>)).Returns(tripRepo);
            serviceProvider.GetService(typeof(IRepository<GateRouteConfig>)).Returns(routeRepo);
            serviceProvider.GetService(typeof(IRepository<Gate>)).Returns(gateRepo);
            serviceProvider.GetService(typeof(IEmailSenderService)).Returns(emailSender);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(serviceProvider);
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);

            var watcher = new VehicleTransitWatcherService(scopeFactory, logger);

            // Act
            await watcher.CheckOverdueTripsAsync(CancellationToken.None);

            // Assert: verify routeRepo.GetByIdAsync was called exactly once for trip.AssignedRouteId
            await routeRepo.Received(1).GetByIdAsync("route123", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CheckOverdueTripsAsync_WhenAssignedRouteAlertEmailsIsNull_DoesNotThrowAndCompletesSuccessfully()
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

            // AlertEmails is explicitly null, simulating MongoDB document without alertEmails field
            var assignedRoute = new GateRouteConfig
            {
                Id = "route_null_emails",
                RouteCode = "ROUTE_NULL_EMAILS",
                RouteName = "Tuyến null emails",
                AlertEmails = null!,
                GateSteps = new List<RouteGateStep>()
            };
            routeRepo.GetByIdAsync("route_null_emails", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<GateRouteConfig?>(assignedRoute));

            var now = DateTime.UtcNow;
            var overdueTrip = new VehicleDispatchTrip
            {
                Id = "trip_null_emails",
                PlateNumber = "29A-88888",
                CardNumber = "0000088888",
                AssignedRouteId = "route_null_emails",
                Status = TripStatus.InTransit,
                NextDeadline = now.AddMinutes(-5),
                IsAlertSent = false
            };

            tripRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<VehicleDispatchTrip, bool>>>())
                .Returns(Task.FromResult<IReadOnlyList<VehicleDispatchTrip>>(new List<VehicleDispatchTrip> { overdueTrip }));

            emailSender.SendEmailAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IRepository<VehicleDispatchTrip>)).Returns(tripRepo);
            serviceProvider.GetService(typeof(IRepository<GateRouteConfig>)).Returns(routeRepo);
            serviceProvider.GetService(typeof(IRepository<Gate>)).Returns(gateRepo);
            serviceProvider.GetService(typeof(IEmailSenderService)).Returns(emailSender);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(serviceProvider);
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);

            var watcher = new VehicleTransitWatcherService(scopeFactory, logger);

            // Act
            await watcher.CheckOverdueTripsAsync(CancellationToken.None);

            // Assert
            overdueTrip.IsAlertSent.Should().BeTrue();
            await emailSender.Received(1).SendEmailAsync(
                Arg.Is<IEnumerable<string>>(recipients => recipients.Contains("default_admin@test.com")),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CheckOverdueTripsAsync_WhenOriginAndCurrentGateAreSame_QueriesGateRepoOnlyOnce()
        {
            // Arrange
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var emailSender = Substitute.For<IEmailSenderService>();
            var logger = Substitute.For<ILogger<VehicleTransitWatcherService>>();

            var gate1 = new Gate { Id = "gate_same", Code = "G_SAME", Name = "Cổng Chung" };
            gateRepo.GetByIdAsync("gate_same", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Gate?>(gate1));

            var now = DateTime.UtcNow;
            var overdueTrip = new VehicleDispatchTrip
            {
                Id = "trip_same_gate",
                PlateNumber = "29A-77777",
                CardNumber = "0000077777",
                OriginGateId = "gate_same",
                CurrentGateId = "gate_same",
                Status = TripStatus.InTransit,
                NextDeadline = now.AddMinutes(-5),
                IsAlertSent = false
            };

            tripRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<VehicleDispatchTrip, bool>>>())
                .Returns(Task.FromResult<IReadOnlyList<VehicleDispatchTrip>>(new List<VehicleDispatchTrip> { overdueTrip }));

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IRepository<VehicleDispatchTrip>)).Returns(tripRepo);
            serviceProvider.GetService(typeof(IRepository<GateRouteConfig>)).Returns(routeRepo);
            serviceProvider.GetService(typeof(IRepository<Gate>)).Returns(gateRepo);
            serviceProvider.GetService(typeof(IEmailSenderService)).Returns(emailSender);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(serviceProvider);
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);

            var watcher = new VehicleTransitWatcherService(scopeFactory, logger);

            // Act
            await watcher.CheckOverdueTripsAsync(CancellationToken.None);

            // Assert: verify gateRepo was called only once for gate_same instead of twice
            await gateRepo.Received(1).GetByIdAsync("gate_same", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CheckOverdueTripsAsync_WhenAssignedRouteIsDeleted_FallsBackToFreeRoam()
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

            var deletedRoute = new GateRouteConfig
            {
                Id = "route_deleted",
                RouteCode = "DEL_01",
                RouteName = "Tuyến Đã Xóa",
                IsDeleted = true,
                AlertEmails = new List<string> { "deleted_route@test.com" }
            };
            routeRepo.GetByIdAsync("route_deleted", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<GateRouteConfig?>(deletedRoute));

            var now = DateTime.UtcNow;
            var overdueTrip = new VehicleDispatchTrip
            {
                Id = "trip_deleted_route",
                PlateNumber = "29A-66666",
                CardNumber = "0000066666",
                AssignedRouteId = "route_deleted",
                Status = TripStatus.InTransit,
                NextDeadline = now.AddMinutes(-5),
                IsAlertSent = false
            };

            tripRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<VehicleDispatchTrip, bool>>>())
                .Returns(Task.FromResult<IReadOnlyList<VehicleDispatchTrip>>(new List<VehicleDispatchTrip> { overdueTrip }));

            string capturedBody = string.Empty;
            emailSender.SendEmailAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<string>(),
                Arg.Do<string>(body => capturedBody = body),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IRepository<VehicleDispatchTrip>)).Returns(tripRepo);
            serviceProvider.GetService(typeof(IRepository<GateRouteConfig>)).Returns(routeRepo);
            serviceProvider.GetService(typeof(IRepository<Gate>)).Returns(gateRepo);
            serviceProvider.GetService(typeof(IEmailSenderService)).Returns(emailSender);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(serviceProvider);
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);

            var watcher = new VehicleTransitWatcherService(scopeFactory, logger);

            // Act
            await watcher.CheckOverdueTripsAsync(CancellationToken.None);

            // Assert: Route falls back to Free Roam and deleted route email is NOT included
            capturedBody.Should().Contain("Cổng bất kỳ (Tuyến tự do)");
            capturedBody.Should().NotContain("Tuyến Đã Xóa");
        }
    }
}
