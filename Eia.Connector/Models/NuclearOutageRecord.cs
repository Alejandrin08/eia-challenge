using System.Text.Json.Serialization;

namespace Eia.Connector.Models
{
    public record NuclearOutageRecord
    {
        [JsonPropertyName("period")] public string? Period { get; init; }
        [JsonPropertyName("capacity")] public double? Capacity { get; init; }
        [JsonPropertyName("outage")] public double? Outage { get; init; }
        [JsonPropertyName("percentOutage")] public double? PercentOutage { get; init; }
    }
}