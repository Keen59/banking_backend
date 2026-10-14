using CardService.Application.DTOs;
using CardService.Application.DTOs.Cards;
using CardService.Application.Interfaces.Repositories;
using CardService.Application.Mapping;
using CardService.Domain.Enums;
using MediatR;

namespace CardService.Application.Commands.FreezeCard;

public class FreezeCardCommand : IRequest<FreezeCardResponse>
{
    public Guid CustomerId { get; set; }

    public Guid CardId { get; set; }
}

public class FreezeCardResponse : Response
{
    public CardDto? Data { get; set; }
}

public class FreezeCardHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<FreezeCardCommand, FreezeCardResponse>
{
    public async Task<FreezeCardResponse> Handle(FreezeCardCommand request, CancellationToken cancellationToken)
    {
        var card = await unitOfWork.Cards.GetByIdAsync(request.CardId)
            ?? throw new KeyNotFoundException("Card not found.");

        if (card.CustomerId != request.CustomerId)
            throw new InvalidOperationException("Card does not belong to the caller.");

        card.Status = ECardStatus.Frozen;
        card.UpdatedAt = DateTimeOffset.UtcNow;
        unitOfWork.Cards.Update(card);
        await unitOfWork.SaveAsync(cancellationToken);

        return new FreezeCardResponse
        {
            Message = "Card frozen.",
            Data = CardMapper.ToDto(card)
        };
    }
}
