using FluentAssertions;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Forms;
using NSubstitute;
using System.Windows.Forms;
using Xunit;

namespace HPParking.Tests.Forms
{
    public class FrmMainHotkeyTests
    {
        [Theory]
        [InlineData(Keys.F1, 0)]
        [InlineData(Keys.F2, 1)]
        [InlineData(Keys.F3, 2)]
        [InlineData(Keys.F4, 3)]
        public void ResolveHotkeySlot_WhenF1ToF4_ShouldReturnCorrectSlotIndex(Keys key, int expectedSlot)
        {
            // Act
            int? slot = FrmMain.ResolveHotkeySlot(key);

            // Assert
            slot.Should().Be(expectedSlot);
        }

        [Theory]
        [InlineData(Keys.Alt | Keys.F4)]
        [InlineData(Keys.Control | Keys.F1)]
        [InlineData(Keys.Shift | Keys.F2)]
        [InlineData(Keys.F5)]
        [InlineData(Keys.Enter)]
        [InlineData(Keys.None)]
        public void ResolveHotkeySlot_WhenModifierOrOtherKeys_ShouldReturnNull(Keys key)
        {
            // Act
            int? slot = FrmMain.ResolveHotkeySlot(key);

            // Assert
            slot.Should().BeNull();
        }

        [Fact]
        public void ApplyLaneContexts_ShouldDisplaySlotHeaderWithHotkeySuffixes_FromLeftToRight()
        {
            RunOnStaThread(() =>
            {
                // Arrange
                using var form = CreateFrmMain();
                var contexts = new System.Collections.Generic.List<HPParking.Models.LaneRuntimeContext>
                {
                    new(new HPParking.Core.Models.Entities.Lane { Id = "L1", Name = "Làn Vào 1", Direction = HPParking.Core.Models.Enums.LaneDirection.In, InputReader = 1, OutputRelay = 1 }),
                    new(new HPParking.Core.Models.Entities.Lane { Id = "L2", Name = "Làn Vào 2", Direction = HPParking.Core.Models.Enums.LaneDirection.In, InputReader = 2, OutputRelay = 2 }),
                    new(new HPParking.Core.Models.Entities.Lane { Id = "L3", Name = "Làn Ra 1", Direction = HPParking.Core.Models.Enums.LaneDirection.Out, InputReader = 3, OutputRelay = 3 }),
                    new(new HPParking.Core.Models.Entities.Lane { Id = "L4", Name = "Làn Ra 2", Direction = HPParking.Core.Models.Enums.LaneDirection.Out, InputReader = 4, OutputRelay = 4 })
                };

                // Act
                form.ApplyLaneContexts(contexts);

                // Assert
                form.GetSlotTitleText(0).Should().Be("LÀN VÀO 1 [F1]");
                form.GetSlotTitleText(1).Should().Be("LÀN VÀO 2 [F2]");
                form.GetSlotTitleText(2).Should().Be("LÀN RA 1 [F3]");
                form.GetSlotTitleText(3).Should().Be("LÀN RA 2 [F4]");
            });
        }

        [Fact]
        public void TryOpenBarrierByHotkey_WhenValidSlot_ShouldCallControllerOpenBarrier_WithDoorIdMatchingRelay()
        {
            RunOnStaThread(() =>
            {
                using var form = CreateFrmMain();
                var fakeController = new FakeControllerService();
                var lane1 = new HPParking.Core.Models.Entities.Lane { Id = "L1", Name = "Làn Vào", Direction = HPParking.Core.Models.Enums.LaneDirection.In, InputReader = 1, OutputRelay = 1 };
                var lane2 = new HPParking.Core.Models.Entities.Lane { Id = "L2", Name = "Làn Ra", Direction = HPParking.Core.Models.Enums.LaneDirection.Out, InputReader = 2, OutputRelay = 2 };

                var context1 = new HPParking.Models.LaneRuntimeContext(lane1) { Controller = fakeController };
                var context2 = new HPParking.Models.LaneRuntimeContext(lane2) { Controller = fakeController };

                form.ApplyLaneContexts(new() { context1, context2 });

                // Act - slot 0 (F1)
                bool result1 = form.TryOpenBarrierByHotkey(0);

                // Act - slot 1 (F2)
                bool result2 = form.TryOpenBarrierByHotkey(1);

                // Assert
                result1.Should().BeTrue();
                result2.Should().BeTrue();
                fakeController.OpenedDoorIds.Should().Equal(1, 2);
            });
        }

