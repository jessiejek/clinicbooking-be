using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClinicApp.API.Serialization;

public sealed class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    private static readonly string[] Formats =
    [
        "HH:mm",
        "HH:mm:ss",
        "HH:mm:ss.FFFFFFF"
    ];

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("Invalid TimeOnly value.");
        }

        if (TimeOnly.TryParseExact(value, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            return time;
        }

        return TimeOnly.Parse(value, CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("HH:mm", CultureInfo.InvariantCulture));
    }
}
