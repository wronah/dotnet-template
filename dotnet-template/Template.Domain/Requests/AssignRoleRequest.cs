using Template.Domain.Enums;

namespace Template.Domain.Requests;
public class AssignRoleRequest
{
    public required Role Role { get; init; }
}
