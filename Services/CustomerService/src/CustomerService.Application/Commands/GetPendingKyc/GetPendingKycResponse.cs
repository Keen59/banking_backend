using CustomerService.Application.DTOs;
using CustomerService.Application.DTOs.Customers;

namespace CustomerService.Application.Commands.GetPendingKyc;

public class GetPendingKycResponse : Response
{
    public IReadOnlyCollection<CustomerDto> Customers { get; set; } = [];
}
