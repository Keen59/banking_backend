using RiskService.Application.Options;
using RiskService.Domain.Entities;

namespace RiskService.Application.Helpers;

public static class WatchlistMatcher
{
    public static bool Matches(CustomerProfile? profile, WatchlistOptions watchlist)
    {
        if (profile is null)
            return false;

        var nationalId = NormalizeNationalId(profile.NationalId);
        if (nationalId.Length > 0 &&
            watchlist.NationalIds.Any(x => NormalizeNationalId(x) == nationalId))
            return true;

        var fullName = NormalizeName($"{profile.FirstName} {profile.LastName}");
        return fullName.Length > 0 &&
               watchlist.Names.Any(x => NormalizeName(x) == fullName);
    }

    public static string NormalizeNationalId(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Where(char.IsDigit).ToArray());

    public static string NormalizeName(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToUpperInvariant();
}
