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

namespace PaymentService.Application.Commands.CreateIncomingFastPayment;

public class CreateIncomingFastPaymentHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher,
    IOptions<TransferLimitOptions> limitOptions)
    : IRequestHandler<CreateIncomingFastPaymentCommand, CreateIncomingFastPaymentResponse>
{
    public async Task<CreateIncomingFastPaymentResponse> Handle(
        CreateIncomingFastPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.IncomingFastPayments.GetByIdempotencyKeyAsync(
            request.IdempotencyKey,
            cancellationToken);

        if (existing is not null)
        {
            return new CreateIncomingFastPaymentResponse
            {
                Message = "Existing incoming FAST payment returned for idempotency key.",
                Data = IncomingFastPaymentMapper.ToDto(existing)
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
        var payment = new IncomingFastPayment
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

        await unitOfWork.IncomingFastPayments.AddAsync(payment, cancellationToken);
        await publisher.PublishAsync(
            new IncomingFastPaymentRequested(
                payment.Id,
                payment.AccountId,
                payment.DestinationIban,
                payment.Amount,
                payment.Currency,
                payment.Description,
                now),
            cancellationToken);

        await unitOfWork.SaveAsync(cancellationToken);

        return new CreateIncomingFastPaymentResponse
        {
            Message = "Incoming FAST payment initiated.",
            Data = IncomingFastPaymentMapper.ToDto(payment)
        };
    }
}
