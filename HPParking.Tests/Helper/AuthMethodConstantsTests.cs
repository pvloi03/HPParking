using FluentAssertions;
using HPParking.Core.Constants;
using Xunit;

namespace HPParking.Tests.Helper
{
    public class AuthMethodConstantsTests
    {
        [Fact]
        public void IsValid_ShouldReturnFalse_WhenNullOrEmpty()
        {
            AuthMethodConstants.IsValid(null).Should().BeFalse();
            AuthMethodConstants.IsValid(new List<string>()).Should().BeFalse();
            AuthMethodConstants.IsValid(new List<string> { "   " }).Should().BeFalse();
        }

        [Fact]
        public void IsValid_ShouldReturnTrue_WhenOnlyCardOrFaceIdOrBoth()
        {
            AuthMethodConstants.IsValid(new List<string> { AuthMethodConstants.Card }).Should().BeTrue();
            AuthMethodConstants.IsValid(new List<string> { AuthMethodConstants.FaceId }).Should().BeTrue();
            AuthMethodConstants.IsValid(new List<string> { AuthMethodConstants.Card, AuthMethodConstants.FaceId }).Should().BeTrue();
        }

        [Fact]
        public void IsValid_ShouldReturnTrue_WhenOnlyNone()
        {
            AuthMethodConstants.IsValid(new List<string> { AuthMethodConstants.None }).Should().BeTrue();
        }

        [Fact]
        public void IsValid_ShouldReturnFalse_WhenNoneIsCombinedWithOthers()
        {
            AuthMethodConstants.IsValid(new List<string> { AuthMethodConstants.None, AuthMethodConstants.Card }).Should().BeFalse();
            AuthMethodConstants.IsValid(new List<string> { AuthMethodConstants.None, AuthMethodConstants.FaceId }).Should().BeFalse();
            AuthMethodConstants.IsValid(new List<string> { AuthMethodConstants.None, AuthMethodConstants.Card, AuthMethodConstants.FaceId }).Should().BeFalse();
        }

        [Fact]
        public void IsValid_ShouldReturnFalse_WhenContainsUnknownMethod()
        {
            AuthMethodConstants.IsValid(new List<string> { "Fingerprint" }).Should().BeFalse();
            AuthMethodConstants.IsValid(new List<string> { AuthMethodConstants.Card, "Password" }).Should().BeFalse();
        }
    }
}
