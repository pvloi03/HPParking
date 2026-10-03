using HPParking.Core.Models.Entities;
using System.Collections.Generic;
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
        public void GetTravelMinutesForStep_WhenStepConfigured_ShouldReturnStepTravelMinutes()
        {
            var route = new GateRouteConfig
            {
                DefaultTravelMinutes = 15,
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "g1", MaxTravelMinutes = 35 }
                }
            };

            Assert.Equal(35, route.GetTravelMinutesForStep(1));
            Assert.Equal(15, route.GetTravelMinutesForStep(2)); // fallback default
        }

        [Fact]
        public void GetStayMinutesForStep_WhenStepConfigured_ShouldReturnStepStayMinutes()
        {
            var route = new GateRouteConfig
            {
                DefaultStayMinutes = 20,
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "g1", MaxStayMinutes = 45 }
                }
            };

            Assert.Equal(45, route.GetStayMinutesForStep(1));
            Assert.Equal(20, route.GetStayMinutesForStep(2)); // fallback default
        }

        [Fact]
        public void GetFinalGateId_ShouldReturnLastStepGateIdOrNull()
        {
            var route = new GateRouteConfig
            {
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "g1" },
                    new() { StepIndex = 2, GateId = "g2" },
                    new() { StepIndex = 3, GateId = "g_final" }
                }
            };

            Assert.Equal("g_final", route.GetFinalGateId());

            var emptyRoute = new GateRouteConfig { GateSteps = [] };
            Assert.Null(emptyRoute.GetFinalGateId());
        }
    }
}
