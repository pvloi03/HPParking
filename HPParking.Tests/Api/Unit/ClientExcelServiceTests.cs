using FluentAssertions;
using HPParking.Api.Common.Excel;
using HPParking.Api.DTOs.Excel;
using HPParking.Api.DTOs.Excel.Clients;
using HPParking.Api.Services.Implementations;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Linq.Expressions;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class ClientExcelServiceTests
    {
        private readonly IExcelService _excelService = Substitute.For<IExcelService>();
        private readonly IRepository<Client> _clientRepo = Substitute.For<IRepository<Client>>();
        private readonly IRepository<Company> _companyRepo = Substitute.For<IRepository<Company>>();
        private readonly IRepository<Department> _departmentRepo = Substitute.For<IRepository<Department>>();
        private readonly IRepository<Contractor> _contractorRepo = Substitute.For<IRepository<Contractor>>();
        private readonly ILogger<ClientExcelService> _logger = Substitute.For<ILogger<ClientExcelService>>();

        private ClientExcelService CreateService()
        {
            return new ClientExcelService(
                _excelService,
                _clientRepo,
                _companyRepo,
                _departmentRepo,
                _contractorRepo,
                _logger);
        }

        private static IFormFile CreateMockFormFile()
        {
            // ZIP PK magic bytes (0x50, 0x4B, 0x03, 0x04) required by ExcelFileValidator
            var bytes = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00, 0x00 };
            var stream = new MemoryStream(bytes);
            return new FormFile(stream, 0, bytes.Length, "file", "clients.xlsx");
        }

        private void SetupExcelParseResult(List<ClientExcelDto> rows)
        {
            var parseResult = new ExcelImportResult<ClientExcelDto>
            {
                TotalRows = rows.Count,
                SuccessData = rows,
                Errors = new List<ExcelRowError>()
            };

            _excelService.ReadAsync(
                Arg.Any<Stream>(),
                Arg.Any<ClientExcelProfile>(),
                Arg.Any<ExcelImportOptions>(),
                Arg.Any<CancellationToken>())
                .Returns(parseResult);
        }

        [Fact]
        public async Task ImportClientsAsync_WhenDuplicateCodeDifferentPhone_AndModeIsSkip_IncrementsSkippedCount()
        {
            // Arrange
            var existingClient = new Client
            {
                Id = "client-1",
                Name = "Nguyễn Văn A",
                Code = "NV-001",
                PhoneNumber = "0364336088",
                IsDeleted = false
            };

            _clientRepo.FindAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Client> { existingClient });
            _companyRepo.FindAsync(Arg.Any<Expression<Func<Company, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Company>());
            _departmentRepo.FindAsync(Arg.Any<Expression<Func<Department, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Department>());
            _contractorRepo.FindAsync(Arg.Any<Expression<Func<Contractor, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Contractor>());

            // Row có trùng mã NV-001 nhưng SĐT khác (0364336099)
            var excelRows = new List<ClientExcelDto>
            {
                new ClientExcelDto
                {
                    Code = "nv-001",
                    Name = "Nguyễn Văn A Đổi SĐT",
                    PhoneNumber = "0364336099"
                }
            };
            SetupExcelParseResult(excelRows);

            var service = CreateService();
            var file = CreateMockFormFile();

            // Act
            var result = await service.ImportClientsAsync(file, dryRun: false, duplicateMode: DuplicateMode.Skip);

            // Assert
            result.SkippedCount.Should().Be(1);
            result.FailedCount.Should().Be(0);
            result.SuccessCount.Should().Be(0);
        }

        [Fact]
        public async Task ImportClientsAsync_WhenDuplicateCodeDifferentPhone_AndModeIsError_IncrementsFailedCount()
        {
            // Arrange
            var existingClient = new Client
            {
                Id = "client-1",
                Name = "Nguyễn Văn A",
                Code = "NV-001",
                PhoneNumber = "0364336088",
                IsDeleted = false
            };

            _clientRepo.FindAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Client> { existingClient });
            _companyRepo.FindAsync(Arg.Any<Expression<Func<Company, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Company>());
            _departmentRepo.FindAsync(Arg.Any<Expression<Func<Department, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Department>());
            _contractorRepo.FindAsync(Arg.Any<Expression<Func<Contractor, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Contractor>());

            var excelRows = new List<ClientExcelDto>
            {
                new ClientExcelDto
                {
                    Code = "NV-001",
                    Name = "Nguyễn Văn A",
                    PhoneNumber = "0364336099"
                }
            };
            SetupExcelParseResult(excelRows);

            var service = CreateService();
            var file = CreateMockFormFile();

            // Act
            var result = await service.ImportClientsAsync(file, dryRun: false, duplicateMode: DuplicateMode.Error);

            // Assert
            result.FailedCount.Should().Be(1);
            result.SkippedCount.Should().Be(0);
            result.Errors.Should().Contain(e => e.Column == "Mã định danh" && e.ErrorMessage.Contains("NV-001"));
        }

        [Fact]
        public async Task ImportClientsAsync_WhenDuplicateCodeDifferentPhone_AndModeIsUpdate_UpdatesClientWithNewPhone()
        {
            // Arrange
            var existingClient = new Client
            {
                Id = "client-1",
                Name = "Nguyễn Văn A",
                Code = "NV-001",
                PhoneNumber = "0364336088",
                IsDeleted = false
            };

            _clientRepo.FindAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Client> { existingClient });
            _companyRepo.FindAsync(Arg.Any<Expression<Func<Company, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Company>());
            _departmentRepo.FindAsync(Arg.Any<Expression<Func<Department, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Department>());
            _contractorRepo.FindAsync(Arg.Any<Expression<Func<Contractor, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Contractor>());

            var excelRows = new List<ClientExcelDto>
            {
                new ClientExcelDto
                {
                    Code = "NV-001",
                    Name = "Nguyễn Văn A Cập Nhật",
                    PhoneNumber = "0364336099"
                }
            };
            SetupExcelParseResult(excelRows);

            var service = CreateService();
            var file = CreateMockFormFile();

            // Act
            var result = await service.ImportClientsAsync(file, dryRun: false, duplicateMode: DuplicateMode.Update);

            // Assert
            result.SuccessCount.Should().Be(1);
            result.FailedCount.Should().Be(0);
            result.SkippedCount.Should().Be(0);
            await _clientRepo.Received(1).UpdateAsync(
                Arg.Is<Client>(c => c.Id == "client-1" && c.PhoneNumber == "0364336099" && c.Name == "Nguyễn Văn A Cập Nhật"),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ImportClientsAsync_WhenCrossConflict_PhoneOfClientA_AndCodeOfClientB_AndModeIsSkip_IncrementsSkippedCount()
        {
            // Arrange
            var clientA = new Client
            {
                Id = "client-a",
                Name = "Người A",
                Code = "NV-001",
                PhoneNumber = "0364336088",
                IsDeleted = false
            };
            var clientB = new Client
            {
                Id = "client-b",
                Name = "Người B",
                Code = "NV-002",
                PhoneNumber = "0364336099",
                IsDeleted = false
            };

            _clientRepo.FindAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Client> { clientA, clientB });
            _companyRepo.FindAsync(Arg.Any<Expression<Func<Company, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Company>());
            _departmentRepo.FindAsync(Arg.Any<Expression<Func<Department, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Department>());
            _contractorRepo.FindAsync(Arg.Any<Expression<Func<Contractor, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Contractor>());

            // Row có SĐT của người A (0364336088) nhưng Mã của người B (NV-002)
            var excelRows = new List<ClientExcelDto>
            {
                new ClientExcelDto
                {
                    Code = "NV-002",
                    Name = "Xung Đột",
                    PhoneNumber = "0364336088"
                }
            };
            SetupExcelParseResult(excelRows);

            var service = CreateService();
            var file = CreateMockFormFile();

            // Act
            var result = await service.ImportClientsAsync(file, dryRun: false, duplicateMode: DuplicateMode.Skip);

            // Assert
            result.SkippedCount.Should().Be(1);
            result.FailedCount.Should().Be(0);
        }

        [Fact]
        public async Task ImportClientsAsync_WhenCrossConflict_PhoneOfClientA_AndCodeOfClientB_AndModeIsErrorOrUpdate_ReportsConflictError()
        {
            // Arrange
            var clientA = new Client
            {
                Id = "client-a",
                Name = "Người A",
                Code = "NV-001",
                PhoneNumber = "0364336088",
                IsDeleted = false
            };
            var clientB = new Client
            {
                Id = "client-b",
                Name = "Người B",
                Code = "NV-002",
                PhoneNumber = "0364336099",
                IsDeleted = false
            };

            _clientRepo.FindAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Client> { clientA, clientB });
            _companyRepo.FindAsync(Arg.Any<Expression<Func<Company, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Company>());
            _departmentRepo.FindAsync(Arg.Any<Expression<Func<Department, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Department>());
            _contractorRepo.FindAsync(Arg.Any<Expression<Func<Contractor, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Contractor>());

            var excelRows = new List<ClientExcelDto>
            {
                new ClientExcelDto
                {
                    Code = "NV-002",
                    Name = "Xung Đột",
                    PhoneNumber = "0364336088"
                }
            };
            SetupExcelParseResult(excelRows);

            var service = CreateService();
            var file = CreateMockFormFile();

            // Act
            var result = await service.ImportClientsAsync(file, dryRun: false, duplicateMode: DuplicateMode.Update);

            // Assert
            result.FailedCount.Should().Be(1);
            result.SuccessCount.Should().Be(0);
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Xung đột dữ liệu"));
        }
    }
}
