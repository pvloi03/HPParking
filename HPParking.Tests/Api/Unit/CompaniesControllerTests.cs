using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HPParking.Api.Controllers.V1;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Companies;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class CompaniesControllerTests
    {
        private readonly ICompanyService _companyService = Substitute.For<ICompanyService>();
        private readonly ILogger<CompaniesController> _logger = Substitute.For<ILogger<CompaniesController>>();
        private readonly CompaniesController _controller;

        public CompaniesControllerTests()
        {
            _controller = new CompaniesController(_companyService, _logger)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task GetCompanies_ReturnsOkWithPagedResult()
        {
            var query = new CompanyFilterQuery();
            var paged = new PagedResult<CompanyDto>(new List<CompanyDto>
            {
                new() { Id = "c1", Code = "CP01", Name = "Công ty A" }
            }, 1, 10, 1);

            _companyService.GetCompaniesPagedAsync(query, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(paged));

            var result = await _controller.GetCompanies(query);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<PagedResult<CompanyDto>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetCompanyById_ReturnsOkWithCompany()
        {
            var dto = new CompanyDto { Id = "c1", Code = "CP01", Name = "Công ty A" };
            _companyService.GetCompanyByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(dto));

            var result = await _controller.GetCompanyById("c1");

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<CompanyDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Id.Should().Be("c1");
        }

        [Fact]
        public async Task CreateCompany_ReturnsCreatedWithCompany()
        {
            var request = new CreateCompanyRequest { Code = "CP01", Name = "Công ty A" };
            var created = new CompanyDto { Id = "c1", Code = "CP01", Name = "Công ty A" };

            _companyService.CreateCompanyAsync(request, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(created));

            var result = await _controller.CreateCompany(request);

            var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
            var response = createdResult.Value.Should().BeOfType<ApiResponse<CompanyDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Code.Should().Be("CP01");
        }

        [Fact]
        public async Task UpdateCompany_ReturnsOkWithUpdatedCompany()
        {
            var request = new UpdateCompanyRequest { Code = "CP01", Name = "Công ty A Mới" };
            var updated = new CompanyDto { Id = "c1", Code = "CP01", Name = "Công ty A Mới" };

            _companyService.UpdateCompanyAsync("c1", request, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(updated));

            var result = await _controller.UpdateCompany("c1", request);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<CompanyDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Name.Should().Be("Công ty A Mới");
        }

        [Fact]
        public async Task DeleteCompany_ReturnsOkWithTrue()
        {
            _companyService.DeleteCompanyAsync("c1", false, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            var result = await _controller.DeleteCompany("c1", hardDelete: false);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().BeTrue();
        }
    }
}
