using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Contractors;
using HPParking.Api.Services.Implementations;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class ContractorServiceTests
    {
        private readonly IRepository<Contractor> _contractorRepo = Substitute.For<IRepository<Contractor>>();
        private readonly IRepository<Client> _clientRepo = Substitute.For<IRepository<Client>>();
        private readonly ILogger<ContractorService> _logger = Substitute.For<ILogger<ContractorService>>();
        private readonly ContractorService _service;

        public ContractorServiceTests()
        {
            _service = new ContractorService(_contractorRepo, _clientRepo, _logger);
        }

        [Fact]
        public async Task GetContractorsPagedAsync_ReturnsPagedResult()
        {
            // Arrange
            var query = new ContractorFilterQuery { PageIndex = 1, PageSize = 10 };
            var contractors = new List<Contractor>
            {
                new() { Id = "nt1", Code = "NT01", Name = "Nhà thầu A", IsActive = true, IsDeleted = false },
                new() { Id = "nt2", Code = "NT02", Name = "Nhà thầu B", IsActive = true, IsDeleted = false }
            };

            _contractorRepo.CountAsync(Arg.Any<FilterDefinition<Contractor>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(2L));

            _contractorRepo.FindAsync(
                Arg.Any<FilterDefinition<Contractor>>(),
                Arg.Any<SortDefinition<Contractor>>(),
                0, 10,
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Contractor>>(contractors));

            // Act
            var result = await _service.GetContractorsPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Pagination.TotalCount.Should().Be(2);
            result.Items.Should().HaveCount(2);
            result.Items[0].Code.Should().Be("NT01");
            result.Items[0].Name.Should().Be("Nhà thầu A");
        }

        [Fact]
        public async Task GetContractorByIdAsync_Found_ReturnsContractorDto()
        {
            // Arrange
            var contractor = new Contractor { Id = "nt1", Code = "NT01", Name = "Nhà thầu A", IsActive = true, IsDeleted = false };
            _contractorRepo.GetByIdAsync("nt1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Contractor?>(contractor));

            // Act
            var result = await _service.GetContractorByIdAsync("nt1");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("nt1");
            result.Code.Should().Be("NT01");
            result.Name.Should().Be("Nhà thầu A");
        }

        [Fact]
        public async Task GetContractorByIdAsync_NotFound_ThrowsNotFoundException()
        {
            // Arrange
            _contractorRepo.GetByIdAsync("nonexistent", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Contractor?>(null));

            // Act & Assert
            var act = () => _service.GetContractorByIdAsync("nonexistent");
            await act.Should().ThrowAsync<NotFoundException>()
                .Where(e => e.ErrorCode == ErrorCodes.CONTRACTOR_NOT_FOUND);
        }

        [Fact]
        public async Task CreateContractorAsync_Valid_CreatesAndReturnsDto()
        {
            // Arrange
            var request = new CreateContractorRequest
            {
                Code = "NT01",
                Name = "Nhà thầu Xây dựng",
                ContactPerson = "Nguyễn Văn A",
                PhoneNumber = "0912345678",
                Email = "a@gmail.com",
                IsActive = true
            };

            _contractorRepo.FindOneAsync(Arg.Any<Expression<Func<Contractor, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Contractor?>(null)); // Không trùng Code

            // Act
            var result = await _service.CreateContractorAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Code.Should().Be("NT01");
            result.Name.Should().Be("Nhà thầu Xây dựng");
            await _contractorRepo.Received(1).AddAsync(Arg.Is<Contractor>(c => c.Code == "NT01" && c.Name == "Nhà thầu Xây dựng"), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateContractorAsync_DuplicateCode_ThrowsConflictException()
        {
            // Arrange
            var existing = new Contractor { Id = "nt_old", Code = "NT01", Name = "Nhà thầu Cũ" };
            _contractorRepo.FindOneAsync(Arg.Any<Expression<Func<Contractor, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Contractor?>(existing));

            var request = new CreateContractorRequest { Code = "NT01", Name = "Nhà thầu Mới" };

            // Act & Assert
            var act = () => _service.CreateContractorAsync(request);
            await act.Should().ThrowAsync<ConflictException>()
                .Where(e => e.ErrorCode == ErrorCodes.CONTRACTOR_CODE_DUPLICATE);
        }

        [Fact]
        public async Task UpdateContractorAsync_Valid_UpdatesAndReturnsDto()
        {
            // Arrange
            var current = new Contractor { Id = "nt1", Code = "NT01", Name = "Nhà thầu Cũ", IsActive = true };
            _contractorRepo.GetByIdAsync("nt1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Contractor?>(current));

            var request = new UpdateContractorRequest { Code = "NT01", Name = "Nhà thầu Mới Đổi Tên", IsActive = true };

            // Act
            var result = await _service.UpdateContractorAsync("nt1", request);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Nhà thầu Mới Đổi Tên");
            await _contractorRepo.Received(1).UpdateAsync(Arg.Is<Contractor>(c => c.Name == "Nhà thầu Mới Đổi Tên"), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateContractorAsync_DuplicateCode_ThrowsConflictException()
        {
            // Arrange
            var current = new Contractor { Id = "nt1", Code = "NT01", Name = "Nhà thầu 1" };
            _contractorRepo.GetByIdAsync("nt1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Contractor?>(current));

            var existingOther = new Contractor { Id = "nt2", Code = "NT02", Name = "Nhà thầu 2" };
            _contractorRepo.FindOneAsync(Arg.Any<Expression<Func<Contractor, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Contractor?>(existingOther));

            var request = new UpdateContractorRequest { Code = "NT02", Name = "Nhà thầu 1 Đổi Mã" };

            // Act & Assert
            var act = () => _service.UpdateContractorAsync("nt1", request);
            await act.Should().ThrowAsync<ConflictException>()
                .Where(e => e.ErrorCode == ErrorCodes.CONTRACTOR_CODE_DUPLICATE);
        }

        [Fact]
        public async Task UpdateContractorAsync_DeactivateWithActiveClients_ThrowsBadRequestException()
        {
            // Arrange
            var current = new Contractor { Id = "nt1", Code = "NT01", Name = "Nhà thầu A", IsActive = true };
            _contractorRepo.GetByIdAsync("nt1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Contractor?>(current));

            _clientRepo.CountAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(3L)); // 3 active clients

            var request = new UpdateContractorRequest { Code = "NT01", Name = "Nhà thầu A", IsActive = false };

            // Act & Assert (Active State Protection)
            var act = () => _service.UpdateContractorAsync("nt1", request);
            await act.Should().ThrowAsync<BadRequestException>()
                .Where(e => e.ErrorCode == ErrorCodes.CONTRACTOR_ACTIVE_CLIENTS_EXIST)
                .WithMessage("*vẫn còn 3 khách hàng/nhân sự đang hoạt động*");
        }

        [Fact]
        public async Task DeleteContractorAsync_HasClients_ThrowsConflictException()
        {
            // Arrange
            var contractor = new Contractor { Id = "nt1", Name = "Nhà thầu A" };
            _contractorRepo.GetByIdAsync("nt1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Contractor?>(contractor));

            _clientRepo.CountAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(2L)); // 2 clients exist

            // Act & Assert (Restrict Deletion Policy)
            var act = () => _service.DeleteContractorAsync("nt1");
            await act.Should().ThrowAsync<ConflictException>()
                .Where(e => e.ErrorCode == ErrorCodes.CONTRACTOR_HAS_CLIENTS);

            await _contractorRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteContractorAsync_SoftDelete_CallsSoftDelete()
        {
            // Arrange
            var contractor = new Contractor { Id = "nt1", Name = "Nhà thầu A" };
            _contractorRepo.GetByIdAsync("nt1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Contractor?>(contractor));

            _clientRepo.CountAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(0L));

            // Act
            var result = await _service.DeleteContractorAsync("nt1", hardDelete: false);

            // Assert
            result.Should().BeTrue();
            await _contractorRepo.Received(1).DeleteAsync("nt1", softDelete: true, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteContractorAsync_HardDelete_CallsHardDelete()
        {
            // Arrange
            var contractor = new Contractor { Id = "nt1", Name = "Nhà thầu A" };
            _contractorRepo.GetByIdAsync("nt1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Contractor?>(contractor));

            _clientRepo.CountAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(0L));

            // Act
            var result = await _service.DeleteContractorAsync("nt1", hardDelete: true);

            // Assert
            result.Should().BeTrue();
            await _contractorRepo.Received(1).DeleteAsync("nt1", softDelete: false, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RestoreContractorAsync_WhenValid_RestoresAndReturnsContractorDto()
        {
            // Arrange
            var contractor = new Contractor
            {
                Id = "nt1",
                Code = "NT01",
                Name = "Nhà thầu A",
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow.AddDays(-2)
            };

            _contractorRepo.GetDeletedByIdAsync("nt1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Contractor?>(contractor));

            _contractorRepo.FindOneAsync(Arg.Any<Expression<Func<Contractor, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Contractor?>(null)); // Không bị trùng code

            _contractorRepo.RestoreAsync("nt1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            // Act
            var result = await _service.RestoreContractorAsync("nt1");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("nt1");
            result.Code.Should().Be("NT01");
            await _contractorRepo.Received(1).RestoreAsync("nt1", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RestoreContractorAsync_WhenNotFoundInTrash_ThrowsNotFoundException()
        {
            // Arrange
            _contractorRepo.GetDeletedByIdAsync("nonexistent", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Contractor?>(null));

            // Act
            var act = () => _service.RestoreContractorAsync("nonexistent");

            // Assert
            var ex = await act.Should().ThrowAsync<NotFoundException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.CONTRACTOR_NOT_FOUND);
        }

        [Fact]
        public async Task RestoreContractorAsync_WhenCodeDuplicateWithActiveContractor_ThrowsConflictException()
        {
            // Arrange
            var contractor = new Contractor
            {
                Id = "nt1",
                Code = "NT01",
                Name = "Nhà thầu Cũ",
                IsDeleted = true
            };
            var activeDuplicate = new Contractor
            {
                Id = "nt2",
                Code = "NT01",
                Name = "Nhà thầu Mới",
                IsDeleted = false
            };

            _contractorRepo.GetDeletedByIdAsync("nt1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Contractor?>(contractor));

            _contractorRepo.FindOneAsync(Arg.Any<Expression<Func<Contractor, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Contractor?>(activeDuplicate));

            // Act & Assert (Re-validation on Restore)
            var act = () => _service.RestoreContractorAsync("nt1");
            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.CONTRACTOR_CODE_DUPLICATE);
            await _contractorRepo.DidNotReceive().RestoreAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
    }
}
