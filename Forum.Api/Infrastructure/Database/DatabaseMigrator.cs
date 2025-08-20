using DbUp;
using MySqlConnector;
using System.Reflection;

namespace Forum.Api.Infrastructure.Database;

public static class DatabaseMigrator
{
    public static async Task RunMigrationsAsync(string connectionString)
    {
        // 确保数据库存在
        await EnsureDatabaseExistsAsync(connectionString);

        // 运行迁移
        var upgrader = DeployChanges.To
            .MySqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            throw new Exception($"Database migration failed: {result.Error}");
        }

        Console.WriteLine("Database migration completed successfully!");
    }

    private static async Task EnsureDatabaseExistsAsync(string connectionString)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database;
        builder.Database = "";

        await using var connection = new MySqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE IF NOT EXISTS `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;";
        await command.ExecuteNonQueryAsync();
    }
}