using ContainerInfo;
using Valleysoft.Dredge;

namespace Shaken;

public class ContainerInfoClient(ContainerManifests manifests) : IDigestClient
{
    private readonly ContainerManifests _manifests = manifests;

    public string Name => nameof(ContainerInfoClient);

    /// <inheritdoc />
    public Task<string> GetDigestAsync(ImageName image)
    {
        if (image.Digest is not null)
        {
            return Task.FromResult(image.Digest);
        }

        var digest = _manifests.Repos
            .FirstOrDefault(repo => repo.Repo.Equals(image.Repo, StringComparison.OrdinalIgnoreCase))
            ?.Images
            .FirstOrDefault(img => img.Platforms.Any(platform => platform.SimpleTags.Contains(image.Tag, StringComparer.OrdinalIgnoreCase)))
            ?.Platforms
            .FirstOrDefault(platform => platform.SimpleTags.Contains(image.Tag, StringComparer.OrdinalIgnoreCase))?
            .Digest ?? throw new ArgumentException($"Image '{image}' not found in manifests.");

        var digestString = digest.Split(':').LastOrDefault() ?? throw new ArgumentException($"Digest for image '{image}' not found in manifests.");
        digestString = $"sha256:{digestString}"; // Ensure the digest is prefixed with "sha256:"
        return Task.FromResult(digestString);
    }
}
