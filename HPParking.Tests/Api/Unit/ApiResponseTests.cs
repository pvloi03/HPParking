using System.Collections.Generic;
using FluentAssertions;
using HPParking.Api.DTOs.Common;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class ApiResponseTests
    {
        [Fact]
        public void SuccessResult_ShouldReturnExpectedEnvelope()
        {
            // Arrange
            var data = new { Name = "Test" };
            var message = "Thao tác thành công";
            var traceId = "trace-123";

            // Act
            var response = ApiResponse<object>.SuccessResult(data, message, traceId);

            // Assert
            response.Success.Should().BeTrue();
            response.Data.Should().Be(data);
            response.Message.Should().Be(message);
            response.TraceId.Should().Be(traceId);
            response.Errors.Should().BeEmpty();
        }

        [Fact]
        public void CreatedResult_ShouldReturnSuccessTrueWithData()
        {
            // Arrange
            var data = "item-id-123";
            var message = "Tạo mới thành công";

            // Act
            var response = ApiResponse<string>.CreatedResult(data, message);

            // Assert
            response.Success.Should().BeTrue();
            response.Data.Should().Be(data);
            response.Message.Should().Be(message);
            response.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Failure_WithErrorsList_ShouldReturnSuccessFalseWithErrors()
        {
            // Arrange
            var message = "Đã xảy ra lỗi";
            var errors = new List<string> { "Lỗi 1", "Lỗi 2" };
            var traceId = "trace-456";

            // Act
            var response = ApiResponse<object>.Failure(message, errors, traceId);

            // Assert
            response.Success.Should().BeFalse();
            response.Data.Should().BeNull();
            response.Message.Should().Be(message);
            response.Errors.Should().BeEquivalentTo(errors);
            response.TraceId.Should().Be(traceId);
        }
    }
}
