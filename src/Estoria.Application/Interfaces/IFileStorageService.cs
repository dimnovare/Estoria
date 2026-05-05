namespace Estoria.Application.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file to the public bucket. Returns a full URL (CDN-served in prod,
    /// /uploads/... in dev) suitable for direct embedding on the website.
    /// </summary>
    /// <param name="folder">Logical folder: "properties", "team", "blog", etc.</param>
    Task<string> UploadPublicAsync(
        Stream stream,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default);

    /// <summary>
    /// Uploads a file to the private bucket. Returns the storage key only — never
    /// publicly addressable. Use <see cref="GetPresignedUrlAsync"/> for time-limited
    /// admin downloads.
    /// </summary>
    Task<string> UploadPrivateAsync(
        Stream stream,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a time-limited download URL for a private object.
    /// </summary>
    Task<string> GetPresignedUrlAsync(
        string key,
        TimeSpan validFor,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a file uploaded via <see cref="UploadPublicAsync"/>, given the URL it
    /// returned. No-op if the file does not exist.
    /// </summary>
    Task DeleteAsync(string fileUrl, CancellationToken ct = default);

    /// <summary>
    /// Deletes a file uploaded via <see cref="UploadPrivateAsync"/>, given its key.
    /// No-op if the file does not exist.
    /// </summary>
    Task DeletePrivateAsync(string key, CancellationToken ct = default);
}
