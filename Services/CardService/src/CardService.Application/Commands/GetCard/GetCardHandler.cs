using CardService.Application.DTOs;
using CardService.Application.DTOs.Cards;
using CardService.Application.Interfaces.Repositories;
using CardService.Application.Mapping;
using MediatR;

namespace CardService.Application.Commands.GetCard;

public class GetCardCommand : IRequest<GetCardResponse>
{
    public Guid CardId { get; set; }

    public Guid CustomerId { get; set; }

    public bool CanReadAny { get; set; }
}

public class GetCardResponse : Response
{
    public CardDto? Data { get; set; }
}

public class GetCardHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetCardCommand, GetCardResponse>
{
    public async Task<GetCardResponse> Handle(GetCardCommand request, CancellationToken cancellationToken)
    {
        var card = await unitOfWork.Cards.GetByIdAsync(request.CardId);
        if (card is null)
            throw new KeyNotFoundException("Card not found.");

        if (!request.CanReadAny && card.CustomerId != request.CustomerId)
            throw new KeyNotFoundException("Card not found.");

        return new GetCardResponse
        {
            Message = "Card retrieved.",
            Data = CardMapper.ToDto(card)
        };
    }
}
