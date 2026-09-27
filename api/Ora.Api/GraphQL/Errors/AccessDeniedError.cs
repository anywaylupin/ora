namespace Ora.Api.GraphQL.Errors;

/// <summary>
/// Thrown when the viewer belongs to the workspace but their role does not allow the action.
/// </summary>
public sealed class AccessDeniedException(string message) : Exception(message);

/// <summary>
/// The payload error for actions above the viewer's role.
/// </summary>
public sealed class AccessDeniedError(AccessDeniedException exception)
{
    public string Message { get; } = exception.Message;
}
