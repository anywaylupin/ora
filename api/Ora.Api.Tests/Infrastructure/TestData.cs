namespace Ora.Api.Tests.Infrastructure;

/// <summary>
/// Shared values that satisfy the API's validation rules.
/// </summary>
public static class TestData
{
    /// <summary>
    /// Meets the default Identity password rules.
    /// </summary>
    public const string Password = "Passw0rd!";

    public static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.test";
}
