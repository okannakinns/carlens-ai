using Carlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Carlens.IntegrationTests;

public sealed class PostgreSqlMigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder(
            "postgres:18-alpine")
        .WithDatabase("carlens_tests")
        .WithUsername("carlens")
        .WithPassword("carlens-integration-tests")
        .Build();

    public Task InitializeAsync()
    {
        return _postgres.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _postgres.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task All_migrations_apply_to_a_clean_database()
    {
        var options = new DbContextOptionsBuilder<CarlensDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using var context = new CarlensDbContext(options);

        await context.Database.MigrateAsync();

        var appliedMigrations = await context.Database
            .GetAppliedMigrationsAsync();
        var pendingMigrations = await context.Database
            .GetPendingMigrationsAsync();

        Assert.NotEmpty(appliedMigrations);
        Assert.Empty(pendingMigrations);
        Assert.Equal(0, await context.CarListings.CountAsync());
    }
}
