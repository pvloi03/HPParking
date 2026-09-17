using System.Collections.Generic;
using HPParking.Api.DTOs.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HPParking.Api.Controllers
{
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        protected readonly ILogger _logger;

        protected BaseApiController(ILogger logger)
        {
            _logger = logger;
        }

        protected string TraceId => HttpContext.TraceIdentifier;

        protected IActionResult OkApiResponse<T>(T data, string message = "Thao tác thành công")
        {
            return Ok(ApiResponse<T>.SuccessResult(data, message, TraceId));
        }

        protected IActionResult CreatedApiResponse<T>(string uri, T data, string message = "Tạo mới thành công")
        {
            return Created(uri, ApiResponse<T>.CreatedResult(data, message, TraceId));
        }

        protected IActionResult CreatedApiResponse<T>(T data, string message = "Tạo mới thành công")
        {
            return StatusCode(StatusCodes.Status201Created, ApiResponse<T>.CreatedResult(data, message, TraceId));
        }

        protected IActionResult ErrorApiResponse(string message, List<string>? errors = null, int statusCode = StatusCodes.Status400BadRequest)
        {
            return StatusCode(statusCode, ApiResponse<object>.Failure(message, errors, TraceId));
        }
    }
}
