using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Valleysoft.Dredge;

public class ImageName(string? registry, string repo, string? tag, string? digest)
{
    public string? Registry { get; } = registry;
    public string Repo { get; } = repo;
    public string? Tag { get; } = tag;
    public string? Digest { get; } = digest;
    
    public static ImageName Parse(ReadOnlySpan<char> imageName)
    {
        /*
        Cases:
        1. mcr.microsoft.com/dotnet/samples
        2. mcr.microsoft.com/dotnet-buildtools/prereqs:debian-12-helix-arm32v7
        3. mcr.microsoft.com/dotnet-buildtools/prereqs:debian-12-helix-arm32v7@sha256:f765e1228b4977a6a18edd88702d444a7ffaa550c7c5b23097635fbdda41e81d
        4. mcr.microsoft.com/dotnet-buildtools/prereqs@sha256:f765e1228b4977a6a18edd88702d444a7ffaa550c7c5b23097635fbdda41e81d

        In essence:
        - ':` -- Zero, one or two
        - `@` -- Zero or one

        */
        string? registry = null;
        int slashIndex = imageName.IndexOf('/');
        int atIndex = imageName.IndexOf('@');
        int colonIndex = imageName.IndexOf(':');

        if (slashIndex >= 0)
        {
            ReadOnlySpan<char> firstSegment = imageName[..slashIndex];
            if (firstSegment.Contains('.') || firstSegment.Contains(':'))
            {
                registry = firstSegment.ToString();
                int index = slashIndex + 1;
                imageName = imageName[index..];
            }
        }

        string? tag = null;
        string? digest = null;
        string? repo = null;

        atIndex = imageName.IndexOf('@');
        colonIndex = imageName.IndexOf(':');
        // Case #1
        if (atIndex < 0 && colonIndex < 0)
        {
            tag = "latest";
        }
        // Case #2
        // prereqs:debian-12-helix-arm32v7
        else if (atIndex < 0)
        {
            int index = colonIndex + 1;
            tag = imageName[index..].ToString();
            repo = imageName[..colonIndex].ToString();
        }
        else if (atIndex > 0 && colonIndex > 0)
        {
            // Case 3
            // prereqs:debian-12-helix-arm32v7@sha256:f765e1228b4977a6a18edd88702d444a7ffaa550c7c5b23097635fbdda41e81d
            if (colonIndex < atIndex)
            {
                int index = colonIndex + 1;
                tag = imageName[index..atIndex].ToString();
                digest = imageName[(atIndex + 1)..].ToString();
                repo = imageName[..colonIndex].ToString();
            }
            // Case 4
            // prereqs@sha256:f765e1228b4977a6a18edd88702d444a7ffaa550c7c5b23097635fbdda41e81d
            else
            {
                digest = imageName[(atIndex + 1)..].ToString();
                repo = imageName[..atIndex].ToString();
            }
        }

        repo = DockerHubHelper.ResolveRepoName(registry, repo ?? "");
        return new ImageName(registry, repo, tag, digest);
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        if (Registry is not null)
        {
            sb.Append(Registry);
            sb.Append('/');
        }

        sb.Append(Repo);

        if (Tag is null && Digest is null)
        {
            return sb.ToString();
        }

        if (Tag is null)
        {
            sb.Append('@');
            sb.Append(Digest);
            return sb.ToString();
        }

        if (Digest is null)
        {
            sb.Append(':');
            sb.Append(Tag);
            return sb.ToString();
        }

        // Both tag and digest are not null
        // mcr.microsoft.com/dotnet-buildtools/prereqs:debian-12-helix-arm32v7@sha256:f765e1228b4977a6a18edd88702d444a7ffaa550c7c5b23097635fbdda41e81d
        sb.Append(':');
        sb.Append(Tag);
        sb.Append('@');
        sb.Append(Digest);

        return sb.ToString();
    }
}
