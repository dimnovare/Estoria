using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Estoria.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Estoria.Infrastructure.Services;

public class R2FileStorageService : IFileStorageService, IDisposable
{
    private readonly AmazonS3Client _client;
    private readonly string _bucket;
    private readonly string _publicUrl;

    public R2FileStorageService(IConfiguration config)
    {
        var r2 = config.GetSection("R2");

        var accountId     = r2["AccountId"]     ?? throw new InvalidOperationException("R2:AccountId is not configured.");
        _bucket           = r2["BucketName"]    ?? throw new InvalidOperationException("R2:BucketName is not configured.");
        _publicUrl        = (r2["PublicUrl"]    ?? throw new InvalidOperationException("R2:PublicUrl is not configured.")).TrimEnd('/');
        var accessKey     = r2["AccessKeyId"]   ?? throw new InvalidOperationException("R2:AccessKeyId is not configured.");
        var secretKey     = r2["SecretAccessKey"] ?? throw new InvalidOperationException("R2:SecretAccessKey is not configured.");

        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        var s3Config    = new AmazonS3Config
        {
            ServiceURL    = $"https://{accountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true
        };

        _client = new AmazonS3Client(credentials, s3Config);
    }

    public async Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default)
    {
        var key = $"{folder}/{Guid.NewGuid()}{Path.GetExtension(fileName).ToLowerInvariant()}";

        var request = new PutObjectRequest
        {
            BucketName      = _bucket,
            Key             = key,
            InputStream     = stream,
            ContentType     = contentType,
            AutoCloseStream = false
        };

        await _client.PutObjectAsync(request, ct);
        return $"{_publicUrl}/{key}";
    }

    public async Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return;

        var key = fileUrl.StartsWith(_publicUrl, StringComparison.OrdinalIgnoreCase)
            ? fileUrl[(_publicUrl.Length + 1)..]   // strip "https://cdn.../key" → "key"
            : fileUrl.TrimStart('/');

        await _client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _bucket,
            Key        = key
        }, ct);
    }

    public void Dispose() => _client.Dispose();
}
