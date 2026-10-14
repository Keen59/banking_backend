using Banking.Contracts.Events;
using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.IncomingEftPayments;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Interfaces.Services;
using PaymentService.Application.Mapping;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands.RequestIncomingEftReturn;

public class RequestIncomingEftReturnCommand : IRequest<RequestIncomingEftReturnResponse>
{
    public Guid IncomingEftPaymentId { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public class RequestIncomingEftReturnResponse : Response
{
    public IncomingEftPaymentDto? Data { get; set; }
}

public class RequestIncomingEftReturnHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher)
    : IRequestHandler<RequestIncomingEftReturnCommand, RequestIncomingEftReturnResponse>
{
    public async Task<RequestIncomingEftReturnResponse> Handle(
        RequestIncomingEftReturnCommand request,
        CancellationToken cancellationToken)
    {
        var payment = await unitOfWork.IncomingEftPayments.GetByIdAsync(request.IncomingEftPaymentId)
            ?? throw new KeyNotFoundException("Incoming EFT payment not found.");

        if (payment.Status == ETransferStatus.Rejected)
        {
            return new RequestIncomingEftReturnResponse
            {
                Message = "Incoming EFT already returned.",
                Data = IncomingEftPaymentMapper.ToDto(payment)
            };
        }

        if (payment.Status == ETransferStatus.Completed)
            throw new InvalidOperationException("Settled incoming EFT cannot be returned.");

        if (payment.Status != ETransferStatus.Held)
            throw new InvalidOperationException("Incoming EFT is not accepted yet.");

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? "Gelen EFT iade." : request.Reason.Trim();
        await publisher.PublishAsync(
            new IncomingEftReturnRequested(payment.Id, reason, DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new RequestIncomingEftReturnResponse
        {
            Message = "Incoming EFT return requested.",
            Data = IncomingEftPaymentMapper.ToDto(payment)
        };
    }
}
