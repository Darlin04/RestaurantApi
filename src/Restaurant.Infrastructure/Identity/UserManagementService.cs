using FluentValidation;
using FluentValidation.Results;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Restaurant.Application.Identity;
using Restaurant.Application.Identity.CreateUser;
using Restaurant.Domain.Common;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Identity;

public sealed class UserManagementService(
    UserManager<ApplicationUser> users,
    RestaurantDbContext dbContext,
    TimeProvider timeProvider) : IUserManagementService
{
    public async Task<UserResponse> CreateAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var existing = await users.FindByEmailAsync(command.Email);
        if (existing is not null)
        {
            throw new ConflictException("User.DuplicateEmail", $"Ya existe un usuario con el correo '{command.Email}'.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var user = new ApplicationUser
        {
            UserName = command.Email,
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName,
            IsActive = true,
            CreatedAt = timeProvider.GetUtcNow(),
        };

        var result = await users.CreateAsync(user, command.Password);
        if (!result.Succeeded)
        {
            throw new ValidationException(result.Errors.Select(e => new ValidationFailure("password", e.Description)).ToList());
        }

        var roleResult = await users.AddToRoleAsync(user, command.Role);
        if (!roleResult.Succeeded)
        {
            throw new ValidationException(roleResult.Errors.Select(e => new ValidationFailure("role", e.Description)).ToList());
        }

        await transaction.CommitAsync(cancellationToken);
        return new UserResponse(user.Id, user.Email!, user.FirstName, user.LastName, command.Role, user.IsActive);
    }

    public async Task<IReadOnlyList<UserResponse>> ListAsync(string? role, bool? isActive, CancellationToken cancellationToken)
    {
        var query = users.Users.AsNoTracking().OrderBy(u => u.Email);
        if (isActive.HasValue)
        {
            query = (IOrderedQueryable<ApplicationUser>)query.Where(u => u.IsActive == isActive.Value);
        }

        var items = await query.ToListAsync(cancellationToken);
        var result = new List<UserResponse>();
        foreach (var user in items)
        {
            var roles = await users.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? string.Empty;
            if (role is not null && userRole != role)
            {
                continue;
            }

            result.Add(new UserResponse(user.Id, user.Email!, user.FirstName, user.LastName, userRole, user.IsActive));
        }

        return result;
    }

    public async Task<UserResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null)
        {
            throw new NotFoundException("User.NotFound", $"No existe el usuario '{id}'.");
        }

        var roles = await users.GetRolesAsync(user);
        return new UserResponse(user.Id, user.Email!, user.FirstName, user.LastName, roles.FirstOrDefault() ?? string.Empty, user.IsActive);
    }
}
