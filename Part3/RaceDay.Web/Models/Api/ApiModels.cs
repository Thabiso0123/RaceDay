namespace RaceDay.Web.Models.Api;

// Plain data shapes exchanged with the RaceDay API. No business logic lives in this project.

public enum EventType { Run, Walk, Cycle }

public class ProfileDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? ContactNumber { get; set; }
    public string Role { get; set; } = "";
    public DateTime DateRegistered { get; set; }
    public string? ProfilePicture { get; set; }
}

public class EventDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateOnly Date { get; set; }
    public string? Location { get; set; }
    public decimal? Distance { get; set; }
    public EventType EventType { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = "";
    public string? BannerImage { get; set; }
}

public class OrganiserEventDto : EventDto
{
    public int CategoryCount { get; set; }
    public int EnrolmentCount { get; set; }
}

public class CategoryDto
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = "";
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? Distance { get; set; }
    public int? MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }
}

public class EnrolmentDto
{
    public int Id { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = "";
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateOnly EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = "";
}

public class ResultDto
{
    public int Id { get; set; }
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = "";
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public DateTime DateRecorded { get; set; }
    public DateOnly EventDate { get; set; }
    public int TotalFinishers { get; set; }
}

// ---- requests ----
public class RegisterRequest
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? ContactNumber { get; set; }
    public string Role { get; set; } = "";
}

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class UpdateProfileRequest
{
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public string? ContactNumber { get; set; }
}

public class EventRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateOnly Date { get; set; }
    public string? Location { get; set; }
    public decimal? Distance { get; set; }
    public EventType EventType { get; set; }
}

public class CategoryRequest
{
    public string Name { get; set; } = "";
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? Distance { get; set; }
    public int? MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }
}

public class EnrolRequest
{
    public int CategoryId { get; set; }
}

public class ResultRequest
{
    public int ParticipantId { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
}

public record MediaFile(byte[] Content, string ContentType);