        [Fact]
        public void TryOpenBarrierByHotkey_WhenSlotIndexExceedsLaneCount_ShouldReturnFalse_WithoutCallingController()
        {
            RunOnStaThread(() =>
            {
                using var form = CreateFrmMain();
                var fakeController = new FakeControllerService();
                var lane1 = new HPParking.Core.Models.Entities.Lane { Id = "L1", Name = "Làn Vào", Direction = HPParking.Core.Models.Enums.LaneDirection.In, InputReader = 1, OutputRelay = 1 };
                var context1 = new HPParking.Models.LaneRuntimeContext(lane1) { Controller = fakeController };

                form.ApplyLaneContexts(new() { context1 });

                // Act - slot 1 (F2), slot 2 (F3), slot 3 (F4) - Chỉ có 1 làn tại slot 0
                bool resultF2 = form.TryOpenBarrierByHotkey(1);
                bool resultF3 = form.TryOpenBarrierByHotkey(2);
                bool resultF4 = form.TryOpenBarrierByHotkey(3);
                bool resultInvalid = form.TryOpenBarrierByHotkey(-1);

                // Assert
                resultF2.Should().BeFalse();
                resultF3.Should().BeFalse();
                resultF4.Should().BeFalse();
                resultInvalid.Should().BeFalse();
                fakeController.OpenedDoorIds.Should().BeEmpty();
            });
        }

        [Fact]
        public void TryOpenBarrierByHotkey_WhenFormConfigurationLocked_ShouldReturnFalse_WithoutCallingController()
        {
            RunOnStaThread(() =>
            {
                using var form = CreateFrmMain();
                var fakeController = new FakeControllerService();
                var lane1 = new HPParking.Core.Models.Entities.Lane { Id = "L1", Name = "Làn Vào", Direction = HPParking.Core.Models.Enums.LaneDirection.In, InputReader = 1, OutputRelay = 1 };
                var context1 = new HPParking.Models.LaneRuntimeContext(lane1) { Controller = fakeController };

                form.ApplyLaneContexts(new() { context1 });
                form.SetFormConfigurationLocked(true);

                // Act
                bool result = form.TryOpenBarrierByHotkey(0);

                // Assert
                result.Should().BeFalse();
                fakeController.OpenedDoorIds.Should().BeEmpty();
            });
        }

        [Fact]
        public void TryOpenBarrierByHotkey_WhenOpenBarrierFails_ShouldLogAndInvokeFailureCallback_WithContext()
        {
            RunOnStaThread(() =>
            {
                using var form = CreateFrmMain();
                var fakeController = new FakeControllerService
                {
                    ShouldSucceed = false,
                    Config = new HPParking.Services.Controller.ControllerConfig { IP = "192.168.1.201" }
                };
                var lane1 = new HPParking.Core.Models.Entities.Lane { Id = "L1", Name = "Làn Vào", Direction = HPParking.Core.Models.Enums.LaneDirection.In, InputReader = 1, OutputRelay = 3 };
                var context1 = new HPParking.Models.LaneRuntimeContext(lane1) { Controller = fakeController };

                form.ApplyLaneContexts(new() { context1 });

                HPParking.Models.LaneRuntimeContext? failedContext = null;
                bool callbackInvoked = false;
                using var consoleWriter = new System.IO.StringWriter();
                var originalOut = System.Console.Out;
                System.Console.SetOut(consoleWriter);

                try
                {
                    // Act
                    bool result = form.TryOpenBarrierByHotkey(0, onBarrierOpenFailed: ctx =>
                    {
                        failedContext = ctx;
                        callbackInvoked = true;
                        return false;
                    });

                    // Assert
                    result.Should().BeFalse();
                    callbackInvoked.Should().BeTrue();
                    failedContext.Should().BeSameAs(context1);
                    fakeController.OpenedDoorIds.Should().Equal(3);
                    consoleWriter.ToString().Should().Contain("[Hotkey F1]")
                        .And.Contain("Làn Vào")
                        .And.Contain("Relay=3")
                        .And.Contain("192.168.1.201");
                }
                finally
                {
                    System.Console.SetOut(originalOut);
                }
            });
        }

