using Microsoft.Extensions.Logging;
using Eia.Connector.Models;

namespace Eia.Connector.Validation
{
    public static class NuclearOutageValidator
    {
        public static bool IsValid(NuclearOutageRecord record, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(record.Period))
            {
                logger.LogWarning("Record skipped — missing 'period'");
                return false;
            }

            if (record.Capacity is null && record.Outage is null)
            {
                logger.LogWarning("Record skipped — no numeric data. Period={P}", record.Period);
                return false;
            }

            return true;
        }
    }
}