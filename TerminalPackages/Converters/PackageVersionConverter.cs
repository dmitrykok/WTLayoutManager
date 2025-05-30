using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.ApplicationModel;

namespace WTLayoutManager.Services
{
    /// <summary>
    /// A custom JSON converter for converting PackageVersionEx objects to and from JSON.
    /// </summary>
    public class PackageVersionConverter : JsonConverter<PackageVersionEx>
    {
        /// <summary>
        /// Reads a PackageVersionEx object from a JSON reader.
        /// </summary>
        /// <param name="reader">The JSON reader to read from.</param>
        /// <param name="typeToConvert">The type of object to convert to.</param>
        /// <param name="options">The JSON serialization options.</param>
        /// <returns>The converted PackageVersionEx object.</returns>
        public override PackageVersionEx Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Check if the reader is at the start of an object
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Failed to parse PackageVersionEx");
            }

            // Initialize version fields to default values
            ushort major = 0, minor = 0, build = 0, revision = 0;

            // Read the JSON object
            while (reader.Read())
            {
                // Check if we've reached the end of the object
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    // Return the constructed PackageVersionEx object
                    return new PackageVersionEx(major, minor, build, revision);
                }

                // Check if we're at a property name
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    // Get the property name
                    string? propertyName = reader.GetString();
                    reader.Read(); // Move to the value

                    // Switch on the property name
                    switch (propertyName)
                    {
                        case "Major":
                            // Read the major version
                            major = reader.GetUInt16();
                            break;
                        case "Minor":
                            // Read the minor version
                            minor = reader.GetUInt16();
                            break;
                        case "Build":
                            // Read the build version
                            build = reader.GetUInt16();
                            break;
                        case "Revision":
                            // Read the revision version
                            revision = reader.GetUInt16();
                            break;
                        default:
                            // Skip unknown properties
                            reader.Skip();
                            break;
                    }
                }
            }

            // If we reach this point, the JSON object was malformed
            throw new JsonException("Failed to parse PackageVersionEx");
        }

        /// <summary>
        /// Writes a PackageVersionEx object to a JSON writer.
        /// </summary>
        /// <param name="writer">The JSON writer to write to.</param>
        /// <param name="value">The PackageVersionEx object to write.</param>
        /// <param name="options">The JSON serialization options.</param>
        public override void Write(Utf8JsonWriter writer, PackageVersionEx value, JsonSerializerOptions options)
        {
            // Start the JSON object
            writer.WriteStartObject();

            // Write the version fields
            writer.WriteNumber("Major", value.Version.Major);
            writer.WriteNumber("Minor", value.Version.Minor);
            writer.WriteNumber("Build", value.Version.Build);
            writer.WriteNumber("Revision", value.Version.Revision);

            // End the JSON object
            writer.WriteEndObject();
        }
    }
}
