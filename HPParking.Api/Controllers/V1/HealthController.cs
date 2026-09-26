using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [AllowAnonymous]
    public class HealthController : BaseApiController
    {
        private readonly IHostEnvironment _env;

        public HealthController(ILogger<HealthController> logger, IHostEnvironment env)
            : base(logger)
        {
            _env = env;
        }

        /// <summary>
        /// Kiểm tra trạng thái hoạt động của dịch vụ API
        /// </summary>
        [HttpGet]
        public IActionResult CheckHealth()
        {
            var healthInfo = new
            {
                Status = "Healthy",
                Service = "HPParking.Api",
                Version = "1.0",
                Environment = _env.EnvironmentName,
                Timestamp = DateTime.UtcNow
            };

            return OkApiResponse<object>(healthInfo, "Dịch vụ HPParking.Api đang hoạt động bình thường.");
        }
    }
}
