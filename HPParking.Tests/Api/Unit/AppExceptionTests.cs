using FluentAssertions;
using HPParking.Api.Common.Exceptions;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class AppExceptionTests
    {
        [Fact]
        public void NotFoundException_ShouldHave404StatusAndDefaultErrorCode()
        {
            var ex = new NotFoundException("Khách hàng không tồn tại");

            ex.StatusCode.Should().Be(404);
            ex.ErrorCode.Should().Be(ErrorCodes.NOT_FOUND);
            ex.Message.Should().Be("Khách hàng không tồn tại");
            ex.Errors.Should().ContainSingle().Which.Should().Be("Khách hàng không tồn tại");
        }

        [Fact]
        public void ConflictException_ShouldHave409StatusAndDefaultErrorCode()
        {
            var ex = new ConflictException("Biển số xe đã tồn tại", ErrorCodes.VEHICLE_PLATE_DUPLICATE);

            ex.StatusCode.Should().Be(409);
            ex.ErrorCode.Should().Be(ErrorCodes.VEHICLE_PLATE_DUPLICATE);
            ex.Message.Should().Be("Biển số xe đã tồn tại");
        }

        [Fact]
        public void ForbiddenException_ShouldHave403Status()
        {
            var ex = new ForbiddenException();

            ex.StatusCode.Should().Be(403);
            ex.ErrorCode.Should().Be(ErrorCodes.FORBIDDEN);
        }

        [Fact]
        public void UnauthorizedException_ShouldHave401Status()
        {
            var ex = new UnauthorizedException();

            ex.StatusCode.Should().Be(401);
            ex.ErrorCode.Should().Be(ErrorCodes.UNAUTHORIZED);
        }
    }
}
