using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TmsOmsIntegration.Api.Converters;

/// <summary>
/// The TMS sends "yyyy-MM-dd HH:mm:ss" without offset, which System.Text.Json does not accept
/// as ISO 8601. Dates are assumed to be Lima time (UTC-5, no daylight saving).
/// </summary>
internal sealed class TmsEventDateConverter : JsonConverter<DateTimeOffset?>
{
    private const string Format = "yyyy-MM-dd HH:mm:ss";
    private static readonly TimeSpan LimaOffset = TimeSpan.FromHours(-5);

    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;

        if (!DateTime.TryParseExact(value, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var localDate))
            throw new JsonException($"eventDate must use the format '{Format}'.");

        return new DateTimeOffset(localDate, LimaOffset);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToOffset(LimaOffset).ToString(Format, CultureInfo.InvariantCulture));
    }
}
