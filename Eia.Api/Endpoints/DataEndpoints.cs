using Eia.Api.DTOs;
using Eia.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Eia.Api.Endpoints
{
    public static class DataEndpoints
    {
        public static void MapDataEndpoints(this WebApplication app)
        {
            app.MapGet("/data", async (
                    [FromQuery] int page = 1,
                    [FromQuery] int limit = 50,
                    [FromQuery] string? dateFrom = null,
                    [FromQuery] string? dateTo = null,
                    [FromQuery] string sortBy = "period",
                    [FromQuery] string sortDir = "desc",
                    [FromQuery] double? minOutage = null,
                    [FromQuery] double? maxOutage = null,
                    OutageRepository repository = default!,
                    CancellationToken ct = default) =>
                {
                    if (page < 1)
                        return Results.BadRequest(new { error = "page must be >= 1" });

                    if (limit is < 1 or > 500)
                        return Results.BadRequest(new { error = "limit must be between 1 and 500" });

                    var validSortFields = new[] { "period", "capacity", "outage", "percent" };
                    if (!validSortFields.Contains(sortBy.ToLower()))
                        return Results.BadRequest(new
                        {
                            error = $"sortBy must be one of: {string.Join(", ", validSortFields)}"
                        });

                    if (sortDir.ToLower() is not "asc" and not "desc")
                        return Results.BadRequest(new { error = "sortDir must be 'asc' or 'desc'" });

                    var (items, total) = await repository.GetOutagesAsync(
                        dateFrom, dateTo, minOutage, maxOutage,
                        page, limit, sortBy, sortDir, ct);

                    var dtos = items.Select(o => new OutageDto(
                        o.Period,
                        o.CapacityMw,
                        o.OutageMw,
                        o.PercentOutage));

                    var totalPages = (int)Math.Ceiling((double)total / limit);

                    return Results.Ok(new PagedResponse<OutageDto>(
                        Data: dtos,
                        Page: page,
                        Limit: limit,
                        Total: total,
                        TotalPages: totalPages));
                })
                .RequireAuthorization()
                .WithName("GetOutages")
                .WithSummary("Get nuclear outage records")
                .WithDescription(
                    "Returns paginated U.S. national nuclear outage totals. " +
                    "Requires Bearer token from POST /auth/login. " +
                    "Filter by date range, outage thresholds and sort by any numeric field.")
                .WithTags("Outages")
                .Produces<PagedResponse<OutageDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized);
        }
    }
}