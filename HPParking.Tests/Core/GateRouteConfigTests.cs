using HPParking.Core.Models.Entities;
using Xunit;

namespace HPParking.Tests.Core
{
    public class GateRouteConfigTests
    {
        [Fact]
        public void IsFreeRoam_WhenDefaultOrEmptySteps_ShouldReturnTrue()
        {
            var defaultRoute = new GateRouteConfig { IsDefault = true };
            var defaultCodeRoute = new GateRouteConfig { RouteCode = "DEFAULT" };
            var emptyStepsRoute = new GateRouteConfig { IsDefault = false, RouteCode = "CUSTOM", GateSteps = [] };

            Assert.True(defaultRoute.IsFreeRoam);
            Assert.True(defaultCodeRoute.IsFreeRoam);
            Assert.True(emptyStepsRoute.IsFreeRoam);
        }

        [Fact]
        public void IsFreeRoam_WhenFixedRouteWithSteps_ShouldReturnFalse()
        {
            var fixedRoute = new GateRouteConfig
            {
                IsDefault = false,
                RouteCode = "ROUTE-FIXED",
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "gate_1" }
                }
            };

            Assert.False(fixedRoute.IsFreeRoam);
        }

        [Fact]
        public void GetOriginGateDisplayName_WhenCalled_ShouldReturnGateNameOrCodeOrFallback()
        {
            var routeWithName = new GateRouteConfig
            {
                GateSteps = [new() { StepIndex = 1, GateId = "g1", GateName = "Cổng Chính" }]
            };
            Assert.Equal("Cổng Chính", routeWithName.GetOriginGateDisplayName());

            var routeWithCode = new GateRouteConfig
            {
                GateSteps = [new() { StepIndex = 1, GateId = "g1", GateCode = "GATE_01" }]
            };
            Assert.Equal("GATE_01", routeWithCode.GetOriginGateDisplayName());

            var emptyRoute = new GateRouteConfig { GateSteps = [] };
            Assert.Equal("Cổng xuất phát", emptyRoute.GetOriginGateDisplayName());
        }

        [Fact]
        public void GetReturnTravelMinutes_WhenConfigured_ShouldReturnOriginStepMaxTravelMinutes()
        {
            var route = new GateRouteConfig
            {
                GateSteps =
                [
                    new() { StepIndex = 1, GateId = "g1", MaxTravelMinutes = 25 },
                    new() { StepIndex = 2, GateId = "g2", MaxTravelMinutes = 15 }
                ]
            };

            // Chặng quay về phải lấy thời gian quy định ở Chặng 1 (25 phút)
            Assert.Equal(25, route.GetReturnTravelMinutes());
        }

        [Fact]
        public void GetReturnTravelMinutes_WhenStepsEmptyOrNull_ShouldFallbackToDefaultTravelMinutes()
        {
            var emptyRoute = new GateRouteConfig { DefaultTravelMinutes = 12, GateSteps = [] };
            var nullStepsRoute = new GateRouteConfig { DefaultTravelMinutes = 12, GateSteps = null! };

            Assert.Equal(12, emptyRoute.GetReturnTravelMinutes());
            Assert.Equal(12, nullStepsRoute.GetReturnTravelMinutes());
        }

        [Fact]
        public void GetTargetGateIdForLeg_WithValid3LegSteps_ShouldResolveExpectedGatePerLeg()
        {
            // Route with 3 steps: g1 (origin & return, maxTravel=15), g2 (dest 1, travel=20, stay=30), g3 (dest 2, travel=25, stay=40)
            var route = new GateRouteConfig
            {
                DefaultTravelMinutes = 10,
                DefaultStayMinutes = 12,
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "g1", MaxTravelMinutes = 15, MaxStayMinutes = 0 },
                    new() { StepIndex = 2, GateId = "g2", MaxTravelMinutes = 20, MaxStayMinutes = 30 },
                    new() { StepIndex = 3, GateId = "g3", MaxTravelMinutes = 25, MaxStayMinutes = 40 }
                }
            };

            // Origin Gate
            Assert.Equal("g1", route.GetOriginGateId());

            // Leg 1: Destination is g2 (GateSteps[1])
            Assert.Equal("g2", route.GetTargetGateIdForLeg(1));
            Assert.Equal(20, route.GetTravelMinutesForLeg(1));
            Assert.Equal(30, route.GetStayMinutesForLeg(1));

            // Leg 2: Destination is g3 (GateSteps[2])
            Assert.Equal("g3", route.GetTargetGateIdForLeg(2));
            Assert.Equal(25, route.GetTravelMinutesForLeg(2));
            Assert.Equal(40, route.GetStayMinutesForLeg(2));

            // Leg 3: Return to g1 (GateSteps[0])
            Assert.Equal("g1", route.GetTargetGateIdForLeg(3));
            Assert.Equal(15, route.GetTravelMinutesForLeg(3)); // return travel = GateSteps[0].MaxTravelMinutes
            Assert.Equal(12, route.GetStayMinutesForLeg(3)); // DefaultStayMinutes (no stay on return)

            // Out-of-bounds legs
            Assert.Null(route.GetTargetGateIdForLeg(0));
            Assert.Null(route.GetTargetGateIdForLeg(4));
            Assert.Equal(10, route.GetTravelMinutesForLeg(4)); // fallback default
            Assert.Equal(12, route.GetStayMinutesForLeg(4)); // fallback default
        }

        [Fact]
        public void GetTargetGateIdForLeg_WithEmptySteps_ShouldHandleGracefully()
        {
            var route = new GateRouteConfig
            {
                DefaultTravelMinutes = 10,
                DefaultStayMinutes = 12,
                GateSteps = []
            };

            Assert.Null(route.GetOriginGateId());
            Assert.Null(route.GetTargetGateIdForLeg(1));
            Assert.Equal(10, route.GetTravelMinutesForLeg(1));
            Assert.Equal(12, route.GetStayMinutesForLeg(1));
        }

        [Fact]
        public void GetStayMinutesForLeg_WhenConfiguredZero_ShouldReturnZeroInsteadOfFallback()
        {
            var route = new GateRouteConfig
            {
                DefaultStayMinutes = 15,
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "g1", MaxTravelMinutes = 10, MaxStayMinutes = 0 },
                    new() { StepIndex = 2, GateId = "g2", MaxTravelMinutes = 15, MaxStayMinutes = 0 } // explicit 0 minutes stay
                }
            };

            // Leg 1: Destination g2 has 0 minutes stay configured -> should return 0, not fallback to DefaultStayMinutes
            Assert.Equal(0, route.GetStayMinutesForLeg(1));
        }

        [Fact]
        public void GetTargetGateIdForLeg_WithUnsortedGateSteps_ShouldResolveByStepIndexAccurately()
        {
            // Arrange: GateSteps are intentionally scrambled out of order: Step 3, Step 1, Step 2
            var route = new GateRouteConfig
            {
                DefaultTravelMinutes = 10,
                DefaultStayMinutes = 15,
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 3, GateId = "gate3", GateName = "Cổng Kho 3", MaxTravelMinutes = 30, MaxStayMinutes = 45 },
                    new() { StepIndex = 1, GateId = "gate1", GateName = "Cổng Trung Tâm", MaxTravelMinutes = 15, MaxStayMinutes = 0 },
                    new() { StepIndex = 2, GateId = "gate2", GateName = "Cổng Xưởng 2", MaxTravelMinutes = 20, MaxStayMinutes = 25 }
                }
            };

            // Origin Gate: must be StepIndex == 1 ("gate1"), not gateSteps[0] ("gate3")
            Assert.Equal("gate1", route.GetOriginGateId());
            Assert.Equal("Cổng Trung Tâm", route.GetOriginGateDisplayName());

            // Leg 1: Destination must be StepIndex == 2 ("gate2")
            Assert.Equal("gate2", route.GetTargetGateIdForLeg(1));
            Assert.Equal(20, route.GetTravelMinutesForLeg(1));
            Assert.Equal(25, route.GetStayMinutesForLeg(1));

            // Leg 2: Destination must be StepIndex == 3 ("gate3")
            Assert.Equal("gate3", route.GetTargetGateIdForLeg(2));
            Assert.Equal(30, route.GetTravelMinutesForLeg(2));
            Assert.Equal(45, route.GetStayMinutesForLeg(2));

            // Leg 3: Return to Origin StepIndex == 1 ("gate1")
            Assert.Equal("gate1", route.GetTargetGateIdForLeg(3));
            Assert.Equal(15, route.GetTravelMinutesForLeg(3)); // Step 1's return travel minutes
            Assert.Equal(15, route.GetStayMinutesForLeg(3)); // Default stay minutes (no stay on return)
        }

        [Fact]
        public void TotalSteps_ShouldReflectGateStepsCount()
        {
            var route = new GateRouteConfig
            {
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "g1" },
                    new() { StepIndex = 2, GateId = "g2" }
                }
            };
            Assert.Equal(2, route.TotalSteps);

            var emptyRoute = new GateRouteConfig { GateSteps = [] };
            Assert.Equal(0, emptyRoute.TotalSteps);

            var nullStepsRoute = new GateRouteConfig { GateSteps = null! };
            Assert.Equal(0, nullStepsRoute.TotalSteps);
        }

        [Fact]
        public void IsReturnLeg_WhenFreeRoamOrEmpty_ShouldReturnFalse()
        {
            var defaultRoute = new GateRouteConfig { IsDefault = true };
            Assert.False(defaultRoute.IsReturnLeg(1));
            Assert.False(defaultRoute.IsReturnLeg(2));

            var emptyRoute = new GateRouteConfig { GateSteps = [] };
            Assert.False(emptyRoute.IsReturnLeg(1));
            Assert.False(emptyRoute.IsReturnLeg(2));

            var nullStepsRoute = new GateRouteConfig { GateSteps = null! };
            Assert.False(nullStepsRoute.IsReturnLeg(1));
        }

        [Fact]
        public void IsReturnLeg_WhenFixedRoute_ShouldCorrectlyIdentifyReturnLeg()
        {
            // Route with 3 steps (Cổng 1 xuất phát/quay về, Cổng 2 đến #1, Cổng 3 đến #2)
            var route = new GateRouteConfig
            {
                IsDefault = false,
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "g1" },
                    new() { StepIndex = 2, GateId = "g2" },
                    new() { StepIndex = 3, GateId = "g3" }
                }
            };

            // Invalid / non-positive leg indices
            Assert.False(route.IsReturnLeg(-1));
            Assert.False(route.IsReturnLeg(0));

            // Intermediate legs (Chặng 1 đến g2, Chặng 2 đến g3)
            Assert.False(route.IsReturnLeg(1));
            Assert.False(route.IsReturnLeg(2));

            // Return leg (Chặng 3 quay về g1)
            Assert.True(route.IsReturnLeg(3));

            // Over-limit leg index (>= TotalSteps)
            Assert.True(route.IsReturnLeg(4));
        }
    }
}
