namespace Suppliers.Domain.Common;

internal static class Guard
{
    public static string Required(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{field} is required.");

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{field} must be at most {maxLength} characters.");

        return trimmed;
    }

    public static string? Optional(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{field} must be at most {maxLength} characters.");

        return trimmed;
    }

    public static int? PositiveOrNull(int? value, string field)
    {
        if (value is <= 0)
            throw new DomainException($"{field} must be positive when provided.");

        return value;
    }

    public static TEnum Defined<TEnum>(TEnum value, string field) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new DomainException($"{field} has an unknown value: {value}.");

        return value;
    }
}
