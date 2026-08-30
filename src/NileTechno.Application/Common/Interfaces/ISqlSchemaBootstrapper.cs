namespace NileTechno.Application.Common.Interfaces;

public interface ISqlSchemaBootstrapper
{
    Task EnsureAsync(CancellationToken cancellationToken = default);
}
