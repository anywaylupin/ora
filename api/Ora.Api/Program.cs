using Microsoft.EntityFrameworkCore;
using Ora.Api.Commands;
using Ora.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IDataScope, HttpContextDataScope>();
builder.Services.AddDbContext<OraDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

if (args is ["migrate", ..])
{
    return await DatabaseCommands.MigrateAsync(app.Services);
}

app.MapGet("/", () => "Ora API");

await app.RunAsync();
return 0;
