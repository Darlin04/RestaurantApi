using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using Restaurant.Application.Abstractions;
using Restaurant.Infrastructure.Identity;

namespace Restaurant.Infrastructure.Persistence;

public sealed class RestaurantDbContext(DbContextOptions<RestaurantDbContext> options) : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IUnitOfWork
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(b => b.ToTable("users", "identity"));
        modelBuilder.Entity<IdentityRole<Guid>>(b => b.ToTable("roles", "identity"));
        modelBuilder.Entity<IdentityUserRole<Guid>>(b => b.ToTable("user_roles", "identity"));
        modelBuilder.Entity<IdentityUserClaim<Guid>>(b => b.ToTable("user_claims", "identity"));
        modelBuilder.Entity<IdentityUserLogin<Guid>>(b => b.ToTable("user_logins", "identity"));
        modelBuilder.Entity<IdentityUserToken<Guid>>(b => b.ToTable("user_tokens", "identity"));
        modelBuilder.Entity<IdentityRoleClaim<Guid>>(b => b.ToTable("role_claims", "identity"));

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RestaurantDbContext).Assembly);
    }
}
