using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Estoria.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Estoria.Infrastructure.Services;

public class R2FileStorageService : IFileStorageService, IDisposable
{
    private readonly AmazonS3Client _client;
    private readonly string _publicBucket;
    private readonly string _privateBucket;
    private readonly string _publicCdnUrl;

    public R2FileStorageService(IConfiguration config)
    {
        var s = config.GetSection("Storage");

        var accountId      = Required(s, "AccountId");
        _publicBucket      = Required(s, "PublicBucket");
        _privateBucket     = Required(s, "PrivateBucket");
        _publicCdnUrl      = Required(s, "PublicCdnUrl").TrimEnd('/');
        var accessKey      = Required(s, "AccessKeyId");
        var secretKey      = Required(s, "SecretAccessKey");

        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        var s3Config    = new AmazonS3Config
        {
            ServiceURL    = $"https://{accountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true
        };

        _client = new AmazonS3Client(credentials, s3Config);
    }

    public async Task<string> UploadPublicAsync(
        Stream stream, string fileName, string contentType, string folder, CancellationToken ct = default)
    {
        var key = BuildKey(folder, fileName);
        await PutAsync(_publicBucket, key, stream, contentType, ct);
        return $"{_publicCdnUrl}/{key}";
    }

    public async Task<string> UploadPrivateAsync(
        Stream stream, string fileName, string contentType, string folder, CancellationToken ct = default)
    {
        var key = BuildKey(folder, fileName);
        await PutAsync(_privateBucket, key, stream, contentType, ct);
        return key;
    }

    public Task<string> GetPresignedUrlAsync(
        string key, TimeSpan validFor, CancellationToken ct = default)
    {
        var url = _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _privateBucket,
            Key        = key,
            Expires    = DateTime.UtcNow.Add(validFor),
            Verb       = HttpVerb.GET,
        });
        return Task.FromResult(url);
    }

    public async Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return;

        var key = fileUrl.StartsWith(_publicCdnUrl, StringComparison.OrdinalIgnoreCase)
            ? fileUrl[(_publicCdnUrl.Length + 1)..]   // strip "https://cdn.../key" → "key"
            : fileUrl.TrimStart('/');

        await _client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _publicBucket,
            Key        = key
        }, ct);
    }

    public async Task DeletePrivateAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        await _client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _privateBucket,
            Key        = key
        }, ct);
    }

    public void Dispose() => _client.Dispose();

    private static string BuildKey(string folder, string fileName)
        => $"{folder}/{Guid.NewGuid()}{Path.GetExtension(fileName).ToLowerInvariant()}";

    private async Task PutAsync(string bucket, string key, Stream stream, string contentType, CancellationToken ct)
    {
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName      = bucket,
            Key             = key,
            InputStream     = stream,
            ContentType     = contentType,
            AutoCloseStream = false
        }, ct);
    }

    private static string Required(IConfigurationSection s, string key)
        => s[key] ?? throw new InvalidOperationException($"Storage:{key} is not configured.");
}
