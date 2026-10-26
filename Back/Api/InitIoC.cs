using Core.Accounts;
using Core.Health;
using Core.Projects;
using Infrastructure.Accounts;
using Infrastructure.Health;
using Infrastructure.Persistence;
using Infrastructure.Projects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Api;

/// <summary>
/// Composition root for application services.
/// </summary>
public static class InitIoC
{
    /// <summary>
    /// Registers Babelary services.
    /// </summary>
    public static IServiceCollection AddBabelary(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("ConnectionStrings:Database is missing.");

        services.AddDbContext<BabelaryDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<IPasswordHasher<Account>, PasswordHasher<Account>>();
        services.AddScoped<IHealth, HealthImpl>();
        services.AddScoped<IAccounts, AccountsImpl>();
        services.AddScoped<IProjects, ProjectsImpl>();
        return services;
    }
}
