using System.Text.Json;
using System.Text.Json.Serialization;
using OpenBankingService.Domain.Enums;

namespace OpenBankingService.Presentation.Serialization;

public sealed class ConsentScopesJsonConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var numeric))
            return numeric;

        if (reader.TokenType == JsonTokenType.String)
            return ParseToken(reader.GetString());

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Scopes must be an integer or an array of AIS/PIS values.");

        var flags = 0;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var value))
            {
                flags |= value;
                continue;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                flags |= ParseToken(reader.GetString());
                continue;
            }

            throw new JsonException("Invalid scopes array item.");
        }

        return flags;
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }

    private static int ParseToken(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new JsonException("Scope value cannot be empty.");

        if (int.TryParse(raw, out var numeric))
            return numeric;

        return raw.Trim().ToLowerInvariant() switch
        {
            "ais" => (int)EConsentScope.Ais,
            "pis" => (int)EConsentScope.Pis,
            _ => throw new JsonException($"Unknown scope '{raw}'.")
        };
    }
}
