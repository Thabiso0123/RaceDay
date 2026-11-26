using Microsoft.EntityFrameworkCore;

namespace RaceDay.Api.Data;

/// <summary>
/// Creates the database when it does not exist and adds the columns Part 3 needs.
/// Retries because SQL Server in Docker takes a few seconds to accept connections.
/// </summary>
public static class DatabaseInitializer
{
    private static readonly string[] Part3Columns =
    {
        "IF COL_LENGTH('Event', 'BannerImage') IS NULL ALTER TABLE [Event] ADD BannerImage VARCHAR(300) NULL;",
        "IF COL_LENGTH('Participant', 'ProfilePicture') IS NULL ALTER TABLE Participant ADD ProfilePicture VARCHAR(300) NULL;",
        "IF COL_LENGTH('Category', 'MinAge') IS NULL ALTER TABLE Category ADD MinAge INT NULL;",
        "IF COL_LENGTH('Category', 'MaxAge') IS NULL ALTER TABLE Category ADD MaxAge INT NULL;"
    };

    public static async Task InitialiseAsync(IServiceProvider services, ILogger logger)
    {
        const int maxAttempts = 15;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
                await db.Database.EnsureCreatedAsync();
                foreach (var sql in Part3Columns)
                {
                    await db.Database.ExecuteSqlRawAsync(sql);
                }
                logger.LogInformation("Database ready.");
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning("Database not ready (attempt {Attempt}/{Max}): {Message}", attempt, maxAttempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(4));
            }
        }
    }
}
