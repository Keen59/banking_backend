using MediatR;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Mapping;

namespace PaymentService.Application.Commands.GetTestCredit;

public class GetTestCreditHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetTestCreditCommand, GetTestCreditResponse>
{
    public async Task<GetTestCreditResponse> Handle(
        GetTestCreditCommand request,
        CancellationToken cancellationToken)
    {
        var credit = await unitOfWork.TestCredits.GetByIdAsync(request.CreditId);
        if (credit is null)
            throw new KeyNotFoundException("Test credit not found.");

        return new GetTestCreditResponse
        {
            Message = "Test credit retrieved.",
            Data = TestCreditMapper.ToDto(credit)
        };
    }
}
