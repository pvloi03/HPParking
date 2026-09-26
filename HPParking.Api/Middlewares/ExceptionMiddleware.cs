using FluentValidation;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Common;

namespace HPParking.Api.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, message, errors) = exception switch
            {
                AppException appEx => (
                    appEx.StatusCode,
                    appEx.Message,
                    appEx.Errors),

                ValidationException valEx => (
                    StatusCodes.Status400BadRequest,
                    "Dữ liệu yêu cầu không hợp lệ.",
                    valEx.Errors.Select(e => e.ErrorMessage).ToList()),

                ArgumentException argEx => (
                    StatusCodes.Status400BadRequest,
                    argEx.Message,
                    new List<string> { argEx.Message }),

                KeyNotFoundException notFoundEx => (
                    StatusCodes.Status404NotFound,
                    notFoundEx.Message,
                    new List<string> { notFoundEx.Message }),

                UnauthorizedAccessException unAuthEx => (
                    StatusCodes.Status401Unauthorized,
                    unAuthEx.Message,
                    new List<string> { unAuthEx.Message }),

                _ => (
                    StatusCodes.Status500InternalServerError,
                    "Đã xảy ra lỗi hệ thống nội bộ.",
                    new List<string> { _env.IsDevelopment() ? exception.ToString() : $"Lỗi máy chủ nội bộ. TraceId: {context.TraceIdentifier}" })
            };

            if (statusCode >= 500)
            {
                _logger.LogError(exception, "Lỗi hệ thống HTTP {StatusCode}: {Message} | TraceId: {TraceId}",
                    statusCode, message, context.TraceIdentifier);
            }
            else
            {
                _logger.LogWarning("Yêu cầu không hợp lệ HTTP {StatusCode}: {Message} | TraceId: {TraceId}",
                    statusCode, message, context.TraceIdentifier);
            }

            if (!context.Response.HasStarted)
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                context.Response.StatusCode = statusCode;

                var response = ApiResponse<object>.Failure(message, errors, context.TraceIdentifier);
                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
}
