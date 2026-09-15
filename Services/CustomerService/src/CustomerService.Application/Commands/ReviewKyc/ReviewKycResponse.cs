using CustomerService.Application.DTOs;
using CustomerService.Application.DTOs.Customers;

namespace CustomerService.Application.Commands.ReviewKyc;

public class ReviewKycResponse : Response
{
    public CustomerDto? Customer { get; set; }
}
