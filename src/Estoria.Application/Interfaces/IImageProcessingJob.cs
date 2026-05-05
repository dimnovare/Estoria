namespace Estoria.Application.Interfaces;

/// <summary>
/// Hangfire entrypoint for the image-processing pipeline. Lives in Application
/// so the upload controller can <c>BackgroundJob.Enqueue&lt;IImageProcessingJob&gt;</c>
/// without referencing the Infrastructure implementation.
/// </summary>
public interface IImageProcessingJob
{
    /// <summary>
    /// Idempotent processing run for one PropertyImage row. Loads the row,
    /// streams the original from the private bucket, generates variants via
    /// IImageProcessingService, and updates the row with the public URLs.
    /// Errors are caught and stored on the row — the job never throws so
    /// Hangfire doesn't enter an infinite retry loop on bad source files.
    /// </summary>
    Task ProcessAsync(Guid propertyImageId);
}
