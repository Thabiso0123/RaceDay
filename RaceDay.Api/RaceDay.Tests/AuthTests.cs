using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using static RaceDay.Tests.TestHelpers;

namespace RaceDay.Tests;

public class AuthTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;
    public AuthTests(RaceDayFactory factory) => _factory = factory;

    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task Register_WithValidDetails_Returns201AndChosenRole(string role)
    {
        var response = await Register(_factory.NewClient(), NewEmail(), role);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>(Json);
        Assert.Equal(role, profile!.Role);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var client = _factory.NewClient();
        var email = NewEmail();
        await Register(client, email, "Participant");

        var response = await Register(client, email, "Participant");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithUnknownRole_Returns400()
    {
        var response = await Register(_factory.NewClient(), NewEmail(), "Admin");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithMissingFields_Returns400()
    {
        var response = await _factory.NewClient().PostAsJsonAsync("/api/auth/register", new
        {
            name = "Test User",
            email = NewEmail(),
            password = "short",
            role = "Participant"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotThePlainText()
    {
        var email = NewEmail();
        await Register(_factory.NewClient(), email, "Participant");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var user = db.Participants.Single(p => p.Email == email);

        Assert.NotEqual(Password, user.PasswordHash);
        Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_Returns200AndOpensSession()
    {
        var client = _factory.NewClient();
        var email = NewEmail();
        var registered = await (await Register(client, email, "Organiser")).Content.ReadFromJsonAsync<ProfileResponse>(Json);

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        var profile = await client.GetAsync($"/api/users/{registered!.Id}");

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _factory.NewClient();
        var email = NewEmail();
        await Register(client, email, "Participant");

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_WithoutLogin_Returns401()
    {
        var response = await _factory.NewClient().GetAsync("/api/users/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        var (client, profile) = await LoginAs(_factory, "Participant");

        var logout = await client.PostAsync("/api/auth/logout", null);
        var after = await client.GetAsync($"/api/users/{profile.Id}");

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
    }

    [Fact]
    public async Task User_CanUpdateOwnProfile()
    {
        var (client, profile) = await LoginAs(_factory, "Participant");

        var response = await client.PutAsJsonAsync($"/api/users/{profile.Id}",
            new { name = "New Name", contactNumber = "0831112222" });
        var updated = await response.Content.ReadFromJsonAsync<ProfileResponse>(Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("New Name", updated!.Name);
    }

    [Fact]
    public async Task User_CannotViewOrUpdateAnotherUsersProfile_Returns403()
    {
        var (_, other) = await LoginAs(_factory, "Participant");
        var (client, _) = await LoginAs(_factory, "Participant");

        var get = await client.GetAsync($"/api/users/{other.Id}");
        var put = await client.PutAsJsonAsync($"/api/users/{other.Id}", new { name = "Hijacked" });

        Assert.Equal(HttpStatusCode.Forbidden, get.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);
    }

    [Fact]
    public async Task Profile_OfUnknownUser_Returns404()
    {
        var (client, _) = await LoginAs(_factory, "Participant");

        var response = await client.GetAsync("/api/users/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
