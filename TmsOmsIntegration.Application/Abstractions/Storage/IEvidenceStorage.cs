namespace TmsOmsIntegration.Application.Abstractions.Storage;

public interface IEvidenceStorage
{
    /// <summary>
    /// Stores the file under the order and returns its location in the storage.
    /// </summary>
    Task<string> SaveAsync(
        string orderNumber,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default);
}
