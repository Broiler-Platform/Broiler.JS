namespace LogParser.Tests;

public class LogSummaryBuilderTests
{
    [Fact]
    public void ParseFile_ReadsShard7AndAdditionalFixture()
    {
        var shardSeven = LogSummaryBuilder.ParseFile(GetShardSevenPath());
        var fixture = LogSummaryBuilder.ParseFile(GetFixturePath());

        Assert.Equal(5927, shardSeven.Results.Length);
        Assert.Equal(shardSeven.Executed, shardSeven.Results.Length);
        Assert.Equal(4, fixture.Results.Length);
        Assert.Equal(fixture.Executed, fixture.Results.Length);
    }

    [Fact]
    public void SummarizeGroups_ByStatus_ComputesCountsMetricsAndNotableEntries()
    {
        var summary = LogSummaryBuilder.ParseAndSummarize(GetFixturePath());
        var failedGroup = Assert.Single(summary.StatusGroups, group => group.Key == "failed");

        Assert.Equal(2, failedGroup.Count);
        Assert.Equal(0, failedGroup.MinStdoutLength);
        Assert.Equal(4, failedGroup.MaxStdoutLength);
        Assert.Equal(2, failedGroup.AverageStdoutLength);
        Assert.Equal(5, failedGroup.MinStderrLength);
        Assert.Equal(15, failedGroup.MaxStderrLength);
        Assert.Equal(10, failedGroup.AverageStderrLength);

        var notableEntry = Assert.IsType<NotableLogEntry>(Assert.Single(failedGroup.NotableEntries.Where(entry => entry.StderrLength == 15)));
        Assert.Equal("test/built-ins/Array/map/c.js", notableEntry.Path);
    }

    [Fact]
    public void SummarizeGroups_ByPathBucket_UsesRequestedDepth()
    {
        var summary = LogSummaryBuilder.ParseAndSummarize(GetFixturePath(), bucketDepth: 4);
        var fromBucket = Assert.Single(summary.PathGroups, group => group.Key == "test/built-ins/Array/from");
        var mapBucket = Assert.Single(summary.PathGroups, group => group.Key == "test/built-ins/Array/map");

        Assert.Equal(2, fromBucket.Count);
        Assert.Equal(1, fromBucket.StatusCounts["passed"]);
        Assert.Equal(1, fromBucket.StatusCounts["failed"]);
        Assert.Equal(1, mapBucket.StatusCounts["failed"]);
    }

    [Fact]
    public void Format_IncludesBothFilesAndSummarySections()
    {
        var formatted = LogReportFormatter.Format(
        [
            LogSummaryBuilder.ParseAndSummarize(GetShardSevenPath()),
            LogSummaryBuilder.ParseAndSummarize(GetFixturePath())
        ]);

        Assert.Contains("File: shard-7.json", formatted, StringComparison.Ordinal);
        Assert.Contains("File: sample-shard.json", formatted, StringComparison.Ordinal);
        Assert.Contains("Status groups:", formatted, StringComparison.Ordinal);
        Assert.Contains("Path groups (depth 4):", formatted, StringComparison.Ordinal);
    }

    private static string GetShardSevenPath()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "shard-7.json"));
    }

    private static string GetFixturePath()
    {
        return Path.Combine(AppContext.BaseDirectory, "TestData", "sample-shard.json");
    }
}
