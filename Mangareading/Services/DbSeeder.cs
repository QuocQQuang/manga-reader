using Mangareading.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mangareading.Services
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(YourDbContext db, ILogger logger)
        {
            try
            {
                // Sources
                if (!await db.Sources.AnyAsync())
                {
                    logger.LogInformation("Seeding Sources...");
                    db.Sources.AddRange(
                        new Source { SourceName = "MangaDex",  SourceUrl = "https://mangadex.org",  ApiBaseUrl = "https://api.mangadex.org", IsActive = true },
                        new Source { SourceName = "Imgur",     SourceUrl = "https://imgur.com",      ApiBaseUrl = "https://api.imgur.com",    IsActive = true },
                        new Source { SourceName = "Local",     SourceUrl = "",                       ApiBaseUrl = "",                         IsActive = true }
                    );
                    await db.SaveChangesAsync();
                }

                // Genres
                if (!await db.Genres.AnyAsync())
                {
                    logger.LogInformation("Seeding Genres...");
                    string[] genres = {
                        "Action", "Adventure", "Comedy", "Drama", "Fantasy",
                        "Horror", "Mystery", "Romance", "Sci-Fi", "Slice of Life",
                        "Sports", "Supernatural", "Thriller", "Historical", "Isekai",
                        "Shounen", "Shoujo", "Seinen", "Josei", "Mecha",
                        "Psychological", "Martial Arts", "School Life", "Harem", "Ecchi"
                    };

                    foreach (var name in genres)
                        db.Genres.Add(new Genre { GenreName = name });

                    await db.SaveChangesAsync();
                }

                logger.LogInformation("Database seeding completed.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }
    }
}
