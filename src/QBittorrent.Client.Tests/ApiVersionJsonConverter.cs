using System;
using Newtonsoft.Json;

namespace QBittorrent.Client.Tests
{
    internal class ApiVersionJsonConverter : JsonConverter<ApiVersion>
    {
        public override void WriteJson(JsonWriter writer, ApiVersion value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override ApiVersion ReadJson(JsonReader reader, Type objectType, ApiVersion existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                return ApiVersion.Parse(reader.Value.ToString());
            }

            throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing integer.");
        }
    }
}
