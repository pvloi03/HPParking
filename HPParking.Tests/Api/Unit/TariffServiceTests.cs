using FluentAssertions;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Tariffs;
using HPParking.Api.Services.Implementations;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Linq.Expressions;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class TariffServiceTests
    {
        private readonly IRepository<Tariff> _tariffRepo;
        private readonly IRepository<Card> _cardRepo;
        private readonly ILogger<TariffService> _logger;
        private readonly IAuditLogService _auditLogService;
        private readonly TariffService _service;

        public TariffServiceTests()
        {
            _tariffRepo = Substitute.For<IRepository<Tariff>>();
            _cardRepo = Substitute.For<IRepository<Card>>();
            _logger = Substitute.For<ILogger<TariffService>>();
            _auditLogService = Substitute.For<IAuditLogService>();

            _service = new TariffService(_tariffRepo, _cardRepo, _logger, _auditLogService);
        }

        [Fact]
        public async Task CreateAsync_WhenCodeAlreadyExists_ThrowsConflictException()
        {
            var request = new CreateTariffRequest
            {
                Code = "FLAT_01",
                Name = "Vé xe con",
                VehicleType = VehicleType.Car,
                PricingMode = TariffPricingMode.Flat,
                FlatPrice = 10000m
            };

            _tariffRepo.FindOneAsync(Arg.Any<Expression<Func<Tariff, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new Tariff { Id = "existing-1", Code = "FLAT_01" });

            var act = () => _service.CreateAsync(request);

            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.TARIFF_CODE_DUPLICATE);
        }

        [Fact]
        public async Task CreateAsync_WhenValid_AddsTariffAndLogsAudit()
        {
            var request = new CreateTariffRequest
            {
                Code = "BLOCK_01",
                Name = "Lũy tiến",
                VehicleType = VehicleType.Car,
                PricingMode = TariffPricingMode.Block,
                BasePrice = 15000m,
                BaseBlockMinutes = 120,
                IsActive = true
            };

            _tariffRepo.FindOneAsync(Arg.Any<Expression<Func<Tariff, bool>>>(), Arg.Any<CancellationToken>())
                .Returns((Tariff?)null);

            var result = await _service.CreateAsync(request);

            result.Should().NotBeNull();
            result.Code.Should().Be("BLOCK_01");
            await _tariffRepo.Received(1).AddAsync(Arg.Any<Tariff>(), Arg.Any<CancellationToken>());
            await _auditLogService.Received(1).LogActivityAsync(
                AuditActionType.Create,
                "Tariff",
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_WhenDuplicateCodeOnOtherTariff_ThrowsConflictException()
        {
            var tariff = new Tariff { Id = "t-1", Code = "OLD_CODE", Name = "Tariff 1", IsActive = true };
            _tariffRepo.GetByIdAsync("t-1", Arg.Any<CancellationToken>()).Returns(tariff);

            var request = new UpdateTariffRequest
            {
                Code = "EXISTING_CODE",
                Name = "Tariff 1 New",
                VehicleType = VehicleType.Car,
                PricingMode = TariffPricingMode.Flat,
                IsActive = true
            };

            _tariffRepo.FindOneAsync(Arg.Any<Expression<Func<Tariff, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new Tariff { Id = "t-other", Code = "EXISTING_CODE" });

            var act = () => _service.UpdateAsync("t-1", request);

            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.TARIFF_CODE_DUPLICATE);
        }

        [Fact]
        public async Task UpdateAsync_WhenDeactivatingWhileActiveCardsExist_ThrowsConflictException()
        {
            var tariff = new Tariff { Id = "t-1", Code = "FLAT_01", Name = "Tariff 1", IsActive = true };
            _tariffRepo.GetByIdAsync("t-1", Arg.Any<CancellationToken>()).Returns(tariff);

            var request = new UpdateTariffRequest
            {
                Code = "FLAT_01",
                Name = "Tariff 1",
                VehicleType = VehicleType.Car,
                PricingMode = TariffPricingMode.Flat,
                IsActive = false // Deactivating
            };

            _tariffRepo.FindOneAsync(Arg.Any<Expression<Func<Tariff, bool>>>(), Arg.Any<CancellationToken>())
                .Returns((Tariff?)null);

            // Active cards exist
            _cardRepo.ExistsAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var act = () => _service.UpdateAsync("t-1", request);

            var ex = await act.Should().ThrowAsync<BadRequestException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.TARIFF_ACTIVE_CARDS_EXIST);
        }

        [Fact]
        public async Task DeleteAsync_WhenCardsInUseExist_ThrowsConflictException()
        {
            var tariff = new Tariff { Id = "t-in-use", Code = "IN_USE", Name = "Tariff In Use", IsDeleted = false };
            _tariffRepo.GetByIdAsync("t-in-use", Arg.Any<CancellationToken>()).Returns(tariff);

            _cardRepo.ExistsAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var act = () => _service.DeleteAsync("t-in-use");

            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.TARIFF_IN_USE_BY_CARDS);
            await _tariffRepo.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default, default);
        }

        [Fact]
        public async Task DeleteAsync_WhenNoCardsInUse_SoftDeletesAndLogsAudit()
        {
            var tariff = new Tariff { Id = "t-unused", Code = "UNUSED", Name = "Tariff Unused", IsDeleted = false };
            _tariffRepo.GetByIdAsync("t-unused", Arg.Any<CancellationToken>()).Returns(tariff);

            _cardRepo.ExistsAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(false);

            await _service.DeleteAsync("t-unused");

            await _tariffRepo.Received(1).DeleteAsync("t-unused", softDelete: true, Arg.Any<CancellationToken>());
            await _auditLogService.Received(1).LogActivityAsync(
                AuditActionType.Delete,
                "Tariff",
                "t-unused",
                Arg.Any<string>(),
                Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RestoreTariffAsync_WhenDuplicateActiveCodeExists_ThrowsConflictException()
        {
            var deletedTariff = new Tariff { Id = "t-del", Code = "DUP_CODE", Name = "Deleted Tariff", IsDeleted = true };
            _tariffRepo.GetByIdAsync("t-del", Arg.Any<CancellationToken>()).Returns(deletedTariff);

            _tariffRepo.FindOneAsync(Arg.Any<Expression<Func<Tariff, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new Tariff { Id = "t-live", Code = "DUP_CODE", IsDeleted = false });

            var act = () => _service.RestoreTariffAsync("t-del");

            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.TARIFF_CODE_DUPLICATE);
            await _tariffRepo.DidNotReceiveWithAnyArgs().RestoreAsync(default!, default);
        }

        [Fact]
        public async Task RestoreTariffAsync_WhenValid_RestoresAndLogsAudit()
        {
            var deletedTariff = new Tariff { Id = "t-del", Code = "RESTORE_ME", Name = "Restore Me", IsDeleted = true };
            _tariffRepo.GetByIdAsync("t-del", Arg.Any<CancellationToken>()).Returns(deletedTariff);

            _tariffRepo.FindOneAsync(Arg.Any<Expression<Func<Tariff, bool>>>(), Arg.Any<CancellationToken>())
                .Returns((Tariff?)null);

            var result = await _service.RestoreTariffAsync("t-del");

            result.Should().NotBeNull();
            result.Code.Should().Be("RESTORE_ME");
            await _tariffRepo.Received(1).RestoreAsync("t-del", Arg.Any<CancellationToken>());
            await _auditLogService.Received(1).LogActivityAsync(
                AuditActionType.Restore,
                "Tariff",
                "t-del",
                Arg.Any<string>(),
                Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>());
        }
    }
}
