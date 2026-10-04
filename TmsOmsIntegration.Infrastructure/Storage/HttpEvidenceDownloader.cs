using TmsOmsIntegration.Application.Abstractions.Storage;

namespace TmsOmsIntegration.Infrastructure.Storage;

internal sealed class HttpEvidenceDownloader(HttpClient httpClient) : IEvidenceDownloader
{
    public async Task<Stream> DownloadAsync(Uri url, CancellationToken cancellationToken = default)
    {
        // ResponseHeadersRead streams the body instead of buffering the whole file in memory first.
        var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }
}