        private class FakeControllerService : HPParking.Services.Controller.ControllerService
        {
            public System.Collections.Generic.List<int> OpenedDoorIds { get; } = new();
            public bool ShouldSucceed { get; set; } = true;

            public override bool OpenBarrier(int doorId, int seconds = 1)
            {
                OpenedDoorIds.Add(doorId);
                return ShouldSucceed;
            }
        }

        [Fact]
        public void TryOpenBarrierByHotkey_WhenSuccessful_ShouldOpenBarrierAndNotRecordAuditLog()
        {
            RunOnStaThread(() =>
            {
                var auditRepo = Substitute.For<HPParking.Core.Interfaces.IRepository<AuditLog>>();
                using var form = CreateFrmMain(auditRepo);
                var fakeController = new FakeControllerService { ShouldSucceed = true };
                var lane1 = new Lane { Id = "L1-AUDIT", Name = "Làn Vào Audit", Direction = LaneDirection.In, InputReader = 1, OutputRelay = 1 };
                var context1 = new HPParking.Models.LaneRuntimeContext(lane1) { Controller = fakeController };

                form.ApplyLaneContexts(new() { context1 });

                // Act - Nhấn phím nóng F1 (slot 0)
                bool result = form.TryOpenBarrierByHotkey(0);

                // Assert
                result.Should().BeTrue();
                fakeController.OpenedDoorIds.Should().Contain(1);
                // Phím nóng mở barrier tại bốt trạm là thao tác vận hành phần cứng trực tiếp, không ghi nhận AuditLog
                auditRepo.DidNotReceiveWithAnyArgs().AddAsync(Arg.Any<AuditLog>());
            });
        }

        [Fact]
        public void TryOpenBarrierByHotkey_WhenHardwareFails_ShouldReturnFalseAndNotRecordAuditLog()
        {
            RunOnStaThread(() =>
            {
                var auditRepo = Substitute.For<HPParking.Core.Interfaces.IRepository<AuditLog>>();
                using var form = CreateFrmMain(auditRepo);
                var fakeController = new FakeControllerService { ShouldSucceed = false };
                var lane1 = new Lane { Id = "L2-FAIL", Name = "Làn Ra Fail", Direction = LaneDirection.Out, InputReader = 2, OutputRelay = 2 };
                var context1 = new HPParking.Models.LaneRuntimeContext(lane1) { Controller = fakeController };

                form.ApplyLaneContexts(new() { context1 });

                // Act - Nhấn phím nóng F1 (slot 0) nhưng phần cứng fail
                bool result = form.TryOpenBarrierByHotkey(0, onBarrierOpenFailed: _ => false);

                // Assert
                result.Should().BeFalse();
                // Khi phần cứng fail cũng không ghi AuditLog
                auditRepo.DidNotReceiveWithAnyArgs().AddAsync(Arg.Any<AuditLog>());
            });
        }

