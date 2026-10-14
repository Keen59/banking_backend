using Banking.Contracts.Events;
using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.IncomingEftPayments;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Interfaces.Services;
using PaymentService.Application.Mapping;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands.RequestIncomingEftSettlement;

public class RequestIncomingEftSettlementCommand : IRequest<RequestIncomingEftSettlementResponse>
{
    public Guid IncomingEftPaymentId { get; set; }
}

public class RequestIncomingEftSettlementResponse : Response
{
    public IncomingEftPaymentDto? Data { get; set; }
}

public class RequestIncomingEftSettlementHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher)
    : IRequestHandler<RequestIncomingEftSettlementCommand, RequestIncomingEftSettlementResponse>
{
    public async Task<RequestIncomingEftSettlementResponse> Handle(
        RequestIncomingEftSettlementCommand request,
        CancellationToken cancellationToken)
    {
        var payment = await unitOfWork.IncomingEftPayments.GetByIdAsync(request.IncomingEftPaymentId)
            ?? throw new KeyNotFoundException("Incoming EFT payment not found.");

        if (payment.Status == ETransferStatus.Completed)
        {
            return new RequestIncomingEftSettlementResponse
            {
                Message = "Incoming EFT already settled.",
                Data = IncomingEftPaymentMapper.ToDto(payment)
            };
        }

        if (payment.Status == ETransferStatus.Rejected)
            throw new InvalidOperationException("Rejected incoming EFT cannot be settled.");

        if (payment.Status != ETransferStatus.Held)
            throw new InvalidOperationException("Incoming EFT is not accepted yet.");

        await publisher.PublishAsync(
            new IncomingEftSettlementRequested(
                payment.Id,
                payment.AccountId,
                payment.Amount,
                payment.Currency,
                payment.Description,
                DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new RequestIncomingEftSettlementResponse
        {
            Message = "Incoming EFT settlement requested.",
            Data = IncomingEftPaymentMapper.ToDto(payment)
        };
    }
}
