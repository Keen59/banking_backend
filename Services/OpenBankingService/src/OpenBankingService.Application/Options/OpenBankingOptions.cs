namespace OpenBankingService.Application.Options;

public class OpenBankingOptions
{
    public const string SectionName = "OpenBanking";

    public string[] AllowedTppClientIds { get; set; } = ["sandbox-tpp"];
}
