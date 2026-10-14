namespace OpenBankingService.Domain.Entities;

public class ConsentAccount : BaseEntity
{
    public Guid ConsentId { get; set; }

    public Guid AccountId { get; set; }

    public Consent? Consent { get; set; }
}
