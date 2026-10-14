using MediatR;
using OpenBankingService.Application.DTOs;
using OpenBankingService.Application.DTOs.Consents;
using OpenBankingService.Application.Interfaces.Repositories;
using OpenBankingService.Application.Mapping;

namespace OpenBankingService.Application.Commands.GetMyConsents;

public class GetMyConsentsCommand : IRequest<GetMyConsentsResponse>
{
    public Guid CustomerId { get; set; }
}

public class GetMyConsentsResponse : Response
{
    public IReadOnlyList<ConsentDto> Data { get; set; } = [];
}

public class GetMyConsentsHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetMyConsentsCommand, GetMyConsentsResponse>
{
    public async Task<GetMyConsentsResponse> Handle(
        GetMyConsentsCommand request,
        CancellationToken cancellationToken)
    {
        var consents = await unitOfWork.Consents.GetByCustomerIdAsync(request.CustomerId, cancellationToken);

        return new GetMyConsentsResponse
        {
            Message = "Consents listed.",
            Data = consents.Select(OpenBankingMapper.ToDto).ToList()
        };
    }
}
