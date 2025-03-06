using System.Buffers;
using System.Data;
using System.Text;
using Valleysoft.DockerRegistryClient;
using Valleysoft.Dredge;

const string registry = "mcr.microsoft.com";

if (args.Length == 0)
{
    Console.WriteLine("Please provide a file path as an argument.");
    return;
}

string filePath = args[0];
string tempFile = $"{filePath}.tmp";

if (!File.Exists(filePath))
{
    Console.WriteLine($"File not found: {filePath}");
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
            line = await Shaken(line);
            Console.WriteLine();
        }
        writer.WriteLine(line);
    }
}

File.Delete(filePath);
File.Move(tempFile, filePath);

static async Task<string> Shaken(string line)
{
    StringBuilder sb = new();
    Console.WriteLine($"Line: {line}");
    int index = line.IndexOf(registry);
    if (index < 0)
    {
        return line;
    }

    sb.Append(line[..index]);
    string image = line[index..];
    Console.WriteLine($"Image: {image}");
    var imageName = ImageName.Parse(image);
    Console.WriteLine($"ImageName: {imageName}");
    using RegistryClient client = new(imageName.Registry ?? registry);

    // Tag is required for digest
    if (imageName.Tag is null)
    {
        Console.WriteLine($"ImageName: {imageName}");
        throw new Exception($"ImageName.Tag is null");
    }

    // Goal format:
    // mcr.microsoft.com/dotnet-buildtools/prereqs:debian-12-helix-arm32v7@sha256:f765e1228b4977a6a18edd88702d444a7ffaa550c7c5b23097635fbdda41e81d
    string digest = await client.Manifests.GetDigestAsync(imageName.Repo, imageName.Tag);
    string shaken = $"{imageName.Registry}/{imageName.Repo}:{imageName.Tag}@{digest}";
    Console.WriteLine($"SHAKEN: {shaken}");
    sb.Append(shaken);
    string newline = sb.ToString();
    Console.WriteLine($"Newline: {newline}");
    return newline;
}
