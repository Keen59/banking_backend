using CustomerService.Application.DTOs;
using CustomerService.Application.DTOs.Customers;

namespace CustomerService.Application.Commands.GetCustomer;

public class GetCustomerResponse : Response
{
    public CustomerDto? Customer { get; set; }
}
