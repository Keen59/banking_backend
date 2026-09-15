using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.Transfers;

namespace PaymentService.Application.Commands.CreateTransfer;

public class CreateTransferCommand : IRequest<CreateTransferResponse>
{
    public Guid CustomerId { get; set; }

    public Guid SourceAccountId { get; set; }

    public Guid DestinationAccountId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public string Description { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;
}

public class CreateTransferResponse : Response
{
    public TransferDto? Data { get; set; }
}
