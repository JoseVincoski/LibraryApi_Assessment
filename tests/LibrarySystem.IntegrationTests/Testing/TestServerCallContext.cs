using Grpc.Core;

namespace LibrarySystem.IntegrationTests.Testing;

internal sealed class TestServerCallContext : ServerCallContext
{
    private TestServerCallContext(CancellationToken cancellationToken)
    {
        CancellationTokenCore = cancellationToken;
    }

    public static ServerCallContext Create(CancellationToken cancellationToken = default) =>
        new TestServerCallContext(cancellationToken);

    protected override CancellationToken CancellationTokenCore { get; }

    protected override string MethodCore => "TestMethod";
    protected override string HostCore => "localhost";
    protected override string PeerCore => "localhost";
    protected override DateTime DeadlineCore => DateTime.MaxValue;
    protected override Metadata RequestHeadersCore { get; } = [];
    protected override Metadata ResponseTrailersCore { get; } = [];
    protected override Status StatusCore { get; set; }
    protected override WriteOptions? WriteOptionsCore { get; set; }
    protected override AuthContext AuthContextCore { get; } = new(string.Empty, new Dictionary<string, List<AuthProperty>>());

    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;

    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
        throw new NotSupportedException("Context propagation isn't needed by these tests.");
}
