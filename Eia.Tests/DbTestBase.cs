using Eia.Data;
using Eia.Data.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Eia.Tests;

public abstract class DbTestBase : IDisposable
{
    protected readonly SqliteConnection Connection;
    protected readonly AppDbContext Context;

    protected DbTestBase()
    {
        Connection = new SqliteConnection("Filename=:memory:");
        Connection.Open();

        Context = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(Connection)
                .Options);

        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        Connection.Dispose();
    }

    protected async Task<int> SeedRunAsync()
    {
        var run = new ExtractionRun
        {
            ExtractedAt = DateTime.UtcNow,
            Status = "Running",
            RecordCount = 0
        };
        Context.ExtractionRuns.Add(run);
        await Context.SaveChangesAsync();
        return run.Id;
    }

    protected static NuclearOutage MakeOutage(string period, int runId) => new()
    {
        Period = period,
        CapacityMw = 100_000,
        OutageMw = 20_000,
        PercentOutage = 20.0,
        ExtractionRunId = runId
    };
}