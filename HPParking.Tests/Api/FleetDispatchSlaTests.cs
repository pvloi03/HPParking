using FluentAssertions;
using HPParking.Api.Services.Background;
using HPParking.Api.Services.Implementations;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Interfaces;
using HPParking.Models;
using HPParking.Services.Controller;
using HPParking.Services.LPR;
using HPParking.Services.Parking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HPParking.Tests.Api
{
    public class FleetDispatchSlaTests
    {
        // ---------------------------------------------------------------------------------
        // Group 1: FleetDispatchService & MapToDtoAsync (Lưu vết SLA vi phạm sau khi hoàn thành)
        // ---------------------------------------------------------------------------------

        [Fact]
        public async Task GetTripByIdAsync_WhenTripCompleted_AndHadOverdueCheckpoint_ShouldMarkIsOverdueTrue()
        {
            // Case 1.1: Chuyến xe đã về nhà máy hoàn tất (Status = Completed, NextDeadline = null)
            // nhưng trước đó có 1 chặng bị trễ hạn (SlaOverdue.IsOverdue = true)
            // KẾT QUẢ MONG MUỐN: isOverdue của toàn chuyến PHẢI là true (không bị tẩy trắng).
            
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();

            var now = DateTime.UtcNow;
            var trip = new VehicleDispatchTrip
            {
                Id = "trip-completed-violated",
                VehicleId = "veh-1",
                PlateNumber = "29A-12345",
                CardNumber = "0012345678",
                OriginGateId = "gate-main",
                CurrentGateId = "gate-main",
                Status = TripStatus.Completed,
                StartTime = now.AddHours(-3),
                EndTime = now,
                NextDeadline = null, // Đã kết thúc chuyến
                Checkpoints =
                [
                    new TripCheckpoint
                    {
                        StepIndex = 1,
                        GateId = "gate-main",
                        GateName = "Cổng Chính",
                        Direction = LaneDirection.Out,
                        Timestamp = now.AddHours(-3),
                        SlaOverdue = new SlaOverdueInfo { IsOverdue = false, OverdueSeconds = 0 }
                    },
                    new TripCheckpoint
                    {
                        StepIndex = 1,
                        GateId = "gate-b",
                        GateName = "Cổng B",
                        Direction = LaneDirection.In,
                        Timestamp = now.AddHours(-2).AddMinutes(15), // Trễ 15 phút
                        SlaOverdue = new SlaOverdueInfo { IsOverdue = true, OverdueSeconds = 900 }
                    },
                    new TripCheckpoint
                    {
                        StepIndex = 2,
                        GateId = "gate-b",
                        GateName = "Cổng B",
                        Direction = LaneDirection.Out,
                        Timestamp = now.AddHours(-1),
                        SlaOverdue = new SlaOverdueInfo { IsOverdue = false, OverdueSeconds = 0 }
                    },
                    new TripCheckpoint
                    {
                        StepIndex = 2,
                        GateId = "gate-main",
                        GateName = "Cổng Chính",
                        Direction = LaneDirection.In,
                        Timestamp = now,
                        SlaOverdue = new SlaOverdueInfo { IsOverdue = false, OverdueSeconds = 0 }
                    }
                ]
            };

            tripRepo.GetByIdAsync("trip-completed-violated")
                .Returns(Task.FromResult<VehicleDispatchTrip?>(trip));

            var service = new FleetDispatchService(tripRepo, gateRepo, routeRepo);

            // Act
            var result = await service.GetTripByIdAsync("trip-completed-violated");

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(TripStatus.Completed);
            result.IsOverdue.Should().BeTrue("chuyến xe từng có checkpoint vi phạm SLA thì toàn chuyến phải giữ trạng thái vi phạm");
            result.Checkpoints.Should().HaveCount(4);
            result.Checkpoints[1].SlaOverdue.IsOverdue.Should().BeTrue();
            result.Checkpoints[1].SlaOverdue.OverdueSeconds.Should().Be(900);
            result.Checkpoints[3].SlaOverdue.IsOverdue.Should().BeFalse();
        }

        [Fact]
        public async Task GetTripByIdAsync_WhenTripCompleted_AndAllCheckpointsCompliant_ShouldMarkIsOverdueFalse()
        {
            // Case 1.2: Chuyến xe hoàn thành đúng giờ toàn bộ các chặng
            // KẾT QUẢ MONG MUỐN: isOverdue = false (Đúng quy chuẩn).

            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();

            var now = DateTime.UtcNow;
            var trip = new VehicleDispatchTrip
            {
                Id = "trip-completed-clean",
                VehicleId = "veh-2",
                PlateNumber = "29A-88888",
                CardNumber = "0088888888",
                OriginGateId = "gate-main",
                CurrentGateId = "gate-main",
                Status = TripStatus.Completed,
                StartTime = now.AddHours(-2),
                EndTime = now,
                NextDeadline = null,
                Checkpoints =
                [
                    new TripCheckpoint
                    {
                        StepIndex = 1,
                        GateId = "gate-main",
                        GateName = "Cổng Chính",
                        Direction = LaneDirection.Out,
                        Timestamp = now.AddHours(-2),
                        SlaOverdue = new SlaOverdueInfo { IsOverdue = false, OverdueSeconds = 0 }
                    },
                    new TripCheckpoint
                    {
                        StepIndex = 1,
                        GateId = "gate-main",
                        GateName = "Cổng Chính",
                        Direction = LaneDirection.In,
                        Timestamp = now,
                        SlaOverdue = new SlaOverdueInfo { IsOverdue = false, OverdueSeconds = 0 }
                    }
                ]
            };

            tripRepo.GetByIdAsync("trip-completed-clean")
                .Returns(Task.FromResult<VehicleDispatchTrip?>(trip));

            var service = new FleetDispatchService(tripRepo, gateRepo, routeRepo);

            // Act
            var result = await service.GetTripByIdAsync("trip-completed-clean");

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(TripStatus.Completed);
            result.IsOverdue.Should().BeFalse("chuyến xe không có checkpoint nào vi phạm thì isOverdue phải là false");
        }

        [Fact]
        public async Task GetTripByIdAsync_WhenTripInTransit_AndCurrentlyOverdue_ShouldMarkIsOverdueTrue()
        {
            // Case 1.3: Chuyến xe đang chạy trên đường và đã quá hạn SLA NextDeadline
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();

            var now = DateTime.UtcNow;
            var trip = new VehicleDispatchTrip
            {
                Id = "trip-intransit-overdue",
                VehicleId = "veh-3",
                PlateNumber = "29A-33333",
                CardNumber = "0033333333",
                Status = TripStatus.InTransit,
                StartTime = now.AddMinutes(-40),
                NextDeadline = now.AddMinutes(-10), // Đã quá hạn 10 phút
                Checkpoints =
                [
                    new TripCheckpoint
                    {
                        StepIndex = 1,
                        GateId = "gate-main",
                        GateName = "Cổng Chính",
                        Direction = LaneDirection.Out,
                        Timestamp = now.AddMinutes(-40),
                        SlaOverdue = new SlaOverdueInfo { IsOverdue = false, OverdueSeconds = 0 }
                    }
                ]
            };

            tripRepo.GetByIdAsync("trip-intransit-overdue")
                .Returns(Task.FromResult<VehicleDispatchTrip?>(trip));

            var service = new FleetDispatchService(tripRepo, gateRepo, routeRepo);

            // Act
            var result = await service.GetTripByIdAsync("trip-intransit-overdue");

            // Assert
            result.IsOverdue.Should().BeTrue();
            result.RemainingSeconds.Should().BeLessThan(0);
        }

        [Fact]
        public async Task GetTripByIdAsync_WhenTripInTransit_AndWithinSla_ShouldMarkIsOverdueFalse()
        {
            // Case 1.4: Chuyến xe đang chạy trên đường và còn thời hạn
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();

            var now = DateTime.UtcNow;
            var trip = new VehicleDispatchTrip
            {
                Id = "trip-intransit-ontime",
                VehicleId = "veh-4",
                PlateNumber = "29A-44444",
                CardNumber = "0044444444",
                Status = TripStatus.InTransit,
                StartTime = now.AddMinutes(-5),
                NextDeadline = now.AddMinutes(25), // Còn 25 phút
                Checkpoints =
                [
                    new TripCheckpoint
                    {
                        StepIndex = 1,
                        GateId = "gate-main",
                        Direction = LaneDirection.Out,
                        Timestamp = now.AddMinutes(-5),
                        SlaOverdue = new SlaOverdueInfo { IsOverdue = false, OverdueSeconds = 0 }
                    }
                ]
            };

            tripRepo.GetByIdAsync("trip-intransit-ontime")
                .Returns(Task.FromResult<VehicleDispatchTrip?>(trip));

            var service = new FleetDispatchService(tripRepo, gateRepo, routeRepo);

            // Act
            var result = await service.GetTripByIdAsync("trip-intransit-ontime");

            // Assert
            result.IsOverdue.Should().BeFalse();
            result.RemainingSeconds.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetTripByIdAsync_WhenTripWorkingAtGate_AndCurrentlyOverdue_ShouldMarkIsOverdueTrue()
        {
            // Case 1.5: Chuyến xe đang dừng đỗ làm việc tại cổng nhưng quá hạn MaxStay
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();

            var now = DateTime.UtcNow;
            var trip = new VehicleDispatchTrip
            {
                Id = "trip-working-overdue",
                VehicleId = "veh-5",
                PlateNumber = "29A-55555",
                CardNumber = "0055555555",
                Status = TripStatus.WorkingAtGate,
                StartTime = now.AddMinutes(-60),
                NextDeadline = now.AddMinutes(-5), // Đã dừng quá hạn 5 phút
                Checkpoints = []
            };

            tripRepo.GetByIdAsync("trip-working-overdue")
                .Returns(Task.FromResult<VehicleDispatchTrip?>(trip));

            var service = new FleetDispatchService(tripRepo, gateRepo, routeRepo);

            // Act
            var result = await service.GetTripByIdAsync("trip-working-overdue");

            // Assert
            result.IsOverdue.Should().BeTrue();
            result.RemainingSeconds.Should().BeLessThan(0);
        }

        [Fact]
        public async Task GetTripByIdAsync_WhenTripWorkingAtGate_AndWithinSla_ShouldMarkIsOverdueFalse()
        {
            // Case 1.6: Chuyến xe đang dừng làm việc tại cổng và còn trong thời hạn MaxStay
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();

            var now = DateTime.UtcNow;
            var trip = new VehicleDispatchTrip
            {
                Id = "trip-working-ontime",
                VehicleId = "veh-6",
                PlateNumber = "29A-66666",
                CardNumber = "0066666666",
                Status = TripStatus.WorkingAtGate,
                StartTime = now.AddMinutes(-10),
                NextDeadline = now.AddMinutes(20), // Còn 20 phút
                Checkpoints = []
            };

            tripRepo.GetByIdAsync("trip-working-ontime")
                .Returns(Task.FromResult<VehicleDispatchTrip?>(trip));

            var service = new FleetDispatchService(tripRepo, gateRepo, routeRepo);

            // Act
            var result = await service.GetTripByIdAsync("trip-working-ontime");

            // Assert
            result.IsOverdue.Should().BeFalse();
            result.RemainingSeconds.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetTripByIdAsync_WhenCheckpointsHaveNullSlaOverdue_ShouldHandleGracefullyWithoutException()
        {
            // Case 1.7: Tương thích ngược với các bản ghi Checkpoint cũ trong DB chưa có object SlaOverdue (null)
            // KẾT QUẢ MONG MUỐN: Không ném NullReferenceException, tự động map mặc định IsOverdue = false, OverdueSeconds = 0.
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();

            var now = DateTime.UtcNow;
            var trip = new VehicleDispatchTrip
            {
                Id = "trip-legacy-null-sla",
                VehicleId = "veh-legacy",
                PlateNumber = "29A-77777",
                CardNumber = "0077777777",
                Status = TripStatus.Completed,
                StartTime = now.AddHours(-1),
                EndTime = now,
                NextDeadline = null,
                Checkpoints =
                [
                    new TripCheckpoint
                    {
                        StepIndex = 1,
                        GateId = "gate-main",
                        Direction = LaneDirection.Out,
                        Timestamp = now.AddHours(-1),
                        SlaOverdue = null! // Dữ liệu cũ null
                    },
                    new TripCheckpoint
                    {
                        StepIndex = 1,
                        GateId = "gate-main",
                        Direction = LaneDirection.In,
                        Timestamp = now,
                        SlaOverdue = null! // Dữ liệu cũ null
                    }
                ]
            };

            tripRepo.GetByIdAsync("trip-legacy-null-sla")
                .Returns(Task.FromResult<VehicleDispatchTrip?>(trip));

            var service = new FleetDispatchService(tripRepo, gateRepo, routeRepo);

            // Act
            var result = await service.GetTripByIdAsync("trip-legacy-null-sla");

            // Assert
            result.Should().NotBeNull();
            result.IsOverdue.Should().BeFalse();
            result.Checkpoints.Should().HaveCount(2);
            result.Checkpoints[0].SlaOverdue.Should().NotBeNull();
            result.Checkpoints[0].SlaOverdue.IsOverdue.Should().BeFalse();
            result.Checkpoints[0].SlaOverdue.OverdueSeconds.Should().Be(0);
            result.Checkpoints[1].SlaOverdue.Should().NotBeNull();
            result.Checkpoints[1].SlaOverdue.IsOverdue.Should().BeFalse();
            result.Checkpoints[1].SlaOverdue.OverdueSeconds.Should().Be(0);
        }

        [Fact]
        public async Task GetTripByIdAsync_WhenTripInTransit_AndNextDeadlineIsNull_ShouldMarkIsOverdueFalse()
        {
            // Case 1.8: Chuyến xe InTransit nhưng NextDeadline bị null (tuyến tự do không thiết lập hạn chót)
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();

            var now = DateTime.UtcNow;
            var trip = new VehicleDispatchTrip
            {
                Id = "trip-no-deadline",
                VehicleId = "veh-8",
                PlateNumber = "29A-88888",
                Status = TripStatus.InTransit,
                StartTime = now.AddMinutes(-10),
                NextDeadline = null,
                Checkpoints = []
            };

            tripRepo.GetByIdAsync("trip-no-deadline")
                .Returns(Task.FromResult<VehicleDispatchTrip?>(trip));

            var service = new FleetDispatchService(tripRepo, gateRepo, routeRepo);

            // Act
            var result = await service.GetTripByIdAsync("trip-no-deadline");

            // Assert
            result.IsOverdue.Should().BeFalse();
            result.RemainingSeconds.Should().Be(0);
        }

        [Fact]
        public async Task GetTripByIdAsync_WhenMultiStepTripHasOnlyOneOverdueLeg_ShouldKeepLegOverdueAndTripOverdue()
        {
            // Case 1.9: Tuyến đa chặng (Chặng 1 đúng giờ, Chặng 2 quá hạn +1200s, Chặng 3 đúng giờ)
            // KẾT QUẢ MONG MUỐN: Checkpoint chặng 2 giữ đúng số giây vi phạm, toàn chuyến ghi nhận IsOverdue = true
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();

            var now = DateTime.UtcNow;
            var trip = new VehicleDispatchTrip
            {
                Id = "trip-multi-step",
                VehicleId = "veh-9",
                PlateNumber = "29A-99999",
                Status = TripStatus.Completed,
                StartTime = now.AddHours(-4),
                EndTime = now,
                NextDeadline = null,
                Checkpoints =
                [
                    new TripCheckpoint
                    {
                        StepIndex = 1,
                        GateName = "Cổng Nhà Máy",
                        Direction = LaneDirection.Out,
                        Timestamp = now.AddHours(-4),
                        SlaOverdue = new SlaOverdueInfo { IsOverdue = false, OverdueSeconds = 0 }
                    },
                    new TripCheckpoint
                    {
                        StepIndex = 1,
                        GateName = "Kho Trung Chuyển A",
                        Direction = LaneDirection.In,
                        Timestamp = now.AddHours(-3),
                        SlaOverdue = new SlaOverdueInfo { IsOverdue = false, OverdueSeconds = 0 }
                    },
                    new TripCheckpoint
                    {
                        StepIndex = 2,
                        GateName = "Kho Trung Chuyển A",
                        Direction = LaneDirection.Out,
                        Timestamp = now.AddHours(-2).AddMinutes(-30),
                        SlaOverdue = new SlaOverdueInfo { IsOverdue = false, OverdueSeconds = 0 }
                    },
                    new TripCheckpoint
                    {
                        StepIndex = 2,
                        GateName = "Cảng B",
                        Direction = LaneDirection.In,
                        Timestamp = now.AddHours(-1), // Quá hạn 1200 giây
                        SlaOverdue = new SlaOverdueInfo { IsOverdue = true, OverdueSeconds = 1200 }
                    },
                    new TripCheckpoint
                    {
                        StepIndex = 3,
                        GateName = "Cảng B",
                        Direction = LaneDirection.Out,
                        Timestamp = now.AddMinutes(-40),
                        SlaOverdue = new SlaOverdueInfo { IsOverdue = false, OverdueSeconds = 0 }
                    },
                    new TripCheckpoint
                    {
                        StepIndex = 3,
                        GateName = "Cổng Nhà Máy",
                        Direction = LaneDirection.In,
                        Timestamp = now,
                        SlaOverdue = new SlaOverdueInfo { IsOverdue = false, OverdueSeconds = 0 }
                    }
                ]
            };

            tripRepo.GetByIdAsync("trip-multi-step")
                .Returns(Task.FromResult<VehicleDispatchTrip?>(trip));

            var service = new FleetDispatchService(tripRepo, gateRepo, routeRepo);

            // Act
            var result = await service.GetTripByIdAsync("trip-multi-step");

            // Assert
            result.IsOverdue.Should().BeTrue("chỉ cần 1 chặng bị trễ thì toàn chuyến phải ghi nhận vi phạm");
            result.Checkpoints.Should().HaveCount(6);
            result.Checkpoints[3].SlaOverdue.IsOverdue.Should().BeTrue();
            result.Checkpoints[3].SlaOverdue.OverdueSeconds.Should().Be(1200);
            result.Checkpoints[5].SlaOverdue.IsOverdue.Should().BeFalse();
            result.Checkpoints[5].SlaOverdue.OverdueSeconds.Should().Be(0);
        }

        [Fact]
        public async Task GetTripByIdAsync_WhenTripHasDeviationCheckpoint_ShouldMapHasDeviationAndPreserveSlaOverdue()
        {
            // Case 1.10: Chuyến xe có checkpoint lạc tuyến (IsRouteCompliant = false) kèm trạng thái SLA
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();

            var now = DateTime.UtcNow;
            var trip = new VehicleDispatchTrip
            {
                Id = "trip-deviation-sla",
                VehicleId = "veh-dev",
                PlateNumber = "29A-11111",
                Status = TripStatus.WorkingAtGate,
                StartTime = now.AddHours(-1),
                NextDeadline = now.AddMinutes(15),
                Checkpoints =
                [
                    new TripCheckpoint
                    {
                        StepIndex = 1,
                        GateName = "Cổng Xuất Phát",
                        Direction = LaneDirection.Out,
                        Timestamp = now.AddHours(-1),
                        IsRouteCompliant = true,
                        SlaOverdue = new SlaOverdueInfo { IsOverdue = false, OverdueSeconds = 0 }
                    },
                    new TripCheckpoint
                    {
                        StepIndex = 1,
                        GateName = "Cổng Sai Lộ Trình",
                        Direction = LaneDirection.In,
                        Timestamp = now.AddMinutes(-20),
                        IsRouteCompliant = false, // Lạc tuyến
                        SlaOverdue = new SlaOverdueInfo { IsOverdue = true, OverdueSeconds = 300 }
                    }
                ]
            };

            tripRepo.GetByIdAsync("trip-deviation-sla")
                .Returns(Task.FromResult<VehicleDispatchTrip?>(trip));

            var service = new FleetDispatchService(tripRepo, gateRepo, routeRepo);

            // Act
            var result = await service.GetTripByIdAsync("trip-deviation-sla");

            // Assert
            result.IsOverdue.Should().BeTrue();
            result.Checkpoints[1].IsRouteCompliant.Should().BeFalse();
            result.Checkpoints[1].SlaOverdue.IsOverdue.Should().BeTrue();
            result.Checkpoints[1].SlaOverdue.OverdueSeconds.Should().Be(300);
        }

        [Fact]
        public async Task GetActiveTripsAsync_ShouldReturnActiveTripsWithAccurateSlaOverdueCalculation()
        {
            // Case 1.11: Danh sách chuyến xe đang hoạt động (GetActiveTripsAsync)
            // Có 2 chuyến: 1 chuyến đang đúng giờ, 1 chuyến đang quá hạn
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();

            var now = DateTime.UtcNow;
            var tripOnTime = new VehicleDispatchTrip
            {
                Id = "active-ontime",
                PlateNumber = "29A-AAA",
                Status = TripStatus.InTransit,
                StartTime = now.AddMinutes(-10),
                NextDeadline = now.AddMinutes(20), // Còn 20p
                Checkpoints = []
            };
            var tripOverdue = new VehicleDispatchTrip
            {
                Id = "active-overdue",
                PlateNumber = "29A-BBB",
                Status = TripStatus.InTransit,
                StartTime = now.AddMinutes(-50),
                NextDeadline = now.AddMinutes(-10), // Trễ 10p
                Checkpoints = []
            };

            tripRepo.FindAsync(
                Arg.Any<FilterDefinition<VehicleDispatchTrip>>(),
                Arg.Any<SortDefinition<VehicleDispatchTrip>?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<VehicleDispatchTrip>>(new List<VehicleDispatchTrip> { tripOverdue, tripOnTime }));

            var service = new FleetDispatchService(tripRepo, gateRepo, routeRepo);

            // Act
            var results = await service.GetActiveTripsAsync();

            // Assert
            results.Should().HaveCount(2);
            var overdueResult = results.First(r => r.Id == "active-overdue");
            var onTimeResult = results.First(r => r.Id == "active-ontime");

            overdueResult.IsOverdue.Should().BeTrue();
            overdueResult.RemainingSeconds.Should().BeLessThan(0);

            onTimeResult.IsOverdue.Should().BeFalse();
            onTimeResult.RemainingSeconds.Should().BeGreaterThan(0);
        }

        // ---------------------------------------------------------------------------------
        // Group 2: ParkingWorkflowService - Checkpoint SlaOverdue calculation
        // ---------------------------------------------------------------------------------

        [Fact]
        public async Task ProcessExitAsync_WhenStartingNewSharedVehicleTrip_CheckpointShouldHaveIsOverdueFalse()
        {
            // Case 2.1: Quẹt RA bắt đầu chuyến đi mới -> Checkpoint đầu tiên không quá hạn
            var clientRepo = Substitute.For<IRepository<Client>>();
            var sessionRepo = Substitute.For<IRepository<ParkingSession>>();
            var lprService = Substitute.For<ILprService>();
            var imageStorage = Substitute.For<IImageStorageService>();
            var deptRepo = Substitute.For<IRepository<Department>>();
            var contractorRepo = Substitute.For<IRepository<Contractor>>();
            var companyRepo = Substitute.For<IRepository<Company>>();
            var vehicleRepo = Substitute.For<IRepository<Vehicle>>();
            var cardRepo = Substitute.For<IRepository<Card>>();
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();

            var vehicle = new Vehicle
            {
                Id = "veh-shared",
                PlateNumber = "35B-26337",
                IsShared = true,
                IsActive = true
            };
            var vehicleCard = new Card
            {
                Id = "card-1",
                CardNumber = "0013181773",
                TargetType = CardTargetType.Vehicle,
                VehicleId = vehicle.Id,
                Status = CardStatus.InUse
            };

            cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Card?>(vehicleCard));

            vehicleRepo.GetByIdAsync(vehicle.Id).Returns(Task.FromResult<Vehicle?>(vehicle));

            tripRepo.FindOneAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<VehicleDispatchTrip?>(null)); // Không có chuyến đang chạy

            var gate = new Gate { Id = "gate-1", Name = "Cổng Chính Nhà Máy", Code = "GATE01" };
            gateRepo.GetByIdAsync("gate-1").Returns(Task.FromResult<Gate?>(gate));

            var workflowService = new ParkingWorkflowService(
                clientRepo, sessionRepo, lprService, imageStorage, deptRepo, contractorRepo, companyRepo,
                vehicleRepo, cardRepo, tripRepo, routeRepo, gateRepo);

            var lane = new Lane { Id = "lane-out", GateId = "gate-1", Direction = LaneDirection.Out, Name = "Làn Ra" };
            var context = new LaneRuntimeContext(lane);

            var data = new RealtimeLog
            {
                CardNo = "0013181773",
                Time = DateTime.UtcNow
            };

            VehicleDispatchTrip? capturedTrip = null;
            await tripRepo.AddAsync(Arg.Do<VehicleDispatchTrip>(t => capturedTrip = t));

            // Act
            var result = await workflowService.ProcessExitAsync(
                context, data, "C:\\Images", onBarrierOpenFailed: _ => true,
                onManualPlateInput: (_, _) => Task.FromResult<string?>("35B-26337"));

            // Assert
            result.Status.Should().Be(ProcessStatus.Success);
            capturedTrip.Should().NotBeNull();
            capturedTrip!.Checkpoints.Should().HaveCount(1);
            capturedTrip.Checkpoints[0].SlaOverdue.Should().NotBeNull();
            capturedTrip.Checkpoints[0].SlaOverdue.IsOverdue.Should().BeFalse();
            capturedTrip.Checkpoints[0].SlaOverdue.OverdueSeconds.Should().Be(0);
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenSwipeInOverdue_CheckpointShouldRecordIsOverdueTrueAndSeconds()
        {
            // Case 2.2: Xe đang di chuyển trên đường và quẹt VÀO cổng trung gian bị trễ 200 giây
            var clientRepo = Substitute.For<IRepository<Client>>();
            var sessionRepo = Substitute.For<IRepository<ParkingSession>>();
            var lprService = Substitute.For<ILprService>();
            var imageStorage = Substitute.For<IImageStorageService>();
            var deptRepo = Substitute.For<IRepository<Department>>();
            var contractorRepo = Substitute.For<IRepository<Contractor>>();
            var companyRepo = Substitute.For<IRepository<Company>>();
            var vehicleRepo = Substitute.For<IRepository<Vehicle>>();
            var cardRepo = Substitute.For<IRepository<Card>>();
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();

            var vehicle = new Vehicle
            {
                Id = "veh-shared",
                PlateNumber = "35B-26337",
                IsShared = true,
                IsActive = true
            };
            var vehicleCard = new Card
            {
                Id = "card-1",
                CardNumber = "0013181773",
                TargetType = CardTargetType.Vehicle,
                VehicleId = vehicle.Id,
                Status = CardStatus.InUse
            };

            cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Card?>(vehicleCard));

            vehicleRepo.GetByIdAsync(vehicle.Id).Returns(Task.FromResult<Vehicle?>(vehicle));

            var now = DateTime.UtcNow;
            var deadline = now.AddSeconds(-200); // Đã hết hạn 200 giây trước

            var existingTrip = new VehicleDispatchTrip
            {
                Id = "active-trip-1",
                VehicleId = vehicle.Id,
                PlateNumber = vehicle.PlateNumber,
                CardNumber = vehicleCard.CardNumber,
                OriginGateId = "gate-1",
                CurrentGateId = "gate-1",
                Status = TripStatus.InTransit,
                StartTime = now.AddMinutes(-30),
                NextDeadline = deadline,
                Checkpoints =
                [
                    new TripCheckpoint
                    {
                        StepIndex = 1,
                        GateId = "gate-1",
                        Direction = LaneDirection.Out,
                        Timestamp = now.AddMinutes(-30),
                        SlaOverdue = new SlaOverdueInfo { IsOverdue = false, OverdueSeconds = 0 }
                    }
                ]
            };

            tripRepo.FindOneAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<VehicleDispatchTrip?>(existingTrip));

            var gate = new Gate { Id = "gate-2", Name = "Cổng Phụ B", Code = "GATE02" };
            gateRepo.GetByIdAsync("gate-2").Returns(Task.FromResult<Gate?>(gate));

            var workflowService = new ParkingWorkflowService(
                clientRepo, sessionRepo, lprService, imageStorage, deptRepo, contractorRepo, companyRepo,
                vehicleRepo, cardRepo, tripRepo, routeRepo, gateRepo);

            var lane = new Lane { Id = "lane-in-2", GateId = "gate-2", Direction = LaneDirection.In, Name = "Làn Vào Cổng 2" };
            var context = new LaneRuntimeContext(lane);

            var data = new RealtimeLog
            {
                CardNo = "0013181773",
                Time = now
            };

            // Act
            var result = await workflowService.ProcessEntryAsync(
                context, data, "C:\\Images", onBarrierOpenFailed: _ => true,
                onManualPlateInput: (_, _) => Task.FromResult<string?>("35B-26337"));

            // Assert
            result.Status.Should().Be(ProcessStatus.Success);
            existingTrip.Checkpoints.Should().HaveCount(2);

            var arrivalCheckpoint = existingTrip.Checkpoints[1];
            arrivalCheckpoint.GateId.Should().Be("gate-2");
            arrivalCheckpoint.Direction.Should().Be(LaneDirection.In);
            arrivalCheckpoint.SlaOverdue.Should().NotBeNull();
            arrivalCheckpoint.SlaOverdue.IsOverdue.Should().BeTrue("quẹt vào sau NextDeadline 200s thì phải bị ghi nhận IsOverdue == true");
            arrivalCheckpoint.SlaOverdue.OverdueSeconds.Should().BeInRange(198, 202);
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenSwipeInOnTime_CheckpointShouldHaveIsOverdueFalse()
        {
            // Case 2.3: Xe quẹt VÀO cổng đến ĐÚNG HẠN (trước NextDeadline 5 phút)
            var clientRepo = Substitute.For<IRepository<Client>>();
            var sessionRepo = Substitute.For<IRepository<ParkingSession>>();
            var lprService = Substitute.For<ILprService>();
            var imageStorage = Substitute.For<IImageStorageService>();
            var deptRepo = Substitute.For<IRepository<Department>>();
            var contractorRepo = Substitute.For<IRepository<Contractor>>();
            var companyRepo = Substitute.For<IRepository<Company>>();
            var vehicleRepo = Substitute.For<IRepository<Vehicle>>();
            var cardRepo = Substitute.For<IRepository<Card>>();
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();

            var vehicle = new Vehicle
            {
                Id = "veh-shared",
                PlateNumber = "35B-26337",
                IsShared = true,
                IsActive = true
            };
            var vehicleCard = new Card
            {
                Id = "card-1",
                CardNumber = "0013181773",
                TargetType = CardTargetType.Vehicle,
                VehicleId = vehicle.Id,
                Status = CardStatus.InUse
            };

            cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Card?>(vehicleCard));

            vehicleRepo.GetByIdAsync(vehicle.Id).Returns(Task.FromResult<Vehicle?>(vehicle));

            var now = DateTime.UtcNow;
            var deadline = now.AddMinutes(5); // Còn 5 phút nữa mới hết hạn

            var existingTrip = new VehicleDispatchTrip
            {
                Id = "active-trip-ontime",
                VehicleId = vehicle.Id,
                PlateNumber = vehicle.PlateNumber,
                CardNumber = vehicleCard.CardNumber,
                OriginGateId = "gate-1",
                CurrentGateId = "gate-1",
                Status = TripStatus.InTransit,
                StartTime = now.AddMinutes(-10),
                NextDeadline = deadline,
                Checkpoints = []
            };

            tripRepo.FindOneAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<VehicleDispatchTrip?>(existingTrip));

            var gate = new Gate { Id = "gate-2", Name = "Cổng Phụ B", Code = "GATE02" };
            gateRepo.GetByIdAsync("gate-2").Returns(Task.FromResult<Gate?>(gate));

            var workflowService = new ParkingWorkflowService(
                clientRepo, sessionRepo, lprService, imageStorage, deptRepo, contractorRepo, companyRepo,
                vehicleRepo, cardRepo, tripRepo, routeRepo, gateRepo);

            var lane = new Lane { Id = "lane-in-2", GateId = "gate-2", Direction = LaneDirection.In, Name = "Làn Vào" };
            var context = new LaneRuntimeContext(lane);

            var data = new RealtimeLog
            {
                CardNo = "0013181773",
                Time = now
            };

            // Act
            var result = await workflowService.ProcessEntryAsync(
                context, data, "C:\\Images", onBarrierOpenFailed: _ => true,
                onManualPlateInput: (_, _) => Task.FromResult<string?>("35B-26337"));

            // Assert
            result.Status.Should().Be(ProcessStatus.Success);
            var arrivalCheckpoint = existingTrip.Checkpoints.Last();
            arrivalCheckpoint.SlaOverdue.Should().NotBeNull();
            arrivalCheckpoint.SlaOverdue.IsOverdue.Should().BeFalse("đến trước NextDeadline thì IsOverdue phải là false");
            arrivalCheckpoint.SlaOverdue.OverdueSeconds.Should().Be(0);
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenSwipeInCompleteTripOverdue_CheckpointShouldCaptureOverdueBeforeDeadlineReset()
        {
            // Case 2.4: Xe quẹt VÀO cổng xuất phát để hoàn tất chuyến đi nhưng bị QUÁ HẠN 180s
            // KẾT QUẢ MONG MUỐN: Checkpoint cuối cùng ghi nhận IsOverdue = true và OverdueSeconds = 180
            // TRƯỚC KHI NextDeadline bị gán thành null!
            var clientRepo = Substitute.For<IRepository<Client>>();
            var sessionRepo = Substitute.For<IRepository<ParkingSession>>();
            var lprService = Substitute.For<ILprService>();
            var imageStorage = Substitute.For<IImageStorageService>();
            var deptRepo = Substitute.For<IRepository<Department>>();
            var contractorRepo = Substitute.For<IRepository<Contractor>>();
            var companyRepo = Substitute.For<IRepository<Company>>();
            var vehicleRepo = Substitute.For<IRepository<Vehicle>>();
            var cardRepo = Substitute.For<IRepository<Card>>();
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();

            var vehicle = new Vehicle
            {
                Id = "veh-shared",
                PlateNumber = "35B-26337",
                IsShared = true,
                IsActive = true
            };
            var vehicleCard = new Card
            {
                Id = "card-1",
                CardNumber = "0013181773",
                TargetType = CardTargetType.Vehicle,
                VehicleId = vehicle.Id,
                Status = CardStatus.InUse
            };

            cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Card?>(vehicleCard));

            vehicleRepo.GetByIdAsync(vehicle.Id).Returns(Task.FromResult<Vehicle?>(vehicle));

            var now = DateTime.UtcNow;
            var deadline = now.AddSeconds(-180); // Quá hạn 180s

            var existingTrip = new VehicleDispatchTrip
            {
                Id = "active-trip-finishing",
                VehicleId = vehicle.Id,
                PlateNumber = vehicle.PlateNumber,
                CardNumber = vehicleCard.CardNumber,
                OriginGateId = "gate-1",
                CurrentGateId = "gate-1",
                CurrentStepIndex = 1,
                Status = TripStatus.InTransit,
                StartTime = now.AddHours(-1),
                NextDeadline = deadline,
                Checkpoints =
                [
                    new TripCheckpoint
                    {
                        StepIndex = 1,
                        GateId = "gate-1",
                        Direction = LaneDirection.Out,
                        Timestamp = now.AddHours(-1),
                        SlaOverdue = new SlaOverdueInfo { IsOverdue = false, OverdueSeconds = 0 }
                    }
                ]
            };

            tripRepo.FindOneAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<VehicleDispatchTrip?>(existingTrip));

            var gate = new Gate { Id = "gate-1", Name = "Cổng Chính Nhà Máy", Code = "GATE01" };
            gateRepo.GetByIdAsync("gate-1").Returns(Task.FromResult<Gate?>(gate));

            var workflowService = new ParkingWorkflowService(
                clientRepo, sessionRepo, lprService, imageStorage, deptRepo, contractorRepo, companyRepo,
                vehicleRepo, cardRepo, tripRepo, routeRepo, gateRepo);

            var lane = new Lane { Id = "lane-in-1", GateId = "gate-1", Direction = LaneDirection.In, Name = "Làn Vào" };
            var context = new LaneRuntimeContext(lane);

            var data = new RealtimeLog
            {
                CardNo = "0013181773",
                Time = now
            };

            // Act
            var result = await workflowService.ProcessEntryAsync(
                context, data, "C:\\Images", onBarrierOpenFailed: _ => true,
                onManualPlateInput: (_, _) => Task.FromResult<string?>("35B-26337"));

            // Assert
            result.Status.Should().Be(ProcessStatus.Success);
            existingTrip.Status.Should().Be(TripStatus.Completed);
            existingTrip.NextDeadline.Should().BeNull("chuyến đi hoàn tất thì NextDeadline được set null");
            existingTrip.Checkpoints.Should().HaveCount(2);

            var finishCheckpoint = existingTrip.Checkpoints.Last();
            finishCheckpoint.SlaOverdue.Should().NotBeNull();
            finishCheckpoint.SlaOverdue.IsOverdue.Should().BeTrue("checkpoint về đích trễ phải được ghi nhận IsOverdue == true");
            finishCheckpoint.SlaOverdue.OverdueSeconds.Should().BeInRange(178, 182);
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenSwipeInCompleteTripOnTime_CheckpointShouldHaveIsOverdueFalseAndStatusCompleted()
        {
            // Case 2.5: Xe quẹt VÀO cổng xuất phát để kết thúc chuyến đi ĐÚNG HẠN
            var clientRepo = Substitute.For<IRepository<Client>>();
            var sessionRepo = Substitute.For<IRepository<ParkingSession>>();
            var lprService = Substitute.For<ILprService>();
            var imageStorage = Substitute.For<IImageStorageService>();
            var deptRepo = Substitute.For<IRepository<Department>>();
            var contractorRepo = Substitute.For<IRepository<Contractor>>();
            var companyRepo = Substitute.For<IRepository<Company>>();
            var vehicleRepo = Substitute.For<IRepository<Vehicle>>();
            var cardRepo = Substitute.For<IRepository<Card>>();
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();

            var vehicle = new Vehicle { Id = "veh-shared", PlateNumber = "35B-26337", IsShared = true, IsActive = true };
            var vehicleCard = new Card { Id = "card-1", CardNumber = "0013181773", TargetType = CardTargetType.Vehicle, VehicleId = vehicle.Id, Status = CardStatus.InUse };

            cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Card?>(vehicleCard));
            vehicleRepo.GetByIdAsync(vehicle.Id).Returns(Task.FromResult<Vehicle?>(vehicle));

            var now = DateTime.UtcNow;
            var deadline = now.AddMinutes(10); // Còn 10 phút mới hết hạn

            var existingTrip = new VehicleDispatchTrip
            {
                Id = "active-trip-finishing-ontime",
                VehicleId = vehicle.Id,
                PlateNumber = vehicle.PlateNumber,
                CardNumber = vehicleCard.CardNumber,
                OriginGateId = "gate-1",
                CurrentGateId = "gate-1",
                CurrentStepIndex = 1,
                Status = TripStatus.InTransit,
                StartTime = now.AddHours(-1),
                NextDeadline = deadline,
                Checkpoints = []
            };

            tripRepo.FindOneAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<VehicleDispatchTrip?>(existingTrip));

            var gate = new Gate { Id = "gate-1", Name = "Cổng Chính Nhà Máy", Code = "GATE01" };
            gateRepo.GetByIdAsync("gate-1").Returns(Task.FromResult<Gate?>(gate));

            var workflowService = new ParkingWorkflowService(
                clientRepo, sessionRepo, lprService, imageStorage, deptRepo, contractorRepo, companyRepo,
                vehicleRepo, cardRepo, tripRepo, routeRepo, gateRepo);

            var lane = new Lane { Id = "lane-in-1", GateId = "gate-1", Direction = LaneDirection.In, Name = "Làn Vào" };
            var context = new LaneRuntimeContext(lane);

            var data = new RealtimeLog { CardNo = "0013181773", Time = now };

            // Act
            var result = await workflowService.ProcessEntryAsync(
                context, data, "C:\\Images", onBarrierOpenFailed: _ => true,
                onManualPlateInput: (_, _) => Task.FromResult<string?>("35B-26337"));

            // Assert
            result.Status.Should().Be(ProcessStatus.Success);
            existingTrip.Status.Should().Be(TripStatus.Completed);
            existingTrip.NextDeadline.Should().BeNull();
            existingTrip.Checkpoints.Should().HaveCount(1);

            var finishCheckpoint = existingTrip.Checkpoints.Last();
            finishCheckpoint.SlaOverdue.IsOverdue.Should().BeFalse("về đích đúng hạn thì IsOverdue phải là false");
            finishCheckpoint.SlaOverdue.OverdueSeconds.Should().Be(0);
        }

        [Fact]
        public async Task ProcessExitAsync_WhenSwipeOutFromIntermediateGateOverdue_CheckpointShouldRecordIsOverdueTrueAndSeconds()
        {
            // Case 2.6: Xe đang dừng đỗ làm việc tại cổng trung gian (WorkingAtGate) nhưng ở quá hạn (quá hạn MaxStay)
            // Khi quẹt RA để tiếp tục hành trình: Checkpoint phải ghi nhận IsOverdue = true và số giây quá hạn dừng đỗ!
            var clientRepo = Substitute.For<IRepository<Client>>();
            var sessionRepo = Substitute.For<IRepository<ParkingSession>>();
            var lprService = Substitute.For<ILprService>();
            var imageStorage = Substitute.For<IImageStorageService>();
            var deptRepo = Substitute.For<IRepository<Department>>();
            var contractorRepo = Substitute.For<IRepository<Contractor>>();
            var companyRepo = Substitute.For<IRepository<Company>>();
            var vehicleRepo = Substitute.For<IRepository<Vehicle>>();
            var cardRepo = Substitute.For<IRepository<Card>>();
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();

            var vehicle = new Vehicle { Id = "veh-shared", PlateNumber = "35B-26337", IsShared = true, IsActive = true };
            var vehicleCard = new Card { Id = "card-1", CardNumber = "0013181773", TargetType = CardTargetType.Vehicle, VehicleId = vehicle.Id, Status = CardStatus.InUse };

            cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Card?>(vehicleCard));
            vehicleRepo.GetByIdAsync(vehicle.Id).Returns(Task.FromResult<Vehicle?>(vehicle));

            var now = DateTime.UtcNow;
            var overdueStayDeadline = now.AddSeconds(-250); // Dừng đỗ quá hạn 250 giây

            var existingTrip = new VehicleDispatchTrip
            {
                Id = "trip-intermediate-stay",
                VehicleId = vehicle.Id,
                PlateNumber = vehicle.PlateNumber,
                CardNumber = vehicleCard.CardNumber,
                OriginGateId = "gate-1",
                CurrentGateId = "gate-2",
                CurrentStepIndex = 1,
                Status = TripStatus.WorkingAtGate,
                StartTime = now.AddHours(-2),
                LastEntryTime = now.AddMinutes(-40),
                NextDeadline = overdueStayDeadline,
                Checkpoints = []
            };

            tripRepo.FindOneAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<VehicleDispatchTrip?>(existingTrip));

            var gate = new Gate { Id = "gate-2", Name = "Cổng Kho Trung Tâm", Code = "GATE02" };
            gateRepo.GetByIdAsync("gate-2").Returns(Task.FromResult<Gate?>(gate));

            var workflowService = new ParkingWorkflowService(
                clientRepo, sessionRepo, lprService, imageStorage, deptRepo, contractorRepo, companyRepo,
                vehicleRepo, cardRepo, tripRepo, routeRepo, gateRepo);

            var lane = new Lane { Id = "lane-out-2", GateId = "gate-2", Direction = LaneDirection.Out, Name = "Làn Ra Cổng 2" };
            var context = new LaneRuntimeContext(lane);

            var data = new RealtimeLog { CardNo = "0013181773", Time = now };

            // Act
            var result = await workflowService.ProcessExitAsync(
                context, data, "C:\\Images", onBarrierOpenFailed: _ => true,
                onManualPlateInput: (_, _) => Task.FromResult<string?>("35B-26337"));

            // Assert
            result.Status.Should().Be(ProcessStatus.Success);
            existingTrip.Status.Should().Be(TripStatus.InTransit, "quẹt ra chuyển từ dừng đỗ sang di chuyển");
            existingTrip.Checkpoints.Should().HaveCount(1);

            var exitCheckpoint = existingTrip.Checkpoints.Last();
            exitCheckpoint.Direction.Should().Be(LaneDirection.Out);
            exitCheckpoint.SlaOverdue.IsOverdue.Should().BeTrue("dừng đỗ quá hạn khi quẹt ra phải ghi nhận IsOverdue == true");
            exitCheckpoint.SlaOverdue.OverdueSeconds.Should().BeInRange(248, 252);
        }

        [Fact]
        public async Task ProcessExitAsync_WhenSwipeOutFromIntermediateGateOnTime_CheckpointShouldHaveIsOverdueFalse()
        {
            // Case 2.7: Xe dừng đỗ làm việc tại cổng trung gian và quẹt RA ĐÚNG HẠN MaxStay
            var clientRepo = Substitute.For<IRepository<Client>>();
            var sessionRepo = Substitute.For<IRepository<ParkingSession>>();
            var lprService = Substitute.For<ILprService>();
            var imageStorage = Substitute.For<IImageStorageService>();
            var deptRepo = Substitute.For<IRepository<Department>>();
            var contractorRepo = Substitute.For<IRepository<Contractor>>();
            var companyRepo = Substitute.For<IRepository<Company>>();
            var vehicleRepo = Substitute.For<IRepository<Vehicle>>();
            var cardRepo = Substitute.For<IRepository<Card>>();
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();

            var vehicle = new Vehicle { Id = "veh-shared", PlateNumber = "35B-26337", IsShared = true, IsActive = true };
            var vehicleCard = new Card { Id = "card-1", CardNumber = "0013181773", TargetType = CardTargetType.Vehicle, VehicleId = vehicle.Id, Status = CardStatus.InUse };

            cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Card?>(vehicleCard));
            vehicleRepo.GetByIdAsync(vehicle.Id).Returns(Task.FromResult<Vehicle?>(vehicle));

            var now = DateTime.UtcNow;
            var stayDeadline = now.AddMinutes(10); // Còn 10 phút

            var existingTrip = new VehicleDispatchTrip
            {
                Id = "trip-intermediate-ontime",
                VehicleId = vehicle.Id,
                PlateNumber = vehicle.PlateNumber,
                CardNumber = vehicleCard.CardNumber,
                OriginGateId = "gate-1",
                CurrentGateId = "gate-2",
                CurrentStepIndex = 1,
                Status = TripStatus.WorkingAtGate,
                StartTime = now.AddHours(-1),
                LastEntryTime = now.AddMinutes(-10),
                NextDeadline = stayDeadline,
                Checkpoints = []
            };

            tripRepo.FindOneAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<VehicleDispatchTrip?>(existingTrip));

            var gate = new Gate { Id = "gate-2", Name = "Cổng Kho Trung Tâm", Code = "GATE02" };
            gateRepo.GetByIdAsync("gate-2").Returns(Task.FromResult<Gate?>(gate));

            var workflowService = new ParkingWorkflowService(
                clientRepo, sessionRepo, lprService, imageStorage, deptRepo, contractorRepo, companyRepo,
                vehicleRepo, cardRepo, tripRepo, routeRepo, gateRepo);

            var lane = new Lane { Id = "lane-out-2", GateId = "gate-2", Direction = LaneDirection.Out, Name = "Làn Ra Cổng 2" };
            var context = new LaneRuntimeContext(lane);

            var data = new RealtimeLog { CardNo = "0013181773", Time = now };

            // Act
            var result = await workflowService.ProcessExitAsync(
                context, data, "C:\\Images", onBarrierOpenFailed: _ => true,
                onManualPlateInput: (_, _) => Task.FromResult<string?>("35B-26337"));

            // Assert
            result.Status.Should().Be(ProcessStatus.Success);
            var exitCheckpoint = existingTrip.Checkpoints.Last();
            exitCheckpoint.SlaOverdue.IsOverdue.Should().BeFalse();
            exitCheckpoint.SlaOverdue.OverdueSeconds.Should().Be(0);
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenSwipeAtUnexpectedGate_ShouldRecordDeviationCheckpointWithAccurateSlaOverdue()
        {
            // Case 2.8: Xe có tuyến quy định đến Cổng 2, nhưng tài xế lái xe lạc đến Cổng 3 và quá hạn 120s
            // KẾT QUẢ MONG MUỐN: Trả về ConfirmRequired (chặn barrier), ghi nhận checkpoint lạc tuyến (IsRouteCompliant = false)
            // kèm theo thông tin SLA quá hạn chính xác!
            var clientRepo = Substitute.For<IRepository<Client>>();
            var sessionRepo = Substitute.For<IRepository<ParkingSession>>();
            var lprService = Substitute.For<ILprService>();
            var imageStorage = Substitute.For<IImageStorageService>();
            var deptRepo = Substitute.For<IRepository<Department>>();
            var contractorRepo = Substitute.For<IRepository<Contractor>>();
            var companyRepo = Substitute.For<IRepository<Company>>();
            var vehicleRepo = Substitute.For<IRepository<Vehicle>>();
            var cardRepo = Substitute.For<IRepository<Card>>();
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();

            var route = new GateRouteConfig
            {
                Id = "route-fixed",
                RouteName = "Tuyến Nhà Máy - Kho 2",
                GateSteps =
                [
                    new RouteGateStep { StepIndex = 1, GateId = "gate-2", GateName = "Cổng Kho 2", MaxTravelMinutes = 15 }
                ]
            };
            routeRepo.GetByIdAsync("route-fixed").Returns(Task.FromResult<GateRouteConfig?>(route));

            var vehicle = new Vehicle
            {
                Id = "veh-shared",
                PlateNumber = "35B-26337",
                IsShared = true,
                IsActive = true,
                AssignedRouteId = "route-fixed"
            };
            var vehicleCard = new Card
            {
                Id = "card-1",
                CardNumber = "0013181773",
                TargetType = CardTargetType.Vehicle,
                VehicleId = vehicle.Id,
                Status = CardStatus.InUse
            };

            cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Card?>(vehicleCard));
            vehicleRepo.GetByIdAsync(vehicle.Id).Returns(Task.FromResult<Vehicle?>(vehicle));

            var now = DateTime.UtcNow;
            var deadline = now.AddSeconds(-120); // Quá hạn 120s

            var existingTrip = new VehicleDispatchTrip
            {
                Id = "trip-deviation-test",
                VehicleId = vehicle.Id,
                PlateNumber = vehicle.PlateNumber,
                CardNumber = vehicleCard.CardNumber,
                OriginGateId = "gate-1",
                CurrentGateId = "gate-1",
                CurrentStepIndex = 1,
                AssignedRouteId = "route-fixed",
                Status = TripStatus.InTransit,
                StartTime = now.AddMinutes(-30),
                NextDeadline = deadline,
                Checkpoints = []
            };

            tripRepo.FindOneAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<VehicleDispatchTrip?>(existingTrip));

            var wrongGate = new Gate { Id = "gate-wrong", Name = "Cổng Bãi Đỗ Phụ", Code = "GATE_WRONG" };
            gateRepo.GetByIdAsync("gate-wrong").Returns(Task.FromResult<Gate?>(wrongGate));

            var workflowService = new ParkingWorkflowService(
                clientRepo, sessionRepo, lprService, imageStorage, deptRepo, contractorRepo, companyRepo,
                vehicleRepo, cardRepo, tripRepo, routeRepo, gateRepo);

            var lane = new Lane { Id = "lane-in-wrong", GateId = "gate-wrong", Direction = LaneDirection.In, Name = "Làn Vào Cổng Phụ" };
            var context = new LaneRuntimeContext(lane);

            var data = new RealtimeLog { CardNo = "0013181773", Time = now };

            // Act
            var result = await workflowService.ProcessEntryAsync(
                context, data, "C:\\Images", onBarrierOpenFailed: _ => true,
                onManualPlateInput: (_, _) => Task.FromResult<string?>("35B-26337"));

            // Assert
            result.Status.Should().Be(ProcessStatus.ConfirmRequired, "quẹt sai cổng theo lộ trình phải yêu cầu xác nhận cảnh báo");
            result.Message.Should().Contain("CẢNH BÁO LẠC TUYẾN");

            existingTrip.Checkpoints.Should().HaveCount(1);
            var devCheckpoint = existingTrip.Checkpoints.Last();
            devCheckpoint.IsRouteCompliant.Should().BeFalse();
            devCheckpoint.SlaOverdue.IsOverdue.Should().BeTrue("dù lạc tuyến nhưng đến trễ vẫn phải ghi nhận IsOverdue == true");
            devCheckpoint.SlaOverdue.OverdueSeconds.Should().BeInRange(118, 122);
        }

        // ---------------------------------------------------------------------------------
        // Group 3: VehicleTransitWatcherService - Background SLA monitoring & Alert rules
        // ---------------------------------------------------------------------------------

        [Fact]
        public async Task CheckOverdueTripsAsync_WhenWorkingAtGateTripOverdue_ShouldSetStatusOverdueStayAndSendAlert()
        {
            // Case 3.1: Xe dừng đỗ quá hạn tại bãi -> Watcher phát hiện chuyển thành OverdueStay và gửi email cảnh báo
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var emailSender = Substitute.For<IEmailSenderService>();
            var logger = Substitute.For<ILogger<VehicleTransitWatcherService>>();

            var now = DateTime.UtcNow;
            var overdueStayTrip = new VehicleDispatchTrip
            {
                Id = "trip-stay-overdue",
                VehicleId = "veh-stay",
                PlateNumber = "30A-77777",
                CardNumber = "0012345678",
                OriginGateId = "gate-1",
                CurrentGateId = "gate-2",
                Status = TripStatus.WorkingAtGate,
                StartTime = now.AddHours(-2),
                LastEntryTime = now.AddMinutes(-50),
                NextDeadline = now.AddMinutes(-20), // Quá hạn dừng đỗ 20 phút
                IsAlertSent = false
            };

            tripRepo.FindAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>())
                .Returns(Task.FromResult<IReadOnlyList<VehicleDispatchTrip>>(new List<VehicleDispatchTrip> { overdueStayTrip }));

            var defaultRoute = new GateRouteConfig
            {
                Id = "def",
                RouteCode = "DEFAULT",
                IsDefault = true,
                AlertEmails = ["admin@company.com"]
            };
            routeRepo.FindOneAsync(Arg.Any<Expression<Func<GateRouteConfig, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<GateRouteConfig?>(defaultRoute));

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IRepository<VehicleDispatchTrip>)).Returns(tripRepo);
            serviceProvider.GetService(typeof(IRepository<GateRouteConfig>)).Returns(routeRepo);
            serviceProvider.GetService(typeof(IRepository<Gate>)).Returns(gateRepo);
            serviceProvider.GetService(typeof(IEmailSenderService)).Returns(emailSender);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(serviceProvider);
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);

            var watcher = new VehicleTransitWatcherService(scopeFactory, logger);

            // Act
            await watcher.CheckOverdueTripsAsync(CancellationToken.None);

            // Assert
            overdueStayTrip.Status.Should().Be(TripStatus.OverdueStay);
            overdueStayTrip.IsAlertSent.Should().BeTrue();
            overdueStayTrip.AlertSentAt.Should().NotBeNull();

            await emailSender.Received(1).SendEmailAsync(
                Arg.Is<IEnumerable<string>>(recipients => recipients.Contains("admin@company.com")),
                Arg.Is<string>(subject => subject.Contains("dừng đỗ quá hạn")),
                Arg.Is<string>(body => body.Contains("QUÁ HẠN DỪNG ĐỖ LÀM VIỆC TẠI CỔNG")),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CheckOverdueTripsAsync_WhenTripAlreadyAlerted_ShouldNotSendDuplicateAlert()
        {
            // Case 3.2: Chuyến xe đã gửi cảnh báo (IsAlertSent = true) -> Watcher không gửi lặp lại
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var emailSender = Substitute.For<IEmailSenderService>();
            var logger = Substitute.For<ILogger<VehicleTransitWatcherService>>();

            // Filter in watcher checks !t.IsAlertSent, so it returns empty list
            tripRepo.FindAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>())
                .Returns(Task.FromResult<IReadOnlyList<VehicleDispatchTrip>>(new List<VehicleDispatchTrip>()));

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IRepository<VehicleDispatchTrip>)).Returns(tripRepo);
            serviceProvider.GetService(typeof(IRepository<GateRouteConfig>)).Returns(routeRepo);
            serviceProvider.GetService(typeof(IRepository<Gate>)).Returns(gateRepo);
            serviceProvider.GetService(typeof(IEmailSenderService)).Returns(emailSender);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(serviceProvider);
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);

            var watcher = new VehicleTransitWatcherService(scopeFactory, logger);

            // Act
            await watcher.CheckOverdueTripsAsync(CancellationToken.None);

            // Assert
            await emailSender.DidNotReceiveWithAnyArgs().SendEmailAsync(
                default!, default!, default!, default, default);
        }

        [Fact]
        public async Task CheckOverdueTripsAsync_WhenTripWithinSla_ShouldNotTriggerAlert()
        {
            // Case 3.3: Chuyến xe đang di chuyển nhưng còn hạn (NextDeadline > now) -> Watcher bỏ qua
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var emailSender = Substitute.For<IEmailSenderService>();
            var logger = Substitute.For<ILogger<VehicleTransitWatcherService>>();

            // NextDeadline > now -> query returns empty
            tripRepo.FindAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>())
                .Returns(Task.FromResult<IReadOnlyList<VehicleDispatchTrip>>(new List<VehicleDispatchTrip>()));

            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IRepository<VehicleDispatchTrip>)).Returns(tripRepo);
            serviceProvider.GetService(typeof(IRepository<GateRouteConfig>)).Returns(routeRepo);
            serviceProvider.GetService(typeof(IRepository<Gate>)).Returns(gateRepo);
            serviceProvider.GetService(typeof(IEmailSenderService)).Returns(emailSender);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(serviceProvider);
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);

            var watcher = new VehicleTransitWatcherService(scopeFactory, logger);

            // Act
            await watcher.CheckOverdueTripsAsync(CancellationToken.None);

            // Assert
            await emailSender.DidNotReceiveWithAnyArgs().SendEmailAsync(
                default!, default!, default!, default, default);
        }

        [Fact]
        public async Task GetTripByIdAsync_WhenCheckpointsHaveDualImages_ShouldMapOverviewImagePathAndPlateImagePath()
        {
            // Arrange
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();

            var now = DateTime.UtcNow;
            var testTrip = new VehicleDispatchTrip
            {
                Id = "60c72b2f9b1d8b2bad000001",
                VehicleId = "veh-1",
                PlateNumber = "29B-999.88",
                OriginGateId = "gate-1",
                Status = TripStatus.InTransit,
                StartTime = now,
                Checkpoints =
                [
                    new TripCheckpoint
                    {
                        StepIndex = 1,
                        GateId = "gate-1",
                        GateName = "Cổng Nhà Máy 1",
                        Direction = LaneDirection.Out,
                        Timestamp = now,
                        OverviewImagePath = "/storage/ImageOut/ToanCanh/2026/09/ov1.jpg",
                        PlateImagePath = "/storage/ImageOut/BienSo/2026/09/pl1.jpg",
                        PlateDetected = "29B-999.88",
                        SlaOverdue = new SlaOverdueInfo { IsOverdue = false }
                    }
                ]
            };

            tripRepo.GetByIdAsync("60c72b2f9b1d8b2bad000001", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<VehicleDispatchTrip?>(testTrip));

            var service = new FleetDispatchService(tripRepo, gateRepo, routeRepo);

            // Act
            var dto = await service.GetTripByIdAsync("60c72b2f9b1d8b2bad000001");

            // Assert
            dto.Should().NotBeNull();
            dto!.Checkpoints.Should().HaveCount(1);
            dto.Checkpoints[0].OverviewImagePath.Should().Be("/storage/ImageOut/ToanCanh/2026/09/ov1.jpg");
            dto.Checkpoints[0].PlateImagePath.Should().Be("/storage/ImageOut/BienSo/2026/09/pl1.jpg");
            dto.Checkpoints[0].PlateDetected.Should().Be("29B-999.88");
        }
    }
}
