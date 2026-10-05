using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using System;
using System.Collections.Generic;
using Xunit;

namespace HPParking.Tests.Core
{
    public class VehicleDispatchTripTests
    {
        [Fact]
        public void Start_ShouldInitializeTripCorrectly()
        {
            // Arrange
            var trip = new VehicleDispatchTrip
            {
                VehicleId = "veh_01",
                PlateNumber = "29A-12345",
                CardId = "card_01",
                CardNumber = "0012345678"
            };
            var now = new DateTime(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc);

            // Act
            trip.Start(originGateId: "gate_A", travelMinutes: 15, now: now);

            // Assert
            Assert.Equal("gate_A", trip.OriginGateId);
            Assert.Equal("gate_A", trip.CurrentGateId);
            Assert.Equal(1, trip.CurrentStepIndex);
            Assert.Equal(TripStatus.InTransit, trip.Status);
            Assert.Equal(now, trip.StartTime);
            Assert.Equal(now, trip.LastExitTime);
            Assert.Null(trip.LastEntryTime);
            Assert.Equal(now.AddMinutes(15), trip.NextDeadline);
            Assert.False(trip.IsAlertSent);
            Assert.Null(trip.AlertSentAt);
            Assert.NotNull(trip.Checkpoints);
        }

        [Fact]
        public void ArriveAtGate_WhenIntermediateStop_ShouldTransitionToWorkingAtGate()
        {
            // Arrange
            var trip = new VehicleDispatchTrip();
            var startTime = new DateTime(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc);
            trip.Start(originGateId: "gate_A", travelMinutes: 15, now: startTime);

            var arriveTime = startTime.AddMinutes(10);

            // Act
            trip.ArriveAtGate(gateId: "gate_B", stayMinutes: 15, isCompleted: false, now: arriveTime);

            // Assert
            Assert.Equal("gate_B", trip.CurrentGateId);
            Assert.Equal(arriveTime, trip.LastEntryTime);
            Assert.Equal(TripStatus.WorkingAtGate, trip.Status);
            Assert.Equal(arriveTime.AddMinutes(15), trip.NextDeadline);
            Assert.Null(trip.EndTime);
            Assert.False(trip.IsAlertSent);
        }

        [Fact]
        public void ArriveAtGate_WhenCompleted_ShouldTransitionToCompleted()
        {
            // Arrange
            var trip = new VehicleDispatchTrip();
            var startTime = new DateTime(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc);
            trip.Start(originGateId: "gate_A", travelMinutes: 15, now: startTime);

            var returnTime = startTime.AddMinutes(40);

            // Act
            trip.ArriveAtGate(gateId: "gate_A", stayMinutes: 0, isCompleted: true, now: returnTime);

            // Assert
            Assert.Equal("gate_A", trip.CurrentGateId);
            Assert.Equal(returnTime, trip.LastEntryTime);
            Assert.Equal(returnTime, trip.EndTime);
            Assert.Equal(TripStatus.Completed, trip.Status);
            Assert.Null(trip.NextDeadline);
        }

        [Fact]
        public void DepartToNextStep_ShouldIncrementStepAndSetInTransit()
        {
            // Arrange
            var trip = new VehicleDispatchTrip();
            var now = new DateTime(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc);
            trip.Start("gate_A", 15, now);
            trip.ArriveAtGate("gate_B", 15, false, now.AddMinutes(10));

            var departTime = now.AddMinutes(20);

            // Act
            trip.DepartToNextStep(currentGateId: "gate_B", nextStepTravelMinutes: 20, now: departTime);

            // Assert
            Assert.Equal("gate_B", trip.CurrentGateId);
            Assert.Equal(2, trip.CurrentStepIndex);
            Assert.Equal(TripStatus.InTransit, trip.Status);
            Assert.Equal(departTime, trip.LastExitTime);
            Assert.Equal(departTime.AddMinutes(20), trip.NextDeadline);
            Assert.False(trip.IsAlertSent);
        }

        [Fact]
        public void IsTripCompletedOnEntry_OnFreeRoam_ShouldOnlyCompleteAtOriginGate()
        {
            // Arrange
            var trip = new VehicleDispatchTrip { OriginGateId = "gate_A", CurrentStepIndex = 1 };
            var defaultRoute = new GateRouteConfig { IsDefault = true, GateSteps = [] };

            // Act & Assert
            // Quẹt vào Cổng B (không phải OriginGateId) -> KHÔNG hoàn thành
            Assert.False(trip.IsTripCompletedOnEntry(defaultRoute, "gate_B"));

            // Quẹt vào lại Cổng A (OriginGateId) -> HOÀN THÀNH
            Assert.True(trip.IsTripCompletedOnEntry(defaultRoute, "gate_A"));
        }

