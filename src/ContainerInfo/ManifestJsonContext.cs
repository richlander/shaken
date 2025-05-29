using System.Text.Json;
using System.Text.Json.Serialization;

namespace ContainerInfo;

[JsonSourceGenerationOptions(
    // Map your Pascal-case record properties to camel-case JSON by default
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ContainerManifests))]
internal partial class ContainerManifestsJsonContext : JsonSerializerContext
{
}

public class ContainerInfoSerializer
{
    public static ValueTask<ContainerManifests?> GetCveRecords(Stream json) => JsonSerializer.DeserializeAsync(json, ContainerManifestsJsonContext.Default.ContainerManifests);
}
