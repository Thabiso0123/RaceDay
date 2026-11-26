using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;
using static RaceDay.Tests.TestHelpers;

namespace RaceDay.Tests;

public class EventTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;
    public EventTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task Events_WithoutLogin_Returns401()
    {
        var response = await _factory.NewClient().GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCreateEvent_Returns201()
    {
        var (organiser, profile) = await LoginAs(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            name = "Joburg 10km",
            description = "City run",
            date = "2026-12-01",
            location = "Johannesburg",
            distance = 10,
            eventType = "Run"
        });
        var created = await response.Content.ReadFromJsonAsync<EventResponse>(Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(profile.Id, created!.OrganiserId);
        Assert.Equal(Api.Models.EventType.Run, created.EventType);
    }

    [Fact]
    public async Task Organiser_CreatingEventWithoutName_Returns400()
    {
        var (organiser, _) = await LoginAs(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", new { date = "2026-12-01", eventType = "Run" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateEvent_Returns403()
    {
        var (participant, _) = await LoginAs(_factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/events", new
        {
            name = "Sneaky Event",
            date = "2026-12-01",
            eventType = "Run"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewEvents_Returns200()
    {
        var (organiser, _) = await LoginAs(_factory, "Organiser");
        await CreateEvent(organiser);
        var (participant, _) = await LoginAs(_factory, "Participant");

        var response = await participant.GetAsync("/api/events");
        var events = await response.Content.ReadFromJsonAsync<List<EventResponse>>(Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(events!);
    }

    [Fact]
    public async Task GetUnknownEvent_Returns404()
    {
        var (participant, _) = await LoginAs(_factory, "Participant");

        var response = await participant.GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        var (organiser, _) = await LoginAs(_factory, "Organiser");
        var ev = await CreateEvent(organiser);

        var response = await organiser.PutAsJsonAsync($"/api/events/{ev.Id}", new
        {
            name = "Renamed Event",
            date = "2026-12-05",
            eventType = "Walk",
            distance = 5
        });
        var updated = await response.Content.ReadFromJsonAsync<EventResponse>(Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Renamed Event", updated!.Name);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent_Returns403()
    {
        var (owner, _) = await LoginAs(_factory, "Organiser");
        var ev = await CreateEvent(owner);
        var (otherOrganiser, _) = await LoginAs(_factory, "Organiser");

        var response = await otherOrganiser.PutAsJsonAsync($"/api/events/{ev.Id}", new
        {
            name = "Hijacked",
            date = "2026-12-05",
            eventType = "Run"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent_ThenItIsGone()
    {
        var (organiser, _) = await LoginAs(_factory, "Organiser");
        var ev = await CreateEvent(organiser);

        var delete = await organiser.DeleteAsync($"/api/events/{ev.Id}");
        var get = await organiser.GetAsync($"/api/events/{ev.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotDeleteEvent_Returns403()
    {
        var (organiser, _) = await LoginAs(_factory, "Organiser");
        var ev = await CreateEvent(organiser);
        var (participant, _) = await LoginAs(_factory, "Participant");

        var response = await participant.DeleteAsync($"/api/events/{ev.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanAddCategory_AndParticipantCanViewIt()
    {
        var (organiser, _) = await LoginAs(_factory, "Organiser");
        var ev = await CreateEvent(organiser);
        await CreateCategory(organiser, ev.Id);
        var (participant, _) = await LoginAs(_factory, "Participant");

        var response = await participant.GetAsync($"/api/events/{ev.Id}/categories");
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>(Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(categories!);
        Assert.Equal(18, categories![0].MinAge);
    }

    [Fact]
    public async Task Organiser_AddingCategoryWithInvalidAgeRange_Returns400()
    {
        var (organiser, _) = await LoginAs(_factory, "Organiser");
        var ev = await CreateEvent(organiser);

        var response = await organiser.PostAsJsonAsync($"/api/events/{ev.Id}/categories",
            new { name = "Bad", minAge = 50, maxAge = 20, distance = 5 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotAddCategory_Returns403()
    {
        var (organiser, _) = await LoginAs(_factory, "Organiser");
        var ev = await CreateEvent(organiser);
        var (participant, _) = await LoginAs(_factory, "Participant");

        var response = await participant.PostAsJsonAsync($"/api/events/{ev.Id}/categories",
            new { name = "Hack", distance = 5 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotAddCategory_Returns403()
    {
        var (owner, _) = await LoginAs(_factory, "Organiser");
        var ev = await CreateEvent(owner);
        var (other, _) = await LoginAs(_factory, "Organiser");

        var response = await other.PostAsJsonAsync($"/api/events/{ev.Id}/categories",
            new { name = "Hack", distance = 5 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Categories_OfUnknownEvent_Returns404()
    {
        var (participant, _) = await LoginAs(_factory, "Participant");

        var response = await participant.GetAsync("/api/events/999999/categories");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
