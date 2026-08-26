using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Infrastructure.Configuration;
using NileTechno.Infrastructure.Identity;
using NileTechno.Infrastructure.Persistence;
using NileTechno.Infrastructure.Repositories;
using NileTechno.Infrastructure.Services;

namespace NileTechno.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = SqlConnectionString.Resolve(configuration);

        services.AddDataProtection()
            .SetApplicationName("NileTechno.API")
            .UseEphemeralDataProtectionProvider();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddHttpContextAccessor();

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddSingleton<IDateTime, DateTimeService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IEmailService, EmailService>();

        services.AddScoped<IItemStockQuery, ItemStockQuery>();

        services.AddScoped<ApplicationDbContextInitializer>();

        return services;
    }
}
