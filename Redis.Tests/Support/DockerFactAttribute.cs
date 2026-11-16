using System.Net.Sockets;
using DotNet.Testcontainers.Configurations;

namespace Redis.Tests.Support;

// A [Fact] for tests that start containers. When no Docker daemon is
// reachable (e.g. a sandbox without socket access) the test is reported as
// skipped instead of failing with DockerUnavailableException.
public sealed class DockerFactAttribute : FactAttribute
{
    private static readonly Lazy<string?> UnavailableReason = new(ProbeDocker);

    public DockerFactAttribute()
    {
        if (UnavailableReason.Value is { } reason)
        {
            Skip = $"Docker is not available: {reason}";
        }
    }

    private static string? ProbeDocker()
    {
        Uri? endpoint;
        try
        {
            // Same endpoint resolution Testcontainers uses (DOCKER_HOST,
            // Docker contexts, well-known socket paths).
            endpoint = TestcontainersSettings.OS.DockerEndpointAuthConfig?.Endpoint;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }

        if (endpoint == null)
        {
            return "no Docker endpoint found";
        }

        // Resolving a unix socket path does not prove it is usable, so try to
        // connect. Other transports (tcp, npipe) are assumed reachable.
        if (endpoint.Scheme != "unix")
        {
            return null;
        }

        try
        {
            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            socket.Connect(new UnixDomainSocketEndPoint(endpoint.AbsolutePath));
            return null;
        }
        catch (SocketException ex)
        {
            return $"{endpoint}: {ex.Message}";
        }
    }
}
