namespace Template.Domain.Exceptions;
public class UserNotFoundByIdException(Guid id) : Exception($"User with id: {id} does not exist.");
