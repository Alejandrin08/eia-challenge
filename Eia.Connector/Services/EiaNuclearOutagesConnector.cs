using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Parquet;
using Parquet.Schema;
using System.Net;
using System.Text.Json;
using Eia.Connector.Models;
using System.Text.Json.Serialization;
using Eia.Connector.Exceptions;
using Eia.Connector.Validation;
using Parquet.Data;

namespace Eia.Connector.Services
{
    public class EiaNuclearOutagesConnector
    {
        private readonly HttpClient _http;
        private readonly ILogger _logger;
        private readonly string _apiKey;
        private readonly CheckpointService _checkpoint;

        private readonly string _baseUrl;
        private readonly int _pageSize;
        private readonly string _dataFields;
        private readonly string _outputPath;

        public EiaNuclearOutagesConnector(
            HttpClient httpClient,
            ILogger<EiaNuclearOutagesConnector> logger,
            IConfiguration config)
        {
            _apiKey = Environment.GetEnvironmentVariable("EIA_API_KEY")
                      ?? throw new InvalidOperationException("EIA_API_KEY environment variable is not set");

            _http = httpClient;
            _logger = logger;

            _baseUrl = config["EiaConnector:BaseUrl"] ?? throw new ArgumentNullException("BaseUrl is missing in config");
            _pageSize = config.GetValue<int>("EiaConnector:PageSize");
            _dataFields = config["EiaConnector:DataFields"] ?? "capacity,outage,percentOutage";

            _outputPath = config["Output:ParquetPath"] ?? "data/nuclear_outages_raw.parquet";
            var checkpointPath = config["Output:CheckpointPath"] ?? "data/checkpoint.json";

            _checkpoint = new CheckpointService(checkpointPath, logger);
        }

        /// <summary>
        /// Fetches nuclear outage records from the EIA API and writes them to a Parquet file.
        /// Supports incremental extraction based on the last saved checkpoint.
        /// </summary>
        /// <returns>Total number of valid records written.</returns>
        public async Task<int> ExtractAsync(CancellationToken ct = default)
        {
            var lastPeriod = _checkpoint.LoadLastPeriod();
            var allRecords = new List<NuclearOutageRecord>();
            var offset = 0;
            var totalAvailable = int.MaxValue; // sentinel value; replaced with actual total on first response
            var skippedCount = 0;

            if (lastPeriod is not null)
                _logger.LogInformation("Incremental mode — fetching records after {LastPeriod}", lastPeriod);
            else
                _logger.LogInformation("Full extraction mode — fetching all records");

            while (offset < totalAvailable)
            {
                _logger.LogInformation("Fetching page offset={Offset} / total={Total}", offset, totalAvailable);

                EiaResponse? response;
                try
                {
                    response = await FetchPageAsync(offset, lastPeriod, ct);
                }
                catch (EiaAuthException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch page at offset={Offset} — stopping extraction", offset);
                    break;
                }

                if (response?.Response?.Data is null || response.Response.Data.Count == 0)
                {
                    _logger.LogInformation("No data returned at offset={Offset} — pagination complete", offset);
                    break;
                }

                if (offset == 0)
                {
                    totalAvailable = response.Response.Total;
                    _logger.LogInformation("Total records available: {Total}", totalAvailable);
                }

                foreach (var record in response.Response.Data)
                {
                    if (NuclearOutageValidator.IsValid(record, _logger))
                        allRecords.Add(record);
                    else
                        skippedCount++;
                }

                offset += response.Response.Data.Count;
            }

            _logger.LogInformation("Extraction complete — valid={Valid} skipped={Skipped}", allRecords.Count, skippedCount);

            if (allRecords.Count == 0)
            {
                _logger.LogWarning("No valid records to save — Parquet file not written");
                return 0;
            }

            await WriteParquetAsync(allRecords, ct);

            // pick the most recent period to persist as the next incremental checkpoint
            var latestPeriod = allRecords
                .Select(r => r.Period)
                .Where(p => p is not null)
                .OrderDescending()
                .FirstOrDefault();

            if (latestPeriod is not null)
                _checkpoint.Save(latestPeriod);

            return allRecords.Count;
        }

        private async Task<EiaResponse?> FetchPageAsync(
            int offset, string? startPeriod, CancellationToken ct)
        {
            var url = $"{_baseUrl}" +
                      $"?api_key={_apiKey}" +
                      $"&data[]=capacity" +
                      $"&data[]=outage" +
                      $"&data[]=percentOutage" +
                      $"&sort[0][column]=period" +
                      $"&sort[0][direction]=desc" +
                      $"&offset={offset}" +
                      $"&length={_pageSize}";

            // append start filter only in incremental mode
            if (startPeriod is not null)
                url += $"&start={startPeriod}";

            var httpResponse = await _http.GetAsync(url, ct);

            if (httpResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                var body = await httpResponse.Content.ReadAsStringAsync(ct);
                throw new EiaAuthException(
                    $"HTTP {(int)httpResponse.StatusCode} — verify your EIA_API_KEY. Body: {body}");
            }

            httpResponse.EnsureSuccessStatusCode();

            var json = await httpResponse.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<EiaResponse>(json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                });
        }

        private async Task WriteParquetAsync(List<NuclearOutageRecord> records, CancellationToken ct)
        {
            var dir = Path.GetDirectoryName(_outputPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var schema = new ParquetSchema(
                new DataField<string>("period"),
                new DataField<double?>("capacity"),
                new DataField<double?>("outage"),
                new DataField<double?>("percentOutage")
            );

            await using var fs = File.Create(_outputPath);
            await using var writer = await ParquetWriter.CreateAsync(schema, fs);
            using var rg = writer.CreateRowGroup();

            await rg.WriteColumnAsync(new DataColumn(
                schema.DataFields[0], records.Select(r => r.Period!).ToArray()));
            await rg.WriteColumnAsync(new DataColumn(
                schema.DataFields[1], records.Select(r => r.Capacity).ToArray()));
            await rg.WriteColumnAsync(new DataColumn(
                schema.DataFields[2], records.Select(r => r.Outage).ToArray()));
            await rg.WriteColumnAsync(new DataColumn(
                schema.DataFields[3], records.Select(r => r.PercentOutage).ToArray()));

            _logger.LogInformation(
                "Parquet written — {Count} rows → {Path}", records.Count, _outputPath);
        }
    }
}