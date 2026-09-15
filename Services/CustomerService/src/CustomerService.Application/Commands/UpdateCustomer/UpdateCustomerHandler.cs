using CustomerService.Application.Interfaces.Repositories;
using CustomerService.Application.Mapping;
using CustomerService.Domain.Entities;
using CustomerService.Domain.Enums;
using MediatR;

namespace CustomerService.Application.Commands.UpdateCustomer;

public class UpdateCustomerHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCustomerCommand, UpdateCustomerResponse>
{
    public async Task<UpdateCustomerResponse> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        if (request.RequestedCustomerId != request.CustomerId)
            throw new UnauthorizedAccessException("Bu müşteri kaydına erişim yetkiniz yok.");

        var customer = await unitOfWork.CustomerRepository.GetByIdWithDetailsAsync(
            request.CustomerId,
            cancellationToken: cancellationToken) ?? throw new InvalidOperationException("Müşteri bulunamadı.");

        if (customer.Status == ECustomerStatus.Closed)
            throw new InvalidOperationException("Kapatılmış müşteri kaydı güncellenemez.");

        customer.Email = request.Email.Trim().ToLowerInvariant();
        customer.PhoneNumber = request.PhoneNumber.Trim();

        if (!string.IsNullOrWhiteSpace(request.City) &&
            !string.IsNullOrWhiteSpace(request.District) &&
            !string.IsNullOrWhiteSpace(request.Line1))
        {
            var primary = customer.Addresses.FirstOrDefault(x => x.IsPrimary)
                ?? customer.Addresses.FirstOrDefault();

            if (primary is null)
            {
                customer.Addresses.Add(new CustomerAddress
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    Type = EAddressType.Home,
                    Country = "TR",
                    City = request.City.Trim(),
                    District = request.District.Trim(),
                    Line1 = request.Line1.Trim(),
                    PostalCode = request.PostalCode?.Trim(),
                    IsPrimary = true
                });
            }
            else
            {
                primary.City = request.City.Trim();
                primary.District = request.District.Trim();
                primary.Line1 = request.Line1.Trim();
                primary.PostalCode = request.PostalCode?.Trim() ?? primary.PostalCode;
            }
        }

        unitOfWork.CustomerRepository.Update(customer);
        await unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            CustomerId = customer.Id,
            Action = EAuditAction.CustomerUpdated,
            Resource = "Customer",
            IpAddress = request.IpAddress
        }, cancellationToken);

        await unitOfWork.SaveAsync(cancellationToken);

        return new UpdateCustomerResponse
        {
            Message = "Müşteri kaydı güncellendi.",
            Customer = CustomerMapper.ToDto(customer)
        };
    }
}
