using MediatR;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Mapping;

namespace PaymentService.Application.Commands.GetMyTransfers;

public class GetMyTransfersHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetMyTransfersCommand, GetMyTransfersResponse>
{
    public async Task<GetMyTransfersResponse> Handle(
        GetMyTransfersCommand request,
        CancellationToken cancellationToken)
    {
        var transfers = await unitOfWork.Transfers.GetByCustomerIdAsync(request.CustomerId, cancellationToken);

        return new GetMyTransfersResponse
        {
            Message = "Transfers listed.",
            Data = transfers.Select(TransferMapper.ToDto).ToList()
        };
    }
}
