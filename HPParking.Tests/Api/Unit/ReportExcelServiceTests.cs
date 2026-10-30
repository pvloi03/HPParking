using FluentAssertions;
using HPParking.Api.Common.Excel;
using HPParking.Api.DTOs.Excel.Reports;
using HPParking.Api.DTOs.ParkingSessions;
using HPParking.Api.DTOs.Statistics;
using HPParking.Api.Services.Implementations;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class ReportExcelServiceTests
    {
        private readonly IExcelService _excelService = Substitute.For<IExcelService>();
        private readonly IRepository<ParkingSession> _sessionRepo = Substitute.For<IRepository<ParkingSession>>();
        private readonly IRepository<AuditLog> _auditLogRepo = Substitute.For<IRepository<AuditLog>>();
        private readonly IRepository<Client> _clientRepo = Substitute.For<IRepository<Client>>();
        private readonly IRepository<Vehicle> _vehicleRepo = Substitute.For<IRepository<Vehicle>>();
        private readonly IStatisticsService _statisticsService = Substitute.For<IStatisticsService>();
        private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
        private readonly ILogger<ReportExcelService> _logger = Substitute.For<ILogger<ReportExcelService>>();

        private ReportExcelService CreateService()
        {
            return new ReportExcelService(
                _excelService,
                _sessionRepo,
                _auditLogRepo,
                _clientRepo,
                _vehicleRepo,
                _statisticsService,
                _logger,
                _auditLogService);
        }

        [Fact]
        public async Task ExportParkingSessionsAsync_WhenCalled_CalculatesTotalRevenueAndAppendsSummaryRow()
        {
            // Arrange
            var service = CreateService();
            IReadOnlyList<ParkingSession> sessions = new List<ParkingSession>
            {
                new ParkingSession
                {
                    Id = "s1",
                    PlateNumber = "30A-11111",
                    Status = ParkingSessionStatus.Completed,
                    IsFeeCollected = true,
                    FeeAmount = 15000,
                    IsDeleted = false,
                    InTime = DateTime.UtcNow.AddHours(-3),
                    OutTime = DateTime.UtcNow.AddHours(-1)
                },
                new ParkingSession
                {
                    Id = "s2",
                    PlateNumber = "29B-22222",
                    Status = ParkingSessionStatus.Completed,
                    IsFeeCollected = true,
                    FeeAmount = 25000,
                    IsDeleted = false,
                    InTime = DateTime.UtcNow.AddHours(-2),
                    OutTime = DateTime.UtcNow
                },
                new ParkingSession
                {
                    Id = "s3",
                    PlateNumber = "15C-33333",
                    Status = ParkingSessionStatus.Active,
                    IsFeeCollected = false,
                    FeeAmount = 10000,
                    IsDeleted = false,
                    InTime = DateTime.UtcNow.AddMinutes(-30)
                },
                new ParkingSession
                {
                    Id = "s4",
                    PlateNumber = "14A-44444",
                    Status = ParkingSessionStatus.Completed,
                    IsFeeCollected = false, // Chưa thu tiền -> Không tính doanh thu thực thu
                    FeeAmount = 20000,
                    IsDeleted = false,
                    InTime = DateTime.UtcNow.AddHours(-5),
                    OutTime = DateTime.UtcNow.AddHours(-4)
                },
                new ParkingSession
                {
                    Id = "s5",
                    PlateNumber = "51F-55555",
                    Status = ParkingSessionStatus.Completed,
                    IsFeeCollected = true,
                    FeeAmount = 30000,
                    IsDeleted = true, // Đã xóa mềm -> Không tính doanh thu
                    InTime = DateTime.UtcNow.AddHours(-6),
                    OutTime = DateTime.UtcNow.AddHours(-5)
                }
            };

            _sessionRepo.CountAsync(Arg.Any<FilterDefinition<ParkingSession>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult((long)sessions.Count));

            _sessionRepo.FindAsync(
                filter: Arg.Any<FilterDefinition<ParkingSession>>(),
                sort: Arg.Any<SortDefinition<ParkingSession>>(),
                skip: Arg.Any<int>(),
                limit: Arg.Any<int>(),
                cancellationToken: Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(sessions));

            _vehicleRepo.FindAsync(Arg.Any<FilterDefinition<Vehicle>>(), cancellationToken: Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>(new List<Vehicle>()));

            IEnumerable<ParkingSessionExcelDto>? capturedData = null;
            string? capturedTitle = null;

            _excelService.WriteAsync(
                Arg.Do<IEnumerable<ParkingSessionExcelDto>>(x => capturedData = x.ToList()),
                Arg.Any<ParkingSessionExcelProfile>(),
                Arg.Any<string>(),
                Arg.Do<string?>(t => capturedTitle = t),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new byte[] { 1, 2, 3 }));

            // Act
            var query = new ParkingSessionFilterQuery();
            var result = await service.ExportParkingSessionsAsync(query);

            // Assert
            result.Content.Should().NotBeNull();
            result.FileName.Should().StartWith("Lich_Su_Do_Xe_");
            result.IsTruncated.Should().BeFalse();

            capturedData.Should().NotBeNull();
            var dataList = capturedData!.ToList();
            dataList.Count.Should().Be(6); // 5 records + 1 total row

            // Dòng tổng kết ở cuối
            var summaryRow = dataList.Last();
            summaryRow.InOrderDisplay.Should().Be("TỔNG CỘNG THỰC THU");
            summaryRow.PlateNumber.Should().Be("Đã thu: 2/5 lượt");
            summaryRow.FeeAmount.Should().Be(40000m); // 15,000 + 25,000
            summaryRow.IsFeeCollected.Should().BeTrue();
            summaryRow.Status.Should().BeNull();
            summaryRow.Duration.Should().Be("Tổng: 40,000 VNĐ");

            // Report title banner
            capturedTitle.Should().Be("BÁO CÁO LỊCH SỬ PHIÊN ĐỖ XE (Tổng thực thu: 40,000 VNĐ)");
        }

        [Fact]
        public async Task ExportParkingSessionsAsync_WhenNoSessions_DoesNotAppendSummaryRow()
        {
            // Arrange
            var service = CreateService();
            IReadOnlyList<ParkingSession> sessions = new List<ParkingSession>();

            _sessionRepo.CountAsync(Arg.Any<FilterDefinition<ParkingSession>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(0L));

            _sessionRepo.FindAsync(
                filter: Arg.Any<FilterDefinition<ParkingSession>>(),
                sort: Arg.Any<SortDefinition<ParkingSession>>(),
                skip: Arg.Any<int>(),
                limit: Arg.Any<int>(),
                cancellationToken: Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(sessions));

            IEnumerable<ParkingSessionExcelDto>? capturedData = null;
            string? capturedTitle = null;

            _excelService.WriteAsync(
                Arg.Do<IEnumerable<ParkingSessionExcelDto>>(x => capturedData = x.ToList()),
                Arg.Any<ParkingSessionExcelProfile>(),
                Arg.Any<string>(),
                Arg.Do<string?>(t => capturedTitle = t),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new byte[] { 1, 2, 3 }));

            // Act
            var query = new ParkingSessionFilterQuery();
            var result = await service.ExportParkingSessionsAsync(query);

            // Assert
            result.Content.Should().NotBeNull();
            capturedData.Should().NotBeNull();
            capturedData!.Should().BeEmpty();
            capturedTitle.Should().Be("BÁO CÁO LỊCH SỬ PHIÊN ĐỖ XE");
        }

        [Fact]
        public async Task ExportRevenueReportAsync_WhenCalled_GeneratesTwoSheetsUsingWriteTwoSheetsAsync()
        {
            // Arrange
            var service = CreateService();
            var reportDto = new RevenueReportDto
            {
                TotalRevenue = 100000m,
                PaidSessionsCount = 2,
                CompletedSessionsCount = 3,
                AverageFeePerSession = 50000m,
                PaidRatio = 66.7,
                Timeline = new List<RevenueDailyItemDto>
                {
                    new() { Date = new DateOnly(2026, 10, 28), Revenue = 100000m, PaidCount = 2, CompletedCount = 3, AverageFee = 50000m }
                }
            };

            _statisticsService.GetRevenueReportAsync(Arg.Any<RevenueStatisticsQuery>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(reportDto));

            IReadOnlyList<ParkingSession> sessions = new List<ParkingSession>
            {
                new()
                {
                    Id = "s1",
                    PlateNumber = "29A-12345",
                    VehicleType = VehicleType.Car,
                    Status = ParkingSessionStatus.Completed,
                    IsFeeCollected = true,
                    FeeAmount = 50000m,
                    InTime = new DateTime(2026, 10, 28, 8, 0, 0),
                    OutTime = new DateTime(2026, 10, 28, 10, 0, 0)
                },
                new()
                {
                    Id = "s2",
                    PlateNumber = "29B-67890",
                    VehicleType = VehicleType.Motorbike,
                    Status = ParkingSessionStatus.Completed,
                    IsFeeCollected = true,
                    FeeAmount = 50000m,
                    InTime = new DateTime(2026, 10, 28, 9, 0, 0),
                    OutTime = new DateTime(2026, 10, 28, 11, 0, 0)
                }
            };

            _sessionRepo.CountAsync(Arg.Any<FilterDefinition<ParkingSession>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult((long)sessions.Count));

            _sessionRepo.FindAsync(
                filter: Arg.Any<FilterDefinition<ParkingSession>>(),
                sort: Arg.Any<SortDefinition<ParkingSession>>(),
                skip: Arg.Any<int>(),
                limit: Arg.Any<int>(),
                cancellationToken: Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(sessions));

            _clientRepo.FindAsync(Arg.Any<FilterDefinition<Client>>(), cancellationToken: Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Client>>(new List<Client>()));

            ExcelSheetData<RevenueDailySummaryExcelDto>? capturedSheet1 = null;
            ExcelSheetData<RevenueTransactionExcelDto>? capturedSheet2 = null;
            _excelService.WriteTwoSheetsAsync(
                Arg.Do<ExcelSheetData<RevenueDailySummaryExcelDto>>(x => capturedSheet1 = x),
                Arg.Do<ExcelSheetData<RevenueTransactionExcelDto>>(x => capturedSheet2 = x),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new byte[] { 4, 5, 6 }));

            // Act
            var query = new RevenueStatisticsQuery
            {
                FromDate = new DateTime(2026, 10, 28),
                ToDate = new DateTime(2026, 10, 28)
            };
            var result = await service.ExportRevenueReportAsync(query);

            // Assert
            result.Content.Should().NotBeNull();
            result.FileName.Should().StartWith("Bao_Cao_Doanh_Thu_");
            result.IsTruncated.Should().BeFalse();

            capturedSheet1.Should().NotBeNull();
            capturedSheet1!.SheetName.Should().Be("Tổng hợp theo ngày");
            capturedSheet2.Should().NotBeNull();
            capturedSheet2!.SheetName.Should().Be("Chi tiết giao dịch");
        }

        [Fact]
        public async Task ExportRevenueReportAsync_WhenTransactionsExceed10000_SetsTruncatedFlag()
        {
            // Arrange
            var service = CreateService();
            var reportDto = new RevenueReportDto
            {
                TotalRevenue = 50000000m,
                PaidSessionsCount = 10500,
                CompletedSessionsCount = 11000,
                AverageFeePerSession = 4762m,
                PaidRatio = 95.5,
                Timeline = new List<RevenueDailyItemDto>()
            };

            _statisticsService.GetRevenueReportAsync(Arg.Any<RevenueStatisticsQuery>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(reportDto));

            _sessionRepo.CountAsync(Arg.Any<FilterDefinition<ParkingSession>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(10500L));

            _sessionRepo.FindAsync(
                filter: Arg.Any<FilterDefinition<ParkingSession>>(),
                sort: Arg.Any<SortDefinition<ParkingSession>>(),
                skip: Arg.Any<int>(),
                limit: Arg.Any<int>(),
                cancellationToken: Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<ParkingSession>>(new List<ParkingSession>()));

            _excelService.WriteTwoSheetsAsync(
                Arg.Any<ExcelSheetData<RevenueDailySummaryExcelDto>>(),
                Arg.Any<ExcelSheetData<RevenueTransactionExcelDto>>(),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new byte[] { 7, 8, 9 }));

            // Act
            var query = new RevenueStatisticsQuery
            {
                FromDate = new DateTime(2026, 10, 20),
                ToDate = new DateTime(2026, 10, 28)
            };
            var result = await service.ExportRevenueReportAsync(query);

            // Assert
            result.IsTruncated.Should().BeTrue();
        }
    }
}

