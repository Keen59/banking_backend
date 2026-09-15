using AccountService.Application.DTOs.Accounts;
using AccountService.Domain.Entities;

namespace AccountService.Application.Mapping;

public static class AccountMapper
{
    public static AccountDto ToDto(Account account)
    {
        return new AccountDto
        {
            Id = account.Id,
            CustomerId = account.CustomerId,
            CifNumber = account.CifNumber,
            Iban = account.Iban,
            AccountNumber = account.AccountNumber,
            ProductType = account.ProductType,
            Status = account.Status,
            Currency = account.Currency
        };
    }
}
