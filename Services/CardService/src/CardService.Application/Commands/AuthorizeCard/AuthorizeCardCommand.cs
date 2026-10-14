using CardService.Application.DTOs;
using CardService.Application.DTOs.CardAuthorizations;
using MediatR;

namespace CardService.Application.Commands.AuthorizeCard;

public class AuthorizeCardCommand : IRequest<AuthorizeCardResponse>
{
    public Guid CustomerId { get; set; }

    public Guid CardId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public string Merchant { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;
}

public class AuthorizeCardResponse : Response
{
    public CardAuthorizationDto? Data { get; set; }
}
