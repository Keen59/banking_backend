namespace RiskService.Application.Options;

public class WatchlistOptions
{
    public const string SectionName = "Watchlist";

    public List<string> NationalIds { get; set; } = ["99999999990"];

    public List<string> Names { get; set; } = ["YASAKLI TEST"];
}
