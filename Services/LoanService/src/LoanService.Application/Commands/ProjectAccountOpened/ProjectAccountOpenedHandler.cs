using Banking.Contracts.Events;
using MediatR;
using LoanService.Application.Interfaces.Repositories;
using LoanService.Domain.Entities;
using LoanService.Domain.Enums;

namespace LoanService.Application.Commands.ProjectAccountOpened;

public class ProjectAccountOpenedCommand : IRequest
{
    public required AccountOpened Event { get; init; }
}

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
                Status = EAccountProjectionStatus.Active,
                CreatedAt = evt.OccurredAt
            },
            cancellationToken);

        await unitOfWork.SaveAsync(cancellationToken);
    }
}