        [Fact]
        public void IsTripCompletedOnEntry_OnFixedRoute_ShouldOnlyCompleteAtOriginGateOnFinalStep()
        {
            // Arrange: Chặng 1: gate_A (Xuất phát & Quay về), Chặng 2: gate_B (Điểm đến)
            var fixedRoute = new GateRouteConfig
            {
                IsDefault = false,
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "gate_A", MaxTravelMinutes = 15 },
                    new() { StepIndex = 2, GateId = "gate_B", MaxTravelMinutes = 20, MaxStayMinutes = 30 }
                }
            };
            var trip = new VehicleDispatchTrip { OriginGateId = "gate_A", CurrentStepIndex = 1 };

            // Step 1: Quẹt vào gate_B chưa phải hoàn thành chuyến -> false
            Assert.False(trip.IsTripCompletedOnEntry(fixedRoute, "gate_B"));

            // Step 2: Quẹt vào lại gate_A (OriginGate) ở chặng kết thúc -> true
            trip.CurrentStepIndex = 2;
            Assert.True(trip.IsTripCompletedOnEntry(fixedRoute, "gate_A"));
        }

        [Fact]
        public void IsTripCompletedOnEntry_OnFixedRoute_WhenGateIsNotOriginGate_ShouldReturnFalse()
        {
            // Arrange: Chặng 1: gate_A (Origin), Chặng 2: gate_B (Dest)
            var fixedRoute = new GateRouteConfig
            {
                IsDefault = false,
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "gate_A", MaxTravelMinutes = 15 },
                    new() { StepIndex = 2, GateId = "gate_B", MaxTravelMinutes = 20 }
                }
            };
            var trip = new VehicleDispatchTrip { OriginGateId = "gate_A", CurrentStepIndex = 2 };

            // Act & Assert: Đã đến chặng 2 nhưng quẹt vào nhầm gate_X (không phải gate_A) -> false
            Assert.False(trip.IsTripCompletedOnEntry(fixedRoute, "gate_X"));
        }

        [Fact]
        public void CheckRouteCompliance_OnEntry_ShouldCompareWithExpectedGate()
        {
            // Arrange: Chặng 1: gate_A (Origin), Chặng 2: gate_B (Dest 1), Chặng 3: gate_C (Dest 2)
            var fixedRoute = new GateRouteConfig
            {
                IsDefault = false,
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "gate_A" },
                    new() { StepIndex = 2, GateId = "gate_B" },
                    new() { StepIndex = 3, GateId = "gate_C" }
                }
            };
            var trip = new VehicleDispatchTrip { CurrentStepIndex = 1 };

            // Quẹt vào đúng gate_B ở Step 1 -> Hợp lệ
            Assert.True(trip.CheckRouteCompliance(fixedRoute, "gate_B", isEntry: true));

            // Quẹt vào nhầm gate_C ở Step 1 -> Bất hợp lệ
            Assert.False(trip.CheckRouteCompliance(fixedRoute, "gate_C", isEntry: true));

            // Chặn tuyệt đối quẹt vào lại gate_A (Origin) ở Step 1 -> Bất hợp lệ
            Assert.False(trip.CheckRouteCompliance(fixedRoute, "gate_A", isEntry: true));
        }

        [Fact]
        public void CheckRouteCompliance_OnEntry_WhenExpectedStepNotFound_ShouldReturnFalse()
        {
            // Arrange: Tuyến có 2 chặng (gate_A -> gate_B -> quay về gate_A)
            var fixedRoute = new GateRouteConfig
            {
                IsDefault = false,
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "gate_A" },
                    new() { StepIndex = 2, GateId = "gate_B" }
                }
            };
            // Trip có CurrentStepIndex = 3 (vượt quá số chặng)
            var trip = new VehicleDispatchTrip { CurrentStepIndex = 3 };

            // Act & Assert: Không tìm thấy chặng tương ứng -> Bất hợp lệ (false)
            Assert.False(trip.CheckRouteCompliance(fixedRoute, "gate_B", isEntry: true));
        }

        [Fact]
        public void CheckRouteCompliance_OnExit_ShouldEnforceOriginAtStartAndMatchCurrentGateLater()
        {
            // Arrange
            var fixedRoute = new GateRouteConfig
            {
                IsDefault = false,
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "gate_A" },
                    new() { StepIndex = 2, GateId = "gate_B" }
                }
            };
            var trip = new VehicleDispatchTrip { CurrentStepIndex = 1, LastEntryTime = null };

            // Lúc xuất phát ban đầu: Quẹt RA tại đúng gate_A (Origin) -> Hợp lệ
            Assert.True(trip.CheckRouteCompliance(fixedRoute, "gate_A", isEntry: false));

            // Xuất phát ban đầu tại cổng khác gate_B -> Bất hợp lệ
            Assert.False(trip.CheckRouteCompliance(fixedRoute, "gate_B", isEntry: false));

            // Khi đã ở cổng trung gian gate_B
            trip.CurrentGateId = "gate_B";
            trip.LastEntryTime = DateTime.UtcNow;

            // Quẹt ra tại đúng cổng gate_B -> Hợp lệ
            Assert.True(trip.CheckRouteCompliance(fixedRoute, "gate_B", isEntry: false));

            // Quẹt ra tại cổng khác gate_C -> Bất hợp lệ
            Assert.False(trip.CheckRouteCompliance(fixedRoute, "gate_C", isEntry: false));
        }

        [Fact]
        public void ArriveAtGate_On2LegFixedRouteRoundTrip_ShouldProgressAndCompleteAtOrigin()
        {
            // 2-leg round-trip: Chặng 1: gate_A (Xuất phát & Quay về, return travel = 15m), Chặng 2: gate_B (Dest 1, travel = 20m, stay = 30m)
            var route = new GateRouteConfig
            {
                IsDefault = false,
                RouteCode = "ROUTE-NM1-NM2",
                RouteName = "Tuyến NM1 - NM2",
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "gate_A", GateName = "Cổng NM1", MaxTravelMinutes = 15 },
                    new() { StepIndex = 2, GateId = "gate_B", GateName = "Cổng NM2", MaxTravelMinutes = 20, MaxStayMinutes = 30 }
                }
            };

            var trip = new VehicleDispatchTrip();
            var t0 = new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);

            // 1. Quẹt RA tại gate_A (Origin) khởi hành
            Assert.True(trip.CheckRouteCompliance(route, "gate_A", isEntry: false));
            trip.Start(originGateId: "gate_A", travelMinutes: route.GetTravelMinutesForLeg(1), now: t0);
            Assert.Equal("gate_A", trip.OriginGateId);
            Assert.Equal(1, trip.CurrentStepIndex);
            Assert.Equal(t0.AddMinutes(20), trip.NextDeadline); // SLA Leg 1 di chuyển đến gate_B = 20m

            // Test 1: Quay đầu quẹt VÀO lại gate_A ngay -> CheckRouteCompliance trả về false (Lạc tuyến)
            Assert.False(trip.CheckRouteCompliance(route, "gate_A", isEntry: true));

            // Test 2: Quẹt VÀO gate_B -> Hợp lệ, WorkingAtGate, SLA dừng đỗ 30p
            Assert.True(trip.CheckRouteCompliance(route, "gate_B", isEntry: true));
            var t1 = t0.AddMinutes(15);
            bool isCompletedOnB = trip.IsTripCompletedOnEntry(route, "gate_B");
            Assert.False(isCompletedOnB);
            trip.ArriveAtGate("gate_B", stayMinutes: route.GetStayMinutesForLeg(trip.CurrentStepIndex), isCompleted: isCompletedOnB, now: t1);
            Assert.Equal(TripStatus.WorkingAtGate, trip.Status);
            Assert.Equal(t1.AddMinutes(30), trip.NextDeadline);

            // Test 3: Quẹt RA gate_B -> Chuyển sang chặng quay về, SLA di chuyển lấy từ Chặng 1 (15p)
            Assert.True(trip.CheckRouteCompliance(route, "gate_B", isEntry: false));
            var t2 = t1.AddMinutes(25);
            trip.DepartToNextStep("gate_B", nextStepTravelMinutes: route.GetTravelMinutesForLeg(trip.CurrentStepIndex + 1), now: t2);
            Assert.Equal(2, trip.CurrentStepIndex);
            Assert.Equal(TripStatus.InTransit, trip.Status);
            Assert.Equal(t2.AddMinutes(15), trip.NextDeadline); // SLA quay về lấy từ Chặng 1 = 15m

            // Test 4: Quẹt VÀO gate_A -> IsTripCompletedOnEntry = true, Status = Completed
            Assert.True(trip.CheckRouteCompliance(route, "gate_A", isEntry: true));
            var t3 = t2.AddMinutes(10);
            bool isCompletedOnA = trip.IsTripCompletedOnEntry(route, "gate_A");
            Assert.True(isCompletedOnA);
            trip.ArriveAtGate("gate_A", stayMinutes: route.GetStayMinutesForLeg(trip.CurrentStepIndex), isCompleted: isCompletedOnA, now: t3);
            Assert.Equal(TripStatus.Completed, trip.Status);
            Assert.Equal(t3, trip.EndTime);
            Assert.Null(trip.NextDeadline);
        }

        [Fact]
        public void CalculateOverdue_ShouldAccuratelyReflectOverdueStatus()
        {
            // Arrange
            var deadline = new DateTime(2026, 10, 3, 8, 30, 0, DateTimeKind.Utc);
            var trip = new VehicleDispatchTrip { NextDeadline = deadline };

            // 1. Trước deadline
            var before = trip.CalculateOverdue(deadline.AddMinutes(-5));
            Assert.False(before.IsOverdue);
            Assert.Equal(0, before.OverdueSeconds);

            // 2. Sau deadline
            var after = trip.CalculateOverdue(deadline.AddSeconds(120));
            Assert.True(after.IsOverdue);
            Assert.Equal(120, after.OverdueSeconds);
        }

        [Fact]
        public void RecordCheckpoint_ShouldAppendCheckpointProperly()
        {
            // Arrange
            var trip = new VehicleDispatchTrip { CurrentStepIndex = 1 };
            var now = DateTime.UtcNow;
            var sla = new SlaOverdueInfo { IsOverdue = false, OverdueSeconds = 0 };

            // Act
            var cp = trip.RecordCheckpoint(
                gateId: "gate_A",
                gateName: "Cổng Chính",
                direction: LaneDirection.Out,
                plateDetected: "29A-12345",
                isRouteCompliant: true,
                note: "Xuất phát",
                timestamp: now,
                slaOverdue: sla);

            // Assert
            Assert.Single(trip.Checkpoints);
            Assert.Equal("gate_A", cp.GateId);
            Assert.Equal(LaneDirection.Out, cp.Direction);
            Assert.Equal("29A-12345", cp.PlateDetected);
            Assert.True(cp.IsRouteCompliant);
        }

        [Fact]
        public void ArriveAtGate_On3LegFixedRouteRoundTrip_ShouldProgressAndCompleteAtOrigin()
        {
            // 3-step route: Gate A (Origin & Return, SLA=15m), Gate B (Dest 1, travel=20m, stay=25m), Gate C (Dest 2, travel=30m, stay=35m)
            var route = new GateRouteConfig
            {
                IsDefault = false,
                RouteCode = "ROUTE-3-LEGS",
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "gate_A", GateName = "Cổng A (Origin)", MaxTravelMinutes = 15 },
                    new() { StepIndex = 2, GateId = "gate_B", GateName = "Cổng B (Dest 1)", MaxTravelMinutes = 20, MaxStayMinutes = 25 },
                    new() { StepIndex = 3, GateId = "gate_C", GateName = "Cổng C (Dest 2)", MaxTravelMinutes = 30, MaxStayMinutes = 35 }
                }
            };

            var trip = new VehicleDispatchTrip();
            var t0 = new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);

            // 1. Quẹt RA tại gate_A (Khởi hành chuyến)
            Assert.True(trip.CheckRouteCompliance(route, "gate_A", isEntry: false));
            trip.Start("gate_A", route.GetTravelMinutesForLeg(1), t0);
            Assert.Equal(1, trip.CurrentStepIndex);
            Assert.Equal(TripStatus.InTransit, trip.Status);
            Assert.Equal(t0.AddMinutes(20), trip.NextDeadline); // SLA đến Gate B = 20m

            // 2. Quẹt VÀO tại gate_B
            Assert.True(trip.CheckRouteCompliance(route, "gate_B", isEntry: true));
            Assert.False(trip.IsTripCompletedOnEntry(route, "gate_B"));
            var t1 = t0.AddMinutes(15);
            trip.ArriveAtGate("gate_B", route.GetStayMinutesForLeg(trip.CurrentStepIndex), isCompleted: false, t1);
            Assert.Equal(TripStatus.WorkingAtGate, trip.Status);
            Assert.Equal("gate_B", trip.CurrentGateId);
            Assert.Equal(t1.AddMinutes(25), trip.NextDeadline); // SLA dừng đỗ tại Gate B = 25m

            // 3. Quẹt RA tại gate_B tiếp tục sang Gate C
            Assert.True(trip.CheckRouteCompliance(route, "gate_B", isEntry: false));
            var t2 = t1.AddMinutes(20);
            trip.DepartToNextStep("gate_B", route.GetTravelMinutesForLeg(trip.CurrentStepIndex + 1), t2);
            Assert.Equal(2, trip.CurrentStepIndex);
            Assert.Equal(TripStatus.InTransit, trip.Status);
            Assert.Equal(t2.AddMinutes(30), trip.NextDeadline); // SLA đến Gate C = 30m

            // 4. Quẹt VÀO tại gate_C
            Assert.True(trip.CheckRouteCompliance(route, "gate_C", isEntry: true));
            Assert.False(trip.IsTripCompletedOnEntry(route, "gate_C"));
            var t3 = t2.AddMinutes(25);
            trip.ArriveAtGate("gate_C", route.GetStayMinutesForLeg(trip.CurrentStepIndex), isCompleted: false, t3);
            Assert.Equal(TripStatus.WorkingAtGate, trip.Status);
            Assert.Equal("gate_C", trip.CurrentGateId);
            Assert.Equal(t3.AddMinutes(35), trip.NextDeadline); // SLA dừng đỗ tại Gate C = 35m

            // 5. Quẹt RA tại gate_C tiếp tục chặng quay về Gate A
            Assert.True(trip.CheckRouteCompliance(route, "gate_C", isEntry: false));
            var t4 = t3.AddMinutes(30);
            trip.DepartToNextStep("gate_C", route.GetTravelMinutesForLeg(trip.CurrentStepIndex + 1), t4);
            Assert.Equal(3, trip.CurrentStepIndex);
            Assert.Equal(TripStatus.InTransit, trip.Status);
            Assert.Equal(t4.AddMinutes(15), trip.NextDeadline); // SLA quay về Gate A lấy từ Chặng 1 = 15m

            // 6. Quẹt VÀO tại gate_A hoàn thành chuyến
            Assert.True(trip.CheckRouteCompliance(route, "gate_A", isEntry: true));
            var t5 = t4.AddMinutes(10);
            bool isCompleted = trip.IsTripCompletedOnEntry(route, "gate_A");
            Assert.True(isCompleted);
            trip.ArriveAtGate("gate_A", route.GetStayMinutesForLeg(trip.CurrentStepIndex), isCompleted: true, t5);
            Assert.Equal(TripStatus.Completed, trip.Status);
            Assert.Equal(t5, trip.EndTime);
            Assert.Null(trip.NextDeadline);
        }

        [Fact]
        public void CheckRouteCompliance_On3LegRoute_WhenSkippingIntermediateGate_ShouldFlagRouteDeviation()
        {
            var route = new GateRouteConfig
            {
                IsDefault = false,
                RouteCode = "ROUTE-3-LEGS",
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "gate_A" },
                    new() { StepIndex = 2, GateId = "gate_B" },
                    new() { StepIndex = 3, GateId = "gate_C" }
                }
            };

            var trip = new VehicleDispatchTrip();
            var t0 = new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);
            trip.Start("gate_A", route.GetTravelMinutesForLeg(1), t0);

            // Tại Step 1 (sau khi rời Gate A):
            // - Quẹt vào Gate C (bỏ qua Gate B) -> Phải bị chặn (false)
            Assert.False(trip.CheckRouteCompliance(route, "gate_C", isEntry: true));
            // - Quẹt quay đầu vào Gate A -> Phải bị chặn (false)
            Assert.False(trip.CheckRouteCompliance(route, "gate_A", isEntry: true));

            // Vào Gate B hợp lệ
            trip.ArriveAtGate("gate_B", 20, false, t0.AddMinutes(10));
            // Ra Gate B sang Step 2
            trip.DepartToNextStep("gate_B", route.GetTravelMinutesForLeg(2), t0.AddMinutes(30));
            Assert.Equal(2, trip.CurrentStepIndex);

            // Tại Step 2 (sau khi rời Gate B hướng tới Gate C):
            // - Quẹt quay lại Gate B -> Bị chặn (false)
            Assert.False(trip.CheckRouteCompliance(route, "gate_B", isEntry: true));
            // - Quẹt thẳng về Gate A (bỏ qua Gate C) -> Bị chặn (false)
            Assert.False(trip.CheckRouteCompliance(route, "gate_A", isEntry: true));
            // - Quẹt vào Gate C -> Hợp lệ (true)
            Assert.True(trip.CheckRouteCompliance(route, "gate_C", isEntry: true));
        }
    }
}
