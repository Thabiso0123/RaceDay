using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using RaceDay.Api.DTOs;
using static RaceDay.Tests.TestHelpers;

namespace RaceDay.Tests;

public class Part3Tests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;
    public Part3Tests(RaceDayFactory factory) => _factory = factory;

    private static MultipartFormDataContent ImageContent(byte[] bytes, string fileName, string contentType)
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(file, "file", fileName);
        return content;
    }

    private static readonly byte[] PngBytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x00 };

    [Fact]
    public async Task Organiser_CanUploadBanner_AndImageIsServedThroughTheApi()
    {
        var (organiser, _) = await LoginAs(_factory, "Organiser");
        var ev = await CreateEvent(organiser);

        var upload = await organiser.PostAsync($"/api/events/{ev.Id}/banner", ImageContent(PngBytes, "banner.png", "image/png"));
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

        var updated = await upload.Content.ReadFromJsonAsync<EventResponse>(Json);
        Assert.NotNull(updated!.BannerImage);
        Assert.StartsWith("events/", updated.BannerImage);

        var anonymous = _factory.NewClient();
        var image = await anonymous.GetAsync($"/api/files/{updated.BannerImage}");
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task UploadBanner_RejectsFilesThatAreNotImages()
    {
        var (organiser, _) = await LoginAs(_factory, "Organiser");
        var ev = await CreateEvent(organiser);

        var bytes = System.Text.Encoding.UTF8.GetBytes("this is not an image");
        var response = await organiser.PostAsync($"/api/events/{ev.Id}/banner", ImageContent(bytes, "banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_ByAnotherOrganiser_IsForbidden()
    {
        var (owner, _) = await LoginAs(_factory, "Organiser");
        var (other, _) = await LoginAs(_factory, "Organiser");
        var ev = await CreateEvent(owner);

        var response = await other.PostAsync($"/api/events/{ev.Id}/banner", ImageContent(PngBytes, "banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task User_CanUploadOwnProfilePicture()
    {
        var (participant, profile) = await LoginAs(_factory, "Participant");

        var response = await participant.PostAsync($"/api/users/{profile.Id}/picture", ImageContent(PngBytes, "me.png", "image/png"));
        var updated = await response.Content.ReadFromJsonAsync<ProfileResponse>(Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("profiles/", updated!.ProfilePicture);
    }

    [Fact]
    public async Task Participant_SeesOwnEnrolments()
    {
        var (organiser, _) = await LoginAs(_factory, "Organiser");
        var ev = await CreateEvent(organiser);
        var category = await CreateCategory(organiser, ev.Id);
        var (participant, _) = await LoginAs(_factory, "Participant");
        (await participant.PostAsJsonAsync($"/api/events/{ev.Id}/enrolments", new { categoryId = category.Id }))
            .EnsureSuccessStatusCode();

        var response = await participant.GetAsync("/api/me/enrolments");
        var list = await response.Content.ReadFromJsonAsync<List<EnrolmentResponse>>(Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var mine = Assert.Single(list!);
        Assert.Equal(ev.Id, mine.EventId);
        Assert.Equal("Confirmed", mine.Status);
    }

    [Fact]
    public async Task Organiser_CannotUseParticipantOnlyEndpoints()
    {
        var (organiser, _) = await LoginAs(_factory, "Organiser");

        var response = await organiser.GetAsync("/api/me/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_SeesOwnResultWithFinisherCount()
    {
        var (organiser, _) = await LoginAs(_factory, "Organiser");
        var ev = await CreateEvent(organiser);
        var category = await CreateCategory(organiser, ev.Id);
        var (participant, profile) = await LoginAs(_factory, "Participant");
        (await participant.PostAsJsonAsync($"/api/events/{ev.Id}/enrolments", new { categoryId = category.Id }))
            .EnsureSuccessStatusCode();

        var capture = await organiser.PostAsJsonAsync($"/api/events/{ev.Id}/results",
            new { participantId = profile.Id, finishTime = "00:45:10", position = 1 });
        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);

        var response = await participant.GetAsync("/api/me/results");
        var list = await response.Content.ReadFromJsonAsync<List<ResultResponse>>(Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = Assert.Single(list!);
        Assert.Equal(1, result.Position);
        Assert.Equal(1, result.TotalFinishers);
        Assert.Equal(ev.Id, result.EventId);
    }

    [Fact]
    public async Task Organiser_DashboardShowsEnrolmentCounts()
    {
        var (organiser, _) = await LoginAs(_factory, "Organiser");
        var ev = await CreateEvent(organiser);
        var category = await CreateCategory(organiser, ev.Id);
        var (participant, _) = await LoginAs(_factory, "Participant");
        (await participant.PostAsJsonAsync($"/api/events/{ev.Id}/enrolments", new { categoryId = category.Id }))
            .EnsureSuccessStatusCode();

        var response = await organiser.GetAsync("/api/events/mine");
        var list = await response.Content.ReadFromJsonAsync<List<OrganiserEventResponse>>(Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var mine = Assert.Single(list!);
        Assert.Equal(1, mine.EnrolmentCount);
        Assert.Equal(1, mine.CategoryCount);
    }
}
