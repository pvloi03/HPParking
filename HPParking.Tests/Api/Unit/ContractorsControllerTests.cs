using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HPParking.Api.Controllers.V1;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Contractors;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class ContractorsControllerTests
    {
        private readonly IContractorService _contractorService = Substitute.For<IContractorService>();
        private readonly ILogger<ContractorsController> _logger = Substitute.For<ILogger<ContractorsController>>();
        private readonly ContractorsController _controller;

        public ContractorsControllerTests()
        {
            _controller = new ContractorsController(_contractorService, _logger)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task GetContractors_ReturnsOkWithPagedResult()
        {
            var query = new ContractorFilterQuery();
            var paged = new PagedResult<ContractorDto>(new List<ContractorDto>
            {
                new() { Id = "nt1", Code = "NT01", Name = "Nhà thầu A" }
            }, 1, 10, 1);

            _contractorService.GetContractorsPagedAsync(query, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(paged));

            var result = await _controller.GetContractors(query);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<PagedResult<ContractorDto>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetContractorById_ReturnsOkWithContractor()
        {
            var dto = new ContractorDto { Id = "nt1", Code = "NT01", Name = "Nhà thầu A" };
            _contractorService.GetContractorByIdAsync("nt1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(dto));

            var result = await _controller.GetContractorById("nt1");

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<ContractorDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Id.Should().Be("nt1");
        }

        [Fact]
        public async Task CreateContractor_ReturnsCreatedWithContractor()
        {
            var request = new CreateContractorRequest { Code = "NT01", Name = "Nhà thầu A" };
            var created = new ContractorDto { Id = "nt1", Code = "NT01", Name = "Nhà thầu A" };

            _contractorService.CreateContractorAsync(request, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(created));

            var result = await _controller.CreateContractor(request);

            var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
            var response = createdResult.Value.Should().BeOfType<ApiResponse<ContractorDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Code.Should().Be("NT01");
        }

        [Fact]
        public async Task UpdateContractor_ReturnsOkWithUpdatedContractor()
        {
            var request = new UpdateContractorRequest { Code = "NT01", Name = "Nhà thầu A Mới" };
            var updated = new ContractorDto { Id = "nt1", Code = "NT01", Name = "Nhà thầu A Mới" };

            _contractorService.UpdateContractorAsync("nt1", request, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(updated));

            var result = await _controller.UpdateContractor("nt1", request);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<ContractorDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Name.Should().Be("Nhà thầu A Mới");
        }

        [Fact]
        public async Task DeleteContractor_ReturnsOkWithTrue()
        {
            _contractorService.DeleteContractorAsync("nt1", false, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            var result = await _controller.DeleteContractor("nt1", hardDelete: false);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().BeTrue();
        }

        [Fact]
        public async Task RestoreContractor_ReturnsOkWithRestoredContractor()
        {
            var restored = new ContractorDto { Id = "nt1", Code = "NT01", Name = "Nhà thầu A" };
            _contractorService.RestoreContractorAsync("nt1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(restored));

            var result = await _controller.RestoreContractor("nt1");

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<ContractorDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Id.Should().Be("nt1");
        }
    }
}
