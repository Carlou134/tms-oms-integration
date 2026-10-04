using Microsoft.Extensions.Logging;
using TmsOmsIntegration.Application.Abstractions.Messaging;
using TmsOmsIntegration.Application.Abstractions.Storage;
using TmsOmsIntegration.Application.TmsEvents;

namespace TmsOmsIntegration.Application.Evidences;

public sealed class StoreEvidencesHandler(
    IEvidenceDownloader evidenceDownloader,
    IEvidenceStorage evidenceStorage,
    ILogger<StoreEvidencesHandler> logger) : IEventHandler<TmsEventProcessed>
{
    public async Task HandleAsync(TmsEventProcessed message, CancellationToken cancellationToken = default)
    {
        if (!EvidenceMilestoneFilter.ShouldStore(message))
            return;

        var orderNumber = message.Event.OrderNumber;

        foreach (var evidence in message.Event.Evidences)
        {
            // A malformed URL is a permanent error: retrying would never fix it, so it is skipped.
            if (!TryGetDownloadUrl(evidence.Url, out var url))
            {
                logger.LogWarning("Invalid evidence URL '{Url}' for order {OrderNumber}, evidence skipped",
                    evidence.Url, orderNumber);
                continue;
            }

            var fileName = GetSafeFileName(evidence.FileName, url);

            await using var content = await evidenceDownloader.DownloadAsync(url, cancellationToken);
            var location = await evidenceStorage.SaveAsync(orderNumber, fileName, content, cancellationToken);

            logger.LogInformation("Evidence {FileName} stored at {Location}", fileName, location);
        }
    }

    private static bool TryGetDownloadUrl(string value, out Uri url) =>
        Uri.TryCreate(value, UriKind.Absolute, out url!)
        && (url.Scheme == Uri.UriSchemeHttps || url.Scheme == Uri.UriSchemeHttp);

    // Path.GetFileName drops any directory part, so a name like "../../secrets.txt" cannot escape the order folder.
    private static string GetSafeFileName(string? fileName, Uri url)
    {
        var safeName = Path.GetFileName(fileName);

        return string.IsNullOrWhiteSpace(safeName)
            ? Path.GetFileName(url.AbsolutePath)
            : safeName;
    }
}
