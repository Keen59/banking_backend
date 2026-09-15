using CustomerService.Domain.Enums;
using FluentValidation;

namespace CustomerService.Application.Commands.UploadKycDocument;

public class UploadKycDocumentCommandValidator : AbstractValidator<UploadKycDocumentCommand>
{
    public UploadKycDocumentCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.DocumentType).IsInEnum();
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.ContentType).NotEmpty();
        RuleFor(x => x.Content)
            .Must(stream => stream is { Length: > 0 } || (stream is { CanSeek: false }))
            .WithMessage("Dosya zorunludur.");
    }
}
