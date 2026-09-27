namespace Ora.Api.GraphQL.Errors;

/// <summary>
/// Thrown when a referenced object does not exist or belongs to a workspace the viewer cannot see.
/// </summary>
/// <remarks>
/// Both cases share one error on purpose, so probing IDs reveals nothing about other workspaces.
/// </remarks>
public sealed class NotFoundException(string message) : Exception(message);

/// <summary>
/// The payload error for missing or invisible objects.
/// </summary>
public sealed class NotFoundError(NotFoundException exception)
{
    public string Message { get; } = exception.Message;
}
