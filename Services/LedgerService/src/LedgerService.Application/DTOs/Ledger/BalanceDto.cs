namespace LedgerService.Application.DTOs.Ledger;

public class BalanceDto
{
    public Guid AccountId { get; set; }

    public Guid LedgerAccountId { get; set; }

    public Guid CustomerId { get; set; }

    public string Currency { get; set; } = string.Empty;

    public decimal Ledger { get; set; }

    public decimal Hold { get; set; }

    public decimal Available { get; set; }
}
