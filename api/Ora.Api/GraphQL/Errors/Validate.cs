using System.Text.RegularExpressions;

namespace Ora.Api.GraphQL.Errors;

/// <summary>
/// Input checks shared by mutations, each returning the normalized value or throwing a ValidationException.
/// </summary>
public static partial class Validate
{
    /// <summary>
    /// Trims the name and requires it to be non-empty and within the column length.
    /// </summary>
    public static string Name(string? value, string field, int maxLength)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new ValidationException(field, "Enter a name.");
        }

        if (trimmed.Length > maxLength)
        {
            throw new ValidationException(field, $"Use {maxLength} characters or fewer.");
        }

        return trimmed;
    }

    /// <summary>
    /// Accepts a six digit hex color and stores it in lower case so equal colors compare equal.
    /// </summary>
    public static string Color(string? value, string field)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        if (!HexColor().IsMatch(trimmed))
        {
            throw new ValidationException(field, "Use a hex color such as #3b82f6.");
        }

        return trimmed.ToLowerInvariant();
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColor();
}
