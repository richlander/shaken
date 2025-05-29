namespace ContainerInfo;

public record ContainerManifests(
    string SchemaVersion,
    IReadOnlyList<Repository> Repos
);

public record Repository(
    string Repo,
    IReadOnlyList<Image> Images
);

public record Image(
    IReadOnlyList<Platform> Platforms
);

public record Platform(
    string Dockerfile,
    IReadOnlyList<string> SimpleTags,
    string Digest,
    string BaseImageDigest,
    string OsType,
    string OsVersion,
    string Architecture,
    DateTimeOffset Created,
    string CommitUrl,
    IReadOnlyList<Layer> Layers
);

public record Layer(
    string Digest,
    long Size
);
