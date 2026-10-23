using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using Restaurant.Application.Abstractions;
using Restaurant.Infrastructure.Identity;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Falta la configuración 'ConnectionStrings:Default'. Configúrala con dotnet user-secrets o variables de entorno.");
        }

        services.AddDbContext<RestaurantDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                o => o.MigrationsAssembly(typeof(RestaurantDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<RestaurantDbContext>());

        services.AddOptions<JwtOptions>()
            .BindConfiguration("Jwt")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<DatabaseOptions>()
            .BindConfiguration("Database");

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<RestaurantDbContext>()
            .AddSignInManager();

        var jwtSection = configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var key = jwtSection["Key"];
        var keyBytes = GetKeyBytes(key);

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "sub",
                    RoleClaimType = "role",
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var sub = context.Principal?.FindFirst("sub")?.Value;
                        if (!Guid.TryParse(sub, out var userId))
                        {
                            context.Fail("Invalid sub claim.");
                            return;
                        }

                        var users = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                        var user = await users.FindByIdAsync(userId.ToString());
                        if (user is null || !user.IsActive)
                        {
                            context.Fail("User is inactive or missing.");
                        }
                    },
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.Admin, policy => policy.RequireRole(Roles.Admin));
            options.AddPolicy(Policies.Waiter, policy => policy.RequireRole(Roles.Waiter));
            options.AddPolicy(Policies.Staff, policy => policy.RequireRole(Roles.Admin, Roles.Waiter));
            options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        services.AddScoped<JwtTokenGenerator>();
        services.AddHostedService<IdentitySeeder>();

        var assembly = typeof(DependencyInjection).Assembly;
        var domainAssembly = typeof(Domain.Common.AggregateRoot).Assembly;
        var applicationAssembly = typeof(IUnitOfWork).Assembly;
        var allowedNamespaces = new[]
        {
            domainAssembly.GetName().Name!,
            applicationAssembly.GetName().Name!,
        };

        var candidates = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract &&
            (t.Name.EndsWith("Repository", StringComparison.Ordinal) ||
             t.Name.EndsWith("Service", StringComparison.Ordinal)));

        foreach (var implementation in candidates)
        {
            var interfaces = implementation.GetInterfaces()
                .Where(i => i.Assembly.GetName().Name is not null &&
                    allowedNamespaces.Contains(i.Assembly.GetName().Name))
                .ToList();

            foreach (var serviceInterface in interfaces)
            {
                services.AddScoped(serviceInterface, implementation);
            }
        }

        services.AddHealthChecks().AddDbContextCheck<RestaurantDbContext>();

        return services;
    }

    private static byte[] GetKeyBytes(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return new byte[64];
        }

        try
        {
            return Convert.FromBase64String(key);
        }
        catch (FormatException)
        {
            return Encoding.UTF8.GetBytes(key);
        }
    }
}
