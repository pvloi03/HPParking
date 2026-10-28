using FluentAssertions;
using HPParking.Core.Models.Common;
using System.Configuration;
using System.Reflection;
using Xunit;

namespace HPParking.Tests.Core
{
    public class FeatureOptionsTests
    {
        private static void SetAppSettingsReadOnly(bool readOnly)
        {
            var field = typeof(ConfigurationElementCollection).GetField("_bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?? typeof(ConfigurationElementCollection).GetField("bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(ConfigurationManager.AppSettings, readOnly);
        }

        [Fact]
        public void FromAppConfig_WhenEmptyStringConfigured_ShouldDefaultToFalse()
        {
            SetAppSettingsReadOnly(false);
            var originalVal = ConfigurationManager.AppSettings["EnableFleetDispatch"];
            var originalFallback = ConfigurationManager.AppSettings["Features_EnableFleetDispatch"];

            try
            {
                ConfigurationManager.AppSettings["EnableFleetDispatch"] = "";
                ConfigurationManager.AppSettings["Features_EnableFleetDispatch"] = "";

                var options = FeatureOptions.FromAppConfig();

                options.Should().NotBeNull();
                options.EnableFleetDispatch.Should().BeFalse();
            }
            finally
            {
                ConfigurationManager.AppSettings["EnableFleetDispatch"] = originalVal ?? "";
                ConfigurationManager.AppSettings["Features_EnableFleetDispatch"] = originalFallback ?? "";
                SetAppSettingsReadOnly(true);
            }
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("True", true)]
        [InlineData("TRUE", true)]
        [InlineData("false", false)]
        [InlineData("invalid", false)]
        [InlineData("", false)]
        public void FromAppConfig_WhenEnableFleetDispatchKeySet_ShouldParseCorrectly(string configValue, bool expected)
        {
            SetAppSettingsReadOnly(false);
            var originalVal = ConfigurationManager.AppSettings["EnableFleetDispatch"];

            try
            {
                ConfigurationManager.AppSettings["EnableFleetDispatch"] = configValue;

                var options = FeatureOptions.FromAppConfig();

                options.Should().NotBeNull();
                options.EnableFleetDispatch.Should().Be(expected);
            }
            finally
            {
                ConfigurationManager.AppSettings["EnableFleetDispatch"] = originalVal ?? "";
                SetAppSettingsReadOnly(true);
            }
        }

        [Fact]
        public void FromAppConfig_WhenUsingFallbackKey_ShouldParseCorrectly()
        {
            SetAppSettingsReadOnly(false);
            var originalVal = ConfigurationManager.AppSettings["EnableFleetDispatch"];
            var originalFallback = ConfigurationManager.AppSettings["Features_EnableFleetDispatch"];

            try
            {
                ConfigurationManager.AppSettings["EnableFleetDispatch"] = "";
                ConfigurationManager.AppSettings["Features_EnableFleetDispatch"] = "true";

                var options = FeatureOptions.FromAppConfig();

                options.Should().NotBeNull();
                options.EnableFleetDispatch.Should().BeTrue();
            }
            finally
            {
                ConfigurationManager.AppSettings["EnableFleetDispatch"] = originalVal ?? "";
                ConfigurationManager.AppSettings["Features_EnableFleetDispatch"] = originalFallback ?? "";
                SetAppSettingsReadOnly(true);
            }
        }
    }
}

