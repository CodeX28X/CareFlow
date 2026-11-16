
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CareFlow.Infrastructure.Persistence;

public class CareFlowDbContextFactory
    : IDesignTimeDbContextFactory<CareFlowDbContext>
{
    public CareFlowDbContext CreateDbContext(string[] args)
    {
        string? connectionString =
            Environment.GetEnvironmentVariable(
                "CAREFLOW_DB_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "CAREFLOW_DB_CONNECTION is not configured. " +
                "Set the PostgreSQL connection string before " +
                "running EF Core migrations.");
        }

        var options = new DbContextOptionsBuilder<CareFlowDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new CareFlowDbContext(options);
    }
}
