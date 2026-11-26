using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;

namespace RaceDay.Tests;

/// <summary>Runs the real API in memory, using an in-memory database instead of SQL Server.</summary>
public class RaceDayFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.AddDbContext<RaceDayDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        // Creates the schema and the seeded Organiser/Participant roles.
        scope.ServiceProvider.GetRequiredService<RaceDayDbContext>().Database.EnsureCreated();
        return host;
    }

    /// <summary>A client with its own cookie jar, i.e. its own session.</summary>
    public HttpClient NewClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        HandleCookies = true,
        AllowAutoRedirect = false
    });
}

public static class TestHelpers
{
    public const string Password = "Passw0rd!";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static string NewEmail() => $"{Guid.NewGuid():N}@test.com";

    public static Task<HttpResponseMessage> Register(HttpClient client, string email, string role) =>
        client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Test User",
            email,
            password = Password,
            contactNumber = "0821234567",
            role
        });

    /// <summary>Registers a new user with the role, logs in, and returns the logged-in client and profile.</summary>
    public static async Task<(HttpClient Client, ProfileResponse Profile)> LoginAs(RaceDayFactory factory, string role)
    {
        var client = factory.NewClient();
        var email = NewEmail();

        (await Register(client, email, role)).EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();
        var profile = (await login.Content.ReadFromJsonAsync<ProfileResponse>(Json))!;
        return (client, profile);
    }

    public static async Task<EventResponse> CreateEvent(HttpClient organiser, string name = "Test Run")
    {
        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            name,
            description = "Test event",
            date = "2026-12-01",
            location = "Johannesburg",
            distance = 10,
            eventType = "Run"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventResponse>(Json))!;
    }

    public static async Task<CategoryResponse> CreateCategory(HttpClient organiser, int eventId, int? max = null)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            name = "10km",
            minAge = 18,
            maxAge = 39,
            distance = 10,
            maxParticipants = max,
            entryFee = 150
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryResponse>(Json))!;
    }
}
