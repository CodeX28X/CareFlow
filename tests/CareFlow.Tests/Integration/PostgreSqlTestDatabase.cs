using CareFlow.Infrastructure;
using CareFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace CareFlow.Tests.Integration;

[CollectionDefinition(
    "CareFlow PostgreSQL",
    DisableParallelization = true)]
public sealed class PostgreSqlCollection :
    ICollectionFixture<PostgreSqlTestDatabase>
{
}

public sealed class PostgreSqlTestDatabase : IAsyncLifetime
{
    public string ConnectionString { get; }

    public PostgreSqlTestDatabase()
    {
        ConnectionString =
            Environment.GetEnvironmentVariable(
                "CAREFLOW_TEST_DB_CONNECTION") ?? "";

        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException(
                "Integration tests require the " +
                "CAREFLOW_TEST_DB_CONNECTION environment variable. " +
                "Use a dedicated careflow_test database.");
        }

        var builder =
            new NpgsqlConnectionStringBuilder(ConnectionString);

        if (!string.Equals(
            builder.Database,
            "careflow_test",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Integration tests may only connect to careflow_test.");
        }
    }

    public async Task InitializeAsync()
    {
        await using var db = CreateDbContext();

        await db.Database.MigrateAsync();

        int patientCount = await db.Patients.CountAsync();
        int serviceCount =
            await db.DiagnosticServices.CountAsync();

        if (patientCount < 8 || serviceCount != 4)
        {
            throw new InvalidOperationException(
                "The test database does not contain " +
                "the expected seed data.");
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public CareFlowDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<CareFlowDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

        return new CareFlowDbContext(options);
    }

    public ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();

        services.AddCareFlowInfrastructure(ConnectionString);

        return services.BuildServiceProvider();
    }

    public static DateOnly NewTestDate()
    {
        // Far-future test dates avoid normal booking dates.
        return new DateOnly(2050, 1, 1)
            .AddDays(Random.Shared.Next(0, 30000));
    }

    public async Task CleanupDateAsync(DateOnly date)
    {
        await using var db = CreateDbContext();

        await db.BookingServices
            .Where(bs => bs.Booking.BookingDate == date)
            .ExecuteDeleteAsync();

        await db.Bookings
            .Where(b => b.BookingDate == date)
            .ExecuteDeleteAsync();
    }
}