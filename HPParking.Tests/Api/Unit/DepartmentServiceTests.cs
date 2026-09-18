using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Departments;
using HPParking.Api.Services.Implementations;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class DepartmentServiceTests
    {
        private readonly IRepository<Department> _departmentRepo = Substitute.For<IRepository<Department>>();
        private readonly IRepository<Company> _companyRepo = Substitute.For<IRepository<Company>>();
        private readonly IRepository<Client> _clientRepo = Substitute.For<IRepository<Client>>();
        private readonly ILogger<DepartmentService> _logger = Substitute.For<ILogger<DepartmentService>>();
        private readonly DepartmentService _service;

        public DepartmentServiceTests()
        {
            _service = new DepartmentService(_departmentRepo, _companyRepo, _clientRepo, _logger);
        }

        [Fact]
        public async Task GetDepartmentsPagedAsync_ReturnsPagedResult_WithCompanyName()
        {
            // Arrange
            var query = new DepartmentFilterQuery { PageIndex = 1, PageSize = 10, CompanyId = "c1" };
            var departments = new List<Department>
            {
                new() { Id = "d1", CompanyId = "c1", Code = "PB01", Name = "Phòng Kế Toán", IsActive = true, IsDeleted = false },
                new() { Id = "d2", CompanyId = "c1", Code = "PB02", Name = "Phòng Kỹ Thuật", IsActive = true, IsDeleted = false }
            };
            var companies = new List<Company>
            {
                new() { Id = "c1", Name = "Công ty Hải Phòng" }
            };

            _departmentRepo.CountAsync(Arg.Any<FilterDefinition<Department>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(2L));

            _departmentRepo.FindAsync(
                Arg.Any<FilterDefinition<Department>>(),
                Arg.Any<SortDefinition<Department>>(),
                0, 10,
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Department>>(departments));

            _companyRepo.FindAsync(Arg.Any<FilterDefinition<Company>>(), cancellationToken: Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Company>>(companies));

            // Act
            var result = await _service.GetDepartmentsPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Pagination.TotalCount.Should().Be(2);
            result.Items.Should().HaveCount(2);
            result.Items[0].CompanyName.Should().Be("Công ty Hải Phòng");
            result.Items[1].CompanyName.Should().Be("Công ty Hải Phòng");
        }

        [Fact]
        public async Task GetDepartmentByIdAsync_Found_ReturnsDepartmentDto_WithCompanyName()
        {
            // Arrange
            var department = new Department { Id = "d1", CompanyId = "c1", Code = "PB01", Name = "Phòng Kế Toán", IsActive = true };
            var company = new Company { Id = "c1", Name = "Công ty Hải Phòng" };

            _departmentRepo.GetByIdAsync("d1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Department?>(department));

            _companyRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(company));

            // Act
            var result = await _service.GetDepartmentByIdAsync("d1");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("d1");
            result.Code.Should().Be("PB01");
            result.Name.Should().Be("Phòng Kế Toán");
            result.CompanyName.Should().Be("Công ty Hải Phòng");
        }

        [Fact]
        public async Task GetDepartmentByIdAsync_NotFound_ThrowsNotFoundException()
        {
            // Arrange
            _departmentRepo.GetByIdAsync("nonexistent", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Department?>(null));

            // Act & Assert
            var act = () => _service.GetDepartmentByIdAsync("nonexistent");
            await act.Should().ThrowAsync<NotFoundException>()
                .Where(e => e.ErrorCode == ErrorCodes.DEPARTMENT_NOT_FOUND);
        }

        [Fact]
        public async Task CreateDepartmentAsync_Valid_CreatesAndReturnsDto()
        {
            // Arrange
            var company = new Company { Id = "c1", Name = "Công ty Hải Phòng", IsActive = true, IsDeleted = false };
            _companyRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(company));

            _departmentRepo.FindOneAsync(Arg.Any<Expression<Func<Department, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Department?>(null));

            var request = new CreateDepartmentRequest
            {
                CompanyId = "c1",
                Code = "  pb-kt  ",
                Name = "  Phòng Kế Toán  ",
                ManagerName = "Nguyễn Văn A",
                PhoneNumber = "02253999888",
                IsActive = true
            };

            // Act
            var result = await _service.CreateDepartmentAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Code.Should().Be("PB-KT");
            result.Name.Should().Be("Phòng Kế Toán");
            result.CompanyName.Should().Be("Công ty Hải Phòng");
            await _departmentRepo.Received(1).AddAsync(Arg.Is<Department>(d => d.Code == "PB-KT" && d.CompanyId == "c1"), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateDepartmentAsync_CompanyNotFound_ThrowsNotFoundException()
        {
            // Arrange
            _companyRepo.GetByIdAsync("invalid_c", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(null));

            var request = new CreateDepartmentRequest { CompanyId = "invalid_c", Code = "PB01", Name = "Phòng 1" };

            // Act & Assert
            var act = () => _service.CreateDepartmentAsync(request);
            await act.Should().ThrowAsync<NotFoundException>()
                .Where(e => e.ErrorCode == ErrorCodes.COMPANY_NOT_FOUND);
        }

        [Fact]
        public async Task CreateDepartmentAsync_CompanyInactive_ThrowsBadRequestException()
        {
            // Arrange
            var company = new Company { Id = "c1", Name = "Công ty Khóa", IsActive = false, IsDeleted = false };
            _companyRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(company));

            var request = new CreateDepartmentRequest { CompanyId = "c1", Code = "PB01", Name = "Phòng 1" };

            // Act & Assert
            var act = () => _service.CreateDepartmentAsync(request);
            await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage("*đang bị vô hiệu hóa*");
        }

        [Fact]
        public async Task CreateDepartmentAsync_DuplicateCode_ThrowsConflictException()
        {
            // Arrange
            var company = new Company { Id = "c1", Name = "Công ty A", IsActive = true };
            _companyRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(company));

            var existing = new Department { Id = "d_old", Code = "PB01", Name = "Phòng Kế Toán Cũ" };
            _departmentRepo.FindOneAsync(Arg.Any<Expression<Func<Department, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Department?>(existing));

            var request = new CreateDepartmentRequest { CompanyId = "c1", Code = "PB01", Name = "Phòng Mới" };

            // Act & Assert
            var act = () => _service.CreateDepartmentAsync(request);
            await act.Should().ThrowAsync<ConflictException>()
                .Where(e => e.ErrorCode == ErrorCodes.DEPARTMENT_CODE_DUPLICATE);
        }

        [Fact]
        public async Task UpdateDepartmentAsync_DuplicateCode_ThrowsConflictException()
        {
            // Arrange
            var current = new Department { Id = "d1", CompanyId = "c1", Code = "PB01", Name = "Phòng 1" };
            _departmentRepo.GetByIdAsync("d1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Department?>(current));

            var existingOther = new Department { Id = "d2", CompanyId = "c1", Code = "PB02", Name = "Phòng 2" };
            _departmentRepo.FindOneAsync(Arg.Any<Expression<Func<Department, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Department?>(existingOther));

            var request = new UpdateDepartmentRequest { Code = "PB02", Name = "Phòng 1 Đổi Tên" };

            // Act & Assert
            var act = () => _service.UpdateDepartmentAsync("d1", request);
            await act.Should().ThrowAsync<ConflictException>()
                .Where(e => e.ErrorCode == ErrorCodes.DEPARTMENT_CODE_DUPLICATE);
        }

        [Fact]
        public async Task DeleteDepartmentAsync_HasClients_ThrowsConflictException()
        {
            // Arrange
            var department = new Department { Id = "d1", Name = "Phòng Kỹ Thuật" };
            _departmentRepo.GetByIdAsync("d1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Department?>(department));

            _clientRepo.CountAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(5L)); // 5 clients in this department

            // Act & Assert
            var act = () => _service.DeleteDepartmentAsync("d1");
            await act.Should().ThrowAsync<ConflictException>()
                .Where(e => e.ErrorCode == ErrorCodes.DEPARTMENT_HAS_CLIENTS);

            await _departmentRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteDepartmentAsync_SoftDelete_CallsSoftDelete()
        {
            // Arrange
            var department = new Department { Id = "d1", Name = "Phòng Kỹ Thuật" };
            _departmentRepo.GetByIdAsync("d1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Department?>(department));

            _clientRepo.CountAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(0L));

            // Act
            var result = await _service.DeleteDepartmentAsync("d1", hardDelete: false);

            // Assert
            result.Should().BeTrue();
            await _departmentRepo.Received(1).DeleteAsync("d1", softDelete: true, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteDepartmentAsync_HardDelete_CallsHardDelete()
        {
            // Arrange
            var department = new Department { Id = "d1", Name = "Phòng Kỹ Thuật" };
            _departmentRepo.GetByIdAsync("d1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Department?>(department));

            _clientRepo.CountAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(0L));

            // Act
            var result = await _service.DeleteDepartmentAsync("d1", hardDelete: true);

            // Assert
            result.Should().BeTrue();
            await _departmentRepo.Received(1).DeleteAsync("d1", softDelete: false, Arg.Any<CancellationToken>());
        }
    }
}
