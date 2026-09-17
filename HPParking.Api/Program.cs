using System;
using System.IO;
using System.Reflection;
using System.Text;
using Asp.Versioning;
using FluentValidation.AspNetCore;
using HPParking.Api.Authentication;
using HPParking.Api.Configuration;
using HPParking.Api.Data;
using HPParking.Api.Middlewares;
using HPParking.Api.Services.Implementations;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Data;
using HPParking.Core.Interfaces;
using HPParking.Core.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// 1. CẤU HÌNH SERILOG (Lazy on-demand, cuộn theo ngày, tối đa 50MB, giữ 30 ngày)
// =============================================================================
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "HPParking.Api");

    if (context.HostingEnvironment.IsDevelopment())
    {
        configuration.WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{TraceId}] {Message:lj}{NewLine}{Exception}");
    }

    var logPath = context.Configuration["SerilogSettings:LogDirectory"] ?? "logs";
    var logFilePath = Path.Combine(logPath, "hpparking-api-.log");

    configuration.WriteTo.File(
        path: logFilePath,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 52428800, // 50MB
        rollOnFileSizeLimit: true,
        shared: true,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{TraceId}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
});

// =============================================================================
// 2. ĐĂNG KÝ CẤU HÌNH STRONGLY-TYPED (Options Pattern)
// =============================================================================
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<ApiKeySettings>(builder.Configuration.GetSection("ApiKeySettings"));
builder.Services.Configure<StorageSettings>(builder.Configuration.GetSection("StorageSettings"));
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDb"));

// =============================================================================
// 3. TẦNG DỮ LIỆU & REPOSITORIES (MongoDB) & SERVICES
// =============================================================================
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connStr = config["MongoDb:ConnectionString"] ?? "mongodb://localhost:27017";
    var dbName = config["MongoDb:DatabaseName"] ?? "hpparking";
    return new MongoDbContext(connStr, dbName);
});
builder.Services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));

// Đăng ký tầng dịch vụ nghiệp vụ (Services)
builder.Services.AddScoped<IAuthService, AuthService>();

// =============================================================================
// 4. CONTROLLERS & VALIDATION
// =============================================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddFluentValidationAutoValidation();

// =============================================================================
// 5. API VERSIONING (v1.0)
// =============================================================================
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// =============================================================================
// 6. CORS POLICY (Hỗ trợ Credentials theo ADR 0026)
// =============================================================================
var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000", "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// =============================================================================
// 7. XÁC THỰC KÉP HYBRID AUTH (PolicyScheme: JWT hoặc X-API-KEY) & PHÂN QUYỀN
// =============================================================================
var jwtSecret = builder.Configuration["JwtSettings:SecretKey"] ?? "HPParking_Secret_Key_For_Jwt_Authentication_Must_Be_Long_Enough_2026";
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "JWT_OR_APIKEY";
    options.DefaultChallengeScheme = "JWT_OR_APIKEY";
})
.AddPolicyScheme("JWT_OR_APIKEY", "JWT or API Key", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        if (context.Request.Headers.ContainsKey("X-API-KEY"))
        {
            return ApiKeyAuthenticationHandler.SchemeName;
        }
        return JwtBearerDefaults.AuthenticationScheme;
    };
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "HPParking.Api",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "HPParking.Clients",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Nếu header Authorization chưa có token, thử đọc từ HttpOnly Cookie theo ADR 0026
            if (string.IsNullOrEmpty(context.Token) &&
                context.Request.Cookies.TryGetValue("hpparking_access_token", out var cookieToken))
            {
                context.Token = cookieToken;
            }
            return Task.CompletedTask;
        }
    };
})
.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, _ => { });

builder.Services.AddAuthorization();

// =============================================================================
// 8. RATE LIMITING (Chống Brute-force endpoint đăng nhập & làm mới token)
// =============================================================================
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("LoginRateLimitPolicy", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("RefreshTokenRateLimitPolicy", opt =>
    {
        opt.PermitLimit = 20;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

// =============================================================================
// 9. TÀI LIỆU HÓA SWAGGER (Dual Authentication: Bearer + X-API-KEY)
// =============================================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HPParking API",
        Version = "v1",
        Description = "Hệ thống REST API kiểm soát khách hàng, phương tiện và FaceID HPParking"
    });

    // 1. JWT Bearer Scheme
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập Access Token lấy từ endpoint POST /api/v1/auth/login (không cần gõ từ khóa 'Bearer ')."
    });

    // 2. API Key Scheme
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Name = "X-API-KEY",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Nhập API Key được cấp cho hệ thống bên thứ ba tích hợp."
    });

    // 3. Security Requirements
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });

    // 4. XML Documentation
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// =============================================================================
// 10. THIẾT LẬP CHUỖI MIDDLEWARE PIPELINE (10 BƯỚC ĐỊNH SẴN)
// =============================================================================
var app = builder.Build();

// Khởi tạo tài khoản Quản trị viên mặc định (nếu CSDL chưa có Admin)
await DbSeeder.SeedAdminUserAsync(app.Services);

// 1. Exception Handling (Bắt ngoại lệ toàn cục ở tầng cao nhất)
app.UseMiddleware<ExceptionMiddleware>();

// 2. Trace Context (Gắn W3C traceparent sớm)
app.UseMiddleware<TraceIdMiddleware>();

// 3. Security Headers (Gắn header bảo mật tầng biên)
app.UseMiddleware<SecurityHeadersMiddleware>();

// 4. Serilog Request Logging (Ghi log thời gian phản hồi)
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} phản hồi {StatusCode} trong {Elapsed:0.0000} ms";
});

// 5. Phục vụ Static Files (Ảnh avatar trả ngay từ đĩa)
app.UseStaticFiles();

// 6. Routing (Phân tích endpoint)
app.UseRouting();

// 7. CORS (Đặt trước Auth & RateLimiter để luôn có header CORS)
app.UseCors("DefaultCorsPolicy");

// 8. Authentication (Giải mã JWT lấy UserId hoặc đọc X-API-KEY)
app.UseAuthentication();

// 9. Rate Limiter (Áp dụng chính sách giới hạn tốc độ gọi)
app.UseRateLimiter();

// 10. Authorization (Kiểm tra vai trò Admin/Manager/Viewer)
app.UseAuthorization();

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "HPParking.Api v1.0");
    options.RoutePrefix = "swagger";
});

app.MapControllers();

app.Run();
