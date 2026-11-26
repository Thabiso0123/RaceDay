using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models.ViewModels;

// ---------- account ----------
public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "That does not look like a valid email address.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "That does not look like a valid email address.")]
    [StringLength(100)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please choose a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Your password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";

    [StringLength(20)]
    [Phone(ErrorMessage = "That does not look like a valid phone number.")]
    [Display(Name = "Contact number")]
    public string? ContactNumber { get; set; }

    [Required(ErrorMessage = "Please tell us how you will use RaceDay.")]
    [RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Please choose Organiser or Participant.")]
    public string Role { get; set; } = "Participant";
}

// ---------- participant ----------
public class HomeViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public HashSet<int> EnrolledEventIds { get; set; } = new();
    public EventType? SelectedType { get; set; }
    public string? Query { get; set; }
}

public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public EnrolmentDto? MyEnrolment { get; set; }
    public bool IsOwner { get; set; }
    public bool CanEnrol { get; set; }
}

public class ProfileUpdateModel
{
    [Required(ErrorMessage = "Please enter your name.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string Name { get; set; } = "";

    [EmailAddress(ErrorMessage = "That does not look like a valid email address.")]
    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(20)]
    [Phone(ErrorMessage = "That does not look like a valid phone number.")]
    [Display(Name = "Contact number")]
    public string? ContactNumber { get; set; }
}

public class ProfilePageViewModel
{
    public ProfileDto Profile { get; set; } = new();
    public ProfileUpdateModel Edit { get; set; } = new();
}

// ---------- organiser ----------
public class DashboardViewModel
{
    public List<OrganiserEventDto> Events { get; set; } = new();
    public int TotalEvents => Events.Count;
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
    public int UpcomingCount { get; set; }
    public OrganiserEventDto? NextEvent { get; set; }
}

public class EventPickerViewModel
{
    public string Title { get; set; } = "";
    public string Intro { get; set; } = "";
    public string Controller { get; set; } = "";
    public string Action { get; set; } = "Index";
    public string ButtonText { get; set; } = "Open";
    public List<OrganiserEventDto> Events { get; set; } = new();
}

public class EventFormViewModel
{
    public int? Id { get; set; }
    public bool IsEdit => Id.HasValue;

    [Required(ErrorMessage = "Please enter the event name.")]
    [StringLength(150)]
    [Display(Name = "Event name")]
    public string Name { get; set; } = "";

    [Display(Name = "Description")]
    [StringLength(4000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the event date.")]
    [DataType(DataType.Date)]
    public DateOnly? Date { get; set; }

    [StringLength(150)]
    public string? Location { get; set; }

    [Range(0, 1000, ErrorMessage = "Distance must be between 0 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? Distance { get; set; }

    [Display(Name = "Event type")]
    public EventType EventType { get; set; } = EventType.Run;

    [ImageFile]
    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    public string? CurrentBanner { get; set; }
}

public class CategoryFormViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Please enter a category name.")]
    [StringLength(100)]
    [Display(Name = "Category name")]
    public string Name { get; set; } = "";

    [Range(0, 120, ErrorMessage = "Age must be between 0 and 120.")]
    [Display(Name = "Minimum age")]
    public int? MinAge { get; set; }

    [Range(0, 120, ErrorMessage = "Age must be between 0 and 120.")]
    [Display(Name = "Maximum age")]
    public int? MaxAge { get; set; }

    [Range(0, 1000, ErrorMessage = "Distance must be between 0 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? Distance { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1.")]
    [Display(Name = "Max participants")]
    public int? MaxParticipants { get; set; }

    [Range(0, 100000, ErrorMessage = "Entry fee must be between R0 and R100 000.")]
    [Display(Name = "Entry fee (R)")]
    public decimal EntryFee { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinAge.HasValue && MaxAge.HasValue && MinAge > MaxAge)
        {
            yield return new ValidationResult("Minimum age cannot be greater than maximum age.",
                new[] { nameof(MinAge), nameof(MaxAge) });
        }
    }
}

public class CategoriesPageViewModel
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public CategoryFormViewModel Form { get; set; } = new();
}

public class EventEnrolmentsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
}

public class ResultRow
{
    public EnrolmentDto Enrolment { get; set; } = new();
    public ResultDto? Result { get; set; }
}

public class ResultsEntryViewModel
{
    public EventDto Event { get; set; } = new();
    public List<ResultRow> Rows { get; set; } = new();
    public bool EventHasFinished { get; set; }
}

public class ResultFormModel
{
    [Range(1, int.MaxValue)] public int EventId { get; set; }
    [Range(1, int.MaxValue)] public int ParticipantId { get; set; }

    [Range(0, 99, ErrorMessage = "Hours must be between 0 and 99.")]
    public int Hours { get; set; }

    [Range(0, 59, ErrorMessage = "Minutes must be between 0 and 59.")]
    public int Minutes { get; set; }

    [Range(0, 59, ErrorMessage = "Seconds must be between 0 and 59.")]
    public int Seconds { get; set; }

    [Range(1, 100000, ErrorMessage = "Finishing position must be 1 or more.")]
    public int Position { get; set; }
}

public class ErrorViewModel
{
    public int Code { get; set; }
    public string Title { get; set; } = "Something went wrong";
    public string Message { get; set; } = "";
}
