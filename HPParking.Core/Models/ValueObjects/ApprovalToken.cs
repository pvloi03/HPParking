using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HPParking.Core.Models.ValueObjects
{
    /// <summary>
    /// Value Object đại diện cho Mã Token phê duyệt bảo mật qua email (ADR 0042).
    /// Đóng gói tính bất biến, chuẩn hóa, tự sinh token ngẫu nhiên bảo mật cao và loại bỏ Primitive Obsession.
    /// </summary>
    [BsonSerializer(typeof(ApprovalTokenBsonSerializer))]
    [JsonConverter(typeof(ApprovalTokenJsonConverter))]
    public readonly record struct ApprovalToken : IComparable<ApprovalToken>, IEquatable<ApprovalToken>, IParsable<ApprovalToken>
    {
        public string Value { get; }

        public ApprovalToken(string? raw)
        {
            Value = raw?.Trim().ToLowerInvariant() ?? string.Empty;
        }

        /// <summary>
        /// Tạo token mới với 32 bytes ngẫu nhiên bảo mật cao (64 ký tự hex viết thường)
        /// </summary>
        public static ApprovalToken NewToken()
        {
            return new ApprovalToken(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant());
        }

        public static ApprovalToken From(string? raw) => new(raw);

        public static ApprovalToken Empty => new(string.Empty);

        public bool IsEmpty => string.IsNullOrEmpty(Value);

        /// <summary>
        /// Token hợp lệ khi không rỗng và có độ dài tối thiểu 32 ký tự URL-safe
        /// </summary>
        public bool IsValid => !IsEmpty && Value.Length >= 32;

        public static implicit operator string(ApprovalToken token) => token.Value ?? string.Empty;

        public static implicit operator ApprovalToken(string? raw) => new(raw);

        public override string ToString() => Value ?? string.Empty;

        public int CompareTo(ApprovalToken other) => string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);

        public bool Equals(ApprovalToken other) => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value ?? string.Empty);

        public static ApprovalToken Parse(string s, IFormatProvider? provider = null)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                throw new FormatException("Approval token cannot be empty.");
            }
            return new ApprovalToken(s);
        }

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out ApprovalToken result)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                result = Empty;
                return false;
            }
            result = new ApprovalToken(s);
            return true;
        }

        public static bool TryParse([NotNullWhen(true)] string? s, out ApprovalToken result)
        {
            return TryParse(s, null, out result);
        }
    }

    /// <summary>
    /// Serializer cho MongoDB BSON lưu trữ và đọc ApprovalToken dưới dạng BsonString
    /// </summary>
    public class ApprovalTokenBsonSerializer : SerializerBase<ApprovalToken>
    {
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ApprovalToken value)
        {
            context.Writer.WriteString(value.Value ?? string.Empty);
        }

        public override ApprovalToken Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonType = context.Reader.GetCurrentBsonType();
            if (bsonType == MongoDB.Bson.BsonType.Null)
            {
                context.Reader.ReadNull();
                return ApprovalToken.Empty;
            }
            return ApprovalToken.From(context.Reader.ReadString());
        }
    }

    /// <summary>
    /// Serializer cho System.Text.Json đọc và ghi ApprovalToken dưới dạng chuỗi JSON nguyên thủy
    /// </summary>
    public class ApprovalTokenJsonConverter : JsonConverter<ApprovalToken>
    {
        public override ApprovalToken Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return ApprovalToken.From(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, ApprovalToken value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value ?? string.Empty);
        }
    }
}
