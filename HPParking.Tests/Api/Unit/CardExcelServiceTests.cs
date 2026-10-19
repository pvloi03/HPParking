using FluentAssertions;
using HPParking.Api.Common.Excel;
using HPParking.Api.DTOs.Cards;
using HPParking.Api.DTOs.Excel;
using HPParking.Api.DTOs.Excel.Cards;
using HPParking.Api.Services.Implementations;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using System.Linq.Expressions;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class CardExcelServiceTests
    {
        private readonly IExcelService _excelService = Substitute.For<IExcelService>();
        private readonly ICardService _cardService = Substitute.For<ICardService>();
        private readonly IRepository<Card> _cardRepo = Substitute.For<IRepository<Card>>();
        private readonly IRepository<Client> _clientRepo = Substitute.For<IRepository<Client>>();
        private readonly IRepository<Vehicle> _vehicleRepo = Substitute.For<IRepository<Vehicle>>();
        private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
        private readonly ILogger<CardExcelService> _logger = Substitute.For<ILogger<CardExcelService>>();

        private CardExcelService CreateService()
        {
            return new CardExcelService(
                _excelService,
                _cardService,
                _cardRepo,
                _clientRepo,
                _vehicleRepo,
                _logger,
                _auditLogService);
        }

        private static IFormFile CreateMockFormFile()
        {
            // ZIP PK magic bytes (0x50, 0x4B, 0x03, 0x04) required by ExcelFileValidator
            var bytes = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00, 0x00 };
            var stream = new MemoryStream(bytes);
            return new FormFile(stream, 0, bytes.Length, "file", "cards.xlsx");
        }

        private void SetupExcelParseResult(List<CardExcelDto> rows)
        {
            var parseResult = new ExcelImportResult<CardExcelDto>
            {
                TotalRows = rows.Count,
                SuccessData = rows,
                Errors = new List<ExcelRowError>()
            };

            _excelService.ReadAsync(
                Arg.Any<Stream>(),
                Arg.Any<CardExcelProfile>(),
                Arg.Any<ExcelImportOptions>(),
                Arg.Any<CancellationToken>())
                .Returns(parseResult);
        }

        [Fact]
        public async Task GenerateTemplateAsync_ReturnsValidExcelBytes()
        {
            // Arrange
            var expectedBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
            _excelService.GenerateTemplateAsync(
                Arg.Any<CardExcelProfile>(),
                Arg.Any<CardExcelDto>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
                .Returns(expectedBytes);

            var service = CreateService();

            // Act
            var result = await service.GenerateTemplateAsync();

            // Assert
            result.Should().BeEquivalentTo(expectedBytes);
            await _excelService.Received(1).GenerateTemplateAsync(
                Arg.Any<CardExcelProfile>(),
                Arg.Is<CardExcelDto>(x => x.CardNumber == "0012345678"),
                "The_Dinh_Danh",
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ImportCardsAsync_WhenValidRows_CreatesCardsAndSyncsClientCardCode()
        {
            // Arrange
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Card>());

            var existingClient = new Client
            {
                Id = "client-001",
                Code = "NV001",
                Name = "Trần Văn A",
                PhoneNumber = "0988123456",
                IsDeleted = false
            };
            _clientRepo.FindAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Client> { existingClient });
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle>());

            var excelRows = new List<CardExcelDto>
            {
                new CardExcelDto
                {
                    CardNumber = "12345", // Cần được pad thành 0000012345
                    TargetType = CardTargetType.Person,
                    ClientCode = "NV001"
                }
            };
            SetupExcelParseResult(excelRows);

            var service = CreateService();
            var file = CreateMockFormFile();

            // Act
            var result = await service.ImportCardsAsync(file, dryRun: false, duplicateMode: DuplicateMode.Skip);

            // Assert
            result.TotalRows.Should().Be(1);
            result.SuccessCount.Should().Be(1);
            result.FailedCount.Should().Be(0);
            result.SkippedCount.Should().Be(0);

            // Kiểm tra thu hồi thẻ cũ của Client
            await _cardService.Received(1).RevokeClientCardsAsync("client-001", null, Arg.Any<CancellationToken>());

            // Kiểm tra lưu thẻ mới với mã chuẩn hóa 10 số
            await _cardRepo.Received(1).AddAsync(
                Arg.Is<Card>(c => c.CardNumber == "0000012345" && c.ClientId == "client-001" && c.Status == CardStatus.InUse),
                Arg.Any<CancellationToken>());

            // Kiểm tra cập nhật hồ sơ Client có CardCode đồng bộ
            await _clientRepo.Received(1).UpdateAsync(
                Arg.Is<Client>(c => c.Id == "client-001" && c.CardCode == "0000012345"),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ImportCardsAsync_WhenCardNumberInvalidOrZero_ReturnsRowError()
        {
            // Arrange
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Card>());
            _clientRepo.FindAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Client>());
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle>());

            var excelRows = new List<CardExcelDto>
            {
                new CardExcelDto
                {
                    CardNumber = "0000000000", // Toàn số 0 (tín hiệu rác không hợp lệ)
                    TargetType = CardTargetType.Person
                }
            };
            SetupExcelParseResult(excelRows);

            var service = CreateService();
            var file = CreateMockFormFile();

            // Act
            var result = await service.ImportCardsAsync(file, dryRun: false, duplicateMode: DuplicateMode.Skip);

            // Assert
            result.FailedCount.Should().Be(1);
            result.SuccessCount.Should().Be(0);
            result.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains("không hợp lệ"));
            await _cardRepo.DidNotReceive().AddAsync(Arg.Any<Card>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ImportCardsAsync_WhenDuplicateCardInFile_ReportsDuplicateError()
        {
            // Arrange
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Card>());
            _clientRepo.FindAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Client>());
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle>());

            var excelRows = new List<CardExcelDto>
            {
                new CardExcelDto { CardNumber = "0012345678", TargetType = CardTargetType.Person },
                new CardExcelDto { CardNumber = "0012345678", TargetType = CardTargetType.Person }
            };
            SetupExcelParseResult(excelRows);

            var service = CreateService();
            var file = CreateMockFormFile();

            // Act
            var result = await service.ImportCardsAsync(file, dryRun: false, duplicateMode: DuplicateMode.Skip);

            // Assert
            result.SuccessCount.Should().Be(1);
            result.FailedCount.Should().Be(1);
            result.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains("trùng lặp nhiều lần trong cùng tệp Excel"));
        }

        [Fact]
        public async Task ImportCardsAsync_WhenDuplicateInDbAndModeIsSkip_IncrementsSkippedCount()
        {
            // Arrange
            var existingCard = new Card
            {
                Id = "card-001",
                CardNumber = "0012345678",
                TargetType = CardTargetType.Person,
                Status = CardStatus.Available,
                IsDeleted = false
            };
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Card> { existingCard });
            _clientRepo.FindAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Client>());
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle>());

            var excelRows = new List<CardExcelDto>
            {
                new CardExcelDto { CardNumber = "0012345678", TargetType = CardTargetType.Person }
            };
            SetupExcelParseResult(excelRows);

            var service = CreateService();
            var file = CreateMockFormFile();

            // Act
            var result = await service.ImportCardsAsync(file, dryRun: false, duplicateMode: DuplicateMode.Skip);

            // Assert
            result.SkippedCount.Should().Be(1);
            result.SuccessCount.Should().Be(0);
            result.FailedCount.Should().Be(0);
            await _cardRepo.DidNotReceive().AddAsync(Arg.Any<Card>(), Arg.Any<CancellationToken>());
            await _cardRepo.DidNotReceive().UpdateAsync(Arg.Any<Card>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ImportCardsAsync_WhenDuplicateInDbAndModeIsUpdate_UpdatesCardSuccessfully()
        {
            // Arrange
            var existingCard = new Card
            {
                Id = "card-001",
                CardNumber = "0012345678",
                TargetType = CardTargetType.Person,
                Status = CardStatus.Available,
                IsDeleted = false
            };
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Card> { existingCard });
            _clientRepo.FindAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Client>());
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle>());

            var excelRows = new List<CardExcelDto>
            {
                new CardExcelDto
                {
                    CardNumber = "0012345678",
                    TargetType = CardTargetType.Person,
                    Status = CardStatus.Locked,
                    Note = "Đã khóa thẻ do nghi vấn mất"
                }
            };
            SetupExcelParseResult(excelRows);

            var service = CreateService();
            var file = CreateMockFormFile();

            // Act
            var result = await service.ImportCardsAsync(file, dryRun: false, duplicateMode: DuplicateMode.Update);

            // Assert
            result.SuccessCount.Should().Be(1);
            result.FailedCount.Should().Be(0);
            await _cardRepo.Received(1).UpdateAsync(
                Arg.Is<Card>(c => c.Id == "card-001" && c.Status == CardStatus.Locked && c.Note == "Đã khóa thẻ do nghi vấn mất"),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ImportCardsAsync_WhenDuplicateInDbAndModeIsError_ReturnsConflictError()
        {
            // Arrange
            var existingCard = new Card
            {
                Id = "card-001",
                CardNumber = "0012345678",
                TargetType = CardTargetType.Person,
                Status = CardStatus.Available,
                IsDeleted = false
            };
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Card> { existingCard });
            _clientRepo.FindAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Client>());
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle>());

            var excelRows = new List<CardExcelDto>
            {
                new CardExcelDto { CardNumber = "0012345678", TargetType = CardTargetType.Person }
            };
            SetupExcelParseResult(excelRows);

            var service = CreateService();
            var file = CreateMockFormFile();

            // Act
            var result = await service.ImportCardsAsync(file, dryRun: false, duplicateMode: DuplicateMode.Error);

            // Assert
            result.FailedCount.Should().Be(1);
            result.SuccessCount.Should().Be(0);
            result.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains("đã tồn tại trên hệ thống"));
        }

        [Fact]
        public async Task ImportCardsAsync_WhenDryRunIsTrue_DoesNotCallRepoAddOrUpdate()
        {
            // Arrange
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Card>());
            _clientRepo.FindAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Client>());
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle>());

            var excelRows = new List<CardExcelDto>
            {
                new CardExcelDto { CardNumber = "0012345678", TargetType = CardTargetType.Person }
            };
            SetupExcelParseResult(excelRows);

            var service = CreateService();
            var file = CreateMockFormFile();

            // Act
            var result = await service.ImportCardsAsync(file, dryRun: true, duplicateMode: DuplicateMode.Skip);

            // Assert
            result.IsDryRun.Should().BeTrue();
            result.SuccessCount.Should().Be(1);
            result.FailedCount.Should().Be(0);
            await _cardRepo.DidNotReceive().AddAsync(Arg.Any<Card>(), Arg.Any<CancellationToken>());
            await _cardRepo.DidNotReceive().UpdateAsync(Arg.Any<Card>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExportCardsAsync_WhenCalled_ReturnsExcelContentAndHeader()
        {
            // Arrange
            var cardDto = new CardDto
            {
                Id = "c-1",
                CardNumber = "0012345678",
                TargetType = CardTargetType.Person,
                ClientId = "client-1",
                ClientCode = "NV001",
                ClientName = "Nguyễn Văn B",
                Status = CardStatus.InUse,
                Note = "Ghi chú test"
            };

            _cardService.GetCardsForExportAsync(
                Arg.Any<CardFilterQuery>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
                .Returns(((IReadOnlyList<CardDto>)new List<CardDto> { cardDto }, false));

            var mockBytes = new byte[] { 1, 2, 3, 4 };
            _excelService.WriteAsync(
                Arg.Any<IEnumerable<CardExportExcelDto>>(),
                Arg.Any<CardExportExcelProfile>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
                .Returns(mockBytes);

            var service = CreateService();

            // Act
            var (content, fileName, isTruncated) = await service.ExportCardsAsync(new CardFilterQuery());

            // Assert
            content.Should().BeEquivalentTo(mockBytes);
            fileName.Should().StartWith("danh_sach_the_dinh_danh_").And.EndWith(".xlsx");
            isTruncated.Should().BeFalse();

            await _excelService.Received(1).WriteAsync(
                Arg.Is<IEnumerable<CardExportExcelDto>>(dtos =>
                    dtos.Any(d => d.CardNumber == "0012345678" && d.AssignedCode == "NV001" && d.AssignedName == "Nguyễn Văn B")),
                Arg.Any<CardExportExcelProfile>(),
                "The_Dinh_Danh",
                "DANH SÁCH THẺ ĐỊNH DANH HỆ THỐNG",
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ImportCardsAsync_WhenVehicleCardAssigned_RevokesOldCardsViaCardService()
        {
            // Arrange
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Card>());
            _clientRepo.FindAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Client>());

            var vehicle = new Vehicle
            {
                Id = "veh-001",
                PlateNumber = "29A-12345"
            };
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle> { vehicle });

            var excelRows = new List<CardExcelDto>
            {
                new CardExcelDto
                {
                    CardNumber = "0099887766",
                    TargetType = CardTargetType.Vehicle,
                    PlateNumber = "29A-12345"
                }
            };
            SetupExcelParseResult(excelRows);

            var service = CreateService();
            var file = CreateMockFormFile();

            // Act
            var result = await service.ImportCardsAsync(file, dryRun: false, duplicateMode: DuplicateMode.Skip);

            // Assert
            result.SuccessCount.Should().Be(1);
            await _cardService.Received(1).RevokeVehicleCardsAsync("veh-001", null, Arg.Any<CancellationToken>());
            await _cardRepo.Received(1).AddAsync(
                Arg.Is<Card>(c => c.CardNumber == "0099887766" && c.VehicleId == "veh-001"),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ImportCardsAsync_WhenUpdateModeAndClientReassigned_RevokesOldCardsForNewClient()
        {
            // Arrange
            var existingCard = new Card
            {
                Id = "card-001",
                CardNumber = "0012345678",
                TargetType = CardTargetType.Person,
                ClientId = "client-old",
                Status = CardStatus.InUse,
                IsDeleted = false
            };
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Card> { existingCard });

            var oldClient = new Client
            {
                Id = "client-old",
                Code = "NV_OLD",
                Name = "Nhân viên cũ",
                CardCode = "0012345678"
            };
            var newClient = new Client
            {
                Id = "client-new",
                Code = "NV_NEW",
                Name = "Nhân viên mới",
                CardCode = "OLD_CARD_CODE"
            };

            _clientRepo.GetByIdAsync("client-old", Arg.Any<CancellationToken>())
                .Returns(oldClient);
            _clientRepo.FindAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Client> { oldClient, newClient });
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle>());

            var excelRows = new List<CardExcelDto>
            {
                new CardExcelDto
                {
                    CardNumber = "0012345678",
                    TargetType = CardTargetType.Person,
                    ClientCode = "NV_NEW"
                }
            };
            SetupExcelParseResult(excelRows);

            var service = CreateService();
            var file = CreateMockFormFile();

            // Act
            var result = await service.ImportCardsAsync(file, dryRun: false, duplicateMode: DuplicateMode.Update);

            // Assert
            result.SuccessCount.Should().Be(1);
            // 1. Gỡ mã thẻ khỏi client cũ
            await _clientRepo.Received(1).UpdateAsync(
                Arg.Is<Client>(c => c.Id == "client-old" && c.CardCode == string.Empty),
                Arg.Any<CancellationToken>());
            // 2. Thu hồi thẻ cũ của client mới (ngoại trừ card-001)
            await _cardService.Received(1).RevokeClientCardsAsync("client-new", "card-001", Arg.Any<CancellationToken>());
            // 3. Gán mã thẻ mới vào client mới
            await _clientRepo.Received(1).UpdateAsync(
                Arg.Is<Client>(c => c.Id == "client-new" && c.CardCode == "0012345678"),
                Arg.Any<CancellationToken>());
            // 4. Cập nhật card-001 trỏ về client mới
            await _cardRepo.Received(1).UpdateAsync(
                Arg.Is<Card>(c => c.Id == "card-001" && c.ClientId == "client-new"),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ImportCardsAsync_WhenPersonCardHasPersonalVehicle_LinksVehicleToClientAndAssignsToCard()
        {
            // Arrange
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Card>());

            var client = new Client
            {
                Id = "client-001",
                Code = "NV001",
                Name = "Trần Văn A",
                IsDeleted = false
            };
            _clientRepo.FindAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Client> { client });

            var existingPersonalVehicle = new Vehicle
            {
                Id = "veh-001",
                PlateNumber = "29A-88888",
                IsShared = false,
                OwnerClientId = null
            };
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle> { existingPersonalVehicle });

            var excelRows = new List<CardExcelDto>
            {
                new CardExcelDto
                {
                    CardNumber = "0012345678",
                    TargetType = CardTargetType.Person,
                    ClientCode = "NV001",
                    PlateNumber = "29A-88888"
                }
            };
            SetupExcelParseResult(excelRows);

            var service = CreateService();
            var file = CreateMockFormFile();

            // Act
            var result = await service.ImportCardsAsync(file, dryRun: false, duplicateMode: DuplicateMode.Skip);

            // Assert
            result.SuccessCount.Should().Be(1);
            result.FailedCount.Should().Be(0);

            // Xe cá nhân được liên kết chủ sở hữu OwnerClientId = client-001
            await _vehicleRepo.Received(1).UpdateAsync(
                Arg.Is<Vehicle>(v => v.Id == "veh-001" && v.OwnerClientId == "client-001"),
                Arg.Any<CancellationToken>());

            // Thẻ được tạo với cả ClientId và VehicleId
            await _cardRepo.Received(1).AddAsync(
                Arg.Is<Card>(c => c.CardNumber == "0012345678" && c.ClientId == "client-001" && c.VehicleId == "veh-001" && c.Status == CardStatus.InUse),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ImportCardsAsync_WhenPersonCardHasSharedVehiclePlate_ReturnsError()
        {
            // Arrange
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Card>());

            var client = new Client
            {
                Id = "client-001",
                Code = "NV001",
                Name = "Trần Văn A",
                IsDeleted = false
            };
            _clientRepo.FindAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Client> { client });

            var sharedVehicle = new Vehicle
            {
                Id = "veh-shared",
                PlateNumber = "29A-99999",
                IsShared = true
            };
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle> { sharedVehicle });

            var excelRows = new List<CardExcelDto>
            {
                new CardExcelDto
                {
                    CardNumber = "0012345678",
                    TargetType = CardTargetType.Person,
                    ClientCode = "NV001",
                    PlateNumber = "29A-99999"
                }
            };
            SetupExcelParseResult(excelRows);

            var service = CreateService();
            var file = CreateMockFormFile();

            // Act
            var result = await service.ImportCardsAsync(file, dryRun: false, duplicateMode: DuplicateMode.Skip);

            // Assert
            result.FailedCount.Should().Be(1);
            result.SuccessCount.Should().Be(0);
            result.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains("phương tiện nội bộ dùng chung"));
            await _cardRepo.DidNotReceive().AddAsync(Arg.Any<Card>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ImportCardsAsync_WhenPersonCardHasPlateWithoutClientCode_ReturnsError()
        {
            // Arrange
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Card>());
            _clientRepo.FindAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Client>());
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle>());

            var excelRows = new List<CardExcelDto>
            {
                new CardExcelDto
                {
                    CardNumber = "0012345678",
                    TargetType = CardTargetType.Person,
                    ClientCode = null, // Không chỉ định nhân sự
                    PlateNumber = "29A-12345"
                }
            };
            SetupExcelParseResult(excelRows);

            var service = CreateService();
            var file = CreateMockFormFile();

            // Act
            var result = await service.ImportCardsAsync(file, dryRun: false, duplicateMode: DuplicateMode.Skip);

            // Assert
            result.FailedCount.Should().Be(1);
            result.SuccessCount.Should().Be(0);
            result.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains("chưa chỉ định mã nhân sự"));
        }
    }
}
