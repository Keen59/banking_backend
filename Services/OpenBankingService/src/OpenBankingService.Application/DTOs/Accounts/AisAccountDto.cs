namespace OpenBankingService.Application.DTOs.Accounts;

public class AisAccountDto
{
    public Guid AccountId { get; set; }

    public string Iban { get; set; } = string.Empty;

    public string Currency { get; set; } = string.Empty;
}
