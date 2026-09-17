namespace HPParking.Api.Common.Exceptions
{
    public static class ErrorCodes
    {
        // Common
        public const string INTERNAL_SERVER_ERROR = "INTERNAL_SERVER_ERROR";
        public const string BAD_REQUEST = "BAD_REQUEST";
        public const string VALIDATION_FAILED = "VALIDATION_FAILED";
        public const string NOT_FOUND = "NOT_FOUND";
        public const string UNAUTHORIZED = "UNAUTHORIZED";
        public const string FORBIDDEN = "FORBIDDEN";
        public const string CONFLICT = "CONFLICT";
        public const string TOO_MANY_REQUESTS = "TOO_MANY_REQUESTS";

        // Auth
        public const string AUTH_INVALID_CREDENTIALS = "AUTH_INVALID_CREDENTIALS";
        public const string AUTH_ACCOUNT_LOCKED = "AUTH_ACCOUNT_LOCKED";
        public const string AUTH_INVALID_TOKEN = "AUTH_INVALID_TOKEN";
        public const string AUTH_TOKEN_EXPIRED = "AUTH_TOKEN_EXPIRED";
        public const string AUTH_INVALID_API_KEY = "AUTH_INVALID_API_KEY";
        public const string AUTH_REFRESH_TOKEN_REQUIRED = "AUTH_REFRESH_TOKEN_REQUIRED";
        public const string AUTH_REFRESH_TOKEN_INVALID = "AUTH_REFRESH_TOKEN_INVALID";

        // Client
        public const string CLIENT_NOT_FOUND = "CLIENT_NOT_FOUND";
        public const string CLIENT_PHONE_DUPLICATE = "CLIENT_PHONE_DUPLICATE";
        public const string CLIENT_CODE_DUPLICATE = "CLIENT_CODE_DUPLICATE";

        // Vehicle
        public const string VEHICLE_NOT_FOUND = "VEHICLE_NOT_FOUND";
        public const string VEHICLE_PLATE_DUPLICATE = "VEHICLE_PLATE_DUPLICATE";

        // File & Import
        public const string FILE_INVALID_FORMAT = "FILE_INVALID_FORMAT";
        public const string FILE_SIZE_EXCEEDED = "FILE_SIZE_EXCEEDED";
        public const string EXCEL_PARSE_ERROR = "EXCEL_PARSE_ERROR";
    }
}
