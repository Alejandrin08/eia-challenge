using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Eia.Connector.Services
{
    public class CheckpointService(string checkpointPath, ILogger logger)
    {
        public string? LoadLastPeriod()
        {
            if (!File.Exists(checkpointPath)) return null;

            try
            {
                var json = File.ReadAllText(checkpointPath);
                var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("lastPeriod").GetString();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not read checkpoint file — will perform full extraction");
                return null;
            }
        }

        public void Save(string lastPeriod)
        {
            var json = JsonSerializer.Serialize(new { lastPeriod });
            File.WriteAllText(checkpointPath, json);
            logger.LogInformation("Checkpoint updated — lastPeriod={LastPeriod}", lastPeriod);
        }
    }
}