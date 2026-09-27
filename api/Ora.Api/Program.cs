using Microsoft.AspNetCore.Identity;
using Ora.Api.Commands;
using Ora.Api.Domain;
using Ora.Api.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddOraData()
    .AddOraAuth()
    .AddOraCors()
    .AddOraGraphQL();

var app = builder.Build();

if (args is ["migrate", ..])
{
    return await DatabaseCommands.MigrateAsync(app.Services);
}

if (args is ["seed", ..])
{
    return await SeedCommand.RunAsync(app.Services);
}

app.UseCors(OraHostingExtensions.CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/auth").WithTags("Auth").MapIdentityApi<User>();
app.MapGraphQL().WithOptions(options => options.Tool.Enable = app.Environment.IsDevelopment());

return await app.RunWithGraphQLCommandsAsync(args);
