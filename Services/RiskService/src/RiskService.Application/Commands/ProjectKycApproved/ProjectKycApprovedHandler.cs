using Banking.Contracts.Events;
using MediatR;
using RiskService.Application.Interfaces.Repositories;
using RiskService.Domain.Entities;

namespace RiskService.Application.Commands.ProjectKycApproved;

public class ProjectKycApprovedCommand : IRequest
{
    public required KycApproved Event { get; init; }
}

public class ProjectKycApprovedHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<ProjectKycApprovedCommand>
{
    public async Task Handle(ProjectKycApprovedCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        var existing = await unitOfWork.CustomerProfiles.GetByIdAsync(evt.CustomerId);
        if (existing is not null)
        {
            existing.CifNumber = evt.CifNumber;
            existing.FirstName = evt.FirstName ?? string.Empty;
            existing.LastName = evt.LastName ?? string.Empty;
            existing.NationalId = evt.NationalId;
            existing.UpdatedAt = evt.OccurredAt;
            unitOfWork.CustomerProfiles.Update(existing);
            await unitOfWork.SaveAsync(cancellationToken);
            return;
        }

        await unitOfWork.CustomerProfiles.AddAsync(
            new CustomerProfile
            {
                Id = evt.CustomerId,
                CifNumber = evt.CifNumber,
                FirstName = evt.FirstName ?? string.Empty,
                LastName = evt.LastName ?? string.Empty,
                NationalId = evt.NationalId,
                CreatedAt = evt.OccurredAt
            },
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
