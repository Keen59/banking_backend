using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.TestCredits;

namespace PaymentService.Application.Commands.GetTestCredit;

public class GetTestCreditCommand : IRequest<GetTestCreditResponse>
{
    public Guid CreditId { get; set; }
}

public class GetTestCreditResponse : Response
{
    public TestCreditDto? Data { get; set; }
}
