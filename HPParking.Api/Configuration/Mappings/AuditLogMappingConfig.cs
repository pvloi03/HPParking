using HPParking.Api.DTOs.AuditLogs;
using HPParking.Core.Models.Entities;
using Mapster;

namespace HPParking.Api.Configuration.Mappings
{
    /// <summary>
    /// Cấu hình quy tắc ánh xạ Mapster cho nhật ký kiểm toán (AuditLog)
    /// </summary>
    public class AuditLogMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<AuditLog, AuditLogDto>();
            config.NewConfig<AuditLog, AuditLogDetailDto>();
        }
    }
}
