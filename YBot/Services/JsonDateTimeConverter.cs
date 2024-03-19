using System.Text.Json;
using System.Text.Json.Serialization;

namespace YBot.Services;

public class JsonDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new MethodAccessException();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var result = (value.ToUniversalTime() - DateTime.UnixEpoch).TotalMilliseconds;
        writer.WriteNumberValue(result);
    }
}