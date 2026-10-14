using FluentValidation;

namespace NotificationService.Application.Commands.RecordNotification;

public class RecordNotificationCommandValidator : AbstractValidator<RecordNotificationCommand>
{
    public RecordNotificationCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Type).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(512);
        RuleFor(x => x.SourceId).NotEmpty();
    }
}
