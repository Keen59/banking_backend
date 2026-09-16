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

namespace PaymentService.Application.Commands.CreateTransfer;

public class CreateTransferHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher,
    IOptions<TransferLimitOptions> limitOptions)
    : IRequestHandler<CreateTransferCommand, CreateTransferResponse>
{
    public async Task<CreateTransferResponse> Handle(
        CreateTransferCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.Transfers.GetByIdempotencyKeyAsync(
            request.CustomerId,
            request.IdempotencyKey,
            cancellationToken);

        if (existing is not null)
        {
            return new CreateTransferResponse
            {
                Message = "Existing transfer returned for idempotency key.",
                Data = TransferMapper.ToDto(existing)
            };
        }

        var limits = limitOptions.Value;
        if (request.Amount > limits.MaxAmount)
            throw new InvalidOperationException($"Amount exceeds per-transfer limit of {limits.MaxAmount}.");

        var source = await unitOfWork.AccountProjections.GetByIdAsync(request.SourceAccountId);
        if (source is null)
            throw new InvalidOperationException("Source account not found.");

        if (source.CustomerId != request.CustomerId)
            throw new InvalidOperationException("Source account does not belong to the caller.");

        if (source.Status != EAccountProjectionStatus.Active)
            throw new InvalidOperationException("Source account is not active.");

        if (!string.Equals(source.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Source account currency does not match the transfer.");

        var destination = await unitOfWork.AccountProjections.GetByIdAsync(request.DestinationAccountId);
        if (destination is null)
            throw new InvalidOperationException("Destination account not found.");

        if (destination.Status != EAccountProjectionStatus.Active)
            throw new InvalidOperationException("Destination account is not active.");

        if (!string.Equals(destination.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Destination account currency does not match the transfer.");

        var dailyUsed = await PaymentDailyLimit.SumCountedAsync(
            unitOfWork,
            request.CustomerId,
            cancellationToken);

        if (dailyUsed + request.Amount > limits.DailyAmount)
            throw new InvalidOperationException($"Amount exceeds remaining daily limit of {limits.DailyAmount - dailyUsed}.");

        var now = DateTimeOffset.UtcNow;
        var transfer = new Transfer
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            SourceAccountId = request.SourceAccountId,
            DestinationAccountId = request.DestinationAccountId,
            Amount = request.Amount,
            Currency = request.Currency.ToUpperInvariant(),
            Description = request.Description,
            IdempotencyKey = request.IdempotencyKey,
            Status = ETransferStatus.Initiated,
            CreatedAt = now
        };

        await unitOfWork.Transfers.AddAsync(transfer, cancellationToken);
        await publisher.PublishAsync(
            new TransferRequested(
                transfer.Id,
                transfer.SourceAccountId,
                transfer.DestinationAccountId,
                transfer.Amount,
                transfer.Currency,
                transfer.Description,
                now),
            cancellationToken);

        await unitOfWork.SaveAsync(cancellationToken);

        return new CreateTransferResponse
        {
            Message = "Transfer initiated.",
            Data = TransferMapper.ToDto(transfer)
        };
    }
}
