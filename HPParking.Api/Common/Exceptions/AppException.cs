using System;
using System.Collections.Generic;

namespace HPParking.Api.Common.Exceptions
{
    public class AppException : Exception
    {
        public int StatusCode { get; }
        public string ErrorCode { get; }
        public List<string> Errors { get; }

        public AppException(
            string message,
            int statusCode = 400,
            string errorCode = ErrorCodes.BAD_REQUEST,
            List<string>? errors = null,
            Exception? innerException = null)
            : base(message, innerException)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            Errors = errors ?? new List<string> { message };
        }
    }

    public class NotFoundException : AppException
    {
        public NotFoundException(string message, string errorCode = ErrorCodes.NOT_FOUND)
            : base(message, 404, errorCode)
        {
        }
    }

    public class ConflictException : AppException
    {
        public ConflictException(string message, string errorCode = ErrorCodes.CONFLICT)
            : base(message, 409, errorCode)
        {
        }
    }

    public class BadRequestException : AppException
    {
        public BadRequestException(string message, string errorCode = ErrorCodes.BAD_REQUEST, List<string>? errors = null)
            : base(message, 400, errorCode, errors)
        {
        }
    }

    public class UnauthorizedException : AppException
    {
        public UnauthorizedException(string message = "Yêu cầu chưa được xác thực.", string errorCode = ErrorCodes.UNAUTHORIZED)
            : base(message, 401, errorCode)
        {
        }
    }

    public class ForbiddenException : AppException
    {
        public ForbiddenException(string message = "Bạn không có quyền thực hiện hành động này.", string errorCode = ErrorCodes.FORBIDDEN)
            : base(message, 403, errorCode)
        {
        }
    }
}
