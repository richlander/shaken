using Valleysoft.Dredge;

namespace Shaken;

public interface IDigestClient
{
    public string Name { get; }

    /// <summary>
    /// Gets the digest for the specified image.
    /// </summary>
    /// <param name="image">The image name, which may include a tag.</param>
    /// <returns>The digest of the image.</returns>
    Task<string> GetDigestAsync(ImageName image);

}
