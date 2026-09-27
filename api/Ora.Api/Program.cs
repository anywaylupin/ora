using Microsoft.AspNetCore.Identity;
using Ora.Api.Commands;
using Ora.Api.Domain;
using Ora.Api.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddOraData()
    .AddOraAuth()
    .AddOraCors();

var app = builder.Build();

if (args is ["migrate", ..])
{
    return await DatabaseCommands.MigrateAsync(app.Services);
}

app.UseCors(OraHostingExtensions.CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/auth").WithTags("Auth").MapIdentityApi<User>();
app.MapGet("/", () => "Ora API");

await app.RunAsync();
return 0;
