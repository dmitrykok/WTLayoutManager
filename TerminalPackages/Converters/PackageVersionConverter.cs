using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.ApplicationModel;

namespace WTLayoutManager.Services
{
    public class PackageVersionConverter : JsonConverter<PackageVersionEx>
    {
        public override PackageVersionEx Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Failed to parse PackageVersionEx");
            }

            ushort major = 0, minor = 0, build = 0, revision = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new PackageVersionEx(major, minor, build, revision);
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string? propertyName = reader.GetString();
                    reader.Read(); // Move to the value

                    switch (propertyName)
                    {
                        case "Major":
                            major = reader.GetUInt16();
                            break;
                        case "Minor":
                            minor = reader.GetUInt16();
                            break;
                        case "Build":
                            build = reader.GetUInt16();
                            break;
                        case "Revision":
                            revision = reader.GetUInt16();
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
            throw new JsonException("Failed to parse PackageVersionEx");
        }

        public override void Write(Utf8JsonWriter writer, PackageVersionEx value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("Major", value.Version.Major);
            writer.WriteNumber("Minor", value.Version.Minor);
            writer.WriteNumber("Build", value.Version.Build);
            writer.WriteNumber("Revision", value.Version.Revision);
            writer.WriteEndObject();
        }
    }
}
