using System.Buffers;
using System.Data;
using System.Diagnostics;
using System.Text;
using ContainerInfo;
using Shaken;
using Valleysoft.DockerRegistryClient;
using Valleysoft.Dredge;

const string registry = "mcr.microsoft.com";

if (args.Length == 0)
{
    Console.WriteLine("Please provide a file path as an argument.");
    Console.WriteLine("Usage: Shaken <file-path> [strip]");
    Console.WriteLine("strip: Optional. If specified, the digest will be removed from the image reference.");
    Console.WriteLine("Example: Shaken <file-path>");
    Console.WriteLine("Example: Shaken <file-path> strip");
    return;
}

string filePath = args[0];
string tempFile = $"{filePath}.tmp";

if (!File.Exists(filePath))
{
    Console.WriteLine($"File not found: {filePath}");
    return;
}

bool strip = args.Length > 1 && args[1].Equals("strip", StringComparison.OrdinalIgnoreCase);
bool hasJsonVersions = args.Length > 1 && args[1].StartsWith("https");
int count = 0;
HttpClient httpClient = new();
IDigestClient? digestClient = null;

if (hasJsonVersions)
{
    Console.WriteLine($"Using container info from: {args[1]}");
    var versionStream = await httpClient.GetStreamAsync(args[1]);
    var manifests = await ContainerInfoSerializer.GetCveRecords(versionStream) ?? throw new Exception("Failed to load container manifests.");
    digestClient = new ContainerInfoClient(manifests);
}
else
{
    Console.WriteLine("Using default registry client for digest resolution.");
    // Use the default registry client if no specific version is provided
    digestClient = new RegistryDigestClient();
}

if (digestClient is null)
{
    Console.WriteLine("Failed to create digest client.");
    return;
}


using (var reader = new StreamReader(filePath))
using (var writer = new StreamWriter(tempFile))
{
    string? line;
    while ((line = reader.ReadLine()) != null)
    {
        if (line.Contains(registry))
        {
            count++;
            line = await Shaken(digestClient, line, strip);
        }

        writer.WriteLine(line);
    }
}

Console.WriteLine($"Updated {count} image references in {filePath}");

File.Delete(filePath);
File.Move(tempFile, filePath);

static async Task<string> Shaken(IDigestClient client, string line, bool strip = false)
{
    StringBuilder sb = new();
    WriteLine($"Line: {line}");
    int index = line.IndexOf(registry);
    if (index < 0)
    {
        return line;
    }

    sb.Append(line[..index]);
    string image = line[index..];
    WriteLine($"Image: {image}");
    var imageName = ImageName.Parse(image);
    WriteLine($"ImageName: {imageName}");

    // Tag is required for digest
    if (imageName.Tag is null)
    {
        WriteLine($"ImageName: {imageName}");
        throw new Exception($"ImageName.Tag is null: {imageName}.");
    }

    // Goal format:
    // mcr.microsoft.com/dotnet-buildtools/prereqs:debian-12-helix-arm32v7@sha256:f765e1228b4977a6a18edd88702d444a7ffaa550c7c5b23097635fbdda41e81d
    string digest = await client.GetDigestAsync(imageName);
    string reference = "";
    if (strip)
    {
        reference = $"{imageName.Registry}/{imageName.Repo}:{imageName.Tag}";
    }
    else
    {
        reference = $"{imageName.Registry}/{imageName.Repo}:{imageName.Tag}@{digest}";
    }
    WriteLine($"New image: {reference}");
    Console.WriteLine($"{client.Name}; Resolved digest for {imageName.Repo}:{imageName.Tag} to {digest}");
    sb.Append(reference);
    string newline = sb.ToString();
    WriteLine($"Newline: {newline}\n");
    return newline;
}

[Conditional("DEBUG")]
static void WriteLine(string message)
{
    Console.WriteLine(message);
}
