using FluentAssertions;
using HPParking.Api.DTOs.ParkingSessions;
using HPParking.Api.Services.Implementations;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class ParkingSessionKeywordFilterTests
    {
        private readonly IRepository<ParkingSession> _sessionRepo;
        private readonly IRepository<Client> _clientRepo;
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly ILogger<ParkingSessionService> _logger;
        private readonly ParkingSessionService _service;

        public ParkingSessionKeywordFilterTests()
        {
            _sessionRepo = Substitute.For<IRepository<ParkingSession>>();
            _clientRepo = Substitute.For<IRepository<Client>>();
            _vehicleRepo = Substitute.For<IRepository<Vehicle>>();
            _logger = Substitute.For<ILogger<ParkingSessionService>>();

            _service = new ParkingSessionService(
                _sessionRepo,
                _clientRepo,
                _vehicleRepo,
                _logger);
        }

        [Fact]
        public async Task GetParkingSessionsPagedAsync_WhenPedestrianAndKeywordGiven_QueriesClientsAndFiltersSessions()
        {
            // Arrange
            var query = new ParkingSessionFilterQuery
            {
                TargetType = LaneTargetType.Pedestrian,
                Keyword = "Nguyễn",
                PageIndex = 1,
                PageSize = 10
            };

            var matchedClients = new List<Client>
            {
                new Client { Id = "client-001", Name = "Nguyễn Văn A", Code = "NV001" }
            };

            _clientRepo.FindAsync(Arg.Any<FilterDefinition<Client>>(), cancellationToken: Arg.Any<CancellationToken>())
                .Returns(matchedClients);

            var sessions = new List<ParkingSession>
            {
                new ParkingSession
                {
                    Id = "sess-001",
                    PersonId = "client-001",
                    TargetType = LaneTargetType.Pedestrian,
                    Status = ParkingSessionStatus.Active,
                    InTime = DateTime.UtcNow
                }
            };

            _sessionRepo.CountAsync(Arg.Any<FilterDefinition<ParkingSession>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(1);

            _sessionRepo.FindAsync(
                Arg.Any<FilterDefinition<ParkingSession>>(),
                Arg.Any<SortDefinition<ParkingSession>>(),
                0,
                10,
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
                .Returns(sessions);

            // Act
            var result = await _service.GetParkingSessionsPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.Items[0].PersonFullName.Should().Be("Nguyễn Văn A");
            result.Items[0].PersonCode.Should().Be("NV001");
            await _clientRepo.Received(2).FindAsync(Arg.Any<FilterDefinition<Client>>(), cancellationToken: Arg.Any<CancellationToken>());
        }
    }
}
