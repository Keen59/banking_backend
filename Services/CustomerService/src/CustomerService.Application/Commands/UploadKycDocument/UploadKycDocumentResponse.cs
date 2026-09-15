using CustomerService.Application.DTOs;
using CustomerService.Application.DTOs.Customers;

namespace CustomerService.Application.Commands.UploadKycDocument;

public class UploadKycDocumentResponse : Response
{
    public DocumentDto? Document { get; set; }
}
