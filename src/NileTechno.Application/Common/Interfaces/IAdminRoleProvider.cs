namespace NileTechno.Application.Common.Interfaces;

public interface IAdminRoleProvider
{
    IList<string> ResolveRoles(string email);
    string ResolveRole(string email);
}
