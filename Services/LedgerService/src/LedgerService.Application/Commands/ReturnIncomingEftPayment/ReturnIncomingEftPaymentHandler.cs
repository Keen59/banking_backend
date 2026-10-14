using Banking.Contracts.Events;
using LedgerService.Application.Commands.AcceptIncomingEftPayment;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using LedgerService.Domain.Enums;
using MediatR;

namespace LedgerService.Application.Commands.ReturnIncomingEftPayment;

public class ReturnIncomingEftPaymentHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher eventPublisher)
    : IRequestHandler<ReturnIncomingEftPaymentCommand, ReturnIncomingEftPaymentResponse>
{
    public async Task<ReturnIncomingEftPaymentResponse> Handle(
        ReturnIncomingEftPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var journalKey = AcceptIncomingEftPaymentHandler.JournalKey(request.IncomingEftPaymentId);
        var holdKey = AcceptIncomingEftPaymentHandler.HoldKey(request.IncomingEftPaymentId);
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? "Gelen EFT iade." : request.Reason.Trim();

        var existingJournal = await unitOfWork.JournalEntries.GetByIdempotencyKeyAsync(journalKey, cancellationToken);
        if (existingJournal is not null)
            throw new InvalidOperationException("Gelen EFT zaten takas edilmiş.");

        var hold = await unitOfWork.Holds.GetByIdempotencyKeyAsync(holdKey, cancellationToken)
            ?? throw new InvalidOperationException("Gelen EFT kabul kaydı bulunamadı.");

        if (hold.Status == EHoldStatus.Active)
        {
            hold.Status = EHoldStatus.Released;
            unitOfWork.Holds.Update(hold);
        }

        await eventPublisher.PublishAsync(
            new IncomingEftPaymentRejected(request.IncomingEftPaymentId, reason, DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new ReturnIncomingEftPaymentResponse { Message = "Gelen EFT iade edildi." };
    }
}
