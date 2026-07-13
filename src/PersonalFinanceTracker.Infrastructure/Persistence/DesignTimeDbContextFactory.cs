using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PersonalFinanceTracker.Infrastructure.Persistence;

/// <summary>
/// Lets EF Core tooling (`dotnet ef migrations add ...`) construct an
/// <see cref="AppDbContext"/> without spinning up the full Console host and its
/// DI container. Only used at design time — never touched at runtime.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite("Data Source=financetracker.db");

        return new AppDbContext(optionsBuilder.Options);
    }
}
