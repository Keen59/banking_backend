using MediatR;
using Microsoft.Extensions.Options;
using RiskService.Application.Helpers;
using RiskService.Application.Interfaces.Repositories;
using RiskService.Application.Options;
using RiskService.Domain;
using RiskService.Domain.Entities;
using RiskService.Domain.Enums;

namespace RiskService.Application.Commands.EvaluatePayment;

public class EvaluatePaymentHandler(
    IUnitOfWork unitOfWork,
    IOptions<RiskLimitOptions> limitOptions,
    IOptions<WatchlistOptions> watchlistOptions)
    : IRequestHandler<EvaluatePaymentCommand>
{
    public async Task Handle(EvaluatePaymentCommand request, CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.RiskScreenings.GetByChannelAndSourceIdAsync(
            request.Channel,
            request.SourceId,
            cancellationToken);

        if (existing is not null)
            return;

        var account = await unitOfWork.AccountProjections.GetByIdAsync(request.AccountId);
        if (account is null)
            return;

        var now = DateTimeOffset.UtcNow;
        var screening = new RiskScreening
        {
            Id = Guid.NewGuid(),
            CustomerId = account.CustomerId,
            Channel = request.Channel,
            SourceId = request.SourceId,
            AccountId = request.AccountId,
            Amount = request.Amount,
            Currency = request.Currency,
            Direction = request.Direction,
            CreatedAt = now
        };

        decimal outboundSum = 0m;
        if (request.Direction == ERiskDirection.Outbound)
        {
            outboundSum = await unitOfWork.RiskScreenings.SumOutboundAmountForCustomerOnUtcDayAsync(
                account.CustomerId,
                now,
                cancellationToken);
        }

        await unitOfWork.RiskScreenings.AddAsync(screening, cancellationToken);

        var limits = limitOptions.Value;
        if (request.Amount >= limits.FlagAmount)
        {
            await OpenAlertAsync(
                screening,
                RiskRules.LargeAmount,
                cancellationToken);
        }

        if (request.Direction == ERiskDirection.Outbound
            && outboundSum + request.Amount >= limits.DailyAmount)
        {
            await OpenAlertAsync(
                screening,
                RiskRules.DailyVelocity,
                cancellationToken);
        }

        if (request.Direction == ERiskDirection.Outbound
            && request.Amount >= limits.StructuringFloor
            && request.Amount < limits.FlagAmount)
        {
            var bandCount = await unitOfWork.RiskScreenings.CountOutboundInBandForCustomerOnUtcDayAsync(
                account.CustomerId,
                now,
                limits.StructuringFloor,
                limits.FlagAmount,
                cancellationToken);
            if (bandCount >= limits.StructuringCount)
            {
                await OpenAlertAsync(
                    screening,
                    RiskRules.Structuring,
                    cancellationToken);
            }
        }

        var profile = await unitOfWork.CustomerProfiles.GetByIdAsync(account.CustomerId);
        if (WatchlistMatcher.Matches(profile, watchlistOptions.Value))
        {
            await OpenAlertAsync(
                screening,
                RiskRules.WatchlistHit,
                cancellationToken);
        }

        await unitOfWork.SaveAsync(cancellationToken);
    }

    private async Task OpenAlertAsync(
        RiskScreening screening,
        string rule,
        CancellationToken cancellationToken)
    {
        await unitOfWork.RiskAlerts.AddAsync(
            new RiskAlert
            {
                Id = Guid.NewGuid(),
                ScreeningId = screening.Id,
                CustomerId = screening.CustomerId,
                Channel = screening.Channel,
                SourceId = screening.SourceId,
                Rule = rule,
                Amount = screening.Amount,
                Status = EAlertStatus.Open,
                CreatedAt = screening.CreatedAt
            },
            cancellationToken);
    }
}
