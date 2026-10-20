using FluentAssertions;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Clients;
using HPParking.Api.Services.Implementations;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Constants;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class ClientApprovalRequestTests
    {
        private readonly IRepository<Client> _clientRepo = Substitute.For<IRepository<Client>>();
        private readonly IRepository<Vehicle> _vehicleRepo = Substitute.For<IRepository<Vehicle>>();
        private readonly IRepository<Lane> _laneRepo = Substitute.For<IRepository<Lane>>();
        private readonly IRepository<Device> _deviceRepo = Substitute.For<IRepository<Device>>();
        private readonly IFileStorageService _fileStorage = Substitute.For<IFileStorageService>();
        private readonly IFaceIdService _faceIdService = Substitute.For<IFaceIdService>();
        private readonly ILogger<ClientService> _logger = Substitute.For<ILogger<ClientService>>();
        private readonly IRepository<Department> _departmentRepo = Substitute.For<IRepository<Department>>();
        private readonly IRepository<ClientApprovalRequest> _approvalRepo = Substitute.For<IRepository<ClientApprovalRequest>>();
        private readonly IEmailSenderService _emailSender = Substitute.For<IEmailSenderService>();

        private ClientService CreateService()
        {
            return new ClientService(
                _clientRepo,
                _vehicleRepo,
                _laneRepo,
                _deviceRepo,
                _fileStorage,
                _faceIdService,
                _logger,
                departmentRepo: _departmentRepo,
                approvalRepo: _approvalRepo,
                emailSenderService: _emailSender);
        }

        [Fact]
        public async Task CreateClientAsync_WithApprovingDepartment_SetsInactiveAndCreatesApprovalRequest()
        {
            // Arrange
            var service = CreateService();
            var deptId = "dept-hr-123";
            var dept = new Department
            {
                Id = deptId,
                Name = "Phòng Hành chính Nhân sự",
                Email = "hr@hpparking.local",
                IsActive = true
            };
            _departmentRepo.GetByIdAsync(deptId, Arg.Any<CancellationToken>()).Returns(dept);

            Client? savedClient = null;
            _clientRepo.AddAsync(Arg.Do<Client>(c =>
            {
                c.Id = "client-001";
                savedClient = c;
            }), Arg.Any<CancellationToken>()).Returns(call => Task.FromResult(call.Arg<Client>()));

            ClientApprovalRequest? savedApproval = null;
            _approvalRepo.AddAsync(Arg.Do<ClientApprovalRequest>(a => savedApproval = a), Arg.Any<CancellationToken>())
                .Returns(call => Task.FromResult(call.Arg<ClientApprovalRequest>()));

            var req = new CreateClientRequest
            {
                Code = "NV-001",
                Name = "Trần Thị Lan",
                PhoneNumber = "0987654321",
                ApprovingDepartmentId = deptId,
                IsActive = true, // Gửi lên true nhưng hệ thống phải ép false
                AuthMethods = [AuthMethodConstants.None],
                VerifyVehiclePlate = false
            };

            // Act
            var result = await service.CreateClientAsync(req);

            // Assert
            savedClient.Should().NotBeNull();
            savedClient!.IsActive.Should().BeFalse("Nhân sự mới bắt buộc phải ở trạng thái Chưa kích hoạt");
            savedApproval.Should().NotBeNull();
            savedApproval!.ApprovingDepartmentEmail.Should().Be("hr@hpparking.local");
            savedApproval.ActionType.Should().Be(ClientApprovalActionType.Activate);
            savedApproval.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddHours(23));
            savedApproval.IsProcessed.Should().BeFalse();

            await _emailSender.Received(1).SendEmailAsync(
                Arg.Is<IEnumerable<string>>(emails => emails.Contains("hr@hpparking.local")),
                Arg.Is<string>(s => s.Contains("kích hoạt")),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ConfirmStatusByTokenAsync_ValidToken_ActivatesClientAndMarksProcessed()
        {
            // Arrange
            var service = CreateService();
            var token = "valid-secret-token-123";
            var clientId = "client-123";
            var approval = new ClientApprovalRequest
            {
                Id = "appr-01",
                ClientId = clientId,
                ApprovingDepartmentId = "dept-01",
                ActionType = ClientApprovalActionType.Activate,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(10),
                IsProcessed = false
            };
            var client = new Client
            {
                Id = clientId,
                Name = "Lê Văn Hùng",
                Code = "NV-002",
                IsActive = false
            };

            _approvalRepo.FindOneAsync(Arg.Any<Expression<Func<ClientApprovalRequest, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(approval);
            _clientRepo.GetByIdAsync(clientId, Arg.Any<CancellationToken>())
                .Returns(client);

            // Act
            var res = await service.ConfirmStatusByTokenAsync(token);

            // Assert
            res.Should().NotBeNull();
            res.NewActiveStatus.Should().BeTrue();
            client.IsActive.Should().BeTrue();
            approval.IsProcessed.Should().BeTrue();
            approval.ProcessedAt.Should().NotBeNull();

            await _clientRepo.Received(1).UpdateAsync(client, Arg.Any<CancellationToken>());
            await _approvalRepo.Received(1).UpdateAsync(approval, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ConfirmStatusByTokenAsync_ExpiredToken_ThrowsBadRequestException()
        {
            // Arrange
            var service = CreateService();
            var token = "expired-token";
            var approval = new ClientApprovalRequest
            {
                Id = "appr-02",
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(-1), // Quá hạn 1 tiếng
                IsProcessed = false
            };

            _approvalRepo.FindOneAsync(Arg.Any<Expression<Func<ClientApprovalRequest, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(approval);

            // Act
            var act = () => service.ConfirmStatusByTokenAsync(token);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage("*hết hạn*");
        }

        [Fact]
        public async Task ConfirmStatusByTokenAsync_AlreadyProcessedToken_ThrowsConflictException()
        {
            // Arrange
            var service = CreateService();
            var token = "already-used-token";
            var approval = new ClientApprovalRequest
            {
                Id = "appr-03",
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(10),
                IsProcessed = true,
                ProcessedAt = DateTime.UtcNow.AddMinutes(-30)
            };

            _approvalRepo.FindOneAsync(Arg.Any<Expression<Func<ClientApprovalRequest, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(approval);

            // Act
            var act = () => service.ConfirmStatusByTokenAsync(token);

            // Assert
            await act.Should().ThrowAsync<ConflictException>()
                .WithMessage("*đã được phê duyệt xác nhận*");
        }

        [Fact]
        public async Task RequestStatusChangeAsync_WhenClientAlreadyActiveAndRequestActivate_ThrowsBadRequestException()
        {
            // Arrange
            var service = CreateService();
            var clientId = "client-active";
            var client = new Client { Id = clientId, IsActive = true };
            _clientRepo.GetByIdAsync(clientId, Arg.Any<CancellationToken>()).Returns(client);

            var req = new RequestClientStatusChangeRequest
            {
                ApprovingDepartmentId = "dept-hr",
                ActionType = ClientApprovalActionType.Activate
            };

            // Act
            var act = () => service.RequestStatusChangeAsync(clientId, req);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage("*hiện đã ở trạng thái Hoạt động*");
        }

        [Fact]
        public async Task ConfirmStatusByTokenAsync_Deactivate_WhenClientHasActiveVehicles_ThrowsBadRequestException()
        {
            // Arrange
            var service = CreateService();
            var token = "deactivate-token-123";
            var clientId = "client-with-vehicles";
            var approval = new ClientApprovalRequest
            {
                Id = "appr-deactivate",
                ClientId = clientId,
                ApprovingDepartmentId = "dept-01",
                ActionType = ClientApprovalActionType.Deactivate,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(5),
                IsProcessed = false
            };
            var client = new Client
            {
                Id = clientId,
                Name = "Nguyễn Văn A",
                Code = "NV-003",
                IsActive = true
            };

            _approvalRepo.FindOneAsync(Arg.Any<Expression<Func<ClientApprovalRequest, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(approval);
            _clientRepo.GetByIdAsync(clientId, Arg.Any<CancellationToken>())
                .Returns(client);

            // Giả lập còn phương tiện đang hoạt động liên kết với nhân sự này
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle>
                {
                    new Vehicle { Id = "veh-01", OwnerClientId = clientId, PlateNumber = "29A-12345", IsActive = true }
                });

            // Act
            var act = () => service.ConfirmStatusByTokenAsync(token);

            // Assert (Rule 2 Active State Protection)
            await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage("*phương tiện đang hoạt động*");
        }

        [Fact]
        public async Task ResendApprovalEmailAsync_WhenLatestAlreadyProcessed_ThrowsConflictException()
        {
            // Arrange
            var service = CreateService();
            var clientId = "client-processed";
            var client = new Client { Id = clientId, Name = "Trần B", IsActive = true };
            _clientRepo.GetByIdAsync(clientId, Arg.Any<CancellationToken>()).Returns(client);

            var approval = new ClientApprovalRequest
            {
                Id = "appr-done",
                ClientId = clientId,
                ApprovingDepartmentId = "dept-01",
                IsProcessed = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };
            _approvalRepo.FindAsync(Arg.Any<Expression<Func<ClientApprovalRequest, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<ClientApprovalRequest> { approval });

            // Act
            var act = () => service.ResendApprovalEmailAsync(clientId);

            // Assert
            await act.Should().ThrowAsync<ConflictException>()
                .WithMessage("*đã được xử lý hoàn tất*");
        }
    }
}
