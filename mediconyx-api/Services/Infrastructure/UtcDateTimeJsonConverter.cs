using System.Text.Json;
using System.Text.Json.Serialization;

namespace mediconyx_api.Services.Infrastructure;

/// <summary>
/// Normalizes incoming DateTimes for Npgsql, which rejects Kind=Unspecified for
/// 'timestamp with time zone' columns. HTML date inputs post values like "2026-07-08"
/// which System.Text.Json parses as Unspecified — treat those as UTC (no shift);
/// offset-bearing values are converted to UTC properly.
/// </summary>
public class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetDateTime();
        return value.Kind switch
        {
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => value
        };
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        if (value.Kind == DateTimeKind.Unspecified)
            value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        writer.WriteStringValue(value);
    }
}
