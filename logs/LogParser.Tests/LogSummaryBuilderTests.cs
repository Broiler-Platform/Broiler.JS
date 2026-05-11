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
    public void ParseAndSummarizeDirectory_CombinesAllFilesIntoOneSummary()
    {
        var summary = LogSummaryBuilder.ParseAndSummarizeDirectory(GetTestDataDirectoryPath());

        Assert.True(summary.IsDirectorySummary);
        Assert.Equal(GetTestDataDirectoryPath(), summary.FilePath);
        Assert.Equal(8, summary.LogRun.Executed);
        Assert.Equal(3, summary.LogRun.Passed);
        Assert.Equal(5, summary.LogRun.Failed);
        Assert.Equal(0, summary.LogRun.Skipped);
        Assert.Equal(8, summary.LogRun.Results.Length);
        Assert.Equal(3, summary.ExceptionSummary.TotalEntriesWithExceptions);

        var failedGroup = Assert.Single(summary.StatusGroups, group => group.Key == "failed");
        var passedGroup = Assert.Single(summary.StatusGroups, group => group.Key == "passed");
        Assert.Equal(5, failedGroup.Count);
        Assert.Equal(3, passedGroup.Count);
        Assert.Contains(summary.PathGroups, group => group.Key == "test/built-ins/Array/from" && group.Count == 2);
        Assert.Contains(summary.PathGroups, group => group.Key == "test/annexB/alpha.js" && group.Count == 1);
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
        Assert.Contains("Exception summary:", formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_DirectorySummary_UsesDirectoryHeading()
    {
        var formatted = LogReportFormatter.Format(
        [
            LogSummaryBuilder.ParseAndSummarizeDirectory(GetTestDataDirectoryPath())
        ]);

        Assert.Contains("Directory: TestData", formatted, StringComparison.Ordinal);
        Assert.Contains("Totals: declared executed=8, passed=3, failed=5, skipped=0; parsed results=8", formatted, StringComparison.Ordinal);
        Assert.DoesNotContain("File: sample-shard.json", formatted, StringComparison.Ordinal);
        Assert.DoesNotContain("File: sample-exceptions.json", formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void ParseAndSummarize_ExtractsAndCountsExceptions()
    {
        var summary = LogSummaryBuilder.ParseAndSummarize(GetExceptionFixturePath());

        Assert.Equal(3, summary.ExceptionSummary.TotalEntriesWithExceptions);
        Assert.Equal(0.75, summary.ExceptionSummary.OccurrenceRate);

        var jsExceptionGroup = Assert.Single(
            summary.ExceptionSummary.TypeGroups,
            group => group.Type == "Broiler.JavaScript.Runtime.JSException");
        Assert.Equal(2, jsExceptionGroup.Count);
        Assert.Equal(0.5, jsExceptionGroup.OccurrenceRate);
        Assert.Equal(1, jsExceptionGroup.DistinctMessageCount);

        var parsedException = Assert.IsType<ParsedException>(Assert.Single(
            summary.LogRun.Results,
            entry => entry.Path == "test/annexB/alpha.js").Exception);
        Assert.Equal("Broiler.JavaScript.Runtime.JSException", parsedException.Type);
        Assert.Equal("Cannot get property set of undefined", parsedException.Message);
        Assert.Equal("InitializeFactories", parsedException.Context);
        Assert.StartsWith("Unhandled exception.", parsedException.LogLine, StringComparison.Ordinal);

        var initializeFactoriesGroup = Assert.Single(
            summary.ExceptionSummary.ContextGroups,
            group => group.Type == "Broiler.JavaScript.Runtime.JSException"
                && group.Context == "InitializeFactories");
        Assert.Equal(1, initializeFactoriesGroup.Count);

        var getDateGroup = Assert.Single(
            summary.ExceptionSummary.ContextGroups,
            group => group.Type == "Broiler.JavaScript.Runtime.JSException"
                && group.Context == "GetDate");
        Assert.Equal(1, getDateGroup.Count);

        var repeatedMessageGroup = Assert.Single(
            summary.ExceptionSummary.MessageGroups,
            group => group.Message == "Cannot get property set of undefined");
        Assert.Equal(2, repeatedMessageGroup.Count);
        Assert.Contains(summary.ExceptionSummary.SuggestedPatterns,
            pattern => pattern.Contains("Cannot get property set of undefined", StringComparison.Ordinal));
    }

    [Fact]
    public void Format_IncludesParsedExceptionDetailsAndExamples()
    {
        var formatted = LogReportFormatter.Format(
        [
            LogSummaryBuilder.ParseAndSummarize(GetExceptionFixturePath())
        ]);

        Assert.Contains("entries with parsed exceptions=3/4", formatted, StringComparison.Ordinal);
        Assert.Contains("Broiler.JavaScript.Runtime.JSException: count=2", formatted, StringComparison.Ordinal);
        Assert.Contains("Broiler.JavaScript.Runtime.JSException in InitializeFactories: count=1", formatted, StringComparison.Ordinal);
        Assert.Contains("Broiler.JavaScript.Runtime.JSException in GetDate: count=1", formatted, StringComparison.Ordinal);
        Assert.Contains("Cannot get property set of undefined: count=2", formatted, StringComparison.Ordinal);
        Assert.Contains("example: test/annexB/alpha.js => type=Broiler.JavaScript.Runtime.JSException, context=InitializeFactories, message=Cannot get property set of undefined", formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void ParseAndSummarize_IgnoresExceptionTypesContainingNonSpaceWhitespace()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

        try
        {
            File.WriteAllText(path, """
            {
              "suiteRef": "fixture-tabs",
              "broilerDll": "fixture/BroilerJS.dll",
              "executed": 1,
              "passed": 0,
              "failed": 1,
              "skipped": 0,
              "results": [
                {
                  "path": "test/tabbed.js",
                  "status": "failed",
                  "stderr": "Unhandled exception. Invalid\tException: tabbed type\nat Demo in /repo/File.cs:line 1\n"
                }
              ]
            }
            """);

            var summary = LogSummaryBuilder.ParseAndSummarize(path);

            Assert.Equal(0, summary.ExceptionSummary.TotalEntriesWithExceptions);
            Assert.Null(Assert.Single(summary.LogRun.Results).Exception);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static string GetShardSevenPath()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "shard-7.json"));
    }

    private static string GetFixturePath()
    {
        return Path.Combine(AppContext.BaseDirectory, "TestData", "sample-shard.json");
    }

    private static string GetTestDataDirectoryPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "TestData");
    }

    private static string GetExceptionFixturePath()
    {
        return Path.Combine(AppContext.BaseDirectory, "TestData", "sample-exceptions.json");
    }
}
