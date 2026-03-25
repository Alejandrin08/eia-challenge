using System.Text.Json;
using System.Text.Json.Serialization;

namespace Eia.Connector.Models
{
    public record EiaResponse(
        [property: JsonPropertyName("response")] EiaResponseBody Response,
        [property: JsonPropertyName("request")] EiaRequest Request
    );

    public record EiaResponseBody(
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("dateFormat")] string DateFormat,
    [property: JsonPropertyName("frequency")] string Frequency,
    [property: JsonPropertyName("data")] List<NuclearOutageRecord> Data
    );

    public record EiaRequest(
        [property: JsonPropertyName("command")] string Command,
        [property: JsonPropertyName("params")] JsonElement Params
    );
}