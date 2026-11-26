using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using static RaceDay.Tests.TestHelpers;

namespace RaceDay.Tests;

public class EnrolmentTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;
    public EnrolmentTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentLinksParticipantEventAndCategory()
    {
        var (organiser, _) = await LoginAs(_factory, "Organiser");
        var ev = await CreateEvent(organiser);
        var category = await CreateCategory(organiser, ev.Id);
        var (participant, profile) = await LoginAs(_factory, "Participant");

        var response = await participant.PostAsJsonAsync($"/api/events/{ev.Id}/enrolments", new { categoryId = category.Id });
        var enrolment = await response.Content.ReadFromJsonAsync<EnrolmentResponse>(Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(profile.Id, enrolment!.ParticipantId);
        Assert.Equal(ev.Id, enrolment.EventId);
        Assert.Equal(category.Id, enrolment.CategoryId);

        // The link is really stored in the database.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var stored = db.Enrolments.Single(e => e.EnrolmentId == enrolment.Id);
        Assert.Equal(profile.Id, stored.ParticipantId);
        Assert.Equal(category.Id, stored.CategoryId);
        Assert.Equal(ev.Id, db.Categories.Single(c => c.CategoryId == stored.CategoryId).EventId);
    }

    [Fact]
    public async Task Participant_CannotEnrolTwiceInSameEvent_Returns409()
    {
        var (organiser, _) = await LoginAs(_factory, "Organiser");
        var ev = await CreateEvent(organiser);
        var first = await CreateCategory(organiser, ev.Id);
        var second = await CreateCategory(organiser, ev.Id);
        var (participant, _) = await LoginAs(_factory, "Participant");
        await participant.PostAsJsonAsync($"/api/events/{ev.Id}/enrolments", new { categoryId = first.Id });

        // Same event, different category: still a duplicate per the Part 1 plan.
        var response = await participant.PostAsJsonAsync($"/api/events/{ev.Id}/enrolments", new { categoryId = second.Id });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InFullCategory_Returns409()
    {
        var (organiser, _) = await LoginAs(_factory, "Organiser");
        var ev = await CreateEvent(organiser);
        var category = await CreateCategory(organiser, ev.Id, max: 1);
        var (first, _) = await LoginAs(_factory, "Participant");
        var (second, _) = await LoginAs(_factory, "Participant");
        await first.PostAsJsonAsync($"/api/events/{ev.Id}/enrolments", new { categoryId = category.Id });

        var response = await second.PostAsJsonAsync($"/api/events/{ev.Id}/enrolments", new { categoryId = category.Id });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InUnknownEvent_Returns404()
    {
        var (participant, _) = await LoginAs(_factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/events/999999/enrolments", new { categoryId = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithCategoryFromAnotherEvent_Returns404()
    {
        var (organiser, _) = await LoginAs(_factory, "Organiser");
        var eventA = await CreateEvent(organiser, "Event A");
        var eventB = await CreateEvent(organiser, "Event B");
        var categoryOfB = await CreateCategory(organiser, eventB.Id);
        var (participant, _) = await LoginAs(_factory, "Participant");

        var response = await participant.PostAsJsonAsync($"/api/events/{eventA.Id}/enrolments", new { categoryId = categoryOfB.Id });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrol_Returns403()
    {
        var (organiser, _) = await LoginAs(_factory, "Organiser");
        var ev = await CreateEvent(organiser);
        var category = await CreateCategory(organiser, ev.Id);

        var response = await organiser.PostAsJsonAsync($"/api/events/{ev.Id}/enrolments", new { categoryId = category.Id });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithoutLogin_Returns401()
    {
        var response = await _factory.NewClient().PostAsJsonAsync("/api/events/1/enrolments", new { categoryId = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewEnrolmentsForOwnEvent_ButNotAnotherOrganisersEvent()
    {
        var (organiser, _) = await LoginAs(_factory, "Organiser");
        var ev = await CreateEvent(organiser);
        var category = await CreateCategory(organiser, ev.Id);
        var (participant, _) = await LoginAs(_factory, "Participant");
        await participant.PostAsJsonAsync($"/api/events/{ev.Id}/enrolments", new { categoryId = category.Id });
        var (otherOrganiser, _) = await LoginAs(_factory, "Organiser");

        var own = await organiser.GetAsync($"/api/events/{ev.Id}/enrolments");
        var enrolments = await own.Content.ReadFromJsonAsync<List<EnrolmentResponse>>(Json);
        var other = await otherOrganiser.GetAsync($"/api/events/{ev.Id}/enrolments");

        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Single(enrolments!);
        Assert.Equal(HttpStatusCode.Forbidden, other.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrolments_Returns403()
    {
        var (organiser, _) = await LoginAs(_factory, "Organiser");
        var ev = await CreateEvent(organiser);
        var (participant, _) = await LoginAs(_factory, "Participant");

        var response = await participant.GetAsync($"/api/events/{ev.Id}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
