using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NileTechno.Infrastructure.Configuration;

namespace NileTechno.Infrastructure.Persistence;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        EnvFile.Load();

        var connectionString = SqlConnectionString.Resolve();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .Options;

        return new ApplicationDbContext(options);
    }
}
