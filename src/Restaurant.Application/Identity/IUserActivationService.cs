namespace Restaurant.Application.Identity;

public interface IUserActivationService
{
    Task SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken);
}
