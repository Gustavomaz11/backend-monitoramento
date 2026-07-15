namespace SafeNavigation.Api.LiveStreaming;

public sealed class LiveStreamingOptions
{
    public List<IceServerOptions> IceServers { get; init; } = [];
}

public sealed class IceServerOptions
{
    public List<string> Urls { get; init; } = [];
    public string? Username { get; init; }
    public string? Credential { get; init; }
}

public sealed record LiveStreamConfiguration(IReadOnlyList<IceServerConfiguration> IceServers);

public sealed record IceServerConfiguration(IReadOnlyList<string> Urls, string? Username, string? Credential);

