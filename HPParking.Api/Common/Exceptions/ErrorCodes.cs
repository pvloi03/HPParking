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
        public const string CLIENT_HAS_VEHICLES = "CLIENT_HAS_VEHICLES";

        // Vehicle
        public const string VEHICLE_NOT_FOUND = "VEHICLE_NOT_FOUND";
        public const string VEHICLE_PLATE_DUPLICATE = "VEHICLE_PLATE_DUPLICATE";

        // Company
        public const string COMPANY_NOT_FOUND = "COMPANY_NOT_FOUND";
        public const string COMPANY_CODE_DUPLICATE = "COMPANY_CODE_DUPLICATE";
        public const string COMPANY_HAS_DEPARTMENTS = "COMPANY_HAS_DEPARTMENTS";
        public const string COMPANY_HAS_GATES = "COMPANY_HAS_GATES";
        public const string COMPANY_HAS_CLIENTS = "COMPANY_HAS_CLIENTS";
        public const string COMPANY_ACTIVE_DEPENDENCY_EXISTS = "COMPANY_ACTIVE_DEPENDENCY_EXISTS";

        // Department
        public const string DEPARTMENT_NOT_FOUND = "DEPARTMENT_NOT_FOUND";
        public const string DEPARTMENT_CODE_DUPLICATE = "DEPARTMENT_CODE_DUPLICATE";
        public const string DEPARTMENT_HAS_CLIENTS = "DEPARTMENT_HAS_CLIENTS";
        public const string DEPARTMENT_ACTIVE_CLIENTS_EXIST = "DEPARTMENT_ACTIVE_CLIENTS_EXIST";

        // Contractor
        public const string CONTRACTOR_NOT_FOUND = "CONTRACTOR_NOT_FOUND";
        public const string CONTRACTOR_CODE_DUPLICATE = "CONTRACTOR_CODE_DUPLICATE";
        public const string CONTRACTOR_HAS_CLIENTS = "CONTRACTOR_HAS_CLIENTS";
        public const string CONTRACTOR_ACTIVE_CLIENTS_EXIST = "CONTRACTOR_ACTIVE_CLIENTS_EXIST";
        public const string CONTRACTOR_INACTIVE = "CONTRACTOR_INACTIVE";

        // Device
        public const string DEVICE_NOT_FOUND = "DEVICE_NOT_FOUND";
        public const string DEVICE_CODE_DUPLICATE = "DEVICE_CODE_DUPLICATE";
        public const string DEVICE_ENDPOINT_DUPLICATE = "DEVICE_ENDPOINT_DUPLICATE";
        public const string DEVICE_IN_USE_BY_LANE = "DEVICE_IN_USE_BY_LANE";
        public const string INFRA_ACTIVE_DEPENDENCY_EXISTS = "INFRA_ACTIVE_DEPENDENCY_EXISTS";

        // Restore & Referential Integrity
        public const string PARENT_IS_DELETED = "PARENT_IS_DELETED";
        public const string RESTORE_FAILED = "RESTORE_FAILED";

        // File & Import
        public const string FILE_INVALID_FORMAT = "FILE_INVALID_FORMAT";
        public const string FILE_SIZE_EXCEEDED = "FILE_SIZE_EXCEEDED";
        public const string EXCEL_PARSE_ERROR = "EXCEL_PARSE_ERROR";
    }
}
