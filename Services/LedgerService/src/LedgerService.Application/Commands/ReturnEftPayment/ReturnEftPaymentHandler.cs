using Banking.Contracts.Events;
using LedgerService.Application.Commands.HoldEftPayment;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using LedgerService.Domain.Enums;
using MediatR;

namespace LedgerService.Application.Commands.ReturnEftPayment;

public class ReturnEftPaymentHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher eventPublisher)
    : IRequestHandler<ReturnEftPaymentCommand, ReturnEftPaymentResponse>
{
    public async Task<ReturnEftPaymentResponse> Handle(
        ReturnEftPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var journalKey = HoldEftPaymentHandler.JournalKey(request.EftPaymentId);
        var holdKey = HoldEftPaymentHandler.HoldKey(request.EftPaymentId);
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? "EFT iade." : request.Reason.Trim();

        var existingJournal = await unitOfWork.JournalEntries.GetByIdempotencyKeyAsync(journalKey, cancellationToken);
        if (existingJournal is not null)
            throw new InvalidOperationException("EFT zaten takas edilmiş.");

        var hold = await unitOfWork.Holds.GetByIdempotencyKeyAsync(holdKey, cancellationToken)
            ?? throw new InvalidOperationException("EFT hold bulunamadı.");

        if (hold.Status == EHoldStatus.Active)
        {
            hold.Status = EHoldStatus.Released;
            unitOfWork.Holds.Update(hold);
        }

        await eventPublisher.PublishAsync(
            new EftPaymentRejected(request.EftPaymentId, reason, DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new ReturnEftPaymentResponse { Message = "EFT hold iade edildi." };
    }
}
