using FluentAssertions;
using HPParking.Core.Helpers;
using HPParking.Core.Models.Entities;
using Xunit;

namespace HPParking.Tests.Core
{
    public class VehicleAndClientDomainTests
    {
        #region --- 1. Vehicle.MatchesPlate Domain Tests ---

        [Theory]
        [InlineData("30E-123.45", "30E-123.45", true)]
        [InlineData("30E-123.45", "30E12345", true)]
        [InlineData("30E-123.45", "30e-123.45", true)]
        [InlineData("30E-123.45", "30e 123.45", true)]
        [InlineData("30E-123.45", " 30E_12345 ", true)]
        [InlineData("51F-999.99", "51F99999", true)]
        [InlineData("29A-111.22", "30E-123.45", false)]
        [InlineData("30E-123.45", "30E-123.46", false)]
        [InlineData("30E-123.45", "", false)]
        [InlineData("30E-123.45", "   ", false)]
        [InlineData("30E-123.45", null, false)]
        public void Vehicle_MatchesPlate_ShouldMatchCorrectlyRegardlessOfFormat(string vehiclePlate, string? candidatePlate, bool expected)
        {
            var vehicle = new Vehicle { PlateNumber = vehiclePlate };

            var result = vehicle.MatchesPlate(candidatePlate);

            result.Should().Be(expected);
        }

        [Fact]
        public void Vehicle_MatchesPlate_WhenVehiclePlateNumberIsEmpty_ShouldReturnFalse()
        {
            var vehicle = new Vehicle { PlateNumber = "" };

            vehicle.MatchesPlate("30E-123.45").Should().BeFalse();
            vehicle.MatchesPlate("").Should().BeFalse();
            vehicle.MatchesPlate(null).Should().BeFalse();
        }

        [Theory]
        [InlineData("30E-123.45", "30E12345", true)]
        [InlineData("29A-888.88", "29A-888.88", true)]
        [InlineData("30E-123.45", "29A-888.88", false)]
        [InlineData(null, "29A-888.88", false)]
        [InlineData("30E-123.45", null, false)]
        public void PlateHelper_Matches_ShouldMatchCorrectly(string? plateA, string? plateB, bool expected)
        {
            PlateHelper.Matches(plateA, plateB).Should().Be(expected);
        }

        #endregion

        #region --- 2. Client Rich Domain Tests ---

        [Fact]
        public void Client_RequiresPlateVerification_ShouldReflectVerifyVehiclePlateProperty()
        {
            var clientTrue = new Client { VerifyVehiclePlate = true };
            var clientFalse = new Client { VerifyVehiclePlate = false };

            clientTrue.RequiresPlateVerification().Should().BeTrue();
            clientFalse.RequiresPlateVerification().Should().BeFalse();
        }

        [Fact]
        public void Client_CanPassGate_WhenActiveAndExpiryDisabled_ShouldReturnTrue()
        {
            var client = new Client
            {
                IsActive = true,
                Expired = new Expired { Enable = false }
            };

            var canPass = client.CanPassGate(DateTime.Now, out var reason);

            canPass.Should().BeTrue();
            reason.Should().BeEmpty();
        }

        [Fact]
        public void Client_CanPassGate_WhenInactive_ShouldReturnFalseWithReason()
        {
            var client = new Client
            {
                IsActive = false,
                Expired = new Expired { Enable = false }
            };

            var canPass = client.CanPassGate(DateTime.Now, out var reason);

            canPass.Should().BeFalse();
            reason.Should().Contain("khóa hoặc ngừng hoạt động");
        }

        [Fact]
        public void Client_CanPassGate_WhenExpiryEnabled_WithinValidRange_ShouldReturnTrue()
        {
            var today = DateTime.Today;
            var client = new Client
            {
                IsActive = true,
                Expired = new Expired
                {
                    Enable = true,
                    StartDay = today.AddDays(-5),
                    EndDay = today.AddDays(5)
                }
            };

            var canPass = client.CanPassGate(today, out var reason);

            canPass.Should().BeTrue();
            reason.Should().BeEmpty();
        }

