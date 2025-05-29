using Valleysoft.DockerRegistryClient;
using Valleysoft.Dredge;

namespace Shaken;

public class RegistryDigestClient() : IDigestClient
{
    public string Name => nameof(RegistryDigestClient);

    public async Task<string> GetDigestAsync(ImageName imageName)
    {
        using RegistryClient client = new(imageName.Registry ?? "docker.io");
        var digest = await client.Manifests.GetDigestAsync(imageName.Repo, imageName.Tag ?? "");
        return digest;
    }
}
