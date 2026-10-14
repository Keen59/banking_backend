using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.FxConversions;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Mapping;

namespace PaymentService.Application.Commands.GetMyFxConversions;

public class GetMyFxConversionsCommand : IRequest<GetMyFxConversionsResponse>
{
    public Guid CustomerId { get; set; }
}

public class GetMyFxConversionsResponse : Response
{
    public IReadOnlyList<FxConversionDto> Data { get; set; } = [];
}

public class GetMyFxConversionsHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetMyFxConversionsCommand, GetMyFxConversionsResponse>
{
    public async Task<GetMyFxConversionsResponse> Handle(
        GetMyFxConversionsCommand request,
        CancellationToken cancellationToken)
    {
        var conversions = await unitOfWork.FxConversions.GetByCustomerIdAsync(
            request.CustomerId,
            cancellationToken);

        return new GetMyFxConversionsResponse
        {
            Message = "FX conversions listed.",
            Data = conversions.Select(FxConversionMapper.ToDto).ToList()
        };
    }
}
