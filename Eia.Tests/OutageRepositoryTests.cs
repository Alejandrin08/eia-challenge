using Eia.Data.Entities;
using Eia.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Eia.Tests;

[TestClass]
public class OutageRepositoryTests : DbTestBase
{
    private OutageRepository Repo => new(Context);

    [TestMethod]
    public async Task UpsertOutagesAsync_InsertsNewRecords()
    {
        var runId = await SeedRunAsync();
        var records = new List<NuclearOutage>
        {
            MakeOutage("2026-01-01", runId),
            MakeOutage("2026-01-02", runId)
        };

        var count = await Repo.UpsertOutagesAsync(records, runId);

        Assert.AreEqual(2, count);
        Assert.AreEqual(2, await Context.NuclearOutages.CountAsync());
    }

    [TestMethod]
    public async Task UpsertOutagesAsync_SkipsDuplicatePeriod()
    {
        var runId = await SeedRunAsync();
        var record = new List<NuclearOutage> { MakeOutage("2026-03-25", runId) };

        var first = await Repo.UpsertOutagesAsync(record, runId);
        var second = await Repo.UpsertOutagesAsync(record, runId);

        Assert.AreEqual(1, first);
        Assert.AreEqual(0, second);
        Assert.AreEqual(1, await Context.NuclearOutages.CountAsync());
    }

    [TestMethod]
    public async Task GetOutagesAsync_NoFilters()
    {
        var runId = await SeedRunAsync();
        await Repo.UpsertOutagesAsync(new List<NuclearOutage>
        {
            MakeOutage("2026-01-01", runId),
            MakeOutage("2026-02-01", runId),
            MakeOutage("2026-03-01", runId)
        }, runId);

        var (items, total) = await Repo.GetOutagesAsync(
            null, null, null, null, 1, 50, "period", "desc");

        Assert.AreEqual(3, total);
        Assert.AreEqual(3, items.Count);
    }

    [TestMethod]
    public async Task GetOutagesAsync()
    {
        var runId = await SeedRunAsync();
        var records = Enumerable.Range(1, 10)
            .Select(i => MakeOutage($"2026-01-{i:D2}", runId))
            .ToList();
        await Repo.UpsertOutagesAsync(records, runId);

        var (items, total) = await Repo.GetOutagesAsync(
            null, null, null, null, page: 1, limit: 3, "period", "desc");

        Assert.AreEqual(10, total);
        Assert.AreEqual(3, items.Count);
    }
}