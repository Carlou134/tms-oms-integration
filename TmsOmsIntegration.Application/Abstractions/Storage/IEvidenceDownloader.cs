namespace TmsOmsIntegration.Application.Abstractions.Storage;

public interface IEvidenceDownloader
{
    /// <summary>
    /// The caller owns the returned stream and must dispose it.
    /// </summary>
    Task<Stream> DownloadAsync(Uri url, CancellationToken cancellationToken = default);
}
