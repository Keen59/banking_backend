using MediatR;
using RiskService.Application.Interfaces.Repositories;
using RiskService.Domain.Entities;

namespace RiskService.Application.Commands.ProjectAccountOpened;

public class ProjectAccountOpenedHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<ProjectAccountOpenedCommand>
{
    public async Task Handle(ProjectAccountOpenedCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        if (await unitOfWork.AccountProjections.ExistsAsync(evt.AccountId, cancellationToken))
            return;

        await unitOfWork.AccountProjections.AddAsync(
            new AccountProjection
            {
                Id = evt.AccountId,
                CustomerId = evt.CustomerId,
                Currency = evt.Currency,
                CreatedAt = evt.OccurredAt
            },
            cancellationToken);

        await unitOfWork.SaveAsync(cancellationToken);
    }
}
