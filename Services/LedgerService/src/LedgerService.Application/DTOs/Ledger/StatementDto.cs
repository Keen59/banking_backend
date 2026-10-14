namespace LedgerService.Application.DTOs.Ledger;

public class StatementDto
{
    public Guid AccountId { get; set; }

    public Guid CustomerId { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateTimeOffset From { get; set; }

    public DateTimeOffset To { get; set; }

    public decimal OpeningLedger { get; set; }

    public decimal ClosingLedger { get; set; }

    public IReadOnlyCollection<MovementDto> Movements { get; set; } = [];
}
