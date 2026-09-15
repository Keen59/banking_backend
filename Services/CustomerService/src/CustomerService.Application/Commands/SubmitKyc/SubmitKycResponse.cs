using CustomerService.Application.DTOs;
using CustomerService.Application.DTOs.Customers;

namespace CustomerService.Application.Commands.SubmitKyc;

public class SubmitKycResponse : Response
{
    public CustomerDto? Customer { get; set; }
}
