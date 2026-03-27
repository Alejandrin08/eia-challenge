using Eia.Data.Repositories;
using Eia.Data.Services;

namespace Eia.Api.Services
{
    public class RefreshService(
        OutageRepository repository,
        ParquetReaderService parquetReader,
        IConfiguration config,
        ILogger<RefreshService> logger)
    {
        /// <summary>
        /// Orchestrates the full data refresh pipeline: connector → Parquet → SQLite.
        /// If <c>force</c> is true, re-downloads from the EIA API even if a Parquet file already exists.
        /// </summary>
        /// <returns>A tuple with the final status, number of records loaded, and a descriptive message.</returns>
        public async Task<(string Status, int Count, string Message)> RunAsync(
             bool force = false,
             CancellationToken ct = default)
        {
            var runId = await repository.BeginExtractionRunAsync(ct);

            try
            {
                var parquetPath = config["Output:ParquetPath"]
                    ?? "../Eia.Connector/data/nuclear_outages_raw.parquet";

                var parquetExists = File.Exists(parquetPath);

                if (!parquetExists || force)
                {
                    logger.LogInformation(
                        parquetExists
                            ? "force=true — re-downloading from EIA API"
                            : "Parquet not found — running connector to download data");

                    var result = await RunConnectorAsync(ct);
                    if (!result.success)
                    {
                        await repository.CompleteExtractionRunAsync(
                            runId, 0, "Failed", result.error, ct);
                        return ("Failed", 0, result.error);
                    }
                }
                else
                {
                    logger.LogInformation(
                        "Parquet found at {Path} — loading into SQLite (skipping API call)",
                        parquetPath);
                }

                var records = await parquetReader.ReadAsync(parquetPath, ct);

                if (records.Count == 0)
                {
                    await repository.CompleteExtractionRunAsync(
                        runId, 0, "Success", "No records in Parquet", ct);
                    return ("Success", 0, "Parquet was empty — no records loaded");
                }

                var saved = await repository.UpsertOutagesAsync(records, runId, ct);

                await repository.CompleteExtractionRunAsync(runId, saved, "Success", null, ct);

                return ("Success", saved,
                    $"{saved} records loaded from Parquet into SQLite");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Refresh failed");
                await repository.CompleteExtractionRunAsync(runId, 0, "Failed", ex.Message, ct);
                return ("Failed", 0, ex.Message);
            }
        }

        /// <summary>
        /// Launches the EIA connector as a subprocess and waits for it to complete.
        /// Supports both local (<c>dotnet run</c>) and Docker (<c>dotnet dll</c>) execution paths.
        /// </summary>
        private async Task<(bool success, string error)> RunConnectorAsync(CancellationToken ct)
        {
            var connectorPath = config["Connector:ProjectPath"]
                ?? "../Eia.Connector/Eia.Connector.csproj";

            var apiKey = config["EIA_API_KEY"] ?? "";

            if (string.IsNullOrWhiteSpace(apiKey))
                return (false, "EIA_API_KEY not set in the API configuration.");

            logger.LogInformation("Launching connector subprocess...");

            var isDocker = connectorPath.EndsWith(".dll");

            var arguments = isDocker
                ? $"\"{connectorPath}\""
                : $"run --project \"{connectorPath}\"";

            var connectorDir = isDocker
                ? Path.GetDirectoryName(connectorPath)
                : Path.GetFullPath(Path.GetDirectoryName(connectorPath) ?? "../Eia.Connector");

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                WorkingDirectory = connectorDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            psi.Environment["EIA_API_KEY"] = apiKey;

            var process = new System.Diagnostics.Process { StartInfo = psi };
            process.Start();

            var stdout = await process.StandardOutput.ReadToEndAsync(ct);
            var stderr = await process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
            {
                logger.LogError("Connector exited {Code}:\nOut: {Out}\nErr: {Err}",
                    process.ExitCode, stdout, stderr);
                return (false, $"Exit {process.ExitCode}. Detalle: {stdout} {stderr}");
            }

            logger.LogInformation("Connector output:\n{Out}", stdout);
            return (true, string.Empty);
        }
    }
}