namespace UsersService.Persistence.Entities;

public sealed class UserEntity
{
    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;
}