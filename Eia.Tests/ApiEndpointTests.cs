using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Eia.Api.DTOs;
using Eia.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Eia.Tests;

[TestClass]
public class ApiEndpointTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiEndpointTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(o =>
                        o.UseSqlite(_connection));
                }));

        _client = _factory.CreateClient();
    }

    private async Task<string> GetTokenAsync()
    {
        var res = await _client.PostAsJsonAsync("/auth/login", new
        {
            email = "admin@eia.local",
            password = "Admin1234!"
        });
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString()!;
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _connection.Dispose();
    }

    [TestMethod]
    public async Task GET_Data_WithoutToken()
    {
        var res = await _client.GetAsync("/data");
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task GET_Data_WithToken_WhenDbIsEmpty()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await _client.GetAsync("/data");
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<PagedResponse<OutageDto>>();
        Assert.IsNotNull(body);
        Assert.AreEqual(0, body.Total);
    }

    [TestMethod]
    public async Task POST_Login_WithValidCredentials()
    {
        var res = await _client.PostAsJsonAsync("/auth/login", new
        {
            email = "admin@eia.local",
            password = "Admin1234!"
        });

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.IsTrue(body.TryGetProperty("token", out var token));
        Assert.IsFalse(string.IsNullOrEmpty(token.GetString()));
    }

    [TestMethod]
    public async Task POST_Login_WithWrongPassword()
    {
        var res = await _client.PostAsJsonAsync("/auth/login", new
        {
            email = "admin@eia.local",
            password = "123"
        });

        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task POST_Login_WhenEmailIsEmpty()
    {
        var res = await _client.PostAsJsonAsync("/auth/login", new
        {
            email = "",
            password = "Admin1234!"
        });

        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [TestMethod]
    public async Task POST_Refresh_WithoutToken()
    {
        var res = await _client.PostAsync("/refresh", null);
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [TestMethod]
    public async Task POST_Refresh_WithValidToken()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await _client.PostAsync("/refresh", null);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.IsTrue(body.TryGetProperty("status", out _));
    }
}