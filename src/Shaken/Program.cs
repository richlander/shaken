using System.Buffers;
using System.Data;
using System.Diagnostics;
using System.Text;
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
int count = 0;

using (var reader = new StreamReader(filePath))
using (var writer = new StreamWriter(tempFile))
{
    string? line;
    while ((line = reader.ReadLine()) != null)
    {
        if (line.Contains(registry))
        {
            count++;
            line = await Shaken(line, strip);
        }

        writer.WriteLine(line);
    }
}

Console.WriteLine($"Updated {count} image references in {filePath}");

File.Delete(filePath);
File.Move(tempFile, filePath);

static async Task<string> Shaken(string line, bool strip = false)
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
    using RegistryClient client = new(imageName.Registry ?? registry);

    // Tag is required for digest
    if (imageName.Tag is null)
    {
        WriteLine($"ImageName: {imageName}");
        throw new Exception($"ImageName.Tag is null: {imageName}.");
    }

    // Goal format:
    // mcr.microsoft.com/dotnet-buildtools/prereqs:debian-12-helix-arm32v7@sha256:f765e1228b4977a6a18edd88702d444a7ffaa550c7c5b23097635fbdda41e81d
    string digest = await client.Manifests.GetDigestAsync(imageName.Repo, imageName.Tag);
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