        [Fact]
        public void ApplyLaneContexts_WhenLaneHas2Cameras_ShouldSetBothCamerasVisible_AndResetColumnSpanToOne()
        {
            RunOnStaThread(() =>
            {
                using var form = CreateFrmMain();
                var lane1 = new Lane
                {
                    Id = "L1",
                    Name = "Làn Vào 1",
                    Direction = LaneDirection.In,
                    TargetType = LaneTargetType.Vehicle,
                    UseOverviewCam = true,
                    UsePlateCam = true,
                    OverviewCameraDeviceId = "cam-ov-1",
                    PlateCameraDeviceId = "cam-pl-1"
                };

                var context1 = new HPParking.Models.LaneRuntimeContext(lane1);

                // Act
                form.ApplyLaneContexts(new() { context1 });

                // Assert
                var tlpCams = form.GetLaneCamsContainer(0);
                var (pbOverview, pbPlate, pbFace) = form.GetLaneCamBoxes(0);

                // ColumnSpan must be 1 so both cameras render in 50% / 50% columns without overlapping
                tlpCams.ColumnCount.Should().Be(2);
                tlpCams.GetColumnSpan(pbOverview).Should().Be(1);
                tlpCams.GetColumnSpan(pbPlate).Should().Be(1);
                tlpCams.GetCellPosition(pbOverview).Should().Be(new TableLayoutPanelCellPosition(0, 0));
                tlpCams.GetCellPosition(pbPlate).Should().Be(new TableLayoutPanelCellPosition(1, 0));
            });
        }

        [Fact]
        public void UpdateLaneSlotUI_ShouldPrepareWebViewCardPayload_ForVehicleAndPedestrianLanes()
        {
            RunOnStaThread(() =>
            {
                using var form = CreateFrmMain();
                var vehicleLane = new Lane
                {
                    Id = "L1",
                    Name = "Làn Xe Vào",
                    Direction = LaneDirection.In,
                    TargetType = LaneTargetType.Vehicle
                };
                var pedestrianLane = new Lane
                {
                    Id = "L2",
                    Name = "Làn Đi Bộ Ra",
                    Direction = LaneDirection.Out,
                    TargetType = LaneTargetType.Pedestrian
                };

                var ctxVehicle = new HPParking.Models.LaneRuntimeContext(vehicleLane);
                var ctxPedestrian = new HPParking.Models.LaneRuntimeContext(pedestrianLane);

                form.ApplyLaneContexts(new() { ctxVehicle, ctxPedestrian });

                // Kiểm tra khởi tạo WebView2 & trạng thái chờ ban đầu (TabStop = false để không cướp focus phím nóng)
                form.GetLaneInfoWebView(0).Should().NotBeNull();
                form.GetLaneInfoWebView(0).TabStop.Should().BeFalse();
                form.GetLaneInfoWebView(1).Should().NotBeNull();
                form.GetLaneInfoWebView(1).TabStop.Should().BeFalse();
                form.GetLastSlotCardPayload(0)!.IsIdle.Should().BeTrue();
                form.GetLastSlotCardPayload(0)!.TargetType.Should().Be("XE CƠ GIỚI • LÀN VÀO");
                form.GetLastSlotCardPayload(1)!.TargetType.Should().Be("NGƯỜI ĐI BỘ • LÀN RA");

                // Act 1: Cập nhật kết quả xe cơ giới lệch biển số (Thẻ lượt có phí)
                var vehicleResult = new HPParking.Services.Parking.ProcessResult
                {
                    Status = HPParking.Services.Parking.ProcessStatus.PlateMismatch,
                    Vehicle = new Vehicle { PlateNumber = "29A-123.45" },
                    Card = new Card { CardNumber = "0003456789", TargetType = CardTargetType.Visitor },
                    LprResult = new HPParking.Services.LPR.LprResult { Plate = "29A-999.99" },
                    Client = new Client { Name = "Nguyễn Văn A" },
                    DepartmentName = "Phòng Kỹ Thuật",
                    FeeAmount = 10000,
                    IsFeeCollected = true
                };
                form.UpdateLaneSlotUI(0, ctxVehicle, vehicleResult);

                var vPayload = form.GetLastSlotCardPayload(0)!;
                vPayload.IsIdle.Should().BeFalse();
                vPayload.IsEntry.Should().BeTrue();
                vPayload.IsSuccess.Should().BeFalse();
                vPayload.IsPlateMismatch.Should().BeTrue();
                vPayload.StatusText.Should().Be("SAI BIỂN SỐ");
                vPayload.Row1Title.Should().BeEmpty();
                vPayload.Row2Title.Should().Be("Biển số nhận diện:");
                vPayload.Row2Value.Should().Be("29A-999.99");
                vPayload.SubVal1.Should().Be("Nguyễn Văn A");
                vPayload.SubTitle2.Should().Be("Mã thẻ:");
                vPayload.CardCodeText.Should().Be("0003456789");
                vPayload.FeeHighlight.Should().Be("10.000 VNĐ");
                vPayload.TimeIn.Should().NotBe("---");
                vPayload.TimeOut.Should().Be("---");
                vPayload.ImgTitle1.Should().Be("Ảnh biển số");
                vPayload.ImgTitle2.Should().Be("Ảnh toàn cảnh");

                // Act 2: Cập nhật kết quả người đi bộ hợp lệ ở làn ra (Thẻ tháng miễn phí)
                var pedResult = new HPParking.Services.Parking.ProcessResult
                {
                    Status = HPParking.Services.Parking.ProcessStatus.Success,
                    Card = new Card { CardNumber = "1234567890", TargetType = CardTargetType.Monthly },
                    Client = new Client { Name = "Trần Thị B", Code = "NV009", Type = ClientType.Employee },
                    DepartmentName = "Phòng Nhân Sự"
                };
                form.UpdateLaneSlotUI(1, ctxPedestrian, pedResult);

                var pPayload = form.GetLastSlotCardPayload(1)!;
                pPayload.IsIdle.Should().BeFalse();
                pPayload.IsEntry.Should().BeFalse();
                pPayload.IsSuccess.Should().BeTrue();
                pPayload.StatusText.Should().Be("HỢP LỆ");
                pPayload.Row1Title.Should().Be("Họ và tên:");
                pPayload.Row1Value.Should().Be("Trần Thị B");
                pPayload.Row2Title.Should().Be("CCCD / Mã:");
                pPayload.Row2Value.Should().Be("NV009");
                vPayload.SubTitle2.Should().Be("Mã thẻ:");
                pPayload.SubVal1.Should().Be("Nhân viên");
                pPayload.SubTitle2.Should().Be("Mã thẻ:");
                pPayload.CardCodeText.Should().Be("1234567890");
                pPayload.FeeHighlight.Should().Be("Miễn phí");
                pPayload.TimeIn.Should().Be("---");
                pPayload.TimeOut.Should().NotBe("---");
            });
        }

