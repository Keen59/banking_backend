using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.TestCredits;

namespace PaymentService.Application.Commands.CreateTestCredit;

public class CreateTestCreditCommand : IRequest<CreateTestCreditResponse>
{
    public Guid RequestedByCustomerId { get; set; }

    public Guid AccountId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public string IdempotencyKey { get; set; } = string.Empty;
}

public class CreateTestCreditResponse : Response
{
    public TestCreditDto? Data { get; set; }
}
