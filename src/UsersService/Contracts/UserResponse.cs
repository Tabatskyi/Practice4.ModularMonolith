using UsersService.Persistence.Entities;

namespace UsersService.Contracts;

public sealed record UserResponse(Guid UserId, string DisplayName)
{
    public static UserResponse FromEntity(UserEntity user)
    {
        return new UserResponse(user.UserId, user.DisplayName);
    }
}