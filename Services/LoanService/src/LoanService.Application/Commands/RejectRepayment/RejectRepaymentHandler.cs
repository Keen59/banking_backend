using Banking.Contracts.Events;
using MediatR;
using LoanService.Application.Interfaces.Repositories;
using LoanService.Domain.Enums;

namespace LoanService.Application.Commands.RejectRepayment;

public class RejectRepaymentCommand : IRequest
{
    public required LoanRepaymentRejected Event { get; init; }
}

public class RejectRepaymentHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<RejectRepaymentCommand>
{
    public async Task Handle(RejectRepaymentCommand request, CancellationToken cancellationToken)
    {
        var installment = await unitOfWork.Installments.GetByIdAsync(request.Event.InstallmentId);
        if (installment is null)
            return;

        if (installment.Status == EInstallmentStatus.Paid)
            return;

        installment.Status = EInstallmentStatus.Pending;
        unitOfWork.Installments.Update(installment);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
