using FluentAssertions;
using HPParking.Api.Services.Implementations;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Linq.Expressions;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class StatisticsServiceTests
    {
        private readonly IRepository<Client> _clientRepo = Substitute.For<IRepository<Client>>();
        private readonly IRepository<Vehicle> _vehicleRepo = Substitute.For<IRepository<Vehicle>>();
        private readonly IRepository<ParkingSession> _sessionRepo = Substitute.For<IRepository<ParkingSession>>();
        private readonly IRepository<Company> _companyRepo = Substitute.For<IRepository<Company>>();
        private readonly IRepository<Department> _departmentRepo = Substitute.For<IRepository<Department>>();
        private readonly IRepository<Contractor> _contractorRepo = Substitute.For<IRepository<Contractor>>();
        private readonly IRepository<Gate> _gateRepo = Substitute.For<IRepository<Gate>>();
        private readonly IRepository<Lane> _laneRepo = Substitute.For<IRepository<Lane>>();
        private readonly ILogger<StatisticsService> _logger = Substitute.For<ILogger<StatisticsService>>();

        private StatisticsService CreateService()
        {
            return new StatisticsService(
                _clientRepo,
                _vehicleRepo,
                _sessionRepo,
                _companyRepo,
                _departmentRepo,
                _contractorRepo,
                _gateRepo,
                _laneRepo,
                _logger,
                mongoContext: null);
        }

        [Fact]
        public async Task GetDashboardStatisticsAsync_ViaRepo_CorrectlyCalculatesTotalRevenueAndPaidCount()
        {
            // Arrange
            var service = CreateService();

            IReadOnlyList<Client> clients = new List<Client>
            {
                new Client { Id = "c1", IsActive = true, Type = ClientType.Employee, Avatar = "http://avatar1.jpg" },
                new Client { Id = "c2", IsActive = true, Type = ClientType.Contractor },
                new Client { Id = "c3", IsActive = false, Type = ClientType.Visitor }
            };
            _clientRepo.FindAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(clients));

            IReadOnlyList<Vehicle> vehicles = new List<Vehicle>
            {
                new Vehicle { Id = "v1", IsActive = true, Type = VehicleType.Car },
                new Vehicle { Id = "v2", IsActive = false, Type = VehicleType.Motorbike }
            };
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(vehicles));

            _sessionRepo.CountAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(5L));

            IReadOnlyList<ParkingSession> completedPaidSessions = new List<ParkingSession>
            {
                new ParkingSession
                {
                    Id = "ps1",
                    Status = ParkingSessionStatus.Completed,
                    IsFeeCollected = true,
                    FeeAmount = 50000m,
                    IsDeleted = false
                },
                new ParkingSession
                {
                    Id = "ps2",
                    Status = ParkingSessionStatus.Completed,
                    IsFeeCollected = true,
                    FeeAmount = 75000m,
                    IsDeleted = false
                }
            };

            _sessionRepo.FindAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(completedPaidSessions));

            _gateRepo.CountAsync(Arg.Any<Expression<Func<Gate, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(2L));

            _laneRepo.CountAsync(Arg.Any<Expression<Func<Lane, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(4L));

            // Act
            var result = await service.GetDashboardStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.TotalClients.Should().Be(3);
            result.ActiveClients.Should().Be(2);
            result.ClientsWithFaceId.Should().Be(1);
            result.TotalVehicles.Should().Be(2);
            result.ActiveVehicles.Should().Be(1);
            result.ActiveParkingSessions.Should().Be(5);
            result.TotalGates.Should().Be(2);
            result.TotalLanes.Should().Be(4);
            result.TotalRevenue.Should().Be(125000m); // 50000 + 75000
            result.PaidParkingSessionsCount.Should().Be(2);
        }
    }
}

