using MediatR;
using Microsoft.Extensions.Options;
using OpenBankingService.Application.Helpers;
using OpenBankingService.Application.Interfaces.Repositories;
using OpenBankingService.Application.Mapping;
using OpenBankingService.Application.Options;
using OpenBankingService.Domain.Entities;
using OpenBankingService.Domain.Enums;

namespace OpenBankingService.Application.Commands.CreateConsent;

public class CreateConsentHandler(
    IUnitOfWork unitOfWork,
    IOptions<OpenBankingOptions> options)
    : IRequestHandler<CreateConsentCommand, CreateConsentResponse>
{
    public async Task<CreateConsentResponse> Handle(
        CreateConsentCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.Consents.GetByIdempotencyKeyAsync(
            request.CustomerId,
            request.IdempotencyKey,
            cancellationToken);

        if (existing is not null)
        {
            return new CreateConsentResponse
            {
                Message = "Existing consent returned for idempotency key.",
                Data = OpenBankingMapper.ToDto(existing)
            };
        }

        var allowed = options.Value.AllowedTppClientIds ?? [];
        if (!allowed.Any(id => string.Equals(id, request.TppClientId, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Unknown TPP client id.");

        if (!ConsentAccess.HasAisOrPis(request.Scopes))
            throw new InvalidOperationException("Consent must include AIS and/or PIS.");

        var accountIds = request.AccountIds.Distinct().ToList();
        if (accountIds.Count == 0)
            throw new InvalidOperationException("At least one account is required.");

        foreach (var accountId in accountIds)
        {
            var account = await unitOfWork.AccountProjections.GetByIdAsync(accountId);
            if (account is null)
                throw new InvalidOperationException("Account not found.");

            if (account.CustomerId != request.CustomerId)
                throw new InvalidOperationException("Account does not belong to the caller.");

            if (account.Status != EAccountProjectionStatus.Active)
                throw new InvalidOperationException("Account is not active.");
        }

        var now = DateTimeOffset.UtcNow;
        var consent = new Consent
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            TppClientId = request.TppClientId.Trim(),
            AccessToken = Guid.NewGuid().ToString("N"),
            Scopes = request.Scopes,
            Status = EConsentStatus.Active,
            ExpiresAt = now.AddDays(request.ExpiresInDays),
            IdempotencyKey = request.IdempotencyKey,
            CreatedAt = now
        };

        foreach (var accountId in accountIds)
        {
            consent.Accounts.Add(new ConsentAccount
            {
                Id = Guid.NewGuid(),
                ConsentId = consent.Id,
                AccountId = accountId,
                CreatedAt = now
            });
        }

        await unitOfWork.Consents.AddAsync(consent, cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new CreateConsentResponse
        {
            Message = "Consent created.",
            Data = OpenBankingMapper.ToDto(consent)
        };
    }
}
