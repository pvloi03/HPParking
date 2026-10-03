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
        public void IsTripCompletedOnEntry_OnFixedRoute_ShouldOnlyCompleteAtFinalStep()
        {
            // Arrange
            var fixedRoute = new GateRouteConfig
            {
                IsDefault = false,
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "gate_B" },
                    new() { StepIndex = 2, GateId = "gate_A" }
                }
            };
            var trip = new VehicleDispatchTrip { OriginGateId = "gate_A", CurrentStepIndex = 1 };

            // Step 1 chưa phải final step -> false
            Assert.False(trip.IsTripCompletedOnEntry(fixedRoute, "gate_B"));

            // Step 2 là final step -> true
            trip.CurrentStepIndex = 2;
            Assert.True(trip.IsTripCompletedOnEntry(fixedRoute, "gate_A"));
        }

        [Fact]
        public void CheckRouteCompliance_OnEntry_ShouldCompareWithExpectedGate()
        {
            // Arrange
            var fixedRoute = new GateRouteConfig
            {
                IsDefault = false,
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "gate_B" },
                    new() { StepIndex = 2, GateId = "gate_C" }
                }
            };
            var trip = new VehicleDispatchTrip { CurrentStepIndex = 1 };

            // Quẹt vào đúng gate_B ở Step 1 -> Hợp lệ
            Assert.True(trip.CheckRouteCompliance(fixedRoute, "gate_B", isEntry: true));

            // Quẹt vào nhầm gate_C ở Step 1 -> Bất hợp lệ
            Assert.False(trip.CheckRouteCompliance(fixedRoute, "gate_C", isEntry: true));
        }

        [Fact]
        public void CheckRouteCompliance_OnExit_ShouldBeValidAtStartAndMatchCurrentGateLater()
        {
            // Arrange
            var fixedRoute = new GateRouteConfig
            {
                IsDefault = false,
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "gate_B" }
                }
            };
            var trip = new VehicleDispatchTrip { CurrentStepIndex = 1, LastEntryTime = null };

            // Lúc xuất phát ban đầu (chưa từng entry): Quẹt RA ở bất kỳ cổng nào cũng hợp lệ
            Assert.True(trip.CheckRouteCompliance(fixedRoute, "gate_A", isEntry: false));

            // Khi đã ở cổng trung gian gate_B
            trip.CurrentGateId = "gate_B";
            trip.LastEntryTime = DateTime.UtcNow;

            // Quẹt ra tại đúng cổng gate_B -> Hợp lệ
            Assert.True(trip.CheckRouteCompliance(fixedRoute, "gate_B", isEntry: false));

            // Quẹt ra tại cổng khác gate_C -> Bất hợp lệ
            Assert.False(trip.CheckRouteCompliance(fixedRoute, "gate_C", isEntry: false));
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
    }
}
