namespace LibrarySystem.Service.Infrastructure.Persistence;

public sealed record SeedSettings
{
    public int DeterministicSeed { get; init; }
    public int BooksCount { get; init; }
    public int MinPageCount { get; init; }
    public int MaxPageCount { get; init; }
    public int ReadersCount { get; init; }
    public int LoansCount { get; init; }
    public int MaxMonthsInPast { get; init; }
    public float ProbabilityOfReturn { get; init; }
    public int MinDaysToReturn { get; init; }
    public int MaxDaysToReturn { get; init; }
    public List<string> TitlePatterns { get; init; } = new();
}