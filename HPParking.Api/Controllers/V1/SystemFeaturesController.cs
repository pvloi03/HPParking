using Asp.Versioning;
using HPParking.Api.DTOs.Common;
using HPParking.Core.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/system/features")]
    public class SystemFeaturesController : BaseApiController
    {
        private readonly FeatureOptions _featureOptions;

        public SystemFeaturesController(
            FeatureOptions featureOptions,
            ILogger<SystemFeaturesController> logger)
            : base(logger)
        {
            _featureOptions = featureOptions;
        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public IActionResult GetFeatures()
        {
            return OkApiResponse(new
            {
                enableFleetDispatch = _featureOptions.EnableFleetDispatch
            }, "Lấy cấu hình tính năng hệ thống thành công.");
        }
    }
}
