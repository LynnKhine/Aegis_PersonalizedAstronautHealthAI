using Aegis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Aegis.Infrastructure;

/// <summary>
/// Design-time factory used by `dotnet ef migrations add` when run against the
/// Infrastructure project. Not used at runtime.
/// </summary>
public sealed class AegisDbContextFactory : IDesignTimeDbContextFactory<AegisDbContext>
{
    public AegisDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<AegisDbContext>()
            .UseSqlite("Data Source=aegis-migrations.db")
            .Options;
        return new AegisDbContext(opts);
    }
}
