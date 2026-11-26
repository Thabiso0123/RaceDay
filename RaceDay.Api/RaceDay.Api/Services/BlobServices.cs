using System.Collections.Concurrent;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace RaceDay.Api.Services;

public class BlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Container { get; set; } = "raceday-images";
}

public class InvalidImageException : Exception
{
    public InvalidImageException(string message) : base(message) { }
}

public class BlobStorageUnavailableException : Exception
{
    public BlobStorageUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}

public record BlobContent(Stream Content, string ContentType);

public interface IBlobStorageService
{
    /// <summary>Validates and stores an image under the folder. Returns the blob name.</summary>
    Task<string> UploadAsync(string folder, IFormFile file, CancellationToken ct = default);

    Task<BlobContent?> OpenReadAsync(string blobName, CancellationToken ct = default);

    Task DeleteAsync(string? blobName, CancellationToken ct = default);
}

public static class ImageValidator
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> Types = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp"
    };

    public static string ContentTypeFor(string extension) =>
        Types.TryGetValue(extension, out var type) ? type : "application/octet-stream";

    /// <summary>Checks size, extension and file signature. Returns the normalised extension.</summary>
    public static async Task<string> ValidateAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            throw new InvalidImageException("Please choose an image file to upload.");
        if (file.Length > MaxBytes)
            throw new InvalidImageException("The image must be 5 MB or smaller.");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !Types.ContainsKey(ext))
            throw new InvalidImageException("Only JPG, PNG, GIF or WEBP images are allowed.");

        var header = new byte[12];
        await using (var stream = file.OpenReadStream())
        {
            var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
            if (!HasValidSignature(header, read, ext))
                throw new InvalidImageException("The file is not a valid image.");
        }

        ext = ext.ToLowerInvariant();
        return ext == ".jpeg" ? ".jpg" : ext;
    }

    private static bool HasValidSignature(byte[] h, int length, string ext)
    {
        if (length < 4) return false;
        return ext.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF,
            ".png" => h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47,
            ".gif" => h[0] == 0x47 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x38,
            ".webp" => length >= 12 && h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46
                       && h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50,
            _ => false
        };
    }
}

/// <summary>Stores images in a private Azure Blob container. Images are served back through the API.</summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobStorageOptions _options;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private BlobContainerClient? _container;

    public AzureBlobStorageService(IOptions<BlobStorageOptions> options, ILogger<AzureBlobStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    private BlobContainerClient Container()
    {
        if (_container is not null) return _container;

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            throw new BlobStorageUnavailableException("Image storage is not configured.");

        var client = new BlobContainerClient(_options.ConnectionString, _options.Container);
        client.CreateIfNotExists(PublicAccessType.None);
        _container = client;
        return client;
    }

    public async Task<string> UploadAsync(string folder, IFormFile file, CancellationToken ct = default)
    {
        var ext = await ImageValidator.ValidateAsync(file);
        var name = $"{folder}/{Guid.NewGuid():N}{ext}";

        try
        {
            var blob = Container().GetBlobClient(name);
            await using var stream = file.OpenReadStream();
            await blob.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = ImageValidator.ContentTypeFor(ext) }
            }, ct);
            return name;
        }
        catch (Exception ex) when (ex is not BlobStorageUnavailableException and not OperationCanceledException)
        {
            _logger.LogError(ex, "Blob upload failed for {Name}", name);
            throw new BlobStorageUnavailableException("Image storage is currently unavailable. Please try again later.", ex);
        }
    }

    public async Task<BlobContent?> OpenReadAsync(string blobName, CancellationToken ct = default)
    {
        try
        {
            var response = await Container().GetBlobClient(blobName).DownloadStreamingAsync(cancellationToken: ct);
            return new BlobContent(response.Value.Content, response.Value.Details.ContentType);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
        catch (Exception ex) when (ex is not BlobStorageUnavailableException and not OperationCanceledException)
        {
            _logger.LogError(ex, "Blob download failed for {Name}", blobName);
            throw new BlobStorageUnavailableException("Image storage is currently unavailable.", ex);
        }
    }

    public async Task DeleteAsync(string? blobName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(blobName)) return;
        try
        {
            await Container().GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Failing to remove an old image must never fail the request.
            _logger.LogWarning(ex, "Could not delete blob {Name}", blobName);
        }
    }
}

/// <summary>Used by the automated tests so they do not need Azure.</summary>
public class InMemoryBlobStorageService : IBlobStorageService
{
    private readonly ConcurrentDictionary<string, (byte[] Data, string ContentType)> _blobs = new();

    public async Task<string> UploadAsync(string folder, IFormFile file, CancellationToken ct = default)
    {
        var ext = await ImageValidator.ValidateAsync(file);
        var name = $"{folder}/{Guid.NewGuid():N}{ext}";
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        _blobs[name] = (ms.ToArray(), ImageValidator.ContentTypeFor(ext));
        return name;
    }

    public Task<BlobContent?> OpenReadAsync(string blobName, CancellationToken ct = default)
    {
        BlobContent? result = _blobs.TryGetValue(blobName, out var blob)
            ? new BlobContent(new MemoryStream(blob.Data), blob.ContentType)
            : null;
        return Task.FromResult(result);
    }

    public Task DeleteAsync(string? blobName, CancellationToken ct = default)
    {
        if (blobName is not null) _blobs.TryRemove(blobName, out _);
        return Task.CompletedTask;
    }
}
