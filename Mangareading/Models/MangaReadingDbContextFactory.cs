using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Mangareading.Models
{
    /// <summary>
    /// Design-time factory — used by EF CLI (dotnet ef migrations add/update-database).
    /// Reads appsettings.json at project root so tools don't need a running app.
    /// </summary>
    public class MangaReadingDbContextFactory : IDesignTimeDbContextFactory<YourDbContext>
    {
        public YourDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<YourDbContext>();
            optionsBuilder.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"));

            return new YourDbContext(optionsBuilder.Options);
        }
    }
}
