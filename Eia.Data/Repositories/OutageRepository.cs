using Eia.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eia.Data.Repositories
{
    public class OutageRepository(AppDbContext dbContext)
    {
        private readonly AppDbContext _dbContext = dbContext;

        /// <summary>
        /// Returns a paginated, filtered, and sorted list of nuclear outage records.
        /// </summary>
        /// <returns>A tuple with the records for the requested page and the total matching count.</returns>
        public async Task<(List<NuclearOutage> items, int total)> GetOutagesAsync(
            string? dateFrom,
            string? dateTo,
            double? minOutage,
            double? maxOutage,
            int page,
            int limit,
            string sortBy,
            string sortDir,
            CancellationToken ct = default)
        {
            var query = _dbContext.NuclearOutages.AsQueryable();

            if (!string.IsNullOrWhiteSpace(dateFrom))
                query = query.Where(x => string.Compare(x.Period, dateFrom) >= 0);

            if (!string.IsNullOrWhiteSpace(dateTo))
                query = query.Where(x => string.Compare(x.Period, dateTo) <= 0);

            if (minOutage.HasValue)
                query = query.Where(x => x.OutageMw >= minOutage.Value);

            if (maxOutage.HasValue)
                query = query.Where(x => x.OutageMw <= maxOutage.Value);

            var total = await query.CountAsync(ct);

            var isDesc = sortDir.ToLower() == "desc";

            query = sortBy.ToLower() switch
            {
                "capacity" => isDesc ? query.OrderByDescending(x => x.CapacityMw) : query.OrderBy(x => x.CapacityMw),
                "outage" => isDesc ? query.OrderByDescending(x => x.OutageMw) : query.OrderBy(x => x.OutageMw),
                "percent" => isDesc ? query.OrderByDescending(x => x.PercentOutage) : query.OrderBy(x => x.PercentOutage),
                _ => isDesc ? query.OrderByDescending(x => x.Period) : query.OrderBy(x => x.Period)
            };

            var items = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task<int> BeginExtractionRunAsync(CancellationToken ct = default)
        {
            var run = new ExtractionRun
            {
                ExtractedAt = DateTime.UtcNow,
                Status = "Running",
                RecordCount = 0
            };

            _dbContext.ExtractionRuns.Add(run);
            await _dbContext.SaveChangesAsync(ct);
            return run.Id;
        }

        public async Task CompleteExtractionRunAsync(
            int runId, int count, string status, string? errorMessage, CancellationToken ct = default)
        {
            var run = await _dbContext.ExtractionRuns.FindAsync(new object[] { runId }, ct);
            if (run is null) return;

            run.Status = status;
            run.RecordCount = count;
            run.ErrorMessage = errorMessage;

            await _dbContext.SaveChangesAsync(ct);
        }

        /// <summary>
        /// Inserts records that do not yet exist in the database, grouped under the given extraction run.
        /// Skips duplicates based on <c>Period</c>. Runs inside a transaction.
        /// </summary>
        /// <returns>Number of newly inserted records.</returns>
        public async Task<int> UpsertOutagesAsync(List<NuclearOutage> records, int runId, CancellationToken ct = default)
        {
            int savedCount = 0;

            using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

            foreach (var record in records)
            {
                var exists = await _dbContext.NuclearOutages
                    .AnyAsync(x => x.Period == record.Period, ct);

                if (!exists)
                {
                    record.ExtractionRunId = runId;
                    _dbContext.NuclearOutages.Add(record);
                    savedCount++;
                }
            }

            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return savedCount;
        }

        public async Task<List<ExtractionRun>> GetRecentRunsAsync(int limit = 10, CancellationToken ct = default)
        {
            return await _dbContext.ExtractionRuns
                .OrderByDescending(r => r.ExtractedAt)
                .Take(limit)
                .ToListAsync(ct);
        }
    }
}