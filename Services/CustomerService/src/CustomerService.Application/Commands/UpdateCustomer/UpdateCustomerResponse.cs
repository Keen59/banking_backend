using CustomerService.Application.DTOs;
using CustomerService.Application.DTOs.Customers;

namespace CustomerService.Application.Commands.UpdateCustomer;

public class UpdateCustomerResponse : Response
{
    public CustomerDto? Customer { get; set; }
}
