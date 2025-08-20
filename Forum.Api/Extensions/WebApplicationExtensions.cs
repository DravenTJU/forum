using Forum.Api.Infrastructure.Database;

namespace Forum.Api.Extensions;

public static class WebApplicationExtensions
{
    public static async Task RunDatabaseMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string not found");
        }

        await DatabaseMigrator.RunMigrationsAsync(connectionString);
    }
}