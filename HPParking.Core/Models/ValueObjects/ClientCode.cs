using HPParking.Core.Helpers;
using System;

namespace HPParking.Core.Models.ValueObjects
{
    /// <summary>
    /// Value Object đại diện cho Mã định danh nhân sự (Client Code).
    /// Đóng gói tính bất biến, tự động chuẩn hóa Trim() &amp; ToUpperInvariant(), và loại bỏ Primitive Obsession.
    /// </summary>
    public readonly record struct ClientCode : IComparable<ClientCode>
    {
        public string Value { get; }

        public ClientCode(string? raw)
        {
            Value = ClientCodeHelper.Normalize(raw);
        }

        public static ClientCode From(string? raw) => new(raw);

        public static ClientCode Empty => new(string.Empty);

        public bool IsEmpty => string.IsNullOrEmpty(Value);

        public bool IsValid => ClientCodeHelper.IsValid(Value);

        public static implicit operator string(ClientCode code) => code.Value ?? string.Empty;

        public static implicit operator ClientCode(string? raw) => new(raw);

        public override string ToString() => Value ?? string.Empty;

        public int CompareTo(ClientCode other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
    }
}
