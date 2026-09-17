using HPParking.Core.Models.Common;
using HPParking.Core.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace HPParking.Core.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho tài khoản người dùng trong hệ thống
    /// </summary>
    [BsonIgnoreExtraElements]
    public class User : BaseEntity
    {
        // =========================================================================
        // --- CÁC TRƯỜNG LƯU TRỮ DATABASE (PERSISTED PROPERTIES) ---
        // =========================================================================
        public string Username { get; set; } = string.Empty;
        [SensitiveData]
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        [BsonRepresentation(BsonType.String)]
        public UserRole Role { get; set; } = UserRole.Viewer;
        public bool IsActive { get; set; } = true;
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? LastLoginAt { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? LastLogoutAt { get; set; }

        public User() { }

        public User(string username, string passwordHash, string fullName, UserRole role = UserRole.Viewer)
        {
            Username = username;
            PasswordHash = passwordHash;
            FullName = fullName;
            Role = role;
        }
    }
}
