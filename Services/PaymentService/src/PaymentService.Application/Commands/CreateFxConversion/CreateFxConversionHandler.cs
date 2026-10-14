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

namespace PaymentService.Application.Commands.CreateFxConversion;

public class CreateFxConversionHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher,
    IOptions<TransferLimitOptions> limitOptions,
    IOptions<FxRateOptions> rateOptions)
    : IRequestHandler<CreateFxConversionCommand, CreateFxConversionResponse>
{
    public async Task<CreateFxConversionResponse> Handle(
        CreateFxConversionCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.FxConversions.GetByIdempotencyKeyAsync(
            request.CustomerId,
            request.IdempotencyKey,
            cancellationToken);

        if (existing is not null)
        {
            return new CreateFxConversionResponse
            {
                Message = "Existing FX conversion returned for idempotency key.",
                Data = FxConversionMapper.ToDto(existing)
            };
        }

        if (request.SourceAccountId == request.DestinationAccountId)
            throw new InvalidOperationException("Source and destination account cannot be the same.");

        var source = await unitOfWork.AccountProjections.GetByIdAsync(request.SourceAccountId)
            ?? throw new InvalidOperationException("Source account not found.");

        if (source.CustomerId != request.CustomerId)
            throw new InvalidOperationException("Source account does not belong to the caller.");

        if (source.Status != EAccountProjectionStatus.Active)
            throw new InvalidOperationException("Source account is not active.");

        var destination = await unitOfWork.AccountProjections.GetByIdAsync(request.DestinationAccountId)
            ?? throw new InvalidOperationException("Destination account not found.");

        if (destination.CustomerId != request.CustomerId)
            throw new InvalidOperationException("Destination account does not belong to the caller.");

        if (destination.Status != EAccountProjectionStatus.Active)
            throw new InvalidOperationException("Destination account is not active.");

        if (!FxConverter.IsSupported(source.Currency) || !FxConverter.IsSupported(destination.Currency))
            throw new InvalidOperationException("Supported currencies: TRY, USD, EUR.");

        if (string.Equals(source.Currency, destination.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Same currency must use POST /api/payments/transfers.");

        var rates = rateOptions.Value;
        var sourceAmount = decimal.Round(request.SourceAmount, 4, MidpointRounding.AwayFromZero);
        var destinationAmount = FxConverter.Convert(sourceAmount, source.Currency, destination.Currency, rates);
        var tryEquivalent = FxConverter.ToTry(sourceAmount, source.Currency, rates);
        var rate = decimal.Round(destinationAmount / sourceAmount, 8, MidpointRounding.AwayFromZero);

        var limits = limitOptions.Value;
        if (tryEquivalent > limits.MaxAmount)
            throw new InvalidOperationException($"Amount exceeds per-transfer limit of {limits.MaxAmount}.");

        var dailyUsed = await PaymentDailyLimit.SumCountedAsync(
            unitOfWork,
            request.CustomerId,
            cancellationToken);

        if (dailyUsed + tryEquivalent > limits.DailyAmount)
            throw new InvalidOperationException($"Amount exceeds remaining daily limit of {limits.DailyAmount - dailyUsed}.");

        var now = DateTimeOffset.UtcNow;
        var conversion = new FxConversion
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            SourceAccountId = request.SourceAccountId,
            DestinationAccountId = request.DestinationAccountId,
            SourceAmount = sourceAmount,
            SourceCurrency = source.Currency.ToUpperInvariant(),
            DestinationAmount = destinationAmount,
            DestinationCurrency = destination.Currency.ToUpperInvariant(),
            Rate = rate,
            TryEquivalent = tryEquivalent,
            Description = request.Description,
            IdempotencyKey = request.IdempotencyKey,
            Status = ETransferStatus.Initiated,
            CreatedAt = now
        };

        await unitOfWork.FxConversions.AddAsync(conversion, cancellationToken);
        await publisher.PublishAsync(
            new FxConversionRequested(
                conversion.Id,
                conversion.SourceAccountId,
                conversion.DestinationAccountId,
                conversion.SourceAmount,
                conversion.SourceCurrency,
                conversion.DestinationAmount,
                conversion.DestinationCurrency,
                conversion.Rate,
                conversion.TryEquivalent,
                now),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new CreateFxConversionResponse
        {
            Message = "FX conversion initiated.",
            Data = FxConversionMapper.ToDto(conversion)
        };
    }
}
