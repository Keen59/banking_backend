using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.FxConversions;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Mapping;

namespace PaymentService.Application.Commands.GetFxConversion;

public class GetFxConversionCommand : IRequest<GetFxConversionResponse>
{
    public Guid ConversionId { get; set; }

    public Guid CustomerId { get; set; }

    public bool CanReadAny { get; set; }
}

public class GetFxConversionResponse : Response
{
    public FxConversionDto? Data { get; set; }
}

public class GetFxConversionHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetFxConversionCommand, GetFxConversionResponse>
{
    public async Task<GetFxConversionResponse> Handle(
        GetFxConversionCommand request,
        CancellationToken cancellationToken)
    {
        var conversion = await unitOfWork.FxConversions.GetByIdAsync(request.ConversionId)
            ?? throw new KeyNotFoundException("FX conversion not found.");

        if (!request.CanReadAny && conversion.CustomerId != request.CustomerId)
            throw new KeyNotFoundException("FX conversion not found.");

        return new GetFxConversionResponse
        {
            Message = "FX conversion retrieved.",
            Data = FxConversionMapper.ToDto(conversion)
        };
    }
}
