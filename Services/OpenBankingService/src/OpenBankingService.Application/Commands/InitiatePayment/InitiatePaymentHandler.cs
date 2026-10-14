using Banking.Contracts.Events;
using MediatR;
using OpenBankingService.Application.Helpers;
using OpenBankingService.Application.Interfaces.Repositories;
using OpenBankingService.Application.Interfaces.Services;
using OpenBankingService.Application.Mapping;
using OpenBankingService.Domain.Entities;
using OpenBankingService.Domain.Enums;

namespace OpenBankingService.Application.Commands.InitiatePayment;

public class InitiatePaymentHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher)
    : IRequestHandler<InitiatePaymentCommand, InitiatePaymentResponse>
{
    public async Task<InitiatePaymentResponse> Handle(
        InitiatePaymentCommand request,
        CancellationToken cancellationToken)
    {
        var consent = ConsentAccess.RequireActive(
            await unitOfWork.Consents.GetByAccessTokenAsync(request.AccessToken, cancellationToken),
            EConsentScope.Pis,
            DateTimeOffset.UtcNow);

        var existing = await unitOfWork.PaymentInitiations.GetByIdempotencyKeyAsync(
            consent.CustomerId,
            request.IdempotencyKey,
            cancellationToken);

        if (existing is not null)
        {
            return new InitiatePaymentResponse
            {
                Message = "Existing payment initiation returned for idempotency key.",
                Data = OpenBankingMapper.ToDto(existing)
            };
        }

        var consented = consent.Accounts.Select(x => x.AccountId).ToHashSet();
        if (!consented.Contains(request.SourceAccountId))
            throw new InvalidOperationException("Source account is not included in the consent.");

        if (!consented.Contains(request.DestinationAccountId))
            throw new InvalidOperationException("Destination account is not included in the consent.");

        var source = await unitOfWork.AccountProjections.GetByIdAsync(request.SourceAccountId)
            ?? throw new InvalidOperationException("Source account not found.");

        if (source.CustomerId != consent.CustomerId)
            throw new InvalidOperationException("Source account does not belong to the consent customer.");

        var destination = await unitOfWork.AccountProjections.GetByIdAsync(request.DestinationAccountId)
            ?? throw new InvalidOperationException("Destination account not found.");

        var currency = request.Currency.ToUpperInvariant();
        if (!string.Equals(source.Currency, currency, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(destination.Currency, currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Source and destination currency must match the payment.");

        var now = DateTimeOffset.UtcNow;
        var initiation = new PaymentInitiation
        {
            Id = Guid.NewGuid(),
            CustomerId = consent.CustomerId,
            ConsentId = consent.Id,
            SourceAccountId = request.SourceAccountId,
            DestinationAccountId = request.DestinationAccountId,
            Amount = request.Amount,
            Currency = currency,
            Description = request.Description,
            IdempotencyKey = request.IdempotencyKey,
            Status = EInitiationStatus.Initiated,
            CreatedAt = now
        };

        await unitOfWork.PaymentInitiations.AddAsync(initiation, cancellationToken);
        await publisher.PublishAsync(
            new OpenBankingPaymentRequested(
                initiation.Id,
                initiation.CustomerId,
                initiation.SourceAccountId,
                initiation.DestinationAccountId,
                initiation.Amount,
                initiation.Currency,
                initiation.Description,
                now),
            cancellationToken);

        await unitOfWork.SaveAsync(cancellationToken);

        return new InitiatePaymentResponse
        {
            Message = "Payment initiated.",
            Data = OpenBankingMapper.ToDto(initiation)
        };
    }
}
