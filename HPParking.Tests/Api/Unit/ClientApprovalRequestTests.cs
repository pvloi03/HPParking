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
using System.Linq.Expressions;
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
        private readonly IRepository<ParkingSession> _parkingSessionRepo = Substitute.For<IRepository<ParkingSession>>();
        private readonly IRepository<Card> _cardRepo = Substitute.For<IRepository<Card>>();
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
                cardRepo: _cardRepo,
                approvalRepo: _approvalRepo,
                parkingSessionRepo: _parkingSessionRepo,
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
        public async Task CreateClientAsync_WithVehicles_EnforcesRule2_ChildVehiclesStartInactive_AndActivateOnConfirmation()
        {
            // Arrange
            var service = CreateService();
            var deptId = "dept-hr-123";
            var dept = new Department { Id = deptId, Name = "Phòng Nhân sự", Email = "hr@hpparking.local", IsActive = true };
            _departmentRepo.GetByIdAsync(deptId, Arg.Any<CancellationToken>()).Returns(dept);

            var clientId = "client-rule2";
            Client? savedClient = null;
            _clientRepo.AddAsync(Arg.Do<Client>(c => { c.Id = clientId; savedClient = c; }), Arg.Any<CancellationToken>())
                .Returns(call => Task.FromResult(call.Arg<Client>()));

            Vehicle? savedVehicle = null;
            _vehicleRepo.AddAsync(Arg.Do<Vehicle>(v => { v.Id = "veh-rule2"; savedVehicle = v; }), Arg.Any<CancellationToken>())
                .Returns(call => Task.FromResult(call.Arg<Vehicle>()));

            ClientApprovalRequest? savedApproval = null;
            _approvalRepo.AddAsync(Arg.Do<ClientApprovalRequest>(a => savedApproval = a), Arg.Any<CancellationToken>())
                .Returns(call => Task.FromResult(call.Arg<ClientApprovalRequest>()));

            var req = new CreateClientRequest
            {
                Code = "NV-RULE2",
                Name = "Đặng Văn E",
                PhoneNumber = "0912345678",
                ApprovingDepartmentId = deptId,
                AuthMethods = [AuthMethodConstants.None],
                VerifyVehiclePlate = false,
                Vehicles = new List<HPParking.Api.DTOs.Vehicles.CreateVehicleRequest>
                {
                    new HPParking.Api.DTOs.Vehicles.CreateVehicleRequest
                    {
                        PlateNumber = "30A-99999",
                        Type = VehicleType.Car,
                        IsActive = true // DTO gửi lên true
                    }
                }
            };

            // Act 1: Tạo nhân sự
            var createResult = await service.CreateClientAsync(req);

            // Assert 1 (Rule 2: Child vehicle must start Inactive under an inactive client)
            savedClient.Should().NotBeNull();
            savedClient!.IsActive.Should().BeFalse();
            savedVehicle.Should().NotBeNull();
            savedVehicle!.IsActive.Should().BeFalse("Phương tiện đăng ký kèm nhân sự chưa kích hoạt phải ở trạng thái IsActive = false theo Quy tắc 2");

            // Setup for Act 2: Xác nhận phê duyệt
            _clientRepo.GetByIdAsync(clientId, Arg.Any<CancellationToken>()).Returns(savedClient);
            _approvalRepo.FindOneAsync(Arg.Any<Expression<Func<ClientApprovalRequest, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(savedApproval);
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle> { savedVehicle });

            // Act 2: Xác nhận kích hoạt
            var confirmResult = await service.ConfirmStatusByTokenAsync(savedApproval!.Token);

            // Assert 2: Cả nhân sự và phương tiện đều được chuyển sang Active
            confirmResult.NewActiveStatus.Should().BeTrue();
            savedClient.IsActive.Should().BeTrue();
            savedVehicle.IsActive.Should().BeTrue("Phương tiện liên kết phải được tự động kích hoạt khi hồ sơ nhân sự được phê duyệt");
            await _vehicleRepo.Received(1).UpdateAsync(savedVehicle, Arg.Any<CancellationToken>());
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
            var ex = await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage("*hết hạn*");
            ex.Which.ErrorCode.Should().Be(ErrorCodes.APPROVAL_TOKEN_EXPIRED);
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
            var ex = await act.Should().ThrowAsync<ConflictException>()
                .WithMessage("*đã được phê duyệt xác nhận*");
            ex.Which.ErrorCode.Should().Be(ErrorCodes.APPROVAL_REQUEST_ALREADY_PROCESSED);
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
        public async Task ConfirmStatusByTokenAsync_Deactivate_WhenClientHasActiveVehicles_DeactivatesClientAndCascadesVehicles()
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
            var activeVehicle = new Vehicle { Id = "veh-01", OwnerClientId = clientId, PlateNumber = "29A-12345", IsActive = true };

            _approvalRepo.FindOneAsync(Arg.Any<Expression<Func<ClientApprovalRequest, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(approval);
            _clientRepo.GetByIdAsync(clientId, Arg.Any<CancellationToken>())
                .Returns(client);

            // Giả lập còn phương tiện đang hoạt động liên kết với nhân sự này
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle> { activeVehicle });

            // Act: Người phê duyệt click email để khóa nhân sự
            var result = await service.ConfirmStatusByTokenAsync(token);

            // Assert: Không bị kẹt bởi active vehicles, tự động khóa nhân sự và cascade vô hiệu hóa phương tiện liên kết (Rule 2)
            result.NewActiveStatus.Should().BeFalse();
            client.IsActive.Should().BeFalse();
            approval.IsProcessed.Should().BeTrue();
            await _clientRepo.Received(1).UpdateAsync(client, Arg.Any<CancellationToken>());
            await _vehicleRepo.Received(1).UpdateAsync(Arg.Is<Vehicle>(v => v.Id == "veh-01" && !v.IsActive), Arg.Any<CancellationToken>());
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

        [Fact]
        public async Task ConfirmStatusByTokenAsync_Deactivate_WhenClientHasActiveParkingSession_ThrowsBadRequestExceptionWithRule7Code()
        {
            // Arrange
            var service = CreateService();
            var token = "deactivate-with-session-token";
            var clientId = "client-parked";
            var approval = new ClientApprovalRequest
            {
                Id = "appr-deactivate-parked",
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
                Name = "Lê Thị B",
                Code = "NV-004",
                IsActive = true
            };

            _approvalRepo.FindOneAsync(Arg.Any<Expression<Func<ClientApprovalRequest, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(approval);
            _clientRepo.GetByIdAsync(clientId, Arg.Any<CancellationToken>())
                .Returns(client);

            // Không còn phương tiện active nào
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle>());

            // Giả lập Rule 7: Nhân sự đang có phiên đỗ xe hoạt động trong bãi (Status == Active)
            _parkingSessionRepo.ExistsAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            // Act
            var act = () => service.ConfirmStatusByTokenAsync(token);

            // Assert (Rule 7 Active Parking Session Lockout & Table 1.2 CLIENT_HAS_ACTIVE_PARKING_SESSION)
            var ex = await act.Should().ThrowAsync<BadRequestException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.CLIENT_HAS_ACTIVE_PARKING_SESSION);
            ex.Which.Message.Should().Contain("phiên đỗ xe hoạt động trong bãi");
        }

        [Fact]
        public async Task RequestStatusChangeAsync_Deactivate_WhenClientHasActiveParkingSession_ThrowsBadRequestExceptionWithRule7Code()
        {
            // Arrange
            var service = CreateService();
            var clientId = "client-parked-request";
            var client = new Client
            {
                Id = clientId,
                Name = "Vũ Đình C",
                Code = "NV-005",
                IsActive = true
            };

            _clientRepo.GetByIdAsync(clientId, Arg.Any<CancellationToken>()).Returns(client);
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle>());

            // Giả lập Rule 7: Đang có phiên đỗ xe hoạt động
            _parkingSessionRepo.ExistsAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var req = new RequestClientStatusChangeRequest
            {
                ApprovingDepartmentId = "dept-hr",
                ActionType = ClientApprovalActionType.Deactivate
            };

            // Act
            var act = () => service.RequestStatusChangeAsync(clientId, req);

            // Assert
            var ex = await act.Should().ThrowAsync<BadRequestException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.CLIENT_HAS_ACTIVE_PARKING_SESSION);
        }

        [Fact]
        public async Task DeleteClientAsync_WhenClientHasPendingApprovalRequests_ThrowsConflictException_PerRule1()
        {
            // Arrange
            var service = CreateService();
            var clientId = "client-with-pending-approval";
            var client = new Client
            {
                Id = clientId,
                Name = "Hoàng Văn D",
                Code = "NV-006",
                IsActive = false
            };

            _clientRepo.GetByIdAsync(clientId, Arg.Any<CancellationToken>()).Returns(client);
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle>());

            // Giả lập còn 1 yêu cầu phê duyệt qua email đang chờ xử lý (Rule 1 Universal Restrict Deletion)
            _approvalRepo.CountAsync(Arg.Any<Expression<Func<ClientApprovalRequest, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(1);

            // Act
            var act = () => service.DeleteClientAsync(clientId, hardDelete: false);

            // Assert (Rule 1 & Section 3: Chặn xóa 409 Conflict, không tự động cascade mutation)
            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.CLIENT_HAS_PENDING_APPROVAL_REQUEST);
            ex.Which.Message.Should().Contain("yêu cầu phê duyệt qua email đang chờ xử lý");
        }

        [Fact]
        public async Task DeleteClientAsync_HardDelete_PermanentlyDeletesApprovalRequests_PreventingOrphanedData()
        {
            // Arrange
            var service = CreateService();
            var clientId = "client-to-hard-delete";
            var client = new Client
            {
                Id = clientId,
                Name = "Trịnh Văn E",
                Code = "NV-007",
                IsActive = false,
                IsDeleted = true
            };

            _clientRepo.GetDeletedByIdAsync(clientId, Arg.Any<CancellationToken>()).Returns(client);
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle>());
            _approvalRepo.CountAsync(Arg.Any<Expression<Func<ClientApprovalRequest, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(0);

            var pastApproval = new ClientApprovalRequest
            {
                Id = "appr-past",
                ClientId = clientId,
                IsProcessed = true
            };
            _approvalRepo.FindAsync(Arg.Any<Expression<Func<ClientApprovalRequest, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<ClientApprovalRequest> { pastApproval });

            // Act
            var result = await service.DeleteClientAsync(clientId, hardDelete: true);

            // Assert: Bản ghi ClientApprovalRequest phải được xóa vĩnh viễn (softDelete: false)
            result.Should().BeTrue();
            await _approvalRepo.Received(1).DeleteAsync("appr-past", softDelete: false, Arg.Any<CancellationToken>());
        }
        [Fact]
        public async Task DeleteClientAsync_WhenClientHasCardCodeProperty_UnlinksCard_ClearsCardCode_AndSoftDeletes()
        {
            // Arrange
            var service = CreateService();
            var clientId = "client-with-cardcode";
            var client = new Client
            {
                Id = clientId,
                Name = "Lê Văn Card",
                Code = "NV-008",
                CardCode = "0012345678",
                IsActive = false
            };

            _clientRepo.GetByIdAsync(clientId, Arg.Any<CancellationToken>()).Returns(client);
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle>());
            _approvalRepo.CountAsync(Arg.Any<Expression<Func<ClientApprovalRequest, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(0);

            var assignedCard = new Card
            {
                Id = "card-008",
                CardNumber = "0012345678",
                ClientId = clientId,
                Status = CardStatus.InUse,
                TargetType = CardTargetType.Person
            };
            _cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(assignedCard);

            // Act
            var result = await service.DeleteClientAsync(clientId, hardDelete: false);

            // Assert
            result.Should().BeTrue();
            client.CardCode.Should().BeEmpty();
            await _clientRepo.Received(1).DeleteAsync(clientId, softDelete: true, Arg.Any<CancellationToken>());
            await _cardRepo.Received().UpdateAsync(Arg.Is<Card>(c => c.ClientId == null && c.Status == CardStatus.Available), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteClientAsync_WhenClientHasCardInCardRepo_UnlinksCard_AndSoftDeletes()
        {
            // Arrange
            var service = CreateService();
            var clientId = "client-with-card-repo";
            var client = new Client
            {
                Id = clientId,
                Name = "Đỗ Văn Thẻ",
                Code = "NV-009",
                CardCode = "",
                IsActive = false
            };

            _clientRepo.GetByIdAsync(clientId, Arg.Any<CancellationToken>()).Returns(client);
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle>());
            _approvalRepo.CountAsync(Arg.Any<Expression<Func<ClientApprovalRequest, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(0);

            var assignedCard = new Card
            {
                Id = "card-123",
                CardNumber = "0099988877",
                ClientId = clientId,
                Status = CardStatus.InUse,
                TargetType = CardTargetType.Person
            };
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Card> { assignedCard });

            // Act
            var result = await service.DeleteClientAsync(clientId, hardDelete: false);

            // Assert
            result.Should().BeTrue();
            assignedCard.ClientId.Should().BeNull();
            assignedCard.Status.Should().Be(CardStatus.Available);
            await _cardRepo.Received().UpdateAsync(assignedCard, Arg.Any<CancellationToken>());
            await _clientRepo.Received(1).DeleteAsync(clientId, softDelete: true, Arg.Any<CancellationToken>());
        }
    }
}