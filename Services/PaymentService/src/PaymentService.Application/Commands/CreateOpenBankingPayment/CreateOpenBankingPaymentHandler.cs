using Banking.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Options;
using PaymentService.Application.Commands.CreateTransfer;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Interfaces.Services;
using PaymentService.Application.Options;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands.CreateOpenBankingPayment;

public class CreateOpenBankingPaymentCommand : IRequest
{
    public required OpenBankingPaymentRequested Event { get; init; }
}

public class CreateOpenBankingPaymentHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher,
    IOptions<TransferLimitOptions> limitOptions)
    : IRequestHandler<CreateOpenBankingPaymentCommand>
{
    public static string IdempotencyKey(Guid initiationId) => $"open-banking:{initiationId:D}";

    public async Task Handle(CreateOpenBankingPaymentCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        await new CreateTransferHandler(unitOfWork, publisher, limitOptions).Handle(
            new CreateTransferCommand
            {
                CustomerId = evt.CustomerId,
                TransferId = evt.InitiationId,
                SourceAccountId = evt.SourceAccountId,
                DestinationAccountId = evt.DestinationAccountId,
                Amount = evt.Amount,
                Currency = evt.Currency,
                Description = string.IsNullOrWhiteSpace(evt.Description)
                    ? "Açık bankacılık ödemesi"
                    : evt.Description,
                IdempotencyKey = IdempotencyKey(evt.InitiationId)
            },
            cancellationToken);
    }
}
