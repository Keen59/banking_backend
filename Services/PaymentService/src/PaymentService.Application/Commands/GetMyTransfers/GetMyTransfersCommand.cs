using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.Transfers;

namespace PaymentService.Application.Commands.GetMyTransfers;

public class GetMyTransfersCommand : IRequest<GetMyTransfersResponse>
{
    public Guid CustomerId { get; set; }
}

public class GetMyTransfersResponse : Response
{
    public IReadOnlyList<TransferDto> Data { get; set; } = [];
}
