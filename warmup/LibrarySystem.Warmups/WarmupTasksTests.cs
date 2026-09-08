namespace LibrarySystem.Warmups;

public class WarmupTasksTests
{
    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(1024, true)]
    [InlineData(0, false)]
    [InlineData(3, false)]
    [InlineData(1023, false)]
    [InlineData(-2, false)]
    [InlineData(int.MaxValue, false)]
    public void IsPowerOfTwo_ShouldHandleVariousInputs(int input, bool expected)
    {
        Assert.Equal(expected, WarmupTasks.IsPowerOfTwo(input));
    }

    [Theory]
    [InlineData("Moby Dick", "kciD yboM")]
    [InlineData("A", "A")]
    [InlineData("12345", "54321")]
    [InlineData("racecar", "racecar")]
    [InlineData("   ", "   ")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void ReverseTitle_ShouldHandleEdgeCases(string? input, string? expected)
    {
        Assert.Equal(expected, WarmupTasks.ReverseTitle(input));
    }

    [Theory]
    [InlineData("Read", 3, "ReadReadRead")]
    [InlineData("Book", 1, "Book")]
    [InlineData("Test", 0, "")]
    [InlineData("Test", -5, "")]
    [InlineData("", 10, "")]
    [InlineData("  ", 2, "    ")]
    public void GenerateReplicas_ShouldBeRobust(string title, int times, string expected)
    {
        Assert.Equal(expected, WarmupTasks.GenerateReplicas(title, times));
    }

    [Fact]
    public void GetOddBookIds_ShouldVerifyFullSequenceAndBounds()
    {
        const int limit = 100;
        var results = WarmupTasks.GetOddBookIds(limit).ToList();

        Assert.Equal(1, results.First());
        Assert.Equal(99, results.Last());
        Assert.Equal(50, results.Count);

        Assert.All(results, n => {
            Assert.True(n >= 0 && n < limit);
            Assert.True(n % 2 != 0);
        });
    }
}