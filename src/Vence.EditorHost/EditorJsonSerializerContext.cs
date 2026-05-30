using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vence.EditorHost;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(EditorMessage))]
[JsonSerializable(typeof(string))]
internal sealed partial class EditorJsonSerializerContext : JsonSerializerContext;
