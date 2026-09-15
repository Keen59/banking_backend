using AccountService.Domain.Enums;

namespace AccountService.Application.DTOs.Accounts;

public class AccountDto
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string CifNumber { get; set; } = string.Empty;

    public string Iban { get; set; } = string.Empty;

    public string AccountNumber { get; set; } = string.Empty;

    public EAccountProductType ProductType { get; set; }

    public EAccountStatus Status { get; set; }

    public string Currency { get; set; } = string.Empty;
}
