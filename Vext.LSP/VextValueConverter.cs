using System.Text.Json;
using System.Text.Json.Serialization;
using Vext.Compiler.VM;

namespace Vext.LSP
{
    public class VextValueConverter : JsonConverter<VextValue>
    {
        public override VextValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument doc = JsonDocument.ParseValue(ref reader);
            JsonElement root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                throw new JsonException("VextValue must be a JSON object.");

            if (!root.TryGetProperty("type", out JsonElement typeElement) || typeElement.ValueKind != JsonValueKind.String)
                throw new JsonException("VextValue is missing a valid 'type' property.");

            string? typeName = typeElement.GetString();
            if (!Enum.TryParse(typeName, ignoreCase: true, out VextType parsedType))
                throw new JsonException($"Unknown VextValue type '{typeName}'.");

            if (!root.TryGetProperty("value", out JsonElement valueElement))
                return VextValue.Null();

            return parsedType switch
            {
                VextType.Number => valueElement.ValueKind == JsonValueKind.Number
                    ? VextValue.FromNumber(valueElement.GetDouble())
                    : throw new JsonException("VextValue number type requires numeric 'value'."),
                VextType.Bool => valueElement.ValueKind is JsonValueKind.True or JsonValueKind.False
                    ? VextValue.FromBool(valueElement.GetBoolean())
                    : throw new JsonException("VextValue bool type requires boolean 'value'."),
                VextType.String => valueElement.ValueKind == JsonValueKind.String
                    ? VextValue.FromString(valueElement.GetString() ?? string.Empty)
                    : throw new JsonException("VextValue string type requires string 'value'."),
                VextType.Null => VextValue.Null(),
                _ => throw new JsonException($"Unsupported VextValue type '{parsedType}'.")
            };
        }

        public override void Write(Utf8JsonWriter writer, VextValue value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("type", value.Type.ToString());

            switch (value.Type)
            {
                case VextType.Number:
                    writer.WriteNumber("value", value.AsNumber);
                    break;
                case VextType.Bool:
                    writer.WriteBoolean("value", value.AsBool);
                    break;
                case VextType.String:
                    writer.WriteString("value", value.AsString);
                    break;
                default:
                    writer.WriteNull("value");
                    break;
            }
            writer.WriteEndObject();
        }
    }
}
