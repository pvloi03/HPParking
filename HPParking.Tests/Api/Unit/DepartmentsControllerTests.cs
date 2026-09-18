using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HPParking.Api.Controllers.V1;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Departments;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class DepartmentsControllerTests
    {
        private readonly IDepartmentService _departmentService = Substitute.For<IDepartmentService>();
        private readonly ILogger<DepartmentsController> _logger = Substitute.For<ILogger<DepartmentsController>>();
        private readonly DepartmentsController _controller;

        public DepartmentsControllerTests()
        {
            _controller = new DepartmentsController(_departmentService, _logger)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task GetDepartments_ReturnsOkWithPagedResult()
        {
            var query = new DepartmentFilterQuery();
            var paged = new PagedResult<DepartmentDto>(new List<DepartmentDto>
            {
                new() { Id = "d1", Code = "PB01", Name = "Phòng 1", CompanyName = "Công ty A" }
            }, 1, 10, 1);

            _departmentService.GetDepartmentsPagedAsync(query, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(paged));

            var result = await _controller.GetDepartments(query);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<PagedResult<DepartmentDto>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetDepartmentById_ReturnsOkWithDepartment()
        {
            var dto = new DepartmentDto { Id = "d1", Code = "PB01", Name = "Phòng 1", CompanyName = "Công ty A" };
            _departmentService.GetDepartmentByIdAsync("d1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(dto));

            var result = await _controller.GetDepartmentById("d1");

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<DepartmentDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Id.Should().Be("d1");
        }

        [Fact]
        public async Task CreateDepartment_ReturnsCreatedWithDepartment()
        {
            var request = new CreateDepartmentRequest { CompanyId = "c1", Code = "PB01", Name = "Phòng 1" };
            var created = new DepartmentDto { Id = "d1", Code = "PB01", Name = "Phòng 1", CompanyName = "Công ty A" };

            _departmentService.CreateDepartmentAsync(request, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(created));

            var result = await _controller.CreateDepartment(request);

            var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
            var response = createdResult.Value.Should().BeOfType<ApiResponse<DepartmentDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Code.Should().Be("PB01");
        }

        [Fact]
        public async Task UpdateDepartment_ReturnsOkWithUpdatedDepartment()
        {
            var request = new UpdateDepartmentRequest { Code = "PB01", Name = "Phòng 1 Mới" };
            var updated = new DepartmentDto { Id = "d1", Code = "PB01", Name = "Phòng 1 Mới" };

            _departmentService.UpdateDepartmentAsync("d1", request, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(updated));

            var result = await _controller.UpdateDepartment("d1", request);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<DepartmentDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Name.Should().Be("Phòng 1 Mới");
        }

        [Fact]
        public async Task DeleteDepartment_ReturnsOkWithTrue()
        {
            _departmentService.DeleteDepartmentAsync("d1", false, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            var result = await _controller.DeleteDepartment("d1", hardDelete: false);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().BeTrue();
        }
    }
}