        private static FrmMain CreateFrmMain(HPParking.Core.Interfaces.IRepository<AuditLog>? auditRepo = null)
        {
            var laneRepo = Substitute.For<HPParking.Core.Interfaces.IRepository<Lane>>();
            var gateRepo = Substitute.For<HPParking.Core.Interfaces.IRepository<Gate>>();
            var deviceRepo = Substitute.For<HPParking.Core.Interfaces.IRepository<Device>>();
            var licenseRepo = Substitute.For<HPParking.Core.Interfaces.IRepository<LicenseInfo>>();
            var licenseManager = new HPParking.Services.License.LicenseManager(licenseRepo);
            var workflowService = Substitute.For<HPParking.Interfaces.IParkingWorkflowService>();
            var serverHealthService = Substitute.For<HPParking.Interfaces.IServerHealthService>();
            var auditLogRepo = auditRepo ?? Substitute.For<HPParking.Core.Interfaces.IRepository<AuditLog>>();

            return new FrmMain(laneRepo, gateRepo, deviceRepo, licenseManager, workflowService, serverHealthService, auditLogRepo);
        }

        private static void RunOnStaThread(System.Action action)
        {
            System.Exception? threadEx = null;
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    action();
                }
                catch (System.Exception ex)
                {
                    threadEx = ex;
                }
            });

            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (threadEx != null)
            {
                throw new System.Exception("STA Thread Exception", threadEx);
            }
        }
    }
}
