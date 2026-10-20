using HPParking.Api.Services.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using System.Text;

namespace HPParking.Api.Services.Implementations
{
    /// <summary>
    /// Xây dựng template tiêu đề và nội dung HTML cho email phê duyệt hồ sơ nhân sự (ADR 0042)
    /// </summary>
    public class ClientApprovalEmailTemplateBuilder : IClientApprovalEmailTemplateBuilder
    {
        public string BuildSubject(Client client, ClientApprovalActionType actionType)
        {
            string actionText = actionType == ClientApprovalActionType.Activate ? "kích hoạt" : "khóa";
            return $"[HPParking] Yêu cầu xác nhận {actionText} hồ sơ nhân sự - {client.Name} ({client.Code})";
        }

        public string BuildHtmlBody(ClientApprovalEmailModel model)
        {
            var client = model.Client;
            var department = model.TargetDepartment;
            var approvalRequest = model.ApprovalRequest;
            var confirmUrl = model.ConfirmUrl;
            var workingDeptName = string.IsNullOrWhiteSpace(model.WorkingDepartmentName) ? "Chưa gán" : model.WorkingDepartmentName;

            bool isActivate = approvalRequest.ActionType == ClientApprovalActionType.Activate;
            string actionText = isActivate ? "kích hoạt" : "khóa";
            string actionTitle = isActivate ? "KÍCH HOẠT HỒ SƠ" : "KHÓA HỒ SƠ";
            string buttonColor = isActivate ? "#16a34a" : "#dc2626";
            string buttonText = isActivate ? "XÁC NHẬN KÍCH HOẠT (1-CLICK)" : "XÁC NHẬN KHÓA HỒ SƠ (1-CLICK)";

            string vehicleListHtml = (model.VehicleSummaries != null && model.VehicleSummaries.Count > 0)
                ? string.Join(", ", model.VehicleSummaries)
                : "Chưa đăng ký";

            var htmlBuilder = new StringBuilder();
            htmlBuilder.Append($@"
<div style=""font-family: Arial, 'Segoe UI', Tahoma, sans-serif; max-width: 620px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden; background-color: #ffffff; color: #1e293b;"">
    <div style=""background: linear-gradient(135deg, #1e40af 0%, #3b82f6 100%); padding: 24px 28px; text-align: center; color: #ffffff;"">
        <h1 style=""margin: 0; font-size: 20px; font-weight: 700; letter-spacing: 0.5px;"">HỆ THỐNG QUẢN LÝ BÃI XE HPPARKING</h1>
        <p style=""margin: 6px 0 0 0; font-size: 13px; opacity: 0.9;"">Thông báo yêu cầu phê duyệt trạng thái hồ sơ nhân sự</p>
    </div>
    
    <div style=""padding: 28px;"">
        <p style=""margin: 0 0 16px 0; font-size: 14px; line-height: 1.6;"">
            Kính gửi <strong>{department.Name}</strong>,
        </p>
        <p style=""margin: 0 0 20px 0; font-size: 14px; line-height: 1.6;"">
            Hệ thống vừa tiếp nhận yêu cầu <strong>{actionText}</strong> quyền ra vào bãi đỗ xe cho hồ sơ nhân sự dưới đây. Vui lòng kiểm tra thông tin và xác nhận:
        </p>

        <div style=""background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 18px 20px; margin-bottom: 24px;"">
            <table style=""width: 100%; border-collapse: collapse; font-size: 13px;"">
                <tr>
                    <td style=""padding: 6px 0; color: #64748b; width: 140px;"">Họ và tên:</td>
                    <td style=""padding: 6px 0; font-weight: 600; color: #0f172a;"">{client.Name}</td>
                </tr>
                <tr>
                    <td style=""padding: 6px 0; color: #64748b;"">Mã định danh (CCCD):</td>
                    <td style=""padding: 6px 0; font-weight: 600; color: #0f172a;"">{client.Code}</td>
                </tr>
                <tr>
                    <td style=""padding: 6px 0; color: #64748b;"">Số điện thoại:</td>
                    <td style=""padding: 6px 0; color: #0f172a;"">{client.PhoneNumber}</td>
                </tr>
                <tr>
                    <td style=""padding: 6px 0; color: #64748b;"">Đơn vị công tác:</td>
                    <td style=""padding: 6px 0; color: #0f172a;"">{workingDeptName}</td>
                </tr>
                <tr>
                    <td style=""padding: 6px 0; color: #64748b;"">Phương tiện đăng ký:</td>
                    <td style=""padding: 6px 0; color: #0f172a;"">{vehicleListHtml}</td>
                </tr>
                {(string.IsNullOrWhiteSpace(approvalRequest.Note) ? "" : $@"
                <tr>
                    <td style=""padding: 6px 0; color: #64748b;"">Lý do / Ghi chú:</td>
                    <td style=""padding: 6px 0; color: #b45309; font-weight: 500;"">{approvalRequest.Note}</td>
                </tr>")}
                <tr>
                    <td style=""padding: 6px 0; color: #64748b;"">Hành động yêu cầu:</td>
                    <td style=""padding: 6px 0; font-weight: 700; color: {(isActivate ? "#16a34a" : "#dc2626")};"">{actionTitle}</td>
                </tr>
            </table>
        </div>

        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""{confirmUrl}"" style=""background-color: {buttonColor}; color: #ffffff; text-decoration: none; padding: 14px 32px; border-radius: 8px; font-size: 14px; font-weight: 700; display: inline-block; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);"">
                {buttonText}
            </a>
        </div>

        <p style=""margin: 0; font-size: 12px; color: #94a3b8; text-align: center; line-height: 1.5;"">
            ⏱ Liên kết xác nhận này có hiệu lực trong vòng <strong>24 giờ</strong>.<br/>
            Nếu bạn không có thẩm quyền hoặc nhận nhầm email, vui lòng bỏ qua thư này.
        </p>
    </div>
    
    <div style=""background-color: #f1f5f9; padding: 14px 28px; text-align: center; border-top: 1px solid #e2e8f0; font-size: 11px; color: #64748b;"">
        Email được gửi tự động từ Hệ thống Quản Lý Bãi Xe Thông Minh HPParking. Vui lòng không trả lời trực tiếp email này.
    </div>
</div>");

            return htmlBuilder.ToString();
        }
    }
}
