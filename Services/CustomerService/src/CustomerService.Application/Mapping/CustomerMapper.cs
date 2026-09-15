using CustomerService.Application.DTOs.Customers;
using CustomerService.Domain.Entities;

namespace CustomerService.Application.Mapping;

public static class CustomerMapper
{
    public static CustomerDto ToDto(Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            CifNumber = customer.CifNumber,
            Type = customer.Type,
            Status = customer.Status,
            KycStatus = customer.KycStatus,
            KycLevel = customer.KycLevel,
            KycExpiresAt = customer.KycExpiresAt,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            NationalId = customer.NationalId,
            DateOfBirth = customer.DateOfBirth,
            Nationality = customer.Nationality,
            Email = customer.Email,
            PhoneNumber = customer.PhoneNumber,
            Addresses = customer.Addresses
                .Select(address => new AddressDto
                {
                    Id = address.Id,
                    Type = address.Type,
                    Country = address.Country,
                    City = address.City,
                    District = address.District,
                    Line1 = address.Line1,
                    PostalCode = address.PostalCode,
                    IsPrimary = address.IsPrimary
                })
                .ToList(),
            Documents = customer.Documents
                .Select(document => new DocumentDto
                {
                    Id = document.Id,
                    DocumentType = document.DocumentType,
                    FileReference = document.FileReference,
                    OriginalFileName = document.OriginalFileName,
                    ContentType = document.ContentType,
                    SizeBytes = document.SizeBytes,
                    Status = document.Status
                })
                .ToList(),
            Consents = customer.Consents
                .Select(consent => new ConsentDto
                {
                    Type = consent.Type,
                    Granted = consent.Granted,
                    GrantedAt = consent.GrantedAt
                })
                .ToList()
        };
    }
}
