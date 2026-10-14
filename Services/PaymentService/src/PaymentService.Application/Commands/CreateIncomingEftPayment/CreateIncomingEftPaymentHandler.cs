using Banking.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Options;
using PaymentService.Application.Helpers;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Interfaces.Services;
using PaymentService.Application.Mapping;
using PaymentService.Application.Options;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands.CreateIncomingEftPayment;

public class CreateIncomingEftPaymentHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher,
    IOptions<TransferLimitOptions> limitOptions)
    : IRequestHandler<CreateIncomingEftPaymentCommand, CreateIncomingEftPaymentResponse>
{
    public async Task<CreateIncomingEftPaymentResponse> Handle(
        CreateIncomingEftPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.IncomingEftPayments.GetByIdempotencyKeyAsync(
            request.IdempotencyKey,
            cancellationToken);

        if (existing is not null)
        {
            return new CreateIncomingEftPaymentResponse
            {
                Message = "Existing incoming EFT payment returned for idempotency key.",
                Data = IncomingEftPaymentMapper.ToDto(existing)
            };
        }

        var limits = limitOptions.Value;
        if (request.Amount > limits.MaxAmount)
            throw new InvalidOperationException($"Amount exceeds per-transfer limit of {limits.MaxAmount}.");

        var destinationIban = TurkishIban.Normalize(request.DestinationIban);
        if (!TurkishIban.IsValid(destinationIban))
            throw new InvalidOperationException("Destination IBAN is not a valid TR IBAN.");

        var destination = await unitOfWork.AccountProjections.GetByIbanAsync(destinationIban, cancellationToken)
            ?? throw new InvalidOperationException("Destination IBAN is not an internal account.");

        if (destination.Status != EAccountProjectionStatus.Active)
            throw new InvalidOperationException("Destination account is not active.");

        if (!string.Equals(destination.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Destination account currency does not match the payment.");

        var sourceIban = TurkishIban.Normalize(request.SourceIban);
        if (!string.IsNullOrEmpty(sourceIban))
        {
            if (!TurkishIban.IsValid(sourceIban))
                throw new InvalidOperationException("Source IBAN is not a valid TR IBAN.");

            if (string.Equals(sourceIban, destinationIban, StringComparison.Ordinal))
                throw new InvalidOperationException("Source and destination IBAN cannot be the same.");

            var internalSource = await unitOfWork.AccountProjections.GetByIbanAsync(sourceIban, cancellationToken);
            if (internalSource is not null)
                throw new InvalidOperationException("Internal source must use POST /api/payments/transfers.");
        }

        var now = DateTimeOffset.UtcNow;
        var payment = new IncomingEftPayment
        {
            Id = Guid.NewGuid(),
            AccountId = destination.Id,
            AccountCustomerId = destination.CustomerId,
            RequestedByCustomerId = request.RequestedByCustomerId,
            DestinationIban = destinationIban,
            SourceIban = sourceIban,
            Amount = request.Amount,
            Currency = request.Currency.ToUpperInvariant(),
            Description = request.Description,
            IdempotencyKey = request.IdempotencyKey,
            Status = ETransferStatus.Initiated,
            CreatedAt = now
        };

        await unitOfWork.IncomingEftPayments.AddAsync(payment, cancellationToken);
        await publisher.PublishAsync(
            new IncomingEftPaymentRequested(
                payment.Id,
                payment.AccountId,
                payment.DestinationIban,
                payment.Amount,
                payment.Currency,
                payment.Description,
                now),
            cancellationToken);

        await unitOfWork.SaveAsync(cancellationToken);

        return new CreateIncomingEftPaymentResponse
        {
            Message = "Incoming EFT payment initiated.",
            Data = IncomingEftPaymentMapper.ToDto(payment)
        };
    }
}
