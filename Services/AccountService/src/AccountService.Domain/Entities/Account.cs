using AccountService.Domain.Enums;

namespace AccountService.Domain.Entities;

public class Account : BaseEntity
{
    public Guid CustomerId { get; set; }

    public string CifNumber { get; set; } = null!;

    public string Iban { get; set; } = null!;

    public string AccountNumber { get; set; } = null!;

    public EAccountProductType ProductType { get; set; }

    public EAccountStatus Status { get; set; }

    public string Currency { get; set; } = "TRY";
}
