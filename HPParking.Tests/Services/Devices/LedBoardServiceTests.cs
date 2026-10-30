using FluentAssertions;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Models;
using HPParking.SDK.CtrlSDK;
using HPParking.Services.Controller;
using HPParking.Services.Devices;
using NSubstitute;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HPParking.Tests.Services.Devices
{
    public class LedBoardServiceTests
    {
        [Theory]
        [InlineData("XIN CHAO", "*[H1]XIN CHAO[!]")]
        [InlineData("5.000 VND", "*[H1]5.000 VND[!]")]
        [InlineData("VE THANG", "*[H1]VE THANG[!]")]
        [InlineData("VE VIP", "*[H1]VE VIP[!]")]
        [InlineData("MIEN PHI", "*[H1]MIEN PHI[!]")]
        [InlineData("XE NOI BO", "*[H1]XE NOI BO[!]")]
        [InlineData("HEN GAP LAI", "*[H1]HEN GAP LAI[!]")]
        [InlineData("XIN CHÀO ĐỒNG CHÍ", "*[H1]XIN CHAO DONG CHI[!]")]
        [InlineData("HẸN GẶP LẠI", "*[H1]HEN GAP LAI[!]")]
        [InlineData("MIỄN PHÍ", "*[H1]MIEN PHI[!]")]
        [InlineData("VÉ THÁNG", "*[H1]VE THANG[!]")]
        public void FormatFrame_FormatsProtocolCorrectly(string input, string expected)
        {
            var result = LedBoardService.FormatFrame(input);
            result.Should().Be(expected);
        }

        [Fact]
        public async Task Send_SequentialCalls_ProcessedInFIFOOrder()
        {
            // Arrange
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            try
            {
                using var service = new LedBoardService("127.0.0.1", port);

                // Act
                service.Send("FRAME 1");
                service.Send("FRAME 2");
                service.Send("FRAME 3");

                using var serverClient = await listener.AcceptTcpClientAsync();
                using var stream = serverClient.GetStream();

                var buffer = new byte[1024];
                var sb = new StringBuilder();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                while (sb.Length < 3 * 16 && !cts.IsCancellationRequested)
                {
                    if (stream.DataAvailable || sb.Length == 0)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                        if (bytesRead > 0)
                        {
                            sb.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
                            if (sb.ToString().Contains("*[H1]FRAME 3[!]"))
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        await Task.Delay(50, cts.Token);
                    }
                }

                // Assert
                string received = sb.ToString();
                received.Should().Be("*[H1]FRAME 1[!]*[H1]FRAME 2[!]*[H1]FRAME 3[!]");
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task Send_WhenServerUnreachable_DoesNotThrowAndCompletes()
        {
            // Find a free closed port
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop(); // Ensure port is closed

            using var service = new LedBoardService("127.0.0.1", port);

            // Act & Assert
            var act = () => service.Send("TEST_MSG");
            act.Should().NotThrow();

            // The pending task should finish without crashing threadpool
            var awaitAct = async () => await service.PendingTask;
            await awaitAct.Should().NotThrowAsync();
        }

        [Fact]
        public async Task Send_AfterDispose_DoesNothing()
        {
            // Arrange: Khởi động TcpListener để quan sát kết nối mạng (chống zombie socket - spec §5.1)
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            try
            {
                var service = new LedBoardService("127.0.0.1", port);
                service.Dispose();

                // Act
                var act = () => service.Send("AFTER_DISPOSE");
                act.Should().NotThrow();

                // Assert: Task hoàn thành ngay lập tức
                service.PendingTask.IsCompleted.Should().BeTrue();

                // Quan sát ở cấp độ đối tượng: Không khởi tạo instance TcpClient mới
                service.Client.Should().BeNull("service đã Dispose không được khởi tạo TcpClient mới (chống zombie socket)");

                // Quan sát ở cấp độ mạng: Không có bất kỳ kết nối TCP nào được tạo tới listener
                await Task.Delay(100);
                listener.Pending().Should().BeFalse("không được phát sinh kết nối socket tới server sau khi Dispose");
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task Send_WhenDisposedDuringActiveConnection_ClosesSocketAndNeverRecreatesTcpClient()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            try
            {
                var service = new LedBoardService("127.0.0.1", port);

                // 1. Gửi bản tin đầu tiên để thiết lập kết nối socket thành công
                service.Send("INITIAL_FRAME");
                using var serverClient = await listener.AcceptTcpClientAsync();
                await service.PendingTask;

                service.Client.Should().NotBeNull();
                service.Client!.Connected.Should().BeTrue();

                // 2. Dispose service: Socket hiện tại phải được đóng và giải phóng triệt để
                service.Dispose();
                service.Client.Should().BeNull("Dispose phải giải phóng và dọn sạch TcpClient");

                // Đọc từ stream phía server: kết nối phía client phải bị ngắt (Read trả về 0)
                using var stream = serverClient.GetStream();
                var buffer = new byte[64];
                int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                read.Should().BeGreaterThan(0); // Đọc được frame INITIAL_FRAME

                int closeRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                closeRead.Should().Be(0, "Dispose phải ngắt kết nối socket hiện tại");

                // 3. Gửi bản tin tiếp theo sau khi Dispose: Tuyệt đối không được tạo TcpClient mới (chống zombie socket)
                service.Send("ZOMBIE_PREVENTION_FRAME");
                await service.PendingTask;
                await Task.Delay(100);

                service.Client.Should().BeNull("không được tạo lại TcpClient sau khi Dispose");
                listener.Pending().Should().BeFalse("không được có yêu cầu kết nối TCP mới tới server");
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task LaneRuntimeContext_DisplayFee_SendsFormattedFeeToLedBoard()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            try
            {
                using var ledBoard = new LedBoardService("127.0.0.1", port);
                var lane = new Lane { Direction = LaneDirection.Out };
                var context = new LaneRuntimeContext(lane)
                {
                    LedBoard = ledBoard
                };

                // Act
                context.DisplayFee(15000m, "MIEN PHI");

                using var serverClient = await listener.AcceptTcpClientAsync();
                using var stream = serverClient.GetStream();

                var buffer = new byte[256];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string received = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                // Assert
                received.Should().Be("*[H1]15,000 VND[!]");
            }
            finally
            {
                listener.Stop();
            }
        }

        [Theory]
        [InlineData(0, "VE THANG", "*[H1]VE THANG[!]")]
        [InlineData(0, "VE VIP", "*[H1]VE VIP[!]")]
        [InlineData(0, "MIEN PHI", "*[H1]MIEN PHI[!]")]
        [InlineData(0, "XE NOI BO", "*[H1]XE NOI BO[!]")]
        public async Task LaneRuntimeContext_DisplayFee_SendsZeroFeeTextWhenFeeIsZero(decimal fee, string zeroText, string expected)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            try
            {
                using var ledBoard = new LedBoardService("127.0.0.1", port);
                var lane = new Lane { Direction = LaneDirection.Out };
                var context = new LaneRuntimeContext(lane)
                {
                    LedBoard = ledBoard
                };

                // Act
                context.DisplayFee(fee, zeroText);

                using var serverClient = await listener.AcceptTcpClientAsync();
                using var stream = serverClient.GetStream();

                var buffer = new byte[256];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string received = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                // Assert
                received.Should().Be(expected);
            }
            finally
            {
                listener.Stop();
            }
        }

        [Theory]
        [InlineData(LaneDirection.In, "*[H1]XIN CHAO[!]")]
        [InlineData(LaneDirection.Out, "*[H1]HEN GAP LAI[!]")]
        public async Task LaneRuntimeContext_OpenBarrier_SendsGreetingWhenOpened(LaneDirection direction, string expectedGreeting)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            try
            {
                using var ledBoard = new LedBoardService("127.0.0.1", port);
                var lane = new Lane { Direction = direction, OutputRelay = 1 };
                var context = new LaneRuntimeContext(lane)
                {
                    LedBoard = ledBoard
                };

                var mockSdk = Substitute.For<IZKTecoSdk>();
                mockSdk.Connect(Arg.Any<string>()).Returns(new IntPtr(100));
                mockSdk.ControlDevice(Arg.Any<IntPtr>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>()).Returns(1);

                var controller = new ControllerService(mockSdk)
                {
                    Config = new ControllerConfig { IP = "127.0.0.1", Port = 4370 }
                };
                await controller.ConnectAsync(controller.Config);
                context.Controller = controller;

                // Act
                bool opened = context.OpenBarrier();

                // Assert
                opened.Should().BeTrue();

                using var serverClient = await listener.AcceptTcpClientAsync();
                using var stream = serverClient.GetStream();

                var buffer = new byte[256];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string received = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                received.Should().Be(expectedGreeting);
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public void LaneRuntimeContext_OpenBarrier_WhenControllerFails_DoesNotSendGreeting()
        {
            var lane = new Lane { Direction = LaneDirection.In };
            var context = new LaneRuntimeContext(lane)
            {
                Controller = null,
                LedBoard = new LedBoardService("127.0.0.1", 10000)
            };

            bool opened = context.OpenBarrier();

            opened.Should().BeFalse();
            context.LedBoard.PendingTask.IsCompleted.Should().BeTrue();
        }
    }
}
