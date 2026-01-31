using System.Text.Json;
using System.Text.Json.Serialization;
using Vext.Compiler.VM;

namespace Vext.LSP
{

    public class VextValueConverter : JsonConverter<VextValue>
    {
        public override VextValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
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
