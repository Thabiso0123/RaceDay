using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.DTOs;

// ---------- Auth / users ----------
public class RegisterRequest
{
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(100)] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8), MaxLength(100)] public string Password { get; set; } = string.Empty;
    [MaxLength(20)] public string? ContactNumber { get; set; }

    [Required] public string Role { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;

    [EmailAddress, MaxLength(100)] public string? Email { get; set; }

    [MaxLength(20)] public string? ContactNumber { get; set; }
}

public class ProfileResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime DateRegistered { get; set; }
    public string? ProfilePicture { get; set; }
}

// ---------- Events ----------
public class EventRequest
{
    [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly Date { get; set; }
    [MaxLength(150)] public string? Location { get; set; }
    [Range(0, 1000)] public decimal? Distance { get; set; }

    public EventType EventType { get; set; }
}

public class EventResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly Date { get; set; }
    public string? Location { get; set; }
    public decimal? Distance { get; set; }
    public EventType EventType { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public string? BannerImage { get; set; }
}

/// <summary>Event plus the numbers shown on the Organiser dashboard.</summary>
public class OrganiserEventResponse : EventResponse
{
    public int CategoryCount { get; set; }
    public int EnrolmentCount { get; set; }
}

// ---------- Categories ----------
public class CategoryRequest : IValidatableObject
{
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [Range(0, 120)] public int? MinAge { get; set; }
    [Range(0, 120)] public int? MaxAge { get; set; }
    [Range(0, 1000)] public decimal? Distance { get; set; }

    [Range(1, int.MaxValue)] public int? MaxParticipants { get; set; }

    [Range(0, 100000)] public decimal EntryFee { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinAge.HasValue && MaxAge.HasValue && MinAge > MaxAge)
        {
            yield return new ValidationResult("MinAge cannot be greater than MaxAge.",
                new[] { nameof(MinAge), nameof(MaxAge) });
        }
    }
}

public class CategoryResponse
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? Distance { get; set; }
    public int? MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }
}

// ---------- Enrolments ----------
public class EnrolRequest
{
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
}

public class EnrolmentResponse
{
    public int Id { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

// ---------- Results ----------
public class ResultRequest
{
    [Range(1, int.MaxValue)] public int ParticipantId { get; set; }

    public TimeSpan FinishTime { get; set; }

    [Range(1, int.MaxValue)] public int Position { get; set; }
}

public class ResultResponse
{
    public int Id { get; set; }
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public DateTime DateRecorded { get; set; }
    public DateOnly EventDate { get; set; }

    /// <summary>Number of finishers with a captured result in the same event (0 when not calculated).</summary>
    public int TotalFinishers { get; set; }
}

// ---------- Mapping helpers (entity -> response) ----------
public static class Mapping
{
    // Needs Role loaded.
    public static ProfileResponse ToResponse(this Participant p) => new()
    {
        Id = p.ParticipantId,
        Name = p.FullName,
        Email = p.Email,
        ContactNumber = p.ContactNumber,
        Role = p.Role.RoleName,
        DateRegistered = p.DateRegistered,
        ProfilePicture = p.ProfilePicture
    };

    // Needs Organiser loaded.
    public static EventResponse ToResponse(this Event e) => new()
    {
        Id = e.EventId,
        Name = e.EventName,
        Description = e.Description,
        Date = e.EventDate,
        Location = e.Location,
        Distance = e.Distance,
        EventType = e.EventType,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser.FullName,
        BannerImage = e.BannerImage
    };

    public static CategoryResponse ToResponse(this Category c) => new()
    {
        Id = c.CategoryId,
        EventId = c.EventId,
        Name = c.CategoryName,
        MinAge = c.MinAge,
        MaxAge = c.MaxAge,
        Distance = c.Distance,
        MaxParticipants = c.MaxParticipants,
        EntryFee = c.EntryFee
    };

    // Needs Participant and Category.Event loaded.
    public static EnrolmentResponse ToResponse(this Enrolment e) => new()
    {
        Id = e.EnrolmentId,
        ParticipantId = e.ParticipantId,
        ParticipantName = e.Participant.FullName,
        EventId = e.Category.EventId,
        EventName = e.Category.Event.EventName,
        EventDate = e.Category.Event.EventDate,
        CategoryId = e.CategoryId,
        CategoryName = e.Category.CategoryName,
        EnrolmentDate = e.EnrolmentDate,
        Status = e.Status
    };

    // Needs Enrolment.Participant and Enrolment.Category.Event loaded.
    public static ResultResponse ToResponse(this Result r) => new()
    {
        Id = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        ParticipantId = r.Enrolment.ParticipantId,
        ParticipantName = r.Enrolment.Participant.FullName,
        EventId = r.Enrolment.Category.EventId,
        EventName = r.Enrolment.Category.Event.EventName,
        CategoryName = r.Enrolment.Category.CategoryName,
        FinishTime = r.FinishTime,
        Position = r.Position,
        DateRecorded = r.DateRecorded,
        EventDate = r.Enrolment.Category.Event.EventDate
    };
}
