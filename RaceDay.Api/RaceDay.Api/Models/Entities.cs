using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public enum EventType
{
    Run,
    Walk,
    Cycle
}

public class Role
{
    public int RoleId { get; set; }

    [Required, MaxLength(50)]
    public string RoleName { get; set; } = string.Empty;

    public ICollection<Participant> Participants { get; set; } = new List<Participant>();
}


public class Participant
{
    public int ParticipantId { get; set; }

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? ContactNumber { get; set; }

    public int RoleId { get; set; }
    public DateTime DateRegistered { get; set; }

    /// <summary>Blob name of the profile picture in Azure Blob Storage.</summary>
    [MaxLength(300)]
    public string? ProfilePicture { get; set; }

    public Role Role { get; set; } = null!;
    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

public class Event
{
    public int EventId { get; set; }

    [Required, MaxLength(150)]
    public string EventName { get; set; } = string.Empty;

    public DateOnly EventDate { get; set; }

    [MaxLength(150)]
    public string? Location { get; set; }

    public string? Description { get; set; }

    public EventType EventType { get; set; }

    public decimal? Distance { get; set; }

    public int OrganiserId { get; set; }
    public DateTime CreatedDate { get; set; }

    /// <summary>Blob name of the banner image in Azure Blob Storage.</summary>
    [MaxLength(300)]
    public string? BannerImage { get; set; }

    public Participant Organiser { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}

public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }

    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? Distance { get; set; }
    public int? MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }

    public Event Event { get; set; } = null!;
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int CategoryId { get; set; }
    public DateTime EnrolmentDate { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Confirmed";

    public Participant Participant { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Result? Result { get; set; }
}

public class Result
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public DateTime DateRecorded { get; set; }

    public Enrolment Enrolment { get; set; } = null!;
}
