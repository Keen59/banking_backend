namespace RiskService.Domain.Entities;

public class CustomerProfile : BaseEntity
{
    public string CifNumber { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? NationalId { get; set; }
}
