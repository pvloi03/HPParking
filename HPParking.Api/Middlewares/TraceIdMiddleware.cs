using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace HPParking.Api.Middlewares
{
    public class TraceIdMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly Regex W3CTraceParentRegex = new(
            @"^[0-9a-f]{2}-[0-9a-f]{32}-[0-9a-f]{16}-[0-9a-f]{2}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public TraceIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string traceId;

            // 1. Kiểm tra header traceparent chuẩn W3C từ client gửi lên
            if (context.Request.Headers.TryGetValue("traceparent", out var traceParentHeader) &&
                W3CTraceParentRegex.IsMatch(traceParentHeader.ToString()))
            {
                var parts = traceParentHeader.ToString().Split('-');
                traceId = parts[1];
            }
            else
            {
                // 2. Tự động sinh TraceId chuẩn W3C (32 ký tự hex)
                traceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
            }

            // Gán vào context để Serilog và ExceptionMiddleware dùng chung
            context.TraceIdentifier = traceId;

            // 3. Trả về cả traceparent và X-Trace-ID trong Response Header
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey("traceparent"))
                {
                    context.Response.Headers["traceparent"] = $"00-{traceId}-{Activity.Current?.SpanId.ToString() ?? "0000000000000000"}-01";
                }

                if (!context.Response.Headers.ContainsKey("X-Trace-ID"))
                {
                    context.Response.Headers["X-Trace-ID"] = traceId;
                }

                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}
