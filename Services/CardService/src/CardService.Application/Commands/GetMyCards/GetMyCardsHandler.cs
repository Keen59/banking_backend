using CardService.Application.DTOs;
using CardService.Application.DTOs.Cards;
using CardService.Application.Interfaces.Repositories;
using CardService.Application.Mapping;
using MediatR;

namespace CardService.Application.Commands.GetMyCards;

public class GetMyCardsCommand : IRequest<GetMyCardsResponse>
{
    public Guid CustomerId { get; set; }
}

public class GetMyCardsResponse : Response
{
    public IReadOnlyList<CardDto> Data { get; set; } = [];
}

public class GetMyCardsHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetMyCardsCommand, GetMyCardsResponse>
{
    public async Task<GetMyCardsResponse> Handle(GetMyCardsCommand request, CancellationToken cancellationToken)
    {
        var cards = await unitOfWork.Cards.GetByCustomerIdAsync(request.CustomerId, cancellationToken);

        return new GetMyCardsResponse
        {
            Message = "Cards listed.",
            Data = cards.Select(CardMapper.ToDto).ToList()
        };
    }
}
