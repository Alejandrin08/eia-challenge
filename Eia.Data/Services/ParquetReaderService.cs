using Eia.Data.Entities;
using Microsoft.Extensions.Logging;
using Parquet;
using Parquet.Schema;

namespace Eia.Data.Services
{
    public class ParquetReaderService(ILogger<ParquetReaderService> logger)
    {
        public async Task<List<NuclearOutage>> ReadAsync(
            string parquetPath, CancellationToken ct = default)
        {
            if (!File.Exists(parquetPath))
            {
                logger.LogWarning("Parquet file not found at {Path}", parquetPath);
                return [];
            }

            try
            {
                await using var fs = File.OpenRead(parquetPath);
                using var reader = await ParquetReader.CreateAsync(fs);
                using var rowGroup = reader.OpenRowGroupReader(0);

                var periodCol = await ReadColumnAsync<string>(rowGroup, reader.Schema, "period");
                var capCol = await ReadColumnAsync<double?>(rowGroup, reader.Schema, "capacity");
                var outageCol = await ReadColumnAsync<double?>(rowGroup, reader.Schema, "outage");
                var pctCol = await ReadColumnAsync<double?>(rowGroup, reader.Schema, "percentOutage");

                var results = new List<NuclearOutage>();

                for (var i = 0; i < periodCol.Length; i++)
                {
                    var period = periodCol[i];
                    if (string.IsNullOrWhiteSpace(period)) continue;

                    results.Add(new NuclearOutage
                    {
                        Period = period,
                        CapacityMw = capCol.Length > i ? capCol[i] : null,
                        OutageMw = outageCol.Length > i ? outageCol[i] : null,
                        PercentOutage = pctCol.Length > i ? pctCol[i] : null
                    });
                }

                logger.LogInformation(
                    "Parquet read — {Count} rows from {Path}", results.Count, parquetPath);

                return results;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read Parquet file at {Path}", parquetPath);
                return [];
            }
        }

        /// <summary>
        /// Reads a typed column from a Parquet row group by field name.
        /// Returns an empty array if the field is not found in the schema.
        /// </summary>
        private static async Task<T[]> ReadColumnAsync<T>(
              ParquetRowGroupReader rowGroup, ParquetSchema schema, string fieldName)
        {
            var field = schema.DataFields.FirstOrDefault(f => f.Name == fieldName);
            if (field is null) return [];

            var col = await rowGroup.ReadColumnAsync(field);
            return col.Data as T[] ?? [];
        }
    }
}