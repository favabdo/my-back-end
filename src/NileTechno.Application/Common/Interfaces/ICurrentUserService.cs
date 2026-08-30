namespace NileTechno.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    int? AccountId { get; }
    string? Email { get; }
    bool IsAdmin { get; }
    bool IsAuthenticated { get; }
}
