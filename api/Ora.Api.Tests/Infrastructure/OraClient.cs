using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ora.Api.Tests.Infrastructure;

/// <summary>
/// A thin client that speaks the same auth and GraphQL protocol as the web app.
/// </summary>
public sealed class OraClient(HttpClient http) : IDisposable
{
    public HttpClient Http => http;

    public string? RefreshToken { get; private set; }

    /// <summary>
    /// The email of the user this client signed up, once it has.
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Registers and signs in a user with a unique email so tests never collide.
    /// </summary>
    public async Task<string> SignUpAndSignInAsync(string? email = null, string password = TestData.Password)
    {
        email ??= TestData.UniqueEmail();
        var register = await http.PostAsJsonAsync("/auth/register", new { email, password });
        register.EnsureSuccessStatusCode();
        await SignInAsync(email, password);
        Email = email;
        return email;
    }

    public async Task SignInAsync(string email, string password = TestData.Password)
    {
        var response = await http.PostAsJsonAsync("/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        UseTokens(await response.Content.ReadFromJsonAsync<TokenResponse>());
    }

    public void UseTokens(TokenResponse? tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        RefreshToken = tokens.RefreshToken;
    }

    public void SignOut() => http.DefaultRequestHeaders.Authorization = null;

    /// <summary>
    /// Posts a GraphQL operation and returns the whole response so tests can assert on data and errors alike.
    /// </summary>
    public async Task<GraphQLResponse> GraphQLAsync(string query, object? variables = null)
    {
        var response = await http.PostAsJsonAsync("/graphql", new { query, variables });
        var body = await response.Content.ReadFromJsonAsync<JsonObject>()
            ?? throw new InvalidOperationException("The GraphQL response was empty.");
        return new GraphQLResponse(body);
    }

    public void Dispose() => http.Dispose();
}

/// <summary>
/// The token pair returned by the Identity login and refresh endpoints.
/// </summary>
public sealed record TokenResponse(string TokenType, string AccessToken, int ExpiresIn, string RefreshToken);

/// <summary>
/// Wraps a GraphQL response body with helpers that fail loudly on unexpected errors.
/// </summary>
public sealed class GraphQLResponse(JsonObject body)
{
    public JsonObject Body => body;

    public JsonArray Errors => body["errors"]?.AsArray() ?? [];

    /// <summary>
    /// Returns the data object, failing the test with the server's error messages if there were any.
    /// </summary>
    public JsonNode Data
    {
        get
        {
            Assert.True(Errors.Count == 0, $"Unexpected GraphQL errors: {Errors.ToJsonString()}");
            return body["data"] ?? throw new InvalidOperationException("The response has no data.");
        }
    }

    public IEnumerable<string?> ErrorCodes => Errors.Select(e => e?["extensions"]?["code"]?.GetValue<string>());

    public override string ToString() => body.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
}
