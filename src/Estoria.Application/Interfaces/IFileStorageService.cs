namespace Estoria.Application.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file and returns its full public URL.
    /// </summary>
    /// <param name="folder">Logical bucket folder: "properties", "team", "blog", etc.</param>
    Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a file by its public URL.
    /// </summary>
    Task DeleteAsync(string fileUrl, CancellationToken ct = default);
}
