namespace HPParking.Api.Middlewares
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;

                // Chống MIME-sniffing
                if (!headers.ContainsKey("X-Content-Type-Options"))
                    headers["X-Content-Type-Options"] = "nosniff";

                // Chống Clickjacking qua iframe
                if (!headers.ContainsKey("X-Frame-Options"))
                    headers["X-Frame-Options"] = "DENY";

                // Bảo vệ chuyển hướng URL
                if (!headers.ContainsKey("Referrer-Policy"))
                    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

                // X-XSS-Protection
                if (!headers.ContainsKey("X-XSS-Protection"))
                    headers["X-XSS-Protection"] = "1; mode=block";

                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}
