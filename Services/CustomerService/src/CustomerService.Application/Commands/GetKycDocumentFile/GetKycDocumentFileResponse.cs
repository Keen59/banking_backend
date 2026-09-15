namespace CustomerService.Application.Commands.GetKycDocumentFile;

public class GetKycDocumentFileResponse
{
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public Stream Content { get; set; } = Stream.Null;
}
