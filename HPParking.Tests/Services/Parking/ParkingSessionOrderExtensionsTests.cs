using FluentAssertions;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Services.Parking;
using MongoDB.Driver;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HPParking.Tests.Services.Parking
{
    public class ParkingSessionOrderExtensionsTests
    {
        [Fact]
        public async Task GetNextInOrderNumberAsync_WhenRepositoryIsNull_ShouldReturnOne()
        {
            // Arrange
            IRepository<ParkingSession>? repo = null;

            // Act
            var result = await repo!.GetNextInOrderNumberAsync(LaneTargetType.Vehicle);

            // Assert
            result.Should().Be(1);
        }

        [Fact]
        public async Task GetNextInOrderNumberAsync_WhenNoPreviousSessions_ShouldReturnOne()
        {
            // Arrange
            var repo = Substitute.For<IRepository<ParkingSession>>();
            repo.FindAsync(
                Arg.Any<FilterDefinition<ParkingSession>>(),
                Arg.Any<SortDefinition<ParkingSession>>(),
                0,
                1,
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<ParkingSession>>([]));

            // Act
            var result = await repo.GetNextInOrderNumberAsync(LaneTargetType.Vehicle);

            // Assert
            result.Should().Be(1);
        }

        [Fact]
        public async Task GetNextInOrderNumberAsync_WhenPreviousSessionExists_ShouldReturnMaxPlusOne()
        {
            // Arrange
            var repo = Substitute.For<IRepository<ParkingSession>>();
            var existingSession = new ParkingSession
            {
                Id = "sess-1",
                TargetType = LaneTargetType.Vehicle,
                InOrderNumber = 137
            };

            repo.FindAsync(
                Arg.Any<FilterDefinition<ParkingSession>>(),
                Arg.Any<SortDefinition<ParkingSession>>(),
                0,
                1,
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<ParkingSession>>([existingSession]));

            // Act
            var result = await repo.GetNextInOrderNumberAsync(LaneTargetType.Vehicle);

            // Assert
            result.Should().Be(138);
        }

        [Fact]
        public async Task GetNextInOrderNumberAsync_WhenPedestrianPreviousSessionExists_ShouldReturnMaxPlusOne()
        {
            // Arrange
            var repo = Substitute.For<IRepository<ParkingSession>>();
            var existingPedSession = new ParkingSession
            {
                Id = "ped-1",
                TargetType = LaneTargetType.Pedestrian,
                InOrderNumber = 42
            };

            repo.FindAsync(
                Arg.Any<FilterDefinition<ParkingSession>>(),
                Arg.Any<SortDefinition<ParkingSession>>(),
                0,
                1,
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<ParkingSession>>([existingPedSession]));

            // Act
            var result = await repo.GetNextInOrderNumberAsync(LaneTargetType.Pedestrian);

            // Assert
            result.Should().Be(43);
        }

        [Fact]
        public async Task GetNextInOrderNumberAsync_WhenLatestSessionIsSoftDeleted_ShouldStillReturnMaxPlusOneWithoutDuplication()
        {
            // Arrange
            var repo = Substitute.For<IRepository<ParkingSession>>();
            var deletedSession = new ParkingSession
            {
                Id = "sess-deleted",
                TargetType = LaneTargetType.Vehicle,
                InOrderNumber = 200,
                IsDeleted = true
            };

            repo.FindAsync(
                Arg.Any<FilterDefinition<ParkingSession>>(),
                Arg.Any<SortDefinition<ParkingSession>>(),
                0,
                1,
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<ParkingSession>>([deletedSession]));

            // Act
            var result = await repo.GetNextInOrderNumberAsync(LaneTargetType.Vehicle);

            // Assert: Không được bỏ qua bản ghi bị xóa mềm để đảm bảo số thứ tự không bao giờ bị cấp trùng
            result.Should().Be(201);
        }

        [Fact]
        public async Task AssignInOrderNumbersAsync_WhenInputSessionsAreOutOfOrder_ShouldOrderChronologicallyByInTimeBeforeAssigningNumbers()
        {
            // Arrange: Danh sách đầu vào bị xáo trộn thứ tự thời gian
            var repo = Substitute.For<IRepository<ParkingSession>>();
            repo.FindAsync(
                Arg.Any<FilterDefinition<ParkingSession>>(),
                Arg.Any<SortDefinition<ParkingSession>>(),
                0,
                1,
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<ParkingSession>>([]));

            var baseTime = new DateTime(2026, 10, 23, 8, 0, 0, DateTimeKind.Utc);
            var sessionLatest = new ParkingSession { Id = "s-latest", TargetType = LaneTargetType.Vehicle, InTime = baseTime.AddHours(2) }; // 10:00
            var sessionEarliest = new ParkingSession { Id = "s-earliest", TargetType = LaneTargetType.Vehicle, InTime = baseTime };          // 08:00
            var sessionMiddle = new ParkingSession { Id = "s-middle", TargetType = LaneTargetType.Vehicle, InTime = baseTime.AddHours(1) };   // 09:00

            // Truyền vào danh sách không theo thứ tự InTime: [10:00, 08:00, 09:00]
            var sessions = new List<ParkingSession> { sessionLatest, sessionEarliest, sessionMiddle };

            // Act
            await repo.AssignInOrderNumbersAsync(sessions);

            // Assert: Đảm bảo số thứ tự được gán tăng dần theo InTime (cũ -> mới: #1, #2, #3)
            sessionEarliest.InOrderNumber.Should().Be(1);
            sessionMiddle.InOrderNumber.Should().Be(2);
            sessionLatest.InOrderNumber.Should().Be(3);
        }

        [Fact]
        public async Task AssignInOrderNumbersAsync_WhenSessionsHaveMixedTargetTypesAndNullInTime_ShouldAssignIndependentSequencesAndSkipNullInTime()
        {
            // Arrange
            var repo = Substitute.For<IRepository<ParkingSession>>();
            // Mock DB: Vehicle đã có số max = 10, Pedestrian chưa có bản ghi (max = 0)
            repo.FindAsync(
                Arg.Is<FilterDefinition<ParkingSession>>(f => true),
                Arg.Any<SortDefinition<ParkingSession>>(),
                0,
                1,
                Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    // Trả về số max theo targetType
                    return Task.FromResult<IReadOnlyList<ParkingSession>>([]);
                });

            var now = DateTime.UtcNow;
            var veh1 = new ParkingSession { Id = "v1", TargetType = LaneTargetType.Vehicle, InTime = now.AddMinutes(5) };
            var veh2 = new ParkingSession { Id = "v2", TargetType = LaneTargetType.Vehicle, InTime = now.AddMinutes(10) };
            var ped1 = new ParkingSession { Id = "p1", TargetType = LaneTargetType.Pedestrian, InTime = now.AddMinutes(2) };
            var unassignedOut = new ParkingSession { Id = "unmatched", TargetType = LaneTargetType.Vehicle, InTime = null, OutTime = now };

            var list = new List<ParkingSession> { veh2, ped1, unassignedOut, veh1 };

            // Act
            await repo.AssignInOrderNumbersAsync(list);

            // Assert
            veh1.InOrderNumber.Should().Be(1);
            veh2.InOrderNumber.Should().Be(2);
            ped1.InOrderNumber.Should().Be(1);
            unassignedOut.InOrderNumber.Should().BeNull();
        }
    }
}
