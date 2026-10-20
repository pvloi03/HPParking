using FluentAssertions;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System;
using System.Text.Json;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class ApprovalTokenValueObjectTests
    {
        [Fact]
        public void NewToken_ShouldGenerateCryptographicallySecure64HexChars()
        {
            var token1 = ApprovalToken.NewToken();
            var token2 = ApprovalToken.NewToken();

            token1.IsEmpty.Should().BeFalse();
            token1.IsValid.Should().BeTrue();
            token1.Value.Length.Should().Be(64);
            token1.Value.Should().MatchRegex("^[0-9a-f]{64}$");

            // Entropy check: consecutive tokens must not match
            (token1 == token2).Should().BeFalse();
        }

        [Fact]
        public void Constructor_ShouldTrimAndConvertToLowercase()
        {
            var token = new ApprovalToken("  ABC123DEF45678901234567890123456  ");

            token.Value.Should().Be("abc123def45678901234567890123456");
            token.ToString().Should().Be("abc123def45678901234567890123456");
        }

        [Fact]
        public void ImplicitConversions_ShouldWorkBothWays()
        {
            // string -> ApprovalToken
            ApprovalToken token = "test_approval_token_12345678901234";
            token.Value.Should().Be("test_approval_token_12345678901234");

            // ApprovalToken -> string
            string strValue = token;
            strValue.Should().Be("test_approval_token_12345678901234");
        }

        [Fact]
        public void IsEmpty_ShouldIdentifyNullOrWhitespace()
        {
            ApprovalToken.From(null).IsEmpty.Should().BeTrue();
            ApprovalToken.From("").IsEmpty.Should().BeTrue();
            ApprovalToken.From("   ").IsEmpty.Should().BeTrue();
            ApprovalToken.Empty.IsEmpty.Should().BeTrue();

            ApprovalToken.From("some-token").IsEmpty.Should().BeFalse();
        }

        [Fact]
        public void IsValid_ShouldVerifyMinimumLengthThreshold()
        {
            // Minimum 32 chars for secure one-click token
            ApprovalToken.From("").IsValid.Should().BeFalse();
            ApprovalToken.From("short-token-under-32-chars").IsValid.Should().BeFalse();
            ApprovalToken.From("12345678901234567890123456789012").IsValid.Should().BeTrue(); // exactly 32 chars
            ApprovalToken.NewToken().IsValid.Should().BeTrue();
        }

        [Fact]
        public void Equality_ShouldBeCaseInsensitive()
        {
            ApprovalToken t1 = "A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4";
            ApprovalToken t2 = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4";
            ApprovalToken t3 = "different_token_12345678901234567890";

            (t1 == t2).Should().BeTrue();
            t1.Equals(t2).Should().BeTrue();
            (t1 == t3).Should().BeFalse();
            t1.GetHashCode().Should().Be(t2.GetHashCode());
        }

        [Fact]
        public void ParseAndTryParse_ShouldSupportIParsableContract()
        {
            // Parse success
            var parsed = ApprovalToken.Parse("valid_token_string_1234567890123456");
            parsed.Value.Should().Be("valid_token_string_1234567890123456");

            // Parse fail on empty
            var act = () => ApprovalToken.Parse("   ");
            act.Should().Throw<FormatException>();

            // TryParse success
            ApprovalToken.TryParse("abc_token_123", out var tryParsed).Should().BeTrue();
            tryParsed.Value.Should().Be("abc_token_123");

            // TryParse fail
            ApprovalToken.TryParse("   ", out var failedParsed).Should().BeFalse();
            failedParsed.IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void SystemTextJson_ShouldSerializeAndDeserializeAsString()
        {
            var request = new ClientApprovalRequest
            {
                Id = "req-1",
                ClientId = "client-1",
                Token = "my_custom_json_token_1234567890123456"
            };

            var json = JsonSerializer.Serialize(request);
            json.Should().Contain("\"Token\":\"my_custom_json_token_1234567890123456\"");

            var deserialized = JsonSerializer.Deserialize<ClientApprovalRequest>(json);
            deserialized.Should().NotBeNull();
            deserialized!.Token.Value.Should().Be("my_custom_json_token_1234567890123456");
            (deserialized.Token == request.Token).Should().BeTrue();
        }

        [Fact]
        public void MongoBsonSerializer_ShouldSerializeAndDeserializeAsString()
        {
            var req = new ClientApprovalRequest
            {
                ClientId = ObjectId.GenerateNewId().ToString(),
                ApprovingDepartmentId = ObjectId.GenerateNewId().ToString(),
                Token = ApprovalToken.NewToken()
            };

            var bson = req.ToBson();
            bson.Should().NotBeNull();

            var deserialized = BsonSerializer.Deserialize<ClientApprovalRequest>(bson);
            (deserialized.Token == req.Token).Should().BeTrue();
            deserialized.Token.Value.Should().Be(req.Token.Value);
        }

        [Fact]
        public void ClientApprovalRequest_TokenProperty_IsProtectedThroughApprovalToken()
        {
            var req = new ClientApprovalRequest();
            req.Token = "  UPPERCASE_HEX_TOKEN_1234567890123456  ";

            req.Token.Value.Should().Be("uppercase_hex_token_1234567890123456");
            req.Token.IsValid.Should().BeTrue();
            req.Token.IsEmpty.Should().BeFalse();
        }
    }
}
