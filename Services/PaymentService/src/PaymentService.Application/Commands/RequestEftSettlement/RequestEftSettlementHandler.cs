using Banking.Contracts.Events;
using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.EftPayments;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Interfaces.Services;
using PaymentService.Application.Mapping;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands.RequestEftSettlement;

public class RequestEftSettlementCommand : IRequest<RequestEftSettlementResponse>
{
    public Guid EftPaymentId { get; set; }
}

public class RequestEftSettlementResponse : Response
{
    public EftPaymentDto? Data { get; set; }
}

public class RequestEftSettlementHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher)
    : IRequestHandler<RequestEftSettlementCommand, RequestEftSettlementResponse>
{
    public async Task<RequestEftSettlementResponse> Handle(
        RequestEftSettlementCommand request,
        CancellationToken cancellationToken)
    {
        var payment = await unitOfWork.EftPayments.GetByIdAsync(request.EftPaymentId)
            ?? throw new KeyNotFoundException("EFT payment not found.");

        if (payment.Status == ETransferStatus.Completed)
        {
            return new RequestEftSettlementResponse
            {
                Message = "EFT already settled.",
                Data = EftPaymentMapper.ToDto(payment)
            };
        }

        if (payment.Status == ETransferStatus.Rejected)
            throw new InvalidOperationException("Rejected EFT cannot be settled.");

        if (payment.Status != ETransferStatus.Held)
            throw new InvalidOperationException("EFT hold is not in place yet.");

        await publisher.PublishAsync(
            new EftSettlementRequested(payment.Id, DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new RequestEftSettlementResponse
        {
            Message = "EFT settlement requested.",
            Data = EftPaymentMapper.ToDto(payment)
        };
    }
}