        [Fact]
        public void Client_CanPassGate_WhenExpiryEnabled_PastEndDay_ShouldReturnFalseWithReason()
        {
            var today = DateTime.Today;
            var client = new Client
            {
                IsActive = true,
                Expired = new Expired
                {
                    Enable = true,
                    StartDay = today.AddDays(-30),
                    EndDay = today.AddDays(-1)
                }
            };

            var canPass = client.CanPassGate(today, out var reason);

            canPass.Should().BeFalse();
            reason.Should().Contain("Người dùng chỉ được ra vào từ");
        }

        [Fact]
        public void Client_CanPassGate_WhenExpiryEnabled_BeforeStartDay_ShouldReturnFalseWithReason()
        {
            var today = DateTime.Today;
            var client = new Client
            {
                IsActive = true,
                Expired = new Expired
                {
                    Enable = true,
                    StartDay = today.AddDays(2),
                    EndDay = today.AddDays(30)
                }
            };

            var canPass = client.CanPassGate(today, out var reason);

            canPass.Should().BeFalse();
            reason.Should().Contain("Người dùng chỉ được ra vào từ");
        }

        [Fact]
        public void Client_FindMatchingVehicle_WhenVehicleMatches_ShouldReturnVehicle()
        {
            var client = new Client { Id = "c1" };
            var v1 = new Vehicle { Id = "v1", PlateNumber = "30E-111.11", OwnerClientId = client.Id, IsActive = true };
            var v2 = new Vehicle { Id = "v2", PlateNumber = "29A-222.22", OwnerClientId = client.Id, IsActive = true };
            var list = new List<Vehicle> { v1, v2 };

            var matched = client.FindMatchingVehicle("29A 222.22", list);

            matched.Should().NotBeNull();
            matched!.Id.Should().Be("v2");
        }

        [Fact]
        public void Client_FindMatchingVehicle_WhenNoMatch_ShouldReturnNull()
        {
            var client = new Client { Id = "c1" };
            var v1 = new Vehicle { Id = "v1", PlateNumber = "30E-111.11", OwnerClientId = client.Id, IsActive = true };
            var list = new List<Vehicle> { v1 };

            var matched = client.FindMatchingVehicle("51F-999.99", list);

            matched.Should().BeNull();
        }

        [Fact]
        public void Client_FindMatchingVehicle_WhenInputEmptyOrListNull_ShouldReturnNull()
        {
            var client = new Client { Id = "c1" };

            client.FindMatchingVehicle(null, new List<Vehicle>()).Should().BeNull();
            client.FindMatchingVehicle("", new List<Vehicle>()).Should().BeNull();
            client.FindMatchingVehicle("30E-111.11", null).Should().BeNull();
        }

        [Fact]
        public void Client_FindMatchingVehicle_WhenDetectedPlateContainsMultipleDelimitedPlates_ShouldMatch()
        {
            var client = new Client { Id = "c1" };
            var v1 = new Vehicle { Id = "v1", PlateNumber = "30E-111.11", OwnerClientId = client.Id, IsActive = true };
            var v2 = new Vehicle { Id = "v2", PlateNumber = "29A-222.22", OwnerClientId = client.Id, IsActive = true };
            var list = new List<Vehicle> { v1, v2 };

            var matched = client.FindMatchingVehicle("51F-999.99; 29A-222.22", list);

            matched.Should().NotBeNull();
            matched!.Id.Should().Be("v2");
        }

        [Fact]
        public void Client_FindMatchingVehicle_WhenVehicleIsInactiveOrDeleted_ShouldIgnoreIt()
        {
            var client = new Client { Id = "c1" };
            var vInactive = new Vehicle { Id = "v1", PlateNumber = "30E-111.11", OwnerClientId = client.Id, IsActive = false };
            var vDeleted = new Vehicle { Id = "v2", PlateNumber = "29A-222.22", OwnerClientId = client.Id, IsDeleted = true };
            var list = new List<Vehicle> { vInactive, vDeleted };

            client.FindMatchingVehicle("30E-111.11", list).Should().BeNull();
            client.FindMatchingVehicle("29A-222.22", list).Should().BeNull();
        }

        [Fact]
        public void Client_FindMatchingVehicle_WhenVehicleBelongsToDifferentOwner_ShouldReturnNull()
        {
            var client = new Client { Id = "c1" };
            var vehicleOfOtherClient = new Vehicle { Id = "v1", PlateNumber = "30E-111.11", OwnerClientId = "c2", IsActive = true };
            var list = new List<Vehicle> { vehicleOfOtherClient };

            var matched = client.FindMatchingVehicle("30E-111.11", list);

            matched.Should().BeNull("phương tiện thuộc về khách hàng khác (c2) không được phép khớp với client (c1)");
        }

