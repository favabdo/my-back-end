using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NileTechno.Infrastructure.Configuration;

namespace NileTechno.Infrastructure.Persistence;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        EnvFile.Load();

        var connectionString = SqlConnectionString.Normalize(
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException($"{EnvFile.Connection} is missing. Copy .env.example to .env."));

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .Options;

        return new ApplicationDbContext(options);
    }
}
