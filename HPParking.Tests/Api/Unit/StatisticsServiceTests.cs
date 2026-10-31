using FluentAssertions;
using HPParking.Api.Common.Helpers;
using HPParking.Api.DTOs.Statistics;
using HPParking.Api.Services.Implementations;
using HPParking.Core.Helpers;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
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
                    IsDeleted = false,
                    OutTime = DateTime.UtcNow
                },
                new ParkingSession
                {
                    Id = "ps2",
                    Status = ParkingSessionStatus.Completed,
                    IsFeeCollected = true,
                    FeeAmount = 75000m,
                    IsDeleted = false,
                    OutTime = DateTime.UtcNow
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

        [Fact]
        public async Task GetRevenueReportAsync_ZeroDivisionGuard_ReturnsZeroWithoutException()
        {
            // Arrange
            var service = CreateService();
            _sessionRepo.FindAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<ParkingSession>>(new List<ParkingSession>()));

            var query = new RevenueStatisticsQuery
            {
                FromDate = new DateTime(2026, 10, 20),
                ToDate = new DateTime(2026, 10, 22)
            };

            // Act
            var report = await service.GetRevenueReportAsync(query);

            // Assert
            report.Should().NotBeNull();
            report.TotalRevenue.Should().Be(0m);
            report.PaidSessionsCount.Should().Be(0);
            report.CompletedSessionsCount.Should().Be(0);
            report.AverageFeePerSession.Should().Be(0m);
            report.PaidRatio.Should().Be(0.0);
            report.Timeline.Should().HaveCount(3);
            report.Timeline.All(t => t.Revenue == 0m && t.PaidCount == 0 && t.CompletedCount == 0 && t.AverageFee == 0m).Should().BeTrue();
        }

        [Fact]
        public async Task GetRevenueReportAsync_VietnamTimeGrouping_CorrectlyClassifiesEarlyMorningSessions()
        {
            // Arrange
            var service = CreateService();

            // 23:30 UTC ngày 27/10 = 06:30 VN ngày 28/10
            var earlyMorningUtc = new DateTime(2026, 10, 27, 23, 30, 0, DateTimeKind.Utc);
            var sessions = new List<ParkingSession>
            {
                new ParkingSession
                {
                    Id = "ps_early",
                    Status = ParkingSessionStatus.Completed,
                    IsFeeCollected = true,
                    FeeAmount = 50000m,
                    IsDeleted = false,
                    OutTime = earlyMorningUtc
                }
            };

            _sessionRepo.FindAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var expr = callInfo.Arg<Expression<Func<ParkingSession, bool>>>();
                    var compiled = expr.Compile();
                    return Task.FromResult<IReadOnlyList<ParkingSession>>(sessions.Where(compiled).ToList());
                });

            var query = new RevenueStatisticsQuery
            {
                FromDate = new DateTime(2026, 10, 28),
                ToDate = new DateTime(2026, 10, 28)
            };

            // Act
            var report = await service.GetRevenueReportAsync(query);

            // Assert
            report.TotalRevenue.Should().Be(50000m);
            report.PaidSessionsCount.Should().Be(1);
            report.CompletedSessionsCount.Should().Be(1);
            report.Timeline.Should().HaveCount(1);
            report.Timeline[0].Date.Should().Be(new DateOnly(2026, 10, 28));
            report.Timeline[0].Revenue.Should().Be(50000m);
            report.Timeline[0].PaidCount.Should().Be(1);
        }

        [Fact]
        public async Task GetRevenueReportAsync_ReversedRange_AutomaticallySwapsDatesAndFillsTimeline()
        {
            // Arrange
            var service = CreateService();
            _sessionRepo.FindAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<ParkingSession>>(new List<ParkingSession>()));

            var query = new RevenueStatisticsQuery
            {
                FromDate = new DateTime(2026, 10, 28),
                ToDate = new DateTime(2026, 10, 26) // from > to
            };

            // Act
            var report = await service.GetRevenueReportAsync(query);

            // Assert
            report.Timeline.Should().HaveCount(3);
            report.Timeline[0].Date.Should().Be(new DateOnly(2026, 10, 26));
            report.Timeline[1].Date.Should().Be(new DateOnly(2026, 10, 27));
            report.Timeline[2].Date.Should().Be(new DateOnly(2026, 10, 28));
        }

        [Fact]
        public async Task GetRevenueReportAsync_FromOnly_RangesFromFromDateToToday()
        {
            // Arrange
            var service = CreateService();
            _sessionRepo.FindAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<ParkingSession>>(new List<ParkingSession>()));

            var todayVn = VietnamTime.ToVietnamTime(DateTime.UtcNow).Date;
            var query = new RevenueStatisticsQuery
            {
                FromDate = todayVn.AddDays(-2),
                ToDate = null
            };

            // Act
            var report = await service.GetRevenueReportAsync(query);

            // Assert
            report.Timeline.Should().HaveCount(3);
            report.Timeline[0].Date.Should().Be(DateOnly.FromDateTime(todayVn.AddDays(-2)));
            report.Timeline[1].Date.Should().Be(DateOnly.FromDateTime(todayVn.AddDays(-1)));
            report.Timeline[2].Date.Should().Be(DateOnly.FromDateTime(todayVn));
        }

        [Fact]
        public async Task GetRevenueReportAsync_FiltersOutInvalidSessions_AndAppliesZeroFill()
        {
            // Arrange
            var service = CreateService();
            var targetDate = new DateTime(2026, 10, 20);

            var sessions = new List<ParkingSession>
            {
                // 1. Hợp lệ có thu tiền ngày 20
                new ParkingSession
                {
                    Id = "s1",
                    Status = ParkingSessionStatus.Completed,
                    IsFeeCollected = true,
                    FeeAmount = 30000m,
                    IsDeleted = false,
                    OutTime = targetDate.AddHours(9)
                },
                // 2. Hợp lệ chưa thu tiền ngày 20 (miễn phí)
                new ParkingSession
                {
                    Id = "s2",
                    Status = ParkingSessionStatus.Completed,
                    IsFeeCollected = false,
                    FeeAmount = 0m,
                    IsDeleted = false,
                    OutTime = targetDate.AddHours(10)
                },
                // 3. Đã xóa mềm -> bỏ qua
                new ParkingSession
                {
                    Id = "s3",
                    Status = ParkingSessionStatus.Completed,
                    IsFeeCollected = true,
                    FeeAmount = 50000m,
                    IsDeleted = true,
                    OutTime = targetDate.AddHours(11)
                },
                // 4. Chưa hoàn thành (Active) -> bỏ qua
                new ParkingSession
                {
                    Id = "s4",
                    Status = ParkingSessionStatus.Active,
                    IsFeeCollected = false,
                    FeeAmount = 15000m,
                    IsDeleted = false,
                    OutTime = targetDate.AddHours(12)
                },
                // 5. OutTime null -> bỏ qua
                new ParkingSession
                {
                    Id = "s5",
                    Status = ParkingSessionStatus.Completed,
                    IsFeeCollected = true,
                    FeeAmount = 25000m,
                    IsDeleted = false,
                    OutTime = null
                }
            };

            _sessionRepo.FindAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var expr = callInfo.Arg<Expression<Func<ParkingSession, bool>>>();
                    var compiled = expr.Compile();
                    return Task.FromResult<IReadOnlyList<ParkingSession>>(sessions.Where(compiled).ToList());
                });

            var query = new RevenueStatisticsQuery
            {
                FromDate = targetDate,
                ToDate = targetDate.AddDays(2) // 20, 21, 22
            };

            // Act
            var report = await service.GetRevenueReportAsync(query);

            // Assert
            report.TotalRevenue.Should().Be(30000m);
            report.PaidSessionsCount.Should().Be(1);
            report.CompletedSessionsCount.Should().Be(2); // s1 + s2
            report.AverageFeePerSession.Should().Be(30000m);
            report.PaidRatio.Should().Be(50.0); // 1 / 2 * 100%

            report.Timeline.Should().HaveCount(3);
            // Ngày 20
            report.Timeline[0].Date.Should().Be(new DateOnly(2026, 10, 20));
            report.Timeline[0].Revenue.Should().Be(30000m);
            report.Timeline[0].PaidCount.Should().Be(1);
            report.Timeline[0].CompletedCount.Should().Be(2);
            report.Timeline[0].AverageFee.Should().Be(30000m);

            // Ngày 21 (Zero-fill)
            report.Timeline[1].Date.Should().Be(new DateOnly(2026, 10, 21));
            report.Timeline[1].Revenue.Should().Be(0m);
            report.Timeline[1].PaidCount.Should().Be(0);
            report.Timeline[1].CompletedCount.Should().Be(0);

            // Ngày 22 (Zero-fill)
            report.Timeline[2].Date.Should().Be(new DateOnly(2026, 10, 22));
            report.Timeline[2].Revenue.Should().Be(0m);
            report.Timeline[2].PaidCount.Should().Be(0);
            report.Timeline[2].CompletedCount.Should().Be(0);
        }

        [Fact]
        public async Task GetRevenueReportAsync_FromDateInFutureAndToDateNull_ClampsToTodayAndDoesNotOpenFuture()
        {
            // Arrange: fromDate là ngày mai, toDate = null
            var service = CreateService();
            _sessionRepo.FindAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<ParkingSession>>(new List<ParkingSession>()));

            var todayVn = VietnamTime.ToVietnamTime(DateTime.UtcNow).Date;
            var tomorrow = todayVn.AddDays(1);
            var query = new RevenueStatisticsQuery
            {
                FromDate = tomorrow,
                ToDate = null
            };

            // Act
            var report = await service.GetRevenueReportAsync(query);

            // Assert: Không crash, tuân thủ Spec §1.1 Case 2 "từ ngày FromDate đến hết hôm nay"
            // Khi FromDate > todayVn, kẹp về todayVn, tuyệt đối không mở range về tương lai (Count = 1).
            report.Timeline.Should().HaveCount(1);
            report.Timeline[0].Date.Should().Be(DateOnly.FromDateTime(todayVn));
        }

        [Fact]
        public async Task GetRevenueReportAsync_WhenSessionsHaveZeroFee_ShouldNotCountThemAsPaidSessions()
        {
            // Arrange
            var service = CreateService();
            var targetDate = new DateTime(2026, 10, 30);

            var sessions = new List<ParkingSession>
            {
                // Lượt 1: Có thu phí 25.000đ
                new ParkingSession
                {
                    Id = "s1",
                    Status = ParkingSessionStatus.Completed,
                    IsFeeCollected = true,
                    FeeAmount = 25000m,
                    IsDeleted = false,
                    OutTime = targetDate.AddHours(9)
                },
                // Lượt 2: Không thu phí (0đ) nhưng IsFeeCollected bị đánh dấu true
                new ParkingSession
                {
                    Id = "s2",
                    Status = ParkingSessionStatus.Completed,
                    IsFeeCollected = true,
                    FeeAmount = 0m,
                    IsDeleted = false,
                    OutTime = targetDate.AddHours(10)
                },
                // Lượt 3: Không thu phí (0đ) vé tháng
                new ParkingSession
                {
                    Id = "s3",
                    Status = ParkingSessionStatus.Completed,
                    IsFeeCollected = false,
                    FeeAmount = 0m,
                    IsDeleted = false,
                    OutTime = targetDate.AddHours(11)
                }
            };

            _sessionRepo.FindAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var expr = callInfo.Arg<Expression<Func<ParkingSession, bool>>>();
                    var compiled = expr.Compile();
                    return Task.FromResult<IReadOnlyList<ParkingSession>>(sessions.Where(compiled).ToList());
                });

            var query = new RevenueStatisticsQuery
            {
                FromDate = targetDate,
                ToDate = targetDate
            };

            // Act
            var report = await service.GetRevenueReportAsync(query);

            // Assert
            report.TotalRevenue.Should().Be(25000m);
            report.CompletedSessionsCount.Should().Be(3);
            report.PaidSessionsCount.Should().Be(1); // Chỉ tính lượt thực sự phát sinh thu phí > 0
            report.AverageFeePerSession.Should().Be(25000m); // 25.000 / 1 lượt = 25.000đ, không bị chia cho các lượt 0đ
            report.PaidRatio.Should().Be(33.3); // 1 / 3 = 33.3%
        }

        [Fact]
        public async Task GetRevenueReportAsync_CallsRepoWithDateRangePredicate_AvoidingInMemoryFullLoad()
        {
            // Arrange
            var service = CreateService();
            Expression<Func<ParkingSession, bool>>? capturedPredicate = null;

            _sessionRepo.FindAsync(Arg.Do<Expression<Func<ParkingSession, bool>>>(p => capturedPredicate = p), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<ParkingSession>>(new List<ParkingSession>()));

            var fromDate = new DateTime(2026, 10, 25);
            var toDate = new DateTime(2026, 10, 27);
            var (_, _, fromUtc, toUtc) = DateRangeHelper.NormalizeDateRange(fromDate, toDate);

            // Act
            await service.GetRevenueReportAsync(new RevenueStatisticsQuery { FromDate = fromDate, ToDate = toDate });

            // Assert: Predicate phải lọc OutTime >= fromUtc && OutTime <= toUtc tại database layer
            capturedPredicate.Should().NotBeNull();
            var compiled = capturedPredicate!.Compile();

            // 1. Session trong khoảng [fromUtc, toUtc] -> true
            var validSession = new ParkingSession
            {
                IsDeleted = false,
                Status = ParkingSessionStatus.Completed,
                OutTime = fromUtc.AddHours(5)
            };
            compiled(validSession).Should().BeTrue();

            // 2. Session trước fromUtc -> false
            var beforeSession = new ParkingSession
            {
                IsDeleted = false,
                Status = ParkingSessionStatus.Completed,
                OutTime = fromUtc.AddSeconds(-1)
            };
            compiled(beforeSession).Should().BeFalse();

            // 3. Session sau toUtc -> false
            var afterSession = new ParkingSession
            {
                IsDeleted = false,
                Status = ParkingSessionStatus.Completed,
                OutTime = toUtc.AddSeconds(1)
            };
            compiled(afterSession).Should().BeFalse();

        }

        [Fact]
        public async Task GetDashboardStatisticsAsync_CallsRepoWithDateRangePredicate_AvoidingInMemoryFullLoad()
        {
            // Arrange
            var service = CreateService();
            Expression<Func<ParkingSession, bool>>? capturedPredicate = null;

            _sessionRepo.FindAsync(Arg.Do<Expression<Func<ParkingSession, bool>>>(p => capturedPredicate = p), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<ParkingSession>>(new List<ParkingSession>()));

            _clientRepo.FindAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Client>>(new List<Client>()));
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>(new List<Vehicle>()));
            _sessionRepo.CountAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(0L));
            _gateRepo.CountAsync(Arg.Any<Expression<Func<Gate, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(0L));
            _laneRepo.CountAsync(Arg.Any<Expression<Func<Lane, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(0L));

            var fromDate = new DateTime(2026, 10, 25);
            var toDate = new DateTime(2026, 10, 27);
            var (_, _, fromUtc, toUtc) = DateRangeHelper.NormalizeDateRange(fromDate, toDate);

            // Act
            await service.GetDashboardStatisticsAsync(fromDate, toDate);

            // Assert: Predicate phải lọc OutTime >= fromUtc && OutTime <= toUtc tại database layer
            capturedPredicate.Should().NotBeNull();
            var compiled = capturedPredicate!.Compile();

            // 1. Session hợp lệ trong khoảng [fromUtc, toUtc] -> true
            var validSession = new ParkingSession
            {
                IsDeleted = false,
                Status = ParkingSessionStatus.Completed,
                IsFeeCollected = true,
                FeeAmount = 50000m,
                OutTime = fromUtc.AddHours(5)
            };
            compiled(validSession).Should().BeTrue();

            // 2. Session chưa thu phí -> false
            var unpaidSession = new ParkingSession
            {
                IsDeleted = false,
                Status = ParkingSessionStatus.Completed,
                IsFeeCollected = false,
                OutTime = fromUtc.AddHours(5)
            };
            compiled(unpaidSession).Should().BeFalse();

            // 3. Session trước fromUtc -> false
            var beforeSession = new ParkingSession
            {
                IsDeleted = false,
                Status = ParkingSessionStatus.Completed,
                IsFeeCollected = true,
                FeeAmount = 50000m,
                OutTime = fromUtc.AddSeconds(-1)
            };
            compiled(beforeSession).Should().BeFalse();

            // 4. Session sau toUtc -> false
            var afterSession = new ParkingSession
            {
                IsDeleted = false,
                Status = ParkingSessionStatus.Completed,
                IsFeeCollected = true,
                FeeAmount = 50000m,
                OutTime = toUtc.AddSeconds(1)
            };
            compiled(afterSession).Should().BeFalse();
            // 5. Session có FeeAmount == 0 -> false (không tính vào PaidSessions)
            var zeroFeeSession = new ParkingSession
            {
                IsDeleted = false,
                Status = ParkingSessionStatus.Completed,
                IsFeeCollected = true,
                FeeAmount = 0m,
                OutTime = fromUtc.AddHours(5)
            };
            compiled(zeroFeeSession).Should().BeFalse();
        }

        [Fact]
        public async Task GetTrafficSummaryAsync_QueriesOnlyRelevantVehiclesAndClients_WithoutFullTableLoad()
        {
            // Arrange
            var service = CreateService();
            var sessions = new List<ParkingSession>
            {
                new ParkingSession
                {
                    Id = "s1",
                    PlateNumber = "29A-12345",
                    PersonId = "client-1",
                    VehicleType = VehicleType.Car,
                    InTime = DateTime.UtcNow.AddHours(-2),
                    OutTime = DateTime.UtcNow,
                    Status = ParkingSessionStatus.Completed,
                    IsDeleted = false
                }
            };

            _sessionRepo.FindAsync(
                Arg.Any<FilterDefinition<ParkingSession>>(),
                Arg.Any<SortDefinition<ParkingSession>?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<ParkingSession>>(sessions));

            var vehicles = new List<Vehicle>
            {
                new Vehicle { Id = "v1", PlateNumber = "29A-12345", OwnerClientId = "client-1", IsDeleted = false }
            };
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var expr = callInfo.Arg<Expression<Func<Vehicle, bool>>>();
                    var compiled = expr.Compile();
                    return Task.FromResult<IReadOnlyList<Vehicle>>(vehicles.Where(compiled).ToList());
                });

            var clients = new List<Client>
            {
                new Client { Id = "client-1", Code = "NV001", Name = "Nguyen Van A", Type = ClientType.Employee, IsDeleted = false }
            };
            _clientRepo.FindAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var expr = callInfo.Arg<Expression<Func<Client, bool>>>();
                    var compiled = expr.Compile();
                    return Task.FromResult<IReadOnlyList<Client>>(clients.Where(compiled).ToList());
                });

            _companyRepo.FindAsync(Arg.Any<Expression<Func<Company, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Company>>(new List<Company>()));
            _departmentRepo.FindAsync(Arg.Any<Expression<Func<Department, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Department>>(new List<Department>()));
            _contractorRepo.FindAsync(Arg.Any<Expression<Func<Contractor, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Contractor>>(new List<Contractor>()));

            // Act
            var result = await service.GetTrafficSummaryAsync(new TrafficSummaryFilterQuery
            {
                PlateNumber = "29A-12345"
            });

            // Assert
            result.Should().HaveCount(1);
            var item = result[0];
            item.PlateNumber.Should().Be("29A-12345");
            item.ClientCode.Should().Be("NV001");
            item.ClientName.Should().Be("Nguyen Van A");
            item.ClientType.Should().Be(ClientType.Employee);
            item.InCount.Should().Be(1);
            item.OutCount.Should().Be(1);
            item.CompletedCount.Should().Be(1);
        }

        [Fact]
        public void NormalizeDateRange_DirectEdgeCases_CalculatesAccurateVnAndUtcWindows()
        {
            // 1. Both null
            var todayVn = VietnamTime.ToVietnamTime(DateTime.UtcNow).Date;
            var (fromVn, toVn, fromUtc, toUtc) = DateRangeHelper.NormalizeDateRange(null, null);
            fromVn.Should().Be(todayVn);
            toVn.Date.Should().Be(todayVn);
            fromUtc.Should().Be(DateTime.SpecifyKind(todayVn.Subtract(VietnamTime.VietnamOffset), DateTimeKind.Utc));

            // 2. Reversed range: from > to
            var dateA = new DateTime(2026, 10, 28);
            var dateB = new DateTime(2026, 10, 25);
            var (revFromVn, revToVn, revFromUtc, revToUtc) = DateRangeHelper.NormalizeDateRange(dateA, dateB);
            revFromVn.Date.Should().Be(new DateTime(2026, 10, 25));
            revToVn.Date.Should().Be(new DateTime(2026, 10, 28));
            revFromUtc.Should().Be(DateTime.SpecifyKind(new DateTime(2026, 10, 25).Subtract(VietnamTime.VietnamOffset), DateTimeKind.Utc));
            revToUtc.Should().Be(DateTime.SpecifyKind(new DateTime(2026, 10, 28).AddDays(1).AddTicks(-1).Subtract(VietnamTime.VietnamOffset), DateTimeKind.Utc));

            // 3. ToDate only
            var (toOnlyFromVn, toOnlyToVn, _, _) = DateRangeHelper.NormalizeDateRange(null, dateA);
            toOnlyFromVn.Date.Should().Be(dateA);
            toOnlyToVn.Date.Should().Be(dateA);

            // 4. FromDate only: quá khứ -> [past, todayVn]
            var pastDate = todayVn.AddDays(-5);
            var (fromPastVn, toPastVn, _, _) = DateRangeHelper.NormalizeDateRange(pastDate, null);
            fromPastVn.Date.Should().Be(pastDate);
            toPastVn.Date.Should().Be(todayVn);

            // 5. FromDate only: tương lai -> kẹp về todayVn theo Spec §1.1 Case 2
            var futureDate = todayVn.AddDays(3);
            var (fromFutureVn, toFutureVn, _, _) = DateRangeHelper.NormalizeDateRange(futureDate, null);
            fromFutureVn.Date.Should().Be(todayVn);
            toFutureVn.Date.Should().Be(todayVn);
        }
    }
}
