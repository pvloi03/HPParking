using HPParking.Api.Common.Attributes;
using HPParking.Core.Models.Common;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace HPParking.Api.Common.Conventions
{
    public class FleetDispatchControllerConvention(FeatureOptions featureOptions) : IControllerModelConvention
    {
        private readonly FeatureOptions _featureOptions = featureOptions;

        public void Apply(ControllerModel controller)
        {
            if (_featureOptions.EnableFleetDispatch)
                return;

            bool isFleetDispatch = controller.Attributes.OfType<FleetDispatchFeatureAttribute>().Any() ||
                                   controller.ControllerType.IsDefined(typeof(FleetDispatchFeatureAttribute), inherit: true);

            if (isFleetDispatch)
            {
                controller.ApiExplorer.IsVisible = false;
                controller.Actions.Clear();
            }
        }
    }
}
