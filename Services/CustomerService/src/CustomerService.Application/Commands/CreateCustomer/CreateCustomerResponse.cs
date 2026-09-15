using CustomerService.Application.DTOs;
using CustomerService.Application.DTOs.Customers;

namespace CustomerService.Application.Commands.CreateCustomer;

public class CreateCustomerResponse : Response
{
    public CustomerDto? Customer { get; set; }
}
