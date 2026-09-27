using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Ora.Api.Data;
using Ora.Api.Domain;

namespace Ora.Api.Hosting;

/// <summary>
/// Groups service registration by concern so Program.cs reads as a table of contents.
/// </summary>
public static class OraHostingExtensions
{
    /// <summary>
    /// The CORS policy the API applies to every endpoint.
    /// </summary>
    public const string CorsPolicy = "Web";

    /// <summary>
    /// The connection string is read when the context is built rather than at startup, so tests and hosts can supply it late.
    /// </summary>
    public static WebApplicationBuilder AddOraData(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IDataScope, HttpContextDataScope>();
        builder.Services.AddDbContext<OraDbContext>((services, options) =>
        {
            var connectionString = services.GetRequiredService<IConfiguration>().GetConnectionString("Default");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Set the ConnectionStrings__Default environment variable.");
            }

            options.UseSqlServer(connectionString);
        });

        return builder;
    }

    /// <summary>
    /// Bearer tokens only, because the web app and the API are deployed on different domains.
    /// </summary>
    /// <remarks>
    /// Data protection keys live in the database so tokens survive container restarts and are shared across IIS or Docker instances.
    /// </remarks>
    public static WebApplicationBuilder AddOraAuth(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddAuthentication(IdentityConstants.BearerScheme)
            .AddBearerToken(IdentityConstants.BearerScheme);
        builder.Services.AddAuthorization();

        builder.Services
            .AddIdentityCore<User>(options => options.User.RequireUniqueEmail = true)
            .AddEntityFrameworkStores<OraDbContext>()
            .AddApiEndpoints();

        builder.Services
            .AddDataProtection()
            .SetApplicationName("Ora")
            .PersistKeysToDbContext<OraDbContext>();

        return builder;
    }

    /// <summary>
    /// Allowed origins come from configuration as a comma separated list, which is easy to set from an environment variable.
    /// </summary>
    public static WebApplicationBuilder AddOraCors(this WebApplicationBuilder builder)
    {
        var origins = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        return builder;
    }
}
