using Banking.Contracts.Events;
using LedgerService.Application.Commands.HoldCardAuthorization;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using LedgerService.Domain.Enums;
using MediatR;

namespace LedgerService.Application.Commands.ReverseCardAuthorization;

public class ReverseCardAuthorizationHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher eventPublisher)
    : IRequestHandler<ReverseCardAuthorizationCommand, ReverseCardAuthorizationResponse>
{
    public async Task<ReverseCardAuthorizationResponse> Handle(
        ReverseCardAuthorizationCommand request,
        CancellationToken cancellationToken)
    {
        var journalKey = HoldCardAuthorizationHandler.JournalKey(request.AuthorizationId);
        var holdKey = HoldCardAuthorizationHandler.HoldKey(request.AuthorizationId);
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? "Kart yetkisi iptal." : request.Reason.Trim();

        var existingJournal = await unitOfWork.JournalEntries.GetByIdempotencyKeyAsync(journalKey, cancellationToken);
        if (existingJournal is not null)
            throw new InvalidOperationException("Kart yetkisi zaten tahsil edilmiş.");

        var hold = await unitOfWork.Holds.GetByIdempotencyKeyAsync(holdKey, cancellationToken)
            ?? throw new InvalidOperationException("Kart hold bulunamadı.");

        if (hold.Status == EHoldStatus.Active)
        {
            hold.Status = EHoldStatus.Released;
            unitOfWork.Holds.Update(hold);
        }

        await eventPublisher.PublishAsync(
            new CardAuthorizationRejected(request.AuthorizationId, reason, DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new ReverseCardAuthorizationResponse { Message = "Kart hold iptal edildi." };
    }
}
