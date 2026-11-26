using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Infrastructure;

/// <summary>Friendly pre-check so people get a clear message before the upload is sent to the API.</summary>
public static class ImageRules
{
    public const long MaxBytes = 5 * 1024 * 1024;
    private static readonly string[] Extensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    public static string? Check(IFormFile? file)
    {
        if (file is null) return null;
        if (file.Length == 0) return "The selected file is empty.";
        if (file.Length > MaxBytes) return "The image must be 5 MB or smaller.";
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Extensions.Contains(ext)) return "Only JPG, PNG, GIF or WEBP images are allowed.";
        return null;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class ImageFileAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null) return ValidationResult.Success;
        if (value is not IFormFile file) return new ValidationResult("That is not a valid file.");
        var error = ImageRules.Check(file);
        return error is null ? ValidationResult.Success : new ValidationResult(error);
    }
}
