using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MusicLibrary.Persistence.SqlServer;

public class MusicDbDesignTimeContextFactory : IDesignTimeDbContextFactory<MusicDbContext>
{
    public MusicDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MusicDbContext>();

        optionsBuilder.UseSqlServer(b => b.MigrationsAssembly(Assembly.GetExecutingAssembly()));

        return new MusicDbContext(optionsBuilder.Options);
    }
}