using CustomerService.Application.Interfaces.Repositories;
using CustomerService.Application.Interfaces.Services;
using CustomerService.Application.Mapping;
using CustomerService.Domain.Entities;
using CustomerService.Domain.Enums;
using MediatR;

namespace CustomerService.Application.Commands.CreateCustomer;

public class CreateCustomerHandler(
    IUnitOfWork unitOfWork,
    ICifNumberService cifNumberService) : IRequestHandler<CreateCustomerCommand, CreateCustomerResponse>
{
    public async Task<CreateCustomerResponse> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await unitOfWork.CustomerRepository.GetByIdWithDetailsAsync(
            request.CustomerId,
            cancellationToken: cancellationToken);

        if (customer is not null && !string.IsNullOrWhiteSpace(customer.NationalId))
            throw new InvalidOperationException("Bu hesap için müşteri kaydı zaten var.");

        var nationalId = request.NationalId.Trim();
        var nationalIdOwner = await unitOfWork.CustomerRepository.GetByNationalIdAsync(nationalId, cancellationToken);
        if (nationalIdOwner is not null && nationalIdOwner.Id != request.CustomerId)
            throw new InvalidOperationException("Bu TCKN ile kayıtlı bir müşteri zaten var.");

        var isNew = customer is null;
        if (customer is null)
        {
            customer = new Customer
            {
                Id = request.CustomerId,
                CifNumber = await cifNumberService.GenerateAsync(cancellationToken),
                Type = ECustomerType.Individual,
                Status = ECustomerStatus.Prospect,
                KycStatus = EKycStatus.NotStarted,
                KycLevel = EKycLevel.None
            };
        }

        customer.NationalId = nationalId;
        customer.FirstName = request.FirstName.Trim();
        customer.LastName = request.LastName.Trim();
        customer.DateOfBirth = request.DateOfBirth;
        customer.Nationality = string.IsNullOrWhiteSpace(request.Nationality)
            ? "TR"
            : request.Nationality.Trim().ToUpperInvariant();
        customer.Email = request.Email.Trim().ToLowerInvariant();
        customer.PhoneNumber = request.PhoneNumber.Trim();

        if (!customer.Addresses.Any(x => x.IsPrimary))
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

        if (!customer.Consents.Any(x => x.Type == EConsentType.Kvkk && x.Granted))
        {
            customer.Consents.Add(new CustomerConsent
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                Type = EConsentType.Kvkk,
                Granted = true,
                GrantedAt = DateTimeOffset.UtcNow,
                IpAddress = request.IpAddress
            });
        }

        if (isNew)
            await unitOfWork.CustomerRepository.AddAsync(customer, cancellationToken);
        else
            unitOfWork.CustomerRepository.Update(customer);

        await unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            CustomerId = customer.Id,
            Action = isNew ? EAuditAction.CustomerCreated : EAuditAction.CustomerUpdated,
            Resource = "Customer",
            IpAddress = request.IpAddress
        }, cancellationToken);

        await unitOfWork.SaveAsync(cancellationToken);

        var created = await unitOfWork.CustomerRepository.GetByIdWithDetailsAsync(
            customer.Id,
            asNoTracking: true,
            cancellationToken: cancellationToken) ?? customer;

        return new CreateCustomerResponse
        {
            Message = "Müşteri kaydı oluşturuldu. KYC belgelerini yükleyerek doğrulamayı başlatın.",
            Customer = CustomerMapper.ToDto(created)
        };
    }
}
