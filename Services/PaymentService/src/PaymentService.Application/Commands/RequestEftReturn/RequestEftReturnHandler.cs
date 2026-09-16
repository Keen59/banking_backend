using Banking.Contracts.Events;
using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.EftPayments;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Interfaces.Services;
using PaymentService.Application.Mapping;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands.RequestEftReturn;

public class RequestEftReturnCommand : IRequest<RequestEftReturnResponse>
{
    public Guid EftPaymentId { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public class RequestEftReturnResponse : Response
{
    public EftPaymentDto? Data { get; set; }
}

public class RequestEftReturnHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher)
    : IRequestHandler<RequestEftReturnCommand, RequestEftReturnResponse>
{
    public async Task<RequestEftReturnResponse> Handle(
        RequestEftReturnCommand request,
        CancellationToken cancellationToken)
    {
        var payment = await unitOfWork.EftPayments.GetByIdAsync(request.EftPaymentId)
            ?? throw new KeyNotFoundException("EFT payment not found.");

        if (payment.Status == ETransferStatus.Rejected)
        {
            return new RequestEftReturnResponse
            {
                Message = "EFT already returned.",
                Data = EftPaymentMapper.ToDto(payment)
            };
        }

        if (payment.Status == ETransferStatus.Completed)
            throw new InvalidOperationException("Settled EFT cannot be returned.");

        if (payment.Status != ETransferStatus.Held)
            throw new InvalidOperationException("EFT hold is not in place yet.");

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? "EFT iade." : request.Reason.Trim();
        await publisher.PublishAsync(
            new EftReturnRequested(payment.Id, reason, DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new RequestEftReturnResponse
        {
            Message = "EFT return requested.",
            Data = EftPaymentMapper.ToDto(payment)
        };
    }
}
