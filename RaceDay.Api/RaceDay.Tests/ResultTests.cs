using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;
using static RaceDay.Tests.TestHelpers;

namespace RaceDay.Tests;

public class ResultTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;
    public ResultTests(RaceDayFactory factory) => _factory = factory;

    private async Task<(HttpClient Organiser, HttpClient Participant, ProfileResponse ParticipantProfile, EventResponse Event)> SetUpEnrolment()
    {
        var (organiser, _) = await LoginAs(_factory, "Organiser");
        var ev = await CreateEvent(organiser);
        var category = await CreateCategory(organiser, ev.Id);
        var (participant, profile) = await LoginAs(_factory, "Participant");
        (await participant.PostAsJsonAsync($"/api/events/{ev.Id}/enrolments", new { categoryId = category.Id }))
            .EnsureSuccessStatusCode();
        return (organiser, participant, profile, ev);
    }

    [Fact]
    public async Task Organiser_CanCaptureResult_AndParticipantCanViewOwnResult()
    {
        var (organiser, participant, profile, ev) = await SetUpEnrolment();

        var capture = await organiser.PostAsJsonAsync($"/api/events/{ev.Id}/results",
            new { participantId = profile.Id, finishTime = "00:52:30", position = 14 });
        var mine = await participant.GetAsync($"/api/events/{ev.Id}/results");
        var result = await mine.Content.ReadFromJsonAsync<ResultResponse>(Json);

        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        Assert.Equal(14, result!.Position);
        Assert.Equal(TimeSpan.Parse("00:52:30"), result.FinishTime);
        Assert.Equal(profile.Id, result.ParticipantId);
    }

    [Fact]
    public async Task Participant_WithNoResultYet_Gets404()
    {
        var (_, participant, _, ev) = await SetUpEnrolment();

        var response = await participant.GetAsync($"/api/events/{ev.Id}/results");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCaptureResult_Returns403()
    {
        var (_, participant, profile, ev) = await SetUpEnrolment();

        var response = await participant.PostAsJsonAsync($"/api/events/{ev.Id}/results",
            new { participantId = profile.Id, finishTime = "00:40:00", position = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUseParticipantResultsView_Returns403()
    {
        var (organiser, _, _, ev) = await SetUpEnrolment();

        var response = await organiser.GetAsync($"/api/events/{ev.Id}/results");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotCaptureResultTwice_Returns409()
    {
        var (organiser, _, profile, ev) = await SetUpEnrolment();
        var url = $"/api/events/{ev.Id}/results";
        await organiser.PostAsJsonAsync(url, new { participantId = profile.Id, finishTime = "00:50:00", position = 3 });

        var response = await organiser.PostAsJsonAsync(url,
            new { participantId = profile.Id, finishTime = "00:49:00", position = 2 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CapturingResultForNonEnrolledParticipant_Returns404()
    {
        var (organiser, _, _, ev) = await SetUpEnrolment();
        var (_, stranger) = await LoginAs(_factory, "Participant");

        var response = await organiser.PostAsJsonAsync($"/api/events/{ev.Id}/results",
            new { participantId = stranger.Id, finishTime = "00:50:00", position = 3 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotCaptureResult_Returns403()
    {
        var (_, _, profile, ev) = await SetUpEnrolment();
        var (otherOrganiser, _) = await LoginAs(_factory, "Organiser");

        var response = await otherOrganiser.PostAsJsonAsync($"/api/events/{ev.Id}/results",
            new { participantId = profile.Id, finishTime = "00:50:00", position = 3 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
