namespace CustomerService.Application.Interfaces.Services;

public sealed record StoredKycFile(
    string FileReference,
    string OriginalFileName,
    string ContentType,
    long SizeBytes);

public interface IKycFileStorage
{
    Task<StoredKycFile> SaveAsync(
        Guid customerId,
        string originalFileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string fileReference, CancellationToken cancellationToken = default);
}
