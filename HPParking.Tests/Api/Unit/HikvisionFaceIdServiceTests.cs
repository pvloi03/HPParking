using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HPParking.Api.Services.Implementations;
using HPParking.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class HikvisionFaceIdServiceTests
    {
        private readonly ILogger<HikvisionFaceIdService> _logger = Substitute.For<ILogger<HikvisionFaceIdService>>();

        private class MockHttpMessageHandler : HttpMessageHandler
        {
            public Func<HttpRequestMessage, HttpResponseMessage> HandlerFunc { get; set; } =
                _ => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"statusCode\":1,\"statusString\":\"OK\"}", Encoding.UTF8, "application/json")
                };

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(HandlerFunc(request));
            }
        }

        [Fact]
        public async Task PushUserAsync_AllCallsSucceed_ReturnsSuccess()
        {
            var mockHandler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(mockHandler)
            {
                BaseAddress = new Uri("https://192.168.1.205")
            };

            var service = new HikvisionFaceIdService(_logger);
            var terminal = new FaceIdTerminalConfig
            {
                DeviceIp = "192.168.1.205",
                Username = "admin",
                Password = "password123",
                DeviceName = "Làn 1 Vào"
            };

            var cacheKey = $"{terminal.DeviceIp}|{terminal.Username}|{terminal.Password}";
            service.RegisterTestClient(cacheKey, httpClient);

            var faceImg = Encoding.UTF8.GetBytes("fake-face-image");
            var result = await service.PushUserAsync(terminal, "042203004613", "Phan Văn Lợi", true, "0364336088", faceImg);

            result.IsSuccess.Should().BeTrue();
            result.DeviceIp.Should().Be("192.168.1.205");
            result.ErrorMessage.Should().BeNullOrEmpty();
        }

        [Fact]
        public async Task PushUserAsync_CardFails_TriggersRollback_AndReturnsFailure()
        {
            var rollbackCalled = false;
            var mockHandler = new MockHttpMessageHandler
            {
                HandlerFunc = req =>
                {
                    if (req.RequestUri!.PathAndQuery.Contains("/CardInfo/Record"))
                    {
                        return new HttpResponseMessage(HttpStatusCode.BadRequest)
                        {
                            Content = new StringContent("{\"statusCode\":4,\"statusString\":\"Card Error\"}", Encoding.UTF8, "application/json")
                        };
                    }
                    if (req.RequestUri!.PathAndQuery.Contains("/UserInfo/Delete"))
                    {
                        rollbackCalled = true;
                    }
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"statusCode\":1,\"statusString\":\"OK\"}", Encoding.UTF8, "application/json")
                    };
                }
            };

            var httpClient = new HttpClient(mockHandler)
            {
                BaseAddress = new Uri("https://192.168.1.205")
            };

            var service = new HikvisionFaceIdService(_logger);
            var terminal = new FaceIdTerminalConfig
            {
                DeviceIp = "192.168.1.205",
                Username = "admin",
                Password = "password123",
                DeviceName = "Làn 1 Vào"
            };

            var cacheKey = $"{terminal.DeviceIp}|{terminal.Username}|{terminal.Password}";
            service.RegisterTestClient(cacheKey, httpClient);

            var result = await service.PushUserAsync(terminal, "042203004613", "Phan Văn Lợi", true, "0364336088", null);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Lỗi gán thẻ");
            rollbackCalled.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteUserAsync_SendsDeleteCardAndUser_ReturnsSuccess()
        {
            var cardDeleteCalled = false;
            var userDeleteCalled = false;

            var mockHandler = new MockHttpMessageHandler
            {
                HandlerFunc = req =>
                {
                    if (req.RequestUri!.PathAndQuery.Contains("/CardInfo/Delete")) cardDeleteCalled = true;
                    if (req.RequestUri!.PathAndQuery.Contains("/UserInfo/Delete")) userDeleteCalled = true;

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"statusCode\":1,\"statusString\":\"OK\"}", Encoding.UTF8, "application/json")
                    };
                }
            };

            var httpClient = new HttpClient(mockHandler)
            {
                BaseAddress = new Uri("https://192.168.1.205")
            };

            var service = new HikvisionFaceIdService(_logger);
            var terminal = new FaceIdTerminalConfig
            {
                DeviceIp = "192.168.1.205",
                Username = "admin",
                Password = "password123",
                DeviceName = "Làn 1 Vào"
            };

            var cacheKey = $"{terminal.DeviceIp}|{terminal.Username}|{terminal.Password}";
            service.RegisterTestClient(cacheKey, httpClient);

            var result = await service.DeleteUserAsync(terminal, "042203004613", "0364336088");

            result.IsSuccess.Should().BeTrue();
            cardDeleteCalled.Should().BeTrue();
            userDeleteCalled.Should().BeTrue();
        }

        [Fact]
        public async Task PingDeviceAsync_DeviceOnline_ReturnsTrue()
        {
            var mockHandler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(mockHandler)
            {
                BaseAddress = new Uri("https://192.168.1.205")
            };

            var service = new HikvisionFaceIdService(_logger);
            var terminal = new FaceIdTerminalConfig
            {
                DeviceIp = "192.168.1.205",
                Username = "admin",
                Password = "password123"
            };

            var cacheKey = $"{terminal.DeviceIp}|{terminal.Username}|{terminal.Password}";
            service.RegisterTestClient(cacheKey, httpClient);

            var isOnline = await service.PingDeviceAsync(terminal);
            isOnline.Should().BeTrue();
        }
    }
}
