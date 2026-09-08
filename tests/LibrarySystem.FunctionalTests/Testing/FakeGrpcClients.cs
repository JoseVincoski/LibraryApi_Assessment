using Grpc.Core;
using LibrarySystem.Contracts.Analytics;
using LibrarySystem.Contracts.Loans;
using LibrarySystem.Contracts.ReaderMetrics;
using LibrarySystem.Contracts.Recommendations;

namespace LibrarySystem.FunctionalTests.Testing;

// Subclasses the generated client and overrides its virtual *Async methods -
// ClientBase's protected parameterless constructor exists specifically for this.
public sealed class FakeAnalyticsGrpcClient : AnalyticsGrpc.AnalyticsGrpcClient
{
    public MostBorrowedRequest? LastRequest { get; private set; }
    public Func<MostBorrowedRequest, MostBorrowedResponse>? Handler { get; set; }
    public RpcException? Fault { get; set; }

    public override AsyncUnaryCall<MostBorrowedResponse> GetMostBorrowedBooksAsync(
        MostBorrowedRequest request, Metadata? headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        return Fault is not null
            ? GrpcTestCall.Faulted<MostBorrowedResponse>(Fault)
            : GrpcTestCall.Success(Handler!(request));
    }
}

public sealed class FakeReaderMetricsGrpcClient : ReaderMetricsGrpc.ReaderMetricsGrpcClient
{
    public ActiveReadersRequest? LastActiveReadersRequest { get; private set; }
    public Func<ActiveReadersRequest, ActiveReadersResponse>? ActiveReadersHandler { get; set; }
    public RpcException? ActiveReadersFault { get; set; }

    public ReadingPaceRequest? LastReadingPaceRequest { get; private set; }
    public Func<ReadingPaceRequest, ReadingPaceResponse>? ReadingPaceHandler { get; set; }
    public RpcException? ReadingPaceFault { get; set; }

    public override AsyncUnaryCall<ActiveReadersResponse> GetMostActiveReadersAsync(
        ActiveReadersRequest request, Metadata? headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        LastActiveReadersRequest = request;
        return ActiveReadersFault is not null
            ? GrpcTestCall.Faulted<ActiveReadersResponse>(ActiveReadersFault)
            : GrpcTestCall.Success(ActiveReadersHandler!(request));
    }

    public override AsyncUnaryCall<ReadingPaceResponse> GetReadingPaceAsync(
        ReadingPaceRequest request, Metadata? headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        LastReadingPaceRequest = request;
        return ReadingPaceFault is not null
            ? GrpcTestCall.Faulted<ReadingPaceResponse>(ReadingPaceFault)
            : GrpcTestCall.Success(ReadingPaceHandler!(request));
    }
}

public sealed class FakeRecommendationsGrpcClient : RecommendationsGrpc.RecommendationsGrpcClient
{
    public RecommendationRequest? LastRequest { get; private set; }
    public Func<RecommendationRequest, RecommendationResponse>? Handler { get; set; }
    public RpcException? Fault { get; set; }

    public override AsyncUnaryCall<RecommendationResponse> GetBookRecommendationsAsync(
        RecommendationRequest request, Metadata? headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        return Fault is not null
            ? GrpcTestCall.Faulted<RecommendationResponse>(Fault)
            : GrpcTestCall.Success(Handler!(request));
    }
}

public sealed class FakeLoansGrpcClient : LoansGrpc.LoansGrpcClient
{
    public BorrowBookRequest? LastBorrowRequest { get; private set; }
    public Func<BorrowBookRequest, BorrowBookResponse>? BorrowHandler { get; set; }
    public RpcException? BorrowFault { get; set; }

    public ReturnBookRequest? LastReturnRequest { get; private set; }
    public Func<ReturnBookRequest, ReturnBookResponse>? ReturnHandler { get; set; }
    public RpcException? ReturnFault { get; set; }

    public override AsyncUnaryCall<BorrowBookResponse> BorrowBookAsync(
        BorrowBookRequest request, Metadata? headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        LastBorrowRequest = request;
        return BorrowFault is not null
            ? GrpcTestCall.Faulted<BorrowBookResponse>(BorrowFault)
            : GrpcTestCall.Success(BorrowHandler!(request));
    }

    public override AsyncUnaryCall<ReturnBookResponse> ReturnBookAsync(
        ReturnBookRequest request, Metadata? headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        LastReturnRequest = request;
        return ReturnFault is not null
            ? GrpcTestCall.Faulted<ReturnBookResponse>(ReturnFault)
            : GrpcTestCall.Success(ReturnHandler!(request));
    }
}
