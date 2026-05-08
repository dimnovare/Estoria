using Estoria.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Estoria.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private const string PrivateRoot = "_private";

    private readonly string _wwwRootPath;
    private readonly string? _publicBaseUrl;

    public LocalFileStorageService(string wwwRootPath, IConfiguration? config = null)
    {
        _wwwRootPath   = wwwRootPath;
        _publicBaseUrl = config?["Storage:PublicBaseUrl"]?.TrimEnd('/');
    }

    // Prepend the configured base URL so the browser running on a different
    // port in dev (Vite:8081) can load files served by the API (5247).
    private string ToPublicUrl(string relPath)
    {
        if (string.IsNullOrEmpty(_publicBaseUrl)) return relPath;
        return $"{_publicBaseUrl}{relPath}";
    }

    public async Task<string> UploadPublicAsync(
        Stream stream, string fileName, string contentType, string folder, CancellationToken ct = default)
    {
        var (relPath, fullPath) = BuildPaths($"uploads/{folder}", fileName);
        await WriteAsync(stream, fullPath, ct);
        return ToPublicUrl(relPath);
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

    public async Task<string> UploadPublicWithKeyAsync(
        string key, Stream stream, string contentType, CancellationToken ct = default)
    {
        // Public-bucket equivalent: drop the file under wwwroot at the exact
        // path the prod CDN would serve. UploadPublicAsync auto-generates a
        // key; this variant lets the image pipeline place variants at a known
        // deterministic prefix.
        var relPath = $"/{key}";
        var fullPath = Path.Combine(_wwwRootPath, NormalizePath(key));

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await WriteAsync(stream, fullPath, ct);

        return ToPublicUrl(relPath);
    }

    public Task<Stream> GetPrivateStreamAsync(string key, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_wwwRootPath, PrivateRoot, NormalizePath(key));
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Private file not found: {key}", fullPath);

        // Read into memory so the caller can seek freely. Property images are
        // <= 10 MB by request limit, so this is fine.
        var bytes = File.ReadAllBytes(fullPath);
        return Task.FromResult<Stream>(new MemoryStream(bytes));
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
