using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Companies;
using HPParking.Api.Services.Implementations;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class CompanyServiceTests
    {
        private readonly IRepository<Company> _companyRepo = Substitute.For<IRepository<Company>>();
        private readonly IRepository<Department> _departmentRepo = Substitute.For<IRepository<Department>>();
        private readonly IRepository<Gate> _gateRepo = Substitute.For<IRepository<Gate>>();
        private readonly IRepository<Client> _clientRepo = Substitute.For<IRepository<Client>>();
        private readonly ILogger<CompanyService> _logger = Substitute.For<ILogger<CompanyService>>();
        private readonly CompanyService _service;

        public CompanyServiceTests()
        {
            _service = new CompanyService(_companyRepo, _departmentRepo, _gateRepo, _clientRepo, _logger);
        }

        [Fact]
        public async Task GetCompaniesPagedAsync_ReturnsPagedResult()
        {
            // Arrange
            var query = new CompanyFilterQuery { PageIndex = 1, PageSize = 10 };
            var companies = new List<Company>
            {
                new() { Id = "c1", Code = "CP01", Name = "Công ty A", IsActive = true, IsDeleted = false },
                new() { Id = "c2", Code = "CP02", Name = "Công ty B", IsActive = true, IsDeleted = false }
            };

            _companyRepo.CountAsync(Arg.Any<FilterDefinition<Company>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(2L));

            _companyRepo.FindAsync(
                Arg.Any<FilterDefinition<Company>>(),
                Arg.Any<SortDefinition<Company>>(),
                0, 10,
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Company>>(companies));

            // Act
            var result = await _service.GetCompaniesPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Pagination.TotalCount.Should().Be(2);
            result.Items.Should().HaveCount(2);
            result.Items[0].Code.Should().Be("CP01");
            result.Items[0].Name.Should().Be("Công ty A");
        }

        [Fact]
        public async Task GetCompanyByIdAsync_Found_ReturnsCompanyDto()
        {
            // Arrange
            var company = new Company { Id = "c1", Code = "CP01", Name = "Công ty A", IsActive = true, IsDeleted = false };
            _companyRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(company));

            // Act
            var result = await _service.GetCompanyByIdAsync("c1");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("c1");
            result.Code.Should().Be("CP01");
            result.Name.Should().Be("Công ty A");
        }

        [Fact]
        public async Task GetCompanyByIdAsync_NotFound_ThrowsNotFoundException()
        {
            // Arrange
            _companyRepo.GetByIdAsync("nonexistent", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(null));

            // Act & Assert
            var act = () => _service.GetCompanyByIdAsync("nonexistent");
            await act.Should().ThrowAsync<NotFoundException>()
                .Where(e => e.ErrorCode == ErrorCodes.COMPANY_NOT_FOUND);
        }

        [Fact]
        public async Task CreateCompanyAsync_Valid_CreatesAndReturnsDto()
        {
            // Arrange
            var request = new CreateCompanyRequest
            {
                Code = "  hp01  ",
                Name = "  Công ty Hải Phòng  ",
                PhoneNumber = "02253123456",
                Email = "info@hpparking.vn",
                IsActive = true
            };

            _companyRepo.FindOneAsync(Arg.Any<Expression<Func<Company, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(null));

            // Act
            var result = await _service.CreateCompanyAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Code.Should().Be("HP01"); // Auto uppercase & trimmed
            result.Name.Should().Be("Công ty Hải Phòng");
            await _companyRepo.Received(1).AddAsync(Arg.Is<Company>(c => c.Code == "HP01" && c.Name == "Công ty Hải Phòng"), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateCompanyAsync_DuplicateCode_ThrowsConflictException()
        {
            // Arrange
            var request = new CreateCompanyRequest
            {
                Code = "HP01",
                Name = "Công ty Hải Phòng Mới"
            };

            var existing = new Company { Id = "c_existing", Code = "HP01", Name = "Công ty Hải Phòng Cũ" };
            _companyRepo.FindOneAsync(Arg.Any<Expression<Func<Company, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(existing));

            // Act & Assert
            var act = () => _service.CreateCompanyAsync(request);
            await act.Should().ThrowAsync<ConflictException>()
                .Where(e => e.ErrorCode == ErrorCodes.COMPANY_CODE_DUPLICATE);
        }

        [Fact]
        public async Task UpdateCompanyAsync_DuplicateCode_ThrowsConflictException()
        {
            // Arrange
            var current = new Company { Id = "c1", Code = "HP01", Name = "Công ty 1", IsActive = true };
            _companyRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(current));

            var otherCompany = new Company { Id = "c2", Code = "HP02", Name = "Công ty 2" };
            _companyRepo.FindOneAsync(Arg.Any<Expression<Func<Company, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(otherCompany));

            var request = new UpdateCompanyRequest { Code = "HP02", Name = "Công ty 1 đổi tên" };

            // Act & Assert
            var act = () => _service.UpdateCompanyAsync("c1", request);
            await act.Should().ThrowAsync<ConflictException>()
                .Where(e => e.ErrorCode == ErrorCodes.COMPANY_CODE_DUPLICATE);
        }

        [Fact]
        public async Task UpdateCompanyAsync_DeactivateWithActiveDepartments_ThrowsBadRequestException()
        {
            // Arrange
            var current = new Company { Id = "c1", Code = "HP01", Name = "Công ty 1", IsActive = true };
            _companyRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(current));

            _departmentRepo.CountAsync(Arg.Any<Expression<Func<Department, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(2L)); // 2 active departments

            var request = new UpdateCompanyRequest { Code = "HP01", Name = "Công ty 1", IsActive = false };

            // Act & Assert
            var act = () => _service.UpdateCompanyAsync("c1", request);
            await act.Should().ThrowAsync<BadRequestException>()
                .Where(e => e.ErrorCode == ErrorCodes.COMPANY_ACTIVE_DEPENDENCY_EXISTS)
                .WithMessage("*vẫn còn 2 phòng ban đang hoạt động*");
        }

        [Fact]
        public async Task UpdateCompanyAsync_DeactivateWithActiveGates_ThrowsBadRequestException()
        {
            // Arrange
            var current = new Company { Id = "c1", Code = "HP01", Name = "Công ty 1", IsActive = true };
            _companyRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(current));

            _departmentRepo.CountAsync(Arg.Any<Expression<Func<Department, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(0L));
            _gateRepo.CountAsync(Arg.Any<Expression<Func<Gate, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(1L)); // 1 active gate

            var request = new UpdateCompanyRequest { Code = "HP01", Name = "Công ty 1", IsActive = false };

            // Act & Assert
            var act = () => _service.UpdateCompanyAsync("c1", request);
            await act.Should().ThrowAsync<BadRequestException>()
                .Where(e => e.ErrorCode == ErrorCodes.COMPANY_ACTIVE_DEPENDENCY_EXISTS)
                .WithMessage("*vẫn còn 1 cổng đang hoạt động*");
        }

        [Fact]
        public async Task UpdateCompanyAsync_DeactivateWithActiveClients_ThrowsBadRequestException()
        {
            // Arrange
            var current = new Company { Id = "c1", Code = "HP01", Name = "Công ty 1", IsActive = true };
            _companyRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(current));

            _departmentRepo.CountAsync(Arg.Any<Expression<Func<Department, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(0L));
            _gateRepo.CountAsync(Arg.Any<Expression<Func<Gate, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(0L));
            _clientRepo.CountAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(5L)); // 5 active clients

            var request = new UpdateCompanyRequest { Code = "HP01", Name = "Công ty 1", IsActive = false };

            // Act & Assert
            var act = () => _service.UpdateCompanyAsync("c1", request);
            await act.Should().ThrowAsync<BadRequestException>()
                .Where(e => e.ErrorCode == ErrorCodes.COMPANY_ACTIVE_DEPENDENCY_EXISTS)
                .WithMessage("*vẫn còn 5 khách hàng/nhân sự đang hoạt động*");
        }

        [Fact]
        public async Task DeleteCompanyAsync_HasDepartments_ThrowsConflictException()
        {
            // Arrange
            var company = new Company { Id = "c1", Name = "Công ty A" };
            _companyRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(company));

            _departmentRepo.CountAsync(Arg.Any<Expression<Func<Department, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(3L)); // 3 departments exist

            // Act & Assert
            var act = () => _service.DeleteCompanyAsync("c1");
            await act.Should().ThrowAsync<ConflictException>()
                .Where(e => e.ErrorCode == ErrorCodes.COMPANY_HAS_DEPARTMENTS);

            await _companyRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteCompanyAsync_HasGates_ThrowsConflictException()
        {
            // Arrange
            var company = new Company { Id = "c1", Name = "Công ty A" };
            _companyRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(company));

            _departmentRepo.CountAsync(Arg.Any<Expression<Func<Department, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(0L)); // No departments

            _gateRepo.CountAsync(Arg.Any<Expression<Func<Gate, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(1L)); // 1 gate exists

            // Act & Assert
            var act = () => _service.DeleteCompanyAsync("c1");
            await act.Should().ThrowAsync<ConflictException>()
                .Where(e => e.ErrorCode == ErrorCodes.COMPANY_HAS_GATES);

            await _companyRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteCompanyAsync_HasClients_ThrowsConflictException()
        {
            // Arrange
            var company = new Company { Id = "c1", Name = "Công ty A" };
            _companyRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(company));

            _departmentRepo.CountAsync(Arg.Any<Expression<Func<Department, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(0L));
            _gateRepo.CountAsync(Arg.Any<Expression<Func<Gate, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(0L));
            _clientRepo.CountAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(4L)); // 4 clients exist

            // Act & Assert
            var act = () => _service.DeleteCompanyAsync("c1");
            await act.Should().ThrowAsync<ConflictException>()
                .Where(e => e.ErrorCode == ErrorCodes.COMPANY_HAS_CLIENTS);

            await _companyRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteCompanyAsync_SoftDelete_CallsSoftDelete()
        {
            // Arrange
            var company = new Company { Id = "c1", Name = "Công ty A" };
            _companyRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(company));

            _departmentRepo.CountAsync(Arg.Any<Expression<Func<Department, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(0L));

            _gateRepo.CountAsync(Arg.Any<Expression<Func<Gate, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(0L));

            _clientRepo.CountAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(0L));

            // Act
            var result = await _service.DeleteCompanyAsync("c1", hardDelete: false);

            // Assert
            result.Should().BeTrue();
            await _companyRepo.Received(1).DeleteAsync("c1", softDelete: true, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteCompanyAsync_HardDelete_CallsHardDelete()
        {
            // Arrange
            var company = new Company { Id = "c1", Name = "Công ty A" };
            _companyRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(company));

            _departmentRepo.CountAsync(Arg.Any<Expression<Func<Department, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(0L));

            _gateRepo.CountAsync(Arg.Any<Expression<Func<Gate, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(0L));

            _clientRepo.CountAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(0L));

            // Act
            var result = await _service.DeleteCompanyAsync("c1", hardDelete: true);

            // Assert
            result.Should().BeTrue();
            await _companyRepo.Received(1).DeleteAsync("c1", softDelete: false, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RestoreCompanyAsync_WhenValid_RestoresAndReturnsCompanyDto()
        {
            // Arrange
            var company = new Company
            {
                Id = "c1",
                Code = "CP01",
                Name = "Công ty A",
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow.AddDays(-1)
            };

            _companyRepo.GetDeletedByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(company));

            _companyRepo.FindOneAsync(Arg.Any<Expression<Func<Company, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(null)); // Không bị trùng code

            _companyRepo.RestoreAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            // Act
            var result = await _service.RestoreCompanyAsync("c1");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("c1");
            result.Code.Should().Be("CP01");
            await _companyRepo.Received(1).RestoreAsync("c1", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RestoreCompanyAsync_WhenNotFoundInTrash_ThrowsNotFoundException()
        {
            // Arrange
            _companyRepo.GetDeletedByIdAsync("nonexistent", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(null));

            // Act
            var act = () => _service.RestoreCompanyAsync("nonexistent");

            // Assert
            var ex = await act.Should().ThrowAsync<NotFoundException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.COMPANY_NOT_FOUND);
        }

        [Fact]
        public async Task RestoreCompanyAsync_WhenCodeDuplicateWithActiveCompany_ThrowsConflictException()
        {
            // Arrange
            var company = new Company
            {
                Id = "c1",
                Code = "CP01",
                Name = "Công ty A",
                IsDeleted = true
            };
            var activeDuplicate = new Company
            {
                Id = "c2",
                Code = "CP01",
                Name = "Công ty B",
                IsDeleted = false
            };

            _companyRepo.GetDeletedByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(company));

            _companyRepo.FindOneAsync(Arg.Any<Expression<Func<Company, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(activeDuplicate));

            // Act
            var act = () => _service.RestoreCompanyAsync("c1");

            // Assert - Re-validation on restore 409 Conflict
            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.COMPANY_CODE_DUPLICATE);
            await _companyRepo.DidNotReceive().RestoreAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
    }
}
