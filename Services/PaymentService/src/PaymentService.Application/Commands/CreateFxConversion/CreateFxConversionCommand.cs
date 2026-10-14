using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.FxConversions;

namespace PaymentService.Application.Commands.CreateFxConversion;

public class CreateFxConversionCommand : IRequest<CreateFxConversionResponse>
{
    public Guid CustomerId { get; set; }

    public Guid SourceAccountId { get; set; }

    public Guid DestinationAccountId { get; set; }

    public decimal SourceAmount { get; set; }

    public string Description { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;
}

public class CreateFxConversionResponse : Response
{
    public FxConversionDto? Data { get; set; }
}
