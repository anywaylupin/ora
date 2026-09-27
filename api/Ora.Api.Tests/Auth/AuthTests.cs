using System.Net;
using System.Net.Http.Json;
using Ora.Api.Tests.Infrastructure;

namespace Ora.Api.Tests.Auth;

public sealed class AuthTests(OraApiFactory factory)
{
    [Fact]
    public async Task Sign_in_returns_a_bearer_token_that_authenticates_requests()
    {
        using var client = factory.CreateOraClient();
        var email = await client.SignUpAndSignInAsync();

        var info = await client.Http.GetFromJsonAsync<InfoResponse>("/auth/manage/info", TestContext.Current.CancellationToken);

        Assert.Equal(email, info?.Email);
    }

    [Fact]
    public async Task Requests_without_a_token_are_rejected()
    {
        using var client = factory.CreateOraClient();

        var response = await client.Http.GetAsync("/auth/manage/info", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Sign_in_with_a_wrong_password_is_rejected()
    {
        using var client = factory.CreateOraClient();
        var email = await client.SignUpAndSignInAsync();

        var response = await client.Http.PostAsJsonAsync(
            "/auth/login", new { email, password = "Wrong-passw0rd" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Sign_up_with_a_taken_email_is_a_validation_problem()
    {
        using var client = factory.CreateOraClient();
        var email = await client.SignUpAndSignInAsync();

        var response = await client.Http.PostAsJsonAsync(
            "/auth/register", new { email, password = TestData.Password }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_issues_a_new_working_token_pair()
    {
        using var client = factory.CreateOraClient();
        var email = await client.SignUpAndSignInAsync();
        client.SignOut();

        var response = await client.Http.PostAsJsonAsync(
            "/auth/refresh", new { refreshToken = client.RefreshToken }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        client.UseTokens(await response.Content.ReadFromJsonAsync<TokenResponse>(TestContext.Current.CancellationToken));
        var info = await client.Http.GetFromJsonAsync<InfoResponse>("/auth/manage/info", TestContext.Current.CancellationToken);

        Assert.Equal(email, info?.Email);
    }

    [Fact]
    public async Task Refresh_with_a_forged_token_is_rejected()
    {
        using var client = factory.CreateOraClient();

        var response = await client.Http.PostAsJsonAsync(
            "/auth/refresh", new { refreshToken = "not-a-real-token" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record InfoResponse(string Email, bool IsEmailConfirmed);
}
