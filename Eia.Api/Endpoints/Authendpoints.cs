using Eia.Api.Services;
using Eia.Data.Repositories;
using Eia.Api.DTOs;
using System.Security.Claims;

namespace Eia.Api.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            app.MapPost("/auth/login", async (
                    LoginRequest request,
                    UserRepository users,
                    JwtService jwt,
                    IConfiguration config,
                    CancellationToken ct) =>
                {
                    if (string.IsNullOrWhiteSpace(request.Email) ||
                        string.IsNullOrWhiteSpace(request.Password))
                        return Results.BadRequest(new { error = "Email and password are required" });

                    var user = await users.FindByEmailAsync(request.Email, ct);

                    if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                        return Results.Unauthorized();

                    var expiryHours = config.GetValue<int>("Jwt:ExpiryHours", 8);
                    var token = jwt.GenerateToken(user);

                    return Results.Ok(new LoginResponse(
                        Token: token,
                        Email: user.Email,
                        Role: user.Role,
                        ExpiresAt: DateTime.UtcNow.AddHours(expiryHours)));
                })
                .WithName("Login")
                .WithSummary("Authenticate and receive JWT token")
                .WithDescription(
                    "Returns a Bearer token valid for the configured number of hours. " +
                    "Use it as: Authorization: Bearer <token>")
                .WithTags("Auth")
                .AllowAnonymous()
                .Produces<LoginResponse>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized);
        }
    }
}