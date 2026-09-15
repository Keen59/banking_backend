using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.Transfers;

namespace PaymentService.Application.Commands.GetTransfer;

public class GetTransferCommand : IRequest<GetTransferResponse>
{
    public Guid TransferId { get; set; }

    public Guid CustomerId { get; set; }

    public bool CanReadAny { get; set; }
}

public class GetTransferResponse : Response
{
    public TransferDto? Data { get; set; }
}
