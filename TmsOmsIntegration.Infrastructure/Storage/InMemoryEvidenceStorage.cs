using System.Collections.Concurrent;
using TmsOmsIntegration.Application.Abstractions.Storage;

namespace TmsOmsIntegration.Infrastructure.Storage;

/// <summary>
/// Stands in for a private cloud blob container (e.g. Azure Blob Storage).
/// Paths follow the same "orders/{orderNumber}/{fileName}" layout a real container would use.
/// </summary>
internal sealed class InMemoryEvidenceStorage : IEvidenceStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _blobs = new(StringComparer.OrdinalIgnoreCase);

    public async Task<string> SaveAsync(
        string orderNumber,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var path = $"orders/{orderNumber}/{fileName}";

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        _blobs[path] = buffer.ToArray();

        return $"memory://evidences/{path}";
    }
}
