using Estoria.Application.Interfaces;

namespace Estoria.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _wwwRootPath;

    public LocalFileStorageService(string wwwRootPath) => _wwwRootPath = wwwRootPath;

    public async Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default)
    {
        var ext      = Path.GetExtension(fileName).ToLowerInvariant();
        var fileId   = Guid.NewGuid().ToString("N");
        var relPath  = $"/uploads/{folder}/{fileId}{ext}";
        var fullDir  = Path.Combine(_wwwRootPath, "uploads", folder);
        var fullPath = Path.Combine(fullDir, $"{fileId}{ext}");

        Directory.CreateDirectory(fullDir);

        await using var fs = File.Create(fullPath);
        await stream.CopyToAsync(fs, ct);

        return relPath;
    }

    public Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return Task.CompletedTask;

        // fileUrl is like "/uploads/properties/abc.jpg"
        var relativePart = fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath     = Path.Combine(_wwwRootPath, relativePart);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }
}
