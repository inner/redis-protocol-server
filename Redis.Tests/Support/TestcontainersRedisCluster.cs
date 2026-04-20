using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;

namespace Redis.Tests.Support;

public sealed class TestcontainersRedisCluster : IAsyncDisposable
{
    private const int RedisPort = 6380;
    private const string MasterAlias = "redis-master";
    private static readonly SemaphoreSlim ImageLock = new(1, 1);
    private static readonly string ImageName = $"codecrafters-redis-csharp-test:{Guid.NewGuid():N}";
    private static IFutureDockerImage? futureImage;

    private TestcontainersRedisCluster(
        INetwork network,
        IContainer master,
        IReadOnlyList<IContainer> replicas)
    {
        Network = network;
        Master = master;
        Replicas = replicas;
    }

    public INetwork Network { get; }
    public IContainer Master { get; }
    public IReadOnlyList<IContainer> Replicas { get; }
    public (string Host, int Port) MasterEndpoint => (Master.Hostname, (int)Master.GetMappedPublicPort(RedisPort));

    public IReadOnlyList<(string Host, int Port)> ReplicaEndpoints =>
        Replicas.Select(replica => (replica.Hostname, (int)replica.GetMappedPublicPort(RedisPort))).ToList();

    public static async Task<TestcontainersRedisCluster> StartAsync(
        int replicaCount = 1,
        CancellationToken cancellationToken = default)
    {
        if (replicaCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(replicaCount), "Replica count must be positive.");
        }

        var image = await EnsureImageAsync(cancellationToken);

        var network = new NetworkBuilder()
            .WithName($"redis-test-network-{Guid.NewGuid():N}")
            .WithCleanUp(true)
            .Build();
        await network.CreateAsync(cancellationToken);

        IContainer? master = null;
        var replicas = new List<IContainer>();

        try
        {
            master = BuildContainer(image, network, "master", MasterAlias, command: null);
            await master.StartAsync(cancellationToken);

            for (var i = 0; i < replicaCount; i++)
            {
                var replica = BuildContainer(
                    image,
                    network,
                    $"replica-{i + 1}",
                    alias: null,
                    ["--replicaof", $"{MasterAlias} {RedisPort}"]);
                await replica.StartAsync(cancellationToken);
                replicas.Add(replica);
            }

            return new TestcontainersRedisCluster(network, master, replicas);
        }
        catch
        {
            foreach (var replica in replicas)
            {
                await replica.DisposeAsync();
            }

            if (master != null)
            {
                await master.DisposeAsync();
            }

            await network.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var replica in Replicas.Reverse())
        {
            await replica.DisposeAsync();
        }

        await Master.DisposeAsync();
        await Network.DisposeAsync();
    }

    public async Task<string> BuildDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        var sections = new List<string>
        {
            $"Master: {await DescribeContainerAsync(Master, cancellationToken)}"
        };

        for (var i = 0; i < Replicas.Count; i++)
        {
            sections.Add($"Replica {i + 1}: {await DescribeContainerAsync(Replicas[i], cancellationToken)}");
        }

        return string.Join($"{Environment.NewLine}{Environment.NewLine}", sections);
    }

    private static IContainer BuildContainer(
        IFutureDockerImage image,
        INetwork network,
        string name,
        string? alias,
        IReadOnlyList<string>? command)
    {
        var builder = new ContainerBuilder(image)
            .WithImagePullPolicy(PullPolicy.Never)
            .WithName($"redis-test-{name}-{Guid.NewGuid():N}")
            .WithNetwork(network)
            .WithPortBinding(RedisPort, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(RedisPort))
            .WithCleanUp(true);

        if (!string.IsNullOrEmpty(alias))
        {
            builder = builder.WithNetworkAliases(alias);
        }

        if (command != null)
        {
            builder = builder.WithCommand(command.ToArray());
        }

        return builder.Build();
    }

    private static async Task<string> DescribeContainerAsync(
        IContainer container,
        CancellationToken cancellationToken)
    {
        var logs = await container.GetLogsAsync(
            since: DateTime.MinValue,
            until: DateTime.MaxValue,
            timestampsEnabled: false,
            ct: cancellationToken);

        return $"{container.Name} ({container.Hostname}:{container.GetMappedPublicPort(RedisPort)})" +
               $"{Environment.NewLine}stdout:{Environment.NewLine}{logs.Stdout}" +
               $"{Environment.NewLine}stderr:{Environment.NewLine}{logs.Stderr}";
    }

    private static async Task<IFutureDockerImage> EnsureImageAsync(CancellationToken cancellationToken)
    {
        if (futureImage != null)
        {
            return futureImage;
        }

        await ImageLock.WaitAsync(cancellationToken);
        try
        {
            if (futureImage != null)
            {
                return futureImage;
            }

            futureImage = new ImageFromDockerfileBuilder()
                .WithName(ImageName)
                .WithDockerfile("Dockerfile")
                .WithContextDirectory(FindRepositoryRoot())
                .WithDockerfileDirectory(FindRepositoryRoot())
                .WithDeleteIfExists(false)
                .WithImageBuildPolicy(PullPolicy.Missing)
                .Build();

            await futureImage.CreateAsync(cancellationToken);
            return futureImage;
        }
        finally
        {
            ImageLock.Release();
        }
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "redis-server.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from the test output directory.");
    }
}
