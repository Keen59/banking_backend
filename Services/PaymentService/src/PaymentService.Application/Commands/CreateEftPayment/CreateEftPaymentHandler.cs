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

namespace PaymentService.Application.Commands.CreateEftPayment;

public class CreateEftPaymentHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher,
    IOptions<TransferLimitOptions> limitOptions)
    : IRequestHandler<CreateEftPaymentCommand, CreateEftPaymentResponse>
{
    public async Task<CreateEftPaymentResponse> Handle(
        CreateEftPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.EftPayments.GetByIdempotencyKeyAsync(
            request.CustomerId,
            request.IdempotencyKey,
            cancellationToken);

        if (existing is not null)
        {
            return new CreateEftPaymentResponse
            {
                Message = "Existing EFT payment returned for idempotency key.",
                Data = EftPaymentMapper.ToDto(existing)
            };
        }

        var limits = limitOptions.Value;
        if (request.Amount > limits.MaxAmount)
            throw new InvalidOperationException($"Amount exceeds per-transfer limit of {limits.MaxAmount}.");

        var iban = TurkishIban.Normalize(request.DestinationIban);
        if (!TurkishIban.IsValid(iban))
            throw new InvalidOperationException("Destination IBAN is not a valid TR IBAN.");

        var source = await unitOfWork.AccountProjections.GetByIdAsync(request.SourceAccountId);
        if (source is null)
            throw new InvalidOperationException("Source account not found.");

        if (source.CustomerId != request.CustomerId)
            throw new InvalidOperationException("Source account does not belong to the caller.");

        if (source.Status != EAccountProjectionStatus.Active)
            throw new InvalidOperationException("Source account is not active.");

        if (!string.Equals(source.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Source account currency does not match the transfer.");

        if (string.Equals(TurkishIban.Normalize(source.Iban), iban, StringComparison.Ordinal))
            throw new InvalidOperationException("Source and destination IBAN cannot be the same.");

        var internalDestination = await unitOfWork.AccountProjections.GetByIbanAsync(iban, cancellationToken);
        if (internalDestination is not null)
            throw new InvalidOperationException("Internal destination must use POST /api/payments/transfers.");

        var dailyUsed = await PaymentDailyLimit.SumCountedAsync(
            unitOfWork,
            request.CustomerId,
            cancellationToken);

        if (dailyUsed + request.Amount > limits.DailyAmount)
            throw new InvalidOperationException($"Amount exceeds remaining daily limit of {limits.DailyAmount - dailyUsed}.");

        var now = DateTimeOffset.UtcNow;
        var payment = new EftPayment
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            SourceAccountId = request.SourceAccountId,
            DestinationIban = iban,
            Amount = request.Amount,
            Currency = request.Currency.ToUpperInvariant(),
            Description = request.Description,
            IdempotencyKey = request.IdempotencyKey,
            Status = ETransferStatus.Initiated,
            CreatedAt = now
        };

        await unitOfWork.EftPayments.AddAsync(payment, cancellationToken);
        await publisher.PublishAsync(
            new EftPaymentRequested(
                payment.Id,
                payment.SourceAccountId,
                payment.DestinationIban,
                payment.Amount,
                payment.Currency,
                payment.Description,
                now),
            cancellationToken);

        await unitOfWork.SaveAsync(cancellationToken);

        return new CreateEftPaymentResponse
        {
            Message = "EFT payment initiated.",
            Data = EftPaymentMapper.ToDto(payment)
        };
    }
}
