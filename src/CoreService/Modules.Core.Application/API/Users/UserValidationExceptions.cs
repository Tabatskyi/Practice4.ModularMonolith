namespace Modules.Core.Application.API.Users;

public sealed class UserNotFoundException(Guid userId)
    : Exception($"User with id '{userId}' was not found.");

public sealed class UsersServiceUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);