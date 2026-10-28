using FluentAssertions;
using HPParking.Api.Controllers.V1;
using HPParking.Api.DTOs.Clients;
using HPParking.Api.DTOs.Common;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class ClientsControllerTests
    {
        private readonly IClientService _clientService = Substitute.For<IClientService>();
        private readonly IVehicleService _vehicleService = Substitute.For<IVehicleService>();
        private readonly ILogger<ClientsController> _logger = Substitute.For<ILogger<ClientsController>>();
        private readonly ClientsController _controller;

        public ClientsControllerTests()
        {
            _controller = new ClientsController(_clientService, _vehicleService, _logger);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Fact]
        public async Task CreateClient_WhenEmailSentFails_ReturnsWarningMessageInApiResponse()
        {
            // Arrange
            var req = new CreateClientRequest
            {
                Code = "NV-001",
                Name = "Nguyễn Văn Test",
                PhoneNumber = "0987654321",
                ApprovingDepartmentId = "dept-1"
            };

            var detailDto = new ClientDetailDto
            {
                Id = "client-123",
                Code = "NV-001",
                Name = "Nguyễn Văn Test",
                LatestApprovalRequest = new ClientApprovalRequestDto
                {
                    Id = "req-1",
                    ClientId = "client-123",
                    ApprovingDepartmentEmail = "hr@example.com",
                    EmailSent = false,
                    EmailError = "Connection timed out to SMTP server"
                }
            };

            _clientService.CreateClientAsync(req, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(detailDto));

            // Act
            var actionResult = await _controller.CreateClient(req);

            // Assert
            var createdResult = actionResult as CreatedResult;
            createdResult.Should().NotBeNull();
            var response = createdResult!.Value as ApiResponse<ClientDetailDto>;
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Message.Should().Contain("nhưng gửi email xác nhận thất bại");
            response.Message.Should().Contain("Connection timed out to SMTP server");
            response.Message.Should().Contain("có thể gửi lại email sau");
        }

        [Fact]
        public async Task CreateClient_WhenEmailSentSucceeds_ReturnsSuccessMessageWithEmailNotice()
        {
            // Arrange
            var req = new CreateClientRequest
            {
                Code = "NV-002",
                Name = "Trần Văn An",
                PhoneNumber = "0987654322",
                ApprovingDepartmentId = "dept-1"
            };

            var detailDto = new ClientDetailDto
            {
                Id = "client-124",
                Code = "NV-002",
                Name = "Trần Văn An",
                LatestApprovalRequest = new ClientApprovalRequestDto
                {
                    Id = "req-2",
                    ClientId = "client-124",
                    ApprovingDepartmentEmail = "hr@example.com",
                    EmailSent = true
                }
            };

            _clientService.CreateClientAsync(req, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(detailDto));

            // Act
            var actionResult = await _controller.CreateClient(req);

            // Assert
            var createdResult = actionResult as CreatedResult;
            createdResult.Should().NotBeNull();
            var response = createdResult!.Value as ApiResponse<ClientDetailDto>;
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Message.Should().Contain("Đã gửi email xác nhận kích hoạt tới hr@example.com");
        }

        [Fact]
        public async Task CreateClient_WhenNoApprovalRequest_ReturnsStandardSuccessMessage()
        {
            // Arrange
            var req = new CreateClientRequest
            {
                Code = "NV-003",
                Name = "Lê Thị Lan",
                PhoneNumber = "0987654323"
            };

            var detailDto = new ClientDetailDto
            {
                Id = "client-125",
                Code = "NV-003",
                Name = "Lê Thị Lan",
                LatestApprovalRequest = null
            };

            _clientService.CreateClientAsync(req, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(detailDto));

            // Act
            var actionResult = await _controller.CreateClient(req);

            // Assert
            var createdResult = actionResult as CreatedResult;
            createdResult.Should().NotBeNull();
            var response = createdResult!.Value as ApiResponse<ClientDetailDto>;
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Message.Should().Be("Tạo mới khách hàng thành công.");
        }

        [Fact]
        public async Task RequestStatusChange_WhenEmailSentFails_ReturnsWarningMessage()
        {
            // Arrange
            var req = new RequestClientStatusChangeRequest
            {
                ApprovingDepartmentId = "dept-1",
                ActionType = ClientApprovalActionType.Deactivate,
                Note = "Xin khóa tài khoản"
            };

            var approvalDto = new ClientApprovalRequestDto
            {
                Id = "req-3",
                ClientId = "client-123",
                ApprovingDepartmentEmail = "manager@example.com",
                ActionType = ClientApprovalActionType.Deactivate,
                EmailSent = false,
                EmailError = "SMTP 550 Mailbox unavailable"
            };

            _clientService.RequestStatusChangeAsync("client-123", req, Arg.Any<string?>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(approvalDto));

            // Act
            var actionResult = await _controller.RequestStatusChange("client-123", req, CancellationToken.None);

            // Assert
            var okResult = actionResult as OkObjectResult;
            okResult.Should().NotBeNull();
            var response = okResult!.Value as ApiResponse<ClientApprovalRequestDto>;
            response.Should().NotBeNull();
            response!.Message.Should().Contain("Đã ghi nhận yêu cầu khóa hồ sơ nhưng gửi email thất bại");
            response.Message.Should().Contain("SMTP 550 Mailbox unavailable");
        }

        [Fact]
        public async Task RequestStatusChange_WhenEmailSentSucceeds_ReturnsSuccessMessage()
        {
            // Arrange
            var req = new RequestClientStatusChangeRequest
            {
                ApprovingDepartmentId = "dept-1",
                ActionType = ClientApprovalActionType.Activate
            };

            var approvalDto = new ClientApprovalRequestDto
            {
                Id = "req-4",
                ClientId = "client-123",
                ApprovingDepartmentEmail = "hr@example.com",
                ActionType = ClientApprovalActionType.Activate,
                EmailSent = true
            };

            _clientService.RequestStatusChangeAsync("client-123", req, Arg.Any<string?>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(approvalDto));

            // Act
            var actionResult = await _controller.RequestStatusChange("client-123", req, CancellationToken.None);

            // Assert
            var okResult = actionResult as OkObjectResult;
            okResult.Should().NotBeNull();
            var response = okResult!.Value as ApiResponse<ClientApprovalRequestDto>;
            response.Should().NotBeNull();
            response!.Message.Should().Be("Đã gửi email yêu cầu kích hoạt hồ sơ tới hr@example.com thành công.");
        }

        [Fact]
        public async Task ResendApprovalEmail_WhenEmailSentFails_ReturnsWarningMessage()
        {
            // Arrange
            var approvalDto = new ClientApprovalRequestDto
            {
                Id = "req-5",
                ClientId = "client-123",
                ApprovingDepartmentEmail = "hr@example.com",
                EmailSent = false,
                EmailError = "SocketException: Network unreachable"
            };

            _clientService.ResendApprovalEmailAsync("client-123", Arg.Any<string?>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(approvalDto));

            // Act
            var actionResult = await _controller.ResendApprovalEmail("client-123", CancellationToken.None);

            // Assert
            var okResult = actionResult as OkObjectResult;
            okResult.Should().NotBeNull();
            var response = okResult!.Value as ApiResponse<ClientApprovalRequestDto>;
            response.Should().NotBeNull();
            response!.Message.Should().Contain("Gửi lại email phê duyệt thất bại");
            response.Message.Should().Contain("SocketException: Network unreachable");
        }

        [Fact]
        public async Task ResendApprovalEmail_WhenEmailSentSucceeds_ReturnsSuccessMessage()
        {
            // Arrange
            var approvalDto = new ClientApprovalRequestDto
            {
                Id = "req-6",
                ClientId = "client-123",
                ApprovingDepartmentEmail = "hr@example.com",
                EmailSent = true
            };

            _clientService.ResendApprovalEmailAsync("client-123", Arg.Any<string?>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(approvalDto));

            // Act
            var actionResult = await _controller.ResendApprovalEmail("client-123", CancellationToken.None);

            // Assert
            var okResult = actionResult as OkObjectResult;
            okResult.Should().NotBeNull();
            var response = okResult!.Value as ApiResponse<ClientApprovalRequestDto>;
            response.Should().NotBeNull();
            response!.Message.Should().Be("Đã gửi lại email phê duyệt tới hr@example.com thành công.");
        }
    }
}
