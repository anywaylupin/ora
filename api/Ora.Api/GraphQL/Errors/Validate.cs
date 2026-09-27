namespace Ora.Api.GraphQL.Errors;

/// <summary>
/// Input checks shared by mutations, each returning the normalized value or throwing a ValidationException.
/// </summary>
public static class Validate
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
}
