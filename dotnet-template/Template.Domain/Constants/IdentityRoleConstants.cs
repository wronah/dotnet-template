namespace Template.Domain.Constants;
public static class IdentityRoleConstants
{
    public static readonly Guid AdminRoleGuid = new Guid("a18de96e-cd1f-4b80-92df-f0c36982c376");
    public static readonly Guid UserRoleGuid = new Guid("b2ef4ac0-9883-4c3a-9af1-9b02d415d778");

    public const string Admin = nameof(Admin);
    public const string User = nameof(User);
}