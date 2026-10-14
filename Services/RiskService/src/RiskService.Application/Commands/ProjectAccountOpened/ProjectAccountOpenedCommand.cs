using Banking.Contracts.Events;
using MediatR;

namespace RiskService.Application.Commands.ProjectAccountOpened;

public class ProjectAccountOpenedCommand : IRequest
{
    public required AccountOpened Event { get; init; }
}
