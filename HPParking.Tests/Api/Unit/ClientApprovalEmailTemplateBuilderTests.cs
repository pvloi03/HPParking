using FluentAssertions;
using HPParking.Api.Services.Implementations;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class ClientApprovalEmailTemplateBuilderTests
    {
        private readonly ClientApprovalEmailTemplateBuilder _builder = new();

        [Fact]
        public void BuildSubject_ForActivate_ShouldIncludeCorrectActionAndClientDetails()
        {
            var client = new Client
            {
                Name = "Nguyễn Văn A",
                Code = "012345678901"
            };

            var subject = _builder.BuildSubject(client, ClientApprovalActionType.Activate);

            subject.Should().Be("[HPParking] Yêu cầu xác nhận kích hoạt hồ sơ nhân sự - Nguyễn Văn A (012345678901)");
        }

        [Fact]
        public void BuildSubject_ForDeactivate_ShouldIncludeCorrectActionAndClientDetails()
        {
            var client = new Client
            {
                Name = "Trần Thị B",
                Code = "987654321098"
            };

            var subject = _builder.BuildSubject(client, ClientApprovalActionType.Deactivate);

            subject.Should().Be("[HPParking] Yêu cầu xác nhận khóa hồ sơ nhân sự - Trần Thị B (987654321098)");
        }

        [Fact]
        public void BuildHtmlBody_WithFullDetails_ShouldGenerateCompleteEmailMarkup()
        {
            var client = new Client
            {
                Id = "client-001",
                Name = "Lê Văn C",
                Code = "123456789",
                PhoneNumber = "0901234567"
            };

            var dept = new Department
            {
                Id = "dept-hr",
                Name = "Phòng Tổ chức Nhân sự",
                Email = "hr@example.com"
            };

            var request = new ClientApprovalRequest
            {
                Id = "req-001",
                ClientId = client.Id,
                ActionType = ClientApprovalActionType.Activate,
                Token = "test-token-123",
                Note = "Nhân viên mới tuyển dụng"
            };

            var model = new ClientApprovalEmailModel
            {
                Client = client,
                TargetDepartment = dept,
                ApprovalRequest = request,
                ConfirmUrl = "http://localhost:5173/confirm-client-status?token=test-token-123",
                WorkingDepartmentName = "Ban Quản lý Dự án",
                VehicleSummaries = new[] { "29A-12345 (Car)", "29B-67890 (Motorbike)" }
            };

            var html = _builder.BuildHtmlBody(model);

            html.Should().Contain("Phòng Tổ chức Nhân sự");
            html.Should().Contain("Lê Văn C");
            html.Should().Contain("123456789");
            html.Should().Contain("0901234567");
            html.Should().Contain("Ban Quản lý Dự án");
            html.Should().Contain("29A-12345 (Car), 29B-67890 (Motorbike)");
            html.Should().Contain("Nhân viên mới tuyển dụng");
            html.Should().Contain("KÍCH HOẠT HỒ SƠ");
            html.Should().Contain("XÁC NHẬN KÍCH HOẠT (1-CLICK)");
            html.Should().Contain("http://localhost:5173/confirm-client-status?token=test-token-123");
        }

        [Fact]
        public void BuildHtmlBody_WithNoVehiclesAndNoNote_ShouldRenderFallbackPlaceholders()
        {
            var client = new Client
            {
                Id = "client-002",
                Name = "Phạm Văn D",
                Code = "555555555",
                PhoneNumber = "0988888888"
            };

            var dept = new Department
            {
                Id = "dept-admin",
                Name = "Phòng Hành chính",
                Email = "admin@example.com"
            };

            var request = new ClientApprovalRequest
            {
                Id = "req-002",
                ClientId = client.Id,
                ActionType = ClientApprovalActionType.Deactivate,
                Token = "deact-token-456",
                Note = null
            };

            var model = new ClientApprovalEmailModel
            {
                Client = client,
                TargetDepartment = dept,
                ApprovalRequest = request,
                ConfirmUrl = "http://localhost:5173/confirm-client-status?token=deact-token-456",
                WorkingDepartmentName = null,
                VehicleSummaries = null
            };

            var html = _builder.BuildHtmlBody(model);

            html.Should().Contain("Phòng Hành chính");
            html.Should().Contain("Phạm Văn D");
            html.Should().Contain("Chưa gán");
            html.Should().Contain("Chưa đăng ký");
            html.Should().NotContain("Lý do / Ghi chú:");
            html.Should().Contain("KHÓA HỒ SƠ");
            html.Should().Contain("XÁC NHẬN KHÓA HỒ SƠ (1-CLICK)");
            html.Should().Contain("http://localhost:5173/confirm-client-status?token=deact-token-456");
        }
    }
}
