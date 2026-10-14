using CardService.Application.DTOs;
using CardService.Application.DTOs.CardAuthorizations;
using CardService.Application.Interfaces.Repositories;
using CardService.Application.Mapping;
using MediatR;

namespace CardService.Application.Commands.GetCardAuthorizations;

public class GetCardAuthorizationsCommand : IRequest<GetCardAuthorizationsResponse>
{
    public Guid CustomerId { get; set; }

    public Guid CardId { get; set; }
}

public class GetCardAuthorizationsResponse : Response
{
    public IReadOnlyList<CardAuthorizationDto> Data { get; set; } = [];
}

public class GetCardAuthorizationsHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetCardAuthorizationsCommand, GetCardAuthorizationsResponse>
{
    public async Task<GetCardAuthorizationsResponse> Handle(
        GetCardAuthorizationsCommand request,
        CancellationToken cancellationToken)
    {
        var card = await unitOfWork.Cards.GetByIdAsync(request.CardId)
            ?? throw new KeyNotFoundException("Card not found.");

        if (card.CustomerId != request.CustomerId)
            throw new InvalidOperationException("Card does not belong to the caller.");

        var authorizations = await unitOfWork.CardAuthorizations.GetByCardIdAsync(card.Id, cancellationToken);

        return new GetCardAuthorizationsResponse
        {
            Message = "Card authorizations listed.",
            Data = authorizations.Select(CardAuthorizationMapper.ToDto).ToList()
        };
    }
}
