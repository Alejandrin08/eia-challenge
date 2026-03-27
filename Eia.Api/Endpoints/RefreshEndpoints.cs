using Eia.Api.DTOs;
using Eia.Api.Services;

namespace Eia.Api.Endpoints
{
    public static class RefreshEndpoints
    {
        public static void MapRefreshEndpoints(this WebApplication app)
        {
            app.MapPost("/refresh", async (
                                RefreshService refreshService,
                                bool force = false,
                                CancellationToken ct = default) =>
                            {
                                var (status, count, message) = await refreshService.RunAsync(force, ct);

                                if (status == "Error" || status == "Failed")
                                {
                                    return Results.Problem(
                                        detail: message ?? "Error de extracción",
                                        statusCode: 500
                                    );
                                }

                                var dto = new RefreshResultDto(status, count, message);
                                return Results.Ok(new RefreshResultDto(status, count, message));
                            })
                .WithName("RefreshData")
                .WithSummary("Trigger data refresh")
                .WithDescription(
                    "Runs the EIA connector → saves Parquet → upserts into SQLite. " +
                    "Requires Bearer token (POST /auth/login first). " +
                    "Safe to call multiple times — upsert prevents duplicates.")
                .WithTags("Refresh")
                .RequireAuthorization()
                .Produces<RefreshResultDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized);

            app.MapGet("/refresh/history", async (
                    Eia.Data.Repositories.OutageRepository repository,
                    CancellationToken ct) =>
                {
                    var runs = await repository.GetRecentRunsAsync(10, ct);
                    return Results.Ok(runs.Select(r => new
                    {
                        r.Id,
                        r.ExtractedAt,
                        r.RecordCount,
                        r.Status,
                        r.ErrorMessage
                    }));
                })
                .WithName("GetRefreshHistory")
                .WithSummary("Get recent extraction runs")
                .WithTags("Refresh")
                .RequireAuthorization();
        }
    }
}