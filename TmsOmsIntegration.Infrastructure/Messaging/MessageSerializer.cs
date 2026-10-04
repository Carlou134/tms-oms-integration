using System.Text.Json;
using System.Text.Json.Serialization;
using TmsOmsIntegration.Domain.Abstractions;

namespace TmsOmsIntegration.Infrastructure.Messaging;

internal static class MessageSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(), new DomainEventConverter() }
    };

    public static string Serialize<TMessage>(TMessage message) => JsonSerializer.Serialize(message, Options);

    // System.Text.Json serializes by declared type, so a list of IDomainEvent would come out as empty
    // objects. Writing each event with its runtime type keeps its data.
    private sealed class DomainEventConverter : JsonConverter<IDomainEvent>
    {
        public override IDomainEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotSupportedException("Domain events are only serialized for dead-lettering.");

        public override void Write(Utf8JsonWriter writer, IDomainEvent value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
