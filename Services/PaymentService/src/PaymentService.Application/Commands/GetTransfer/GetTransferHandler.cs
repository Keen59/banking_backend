using MediatR;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Mapping;

namespace PaymentService.Application.Commands.GetTransfer;

public class GetTransferHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetTransferCommand, GetTransferResponse>
{
    public async Task<GetTransferResponse> Handle(
        GetTransferCommand request,
        CancellationToken cancellationToken)
    {
        var transfer = await unitOfWork.Transfers.GetByIdAsync(request.TransferId);
        if (transfer is null)
            throw new KeyNotFoundException("Transfer not found.");

        if (!request.CanReadAny && transfer.CustomerId != request.CustomerId)
            throw new KeyNotFoundException("Transfer not found.");

        return new GetTransferResponse
        {
            Message = "Transfer retrieved.",
            Data = TransferMapper.ToDto(transfer)
        };
    }
}
