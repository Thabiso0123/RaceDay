using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Infrastructure;

/// <summary>Formatting helpers used by the views.</summary>
public static class Display
{
    public static string Ordinal(int n)
    {
        var mod100 = n % 100;
        if (mod100 is >= 11 and <= 13) return $"{n}th";
        return (n % 10) switch { 1 => $"{n}st", 2 => $"{n}nd", 3 => $"{n}rd", _ => $"{n}th" };
    }

    public static string FinishTime(TimeSpan? t) =>
        t is null ? "-" : $"{(int)t.Value.TotalHours}:{t.Value.Minutes:00}:{t.Value.Seconds:00}";

    public static string Km(decimal? d) => d is null ? "-" : $"{d.Value:0.##} km";

    public static string Date(DateOnly d) => d.ToString("ddd, d MMM yyyy");

    public static string Ages(int? min, int? max) =>
        (min, max) switch
        {
            (null, null) => "All ages",
            (not null, null) => $"{min}+",
            (null, not null) => $"Up to {max}",
            _ => $"{min}-{max}"
        };

    public static string Money(decimal d) => d == 0 ? "Free" : $"R {d:0.00}";

    public static string StatusClass(string? status) => (status ?? "").ToLowerInvariant() switch
    {
        "confirmed" => "status-confirmed",
        "pending" => "status-pending",
        "cancelled" or "canceled" => "status-cancelled",
        _ => "status-default"
    };

    public static string TypeIcon(EventType type) => type switch
    {
        EventType.Run => "bi-lightning-charge-fill",
        EventType.Walk => "bi-person-walking",
        EventType.Cycle => "bi-bicycle",
        _ => "bi-flag-fill"
    };

    public static string Media(string? path, string placeholder = "~/img/placeholder-event.svg") =>
        string.IsNullOrEmpty(path) ? placeholder : "~/media/" + path;
}
