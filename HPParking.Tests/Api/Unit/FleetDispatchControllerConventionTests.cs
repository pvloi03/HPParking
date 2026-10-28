using FluentAssertions;
using HPParking.Api.Common.Attributes;
using HPParking.Api.Common.Conventions;
using HPParking.Api.Controllers.V1;
using HPParking.Core.Models.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Reflection;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class FleetDispatchControllerConventionTests
    {
        [FleetDispatchFeature]
        private class SampleFleetController : ControllerBase
        {
            public IActionResult SampleAction() => Ok();
        }

        private class SampleRegularController : ControllerBase
        {
            public IActionResult RegularAction() => Ok();
        }

        private static ControllerModel CreateControllerModel(Type controllerType)
        {
            var typeInfo = controllerType.GetTypeInfo();
            var attributes = new List<object>(typeInfo.GetCustomAttributes(inherit: true));
            var controllerModel = new ControllerModel(typeInfo, attributes);

            var method = controllerType.GetMethod("SampleAction") ?? controllerType.GetMethod("RegularAction");
            if (method != null)
            {
                var actionModel = new ActionModel(method, attributes)
                {
                    Controller = controllerModel
                };
                controllerModel.Actions.Add(actionModel);
            }

            return controllerModel;
        }

        [Fact]
        public void Apply_WhenFleetDispatchEnabled_LeavesActionsAndApiExplorerIntact()
        {
            var options = new FeatureOptions { EnableFleetDispatch = true };
            var convention = new FleetDispatchControllerConvention(options);
            var model = CreateControllerModel(typeof(SampleFleetController));

            convention.Apply(model);

            model.Actions.Should().NotBeEmpty();
            model.ApiExplorer.IsVisible.Should().NotBe(false);
        }

        [Fact]
        public void Apply_WhenFleetDispatchDisabled_AndControllerHasFeature_ClearsActionsAndHidesApiExplorer()
        {
            var options = new FeatureOptions { EnableFleetDispatch = false };
            var convention = new FleetDispatchControllerConvention(options);
            var model = CreateControllerModel(typeof(SampleFleetController));

            convention.Apply(model);

            model.Actions.Should().BeEmpty();
            model.ApiExplorer.IsVisible.Should().BeFalse();
        }

        [Fact]
        public void Apply_WhenFleetDispatchDisabled_AndControllerDoesNotHaveFeature_LeavesActionsIntact()
        {
            var options = new FeatureOptions { EnableFleetDispatch = false };
            var convention = new FleetDispatchControllerConvention(options);
            var model = CreateControllerModel(typeof(SampleRegularController));

            convention.Apply(model);

            model.Actions.Should().NotBeEmpty();
            model.ApiExplorer.IsVisible.Should().NotBe(false);
        }

        [Fact]
        public void Controllers_FleetDispatchAndGateRoutes_HaveFleetDispatchFeatureAttribute()
        {
            typeof(FleetDispatchController).IsDefined(typeof(FleetDispatchFeatureAttribute), inherit: true).Should().BeTrue();
            typeof(GateRoutesController).IsDefined(typeof(FleetDispatchFeatureAttribute), inherit: true).Should().BeTrue();
        }
    }
}
