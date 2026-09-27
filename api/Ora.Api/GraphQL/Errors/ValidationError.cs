namespace Ora.Api.GraphQL.Errors;

/// <summary>
/// Thrown when an input value breaks a business rule the user can fix.
/// </summary>
public sealed class ValidationException(string field, string message) : Exception(message)
{
    /// <summary>
    /// The camelCase input field name, so the client can show the message next to the right control.
    /// </summary>
    public string Field { get; } = field;
}

/// <summary>
/// The payload error for invalid input.
/// </summary>
public sealed class ValidationError(ValidationException exception)
{
    public string Message { get; } = exception.Message;

    public string Field { get; } = exception.Field;
}
