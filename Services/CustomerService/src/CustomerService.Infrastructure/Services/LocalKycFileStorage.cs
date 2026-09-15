using CustomerService.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CustomerService.Infrastructure.Services;

public sealed class LocalKycFileStorage(IConfiguration configuration, IHostEnvironment environment) : IKycFileStorage
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "application/pdf",
        "image/jpeg",
        "image/jpg",
        "image/png"
    ];

    private const long MaxFileBytes = 10 * 1024 * 1024;

    public async Task<StoredKycFile> SaveAsync(
        Guid customerId,
        string originalFileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contentType) || !AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
            throw new InvalidOperationException("Yalnızca PDF, JPEG ve PNG kabul edilir.");

        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (extension is not ".pdf" and not ".jpg" and not ".jpeg" and not ".png")
            throw new InvalidOperationException("Geçersiz dosya uzantısı.");

        if (content.CanSeek)
            content.Position = 0;

        var root = GetRoot();
        var customerFolder = Path.Combine(root, customerId.ToString("N"));
        Directory.CreateDirectory(customerFolder);

        var storedName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(customerFolder, storedName);
        await using (var file = File.Create(fullPath))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        var size = new FileInfo(fullPath).Length;
        if (size is 0 or > MaxFileBytes)
        {
            File.Delete(fullPath);
            throw new InvalidOperationException("Dosya boş olamaz veya 10 MB sınırını aşamaz.");
        }

        var fileReference = $"{customerId:N}/{storedName}";
        return new StoredKycFile(
            fileReference,
            Path.GetFileName(originalFileName),
            contentType.ToLowerInvariant(),
            size);
    }

    public Task<Stream> OpenReadAsync(string fileReference, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveExistingPath(fileReference);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    private string ResolveExistingPath(string fileReference)
    {
        if (string.IsNullOrWhiteSpace(fileReference) ||
            fileReference.Contains("..", StringComparison.Ordinal) ||
            Path.IsPathRooted(fileReference))
        {
            throw new InvalidOperationException("Belge bulunamadı.");
        }

        var root = GetRoot();
        var fullPath = Path.GetFullPath(Path.Combine(root, fileReference));
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
            throw new InvalidOperationException("Belge bulunamadı.");

        return fullPath;
    }

    private string GetRoot()
    {
        var configured = configuration["KycStorage:RootPath"] ?? "App_Data/kyc";
        var root = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(environment.ContentRootPath, configured);

        root = Path.GetFullPath(root);
        Directory.CreateDirectory(root);
        return root;
    }
}
