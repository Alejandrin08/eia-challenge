namespace Eia.Api.DTOs
{
    public record OutageDto(
        string Period,
        double? CapacityMw,
        double? OutageMw,
        double? PercentOutage
    );

    public record PagedResponse<T>(
        IEnumerable<T> Data,
        int Page,
        int Limit,
        int Total,
        int TotalPages
    );

    public record RefreshResultDto(
        string Status,
        int RecordsLoaded,
        string Message
    );
}