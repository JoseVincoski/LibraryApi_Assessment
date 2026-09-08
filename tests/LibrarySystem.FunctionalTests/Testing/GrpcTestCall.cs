using Grpc.Core;

namespace LibrarySystem.FunctionalTests.Testing;

internal static class GrpcTestCall
{
    public static AsyncUnaryCall<TResponse> Success<TResponse>(TResponse response) =>
        new(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });

    public static AsyncUnaryCall<TResponse> Faulted<TResponse>(RpcException exception) =>
        new(
            Task.FromException<TResponse>(exception),
            Task.FromException<Metadata>(exception),
            () => exception.Status,
            () => exception.Trailers,
            () => { });
}