        [Fact]
        public void Client_FindMatchingVehicle_WhenVehicleHasNoOwner_ShouldReturnNull()
        {
            var client = new Client { Id = "c1" };
            var unownedVehicle = new Vehicle { Id = "v1", PlateNumber = "30E-111.11", OwnerClientId = null, IsActive = true };
            var list = new List<Vehicle> { unownedVehicle };

            var matched = client.FindMatchingVehicle("30E-111.11", list);

            matched.Should().BeNull("phương tiện không có chủ sở hữu (OwnerClientId == null) không được phép khớp với client");
        }

        [Fact]
        public void Client_CanPassGate_WhenAtTimeIsUtc_ShouldConvertAndCompareCorrectly()
        {
            var today = DateTime.Today;
            var client = new Client
            {
                IsActive = true,
                Expired = new Expired
                {
                    Enable = true,
                    StartDay = today.AddDays(-1),
                    EndDay = today.AddDays(1)
                }
            };

            var canPass = client.CanPassGate(DateTime.UtcNow, out var reason);

            canPass.Should().BeTrue();
            reason.Should().BeEmpty();
        }

        [Theory]
        [InlineData("30E-123.45", "30E\t123.45", true)]
        [InlineData("30E-123.45", "30E:123.45", true)]
        [InlineData("30E-123.45", "30E\r\n12345", true)]
        public void Vehicle_MatchesPlate_WithSpecialWhitespaceAndColons_ShouldMatch(string vehiclePlate, string candidatePlate, bool expected)
        {
            var vehicle = new Vehicle { PlateNumber = vehiclePlate };

            vehicle.MatchesPlate(candidatePlate).Should().Be(expected);
        }

        [Fact]
        public void Client_UsesFaceAuth_DefaultClient_ShouldReturnTrue()
        {
            var client = new Client();
            client.UsesFaceAuth().Should().BeTrue();
        }

        [Theory]
        [InlineData("FaceId", true)]
        [InlineData("faceid", true)]
        [InlineData(" FACEID ", true)]
        [InlineData("Card", false)]
        [InlineData("None", false)]
        public void Client_UsesFaceAuth_WithSingleMethod_ShouldReturnExpected(string authMethod, bool expected)
        {
            var client = new Client { AuthMethods = [authMethod] };
            client.UsesFaceAuth().Should().Be(expected);
        }

        [Fact]
        public void Client_UsesFaceAuth_WhenMultiAuth_ShouldReturnTrueIfFaceIdPresent()
        {
            var clientWithFaceAndCard = new Client { AuthMethods = [HPParking.Core.Constants.AuthMethodConstants.Card, HPParking.Core.Constants.AuthMethodConstants.FaceId] };
            clientWithFaceAndCard.UsesFaceAuth().Should().BeTrue();

            var clientCardOnly = new Client { AuthMethods = [HPParking.Core.Constants.AuthMethodConstants.Card] };
            clientCardOnly.UsesFaceAuth().Should().BeFalse();
        }

        [Fact]
        public void Client_UsesFaceAuth_WhenAuthMethodsNullOrEmpty_ShouldReturnFalse()
        {
            var clientNull = new Client { AuthMethods = null! };
            clientNull.UsesFaceAuth().Should().BeFalse();

            var clientEmpty = new Client { AuthMethods = [] };
            clientEmpty.UsesFaceAuth().Should().BeFalse();
        }

        [Theory]
        [InlineData(true, null, true)]
        [InlineData(false, "face-device-01", true)]
        [InlineData(true, "face-device-01", true)]
        [InlineData(false, null, false)]
        [InlineData(false, "", false)]
        public void Lane_HasFaceDevice_ShouldEvaluateCorrectly(bool useFaceCam, string? faceDeviceId, bool expected)
        {
            var lane = new Lane
            {
                UseFaceCam = useFaceCam,
                FaceDeviceId = faceDeviceId
            };

            lane.HasFaceDevice().Should().Be(expected);
        }

        #endregion
    }
}
