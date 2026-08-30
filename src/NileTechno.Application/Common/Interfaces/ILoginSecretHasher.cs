using NileTechno.Domain.Entities;

namespace NileTechno.Application.Common.Interfaces;

public interface ILoginSecretHasher
{
    string Hash(LoginAccount account, string secret);
    bool Verify(LoginAccount account, string secret);
}
