using Estoria.Application.Interfaces;

namespace Estoria.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private const string PrivateRoot = "_private";

    private readonly string _wwwRootPath;

    public LocalFileStorageService(string wwwRootPath) => _wwwRootPath = wwwRootPath;

    public async Task<string> UploadPublicAsync(
        Stream stream, string fileName, string contentType, string folder, CancellationToken ct = default)
    {
        var (relPath, fullPath) = BuildPaths($"uploads/{folder}", fileName);
        await WriteAsync(stream, fullPath, ct);
        return relPath;
    }

    public async Task<string> UploadPrivateAsync(
        Stream stream, string fileName, string contentType, string folder, CancellationToken ct = default)
    {
        // In dev, "private" files live under _private/<folder>/<id>.<ext> in wwwroot.
        // The returned key is the path relative to _private/, mirroring the prod
        // behavior (key is stable, never directly URL-addressable in prod).
        var key = $"{folder}/{Guid.NewGuid():N}{Path.GetExtension(fileName).ToLowerInvariant()}";
        var fullPath = Path.Combine(_wwwRootPath, PrivateRoot, NormalizePath(key));

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await WriteAsync(stream, fullPath, ct);

        return key;
    }

    public Task<string> GetPresignedUrlAsync(
        string key, TimeSpan validFor, CancellationToken ct = default)
    {
        // Dev shortcut: serve via static files under /_private/<key>. Not actually
        // signed — dev is single-user and this URL never leaves localhost.
        return Task.FromResult($"/{PrivateRoot}/{key}");
    }

    public Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return Task.CompletedTask;

        // fileUrl is like "/uploads/properties/abc.jpg"
        var fullPath = Path.Combine(_wwwRootPath, NormalizePath(fileUrl.TrimStart('/')));

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    public Task DeletePrivateAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return Task.CompletedTask;

        var fullPath = Path.Combine(_wwwRootPath, PrivateRoot, NormalizePath(key));

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    private (string relPath, string fullPath) BuildPaths(string folder, string fileName)
    {
        var ext      = Path.GetExtension(fileName).ToLowerInvariant();
        var fileId   = Guid.NewGuid().ToString("N");
        var relPath  = $"/{folder}/{fileId}{ext}";
        var fullDir  = Path.Combine(_wwwRootPath, NormalizePath(folder));
        var fullPath = Path.Combine(fullDir, $"{fileId}{ext}");

        Directory.CreateDirectory(fullDir);
        return (relPath, fullPath);
    }

    private static async Task WriteAsync(Stream stream, string fullPath, CancellationToken ct)
    {
        await using var fs = File.Create(fullPath);
        await stream.CopyToAsync(fs, ct);
    }

    private static string NormalizePath(string path)
        => path.Replace('/', Path.DirectorySeparatorChar);
}
