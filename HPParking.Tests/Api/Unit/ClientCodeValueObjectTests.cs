using FluentAssertions;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.ValueObjects;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class ClientCodeValueObjectTests
    {
        [Fact]
        public void ClientCode_NormalizesOnConstruction()
        {
            var code = new ClientCode("  nv-001  ");
            code.Value.Should().Be("NV-001");
            code.ToString().Should().Be("NV-001");
        }

        [Fact]
        public void ClientCode_SupportsImplicitConversions()
        {
            // string -> ClientCode
            ClientCode code = "nv-001";
            code.Value.Should().Be("NV-001");

            // ClientCode -> string
            string str = code;
            str.Should().Be("NV-001");
        }

        [Fact]
        public void ClientCode_ValueEqualityIsCaseInsensitiveThroughNormalization()
        {
            ClientCode code1 = "nv-001";
            ClientCode code2 = "NV-001";
            ClientCode code3 = "NV-002";

            (code1 == code2).Should().BeTrue();
            code1.Equals(code2).Should().BeTrue();
            (code1 == code3).Should().BeFalse();
            code1.GetHashCode().Should().Be(code2.GetHashCode());
        }

        [Fact]
        public void ClientCode_IsEmpty_IdentifiesEmptyOrWhitespace()
        {
            ClientCode.From(null).IsEmpty.Should().BeTrue();
            ClientCode.From("").IsEmpty.Should().BeTrue();
            ClientCode.From("   ").IsEmpty.Should().BeTrue();
            ClientCode.From("NV-001").IsEmpty.Should().BeFalse();
        }

        [Fact]
        public void ClientCode_IsValid_VerifiesDomainRules()
        {
            ClientCode.From("NV-001").IsValid.Should().BeTrue();
            ClientCode.From("emp_2026").IsValid.Should().BeTrue();
            ClientCode.From("001200012345").IsValid.Should().BeTrue();

            ClientCode.From("NV@001").IsValid.Should().BeFalse();
            ClientCode.From("").IsValid.Should().BeFalse();
            ClientCode.From(new string('A', 51)).IsValid.Should().BeFalse();
        }

        [Fact]
        public void ClientEntity_CodeProperty_IsSelfGuardingThroughValueObject()
        {
            var client = new Client();
            client.Code = "  nv-test_99  ";

            client.Code.Should().Be("NV-TEST_99");
        }

        [Fact]
        public void ClientCode_CanBeSortedCorrectly()
        {
            var list = new List<ClientCode> { "NV-003", "NV-001", "NV-002" };
            list.Sort();

            list.Should().ContainInOrder(new ClientCode("NV-001"), new ClientCode("NV-002"), new ClientCode("NV-003"));
        }
    }
}
