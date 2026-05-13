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
        Assert.Contains("Source:", formatted, StringComparison.Ordinal);
        Assert.Contains("Metadata:", formatted, StringComparison.Ordinal);
        Assert.Contains("Totals:", formatted, StringComparison.Ordinal);
        Assert.Contains("Status groups:", formatted, StringComparison.Ordinal);
        Assert.Contains("Path groups (depth 4):", formatted, StringComparison.Ordinal);
        Assert.Contains("Exception summary:", formatted, StringComparison.Ordinal);
        Assert.Contains("key: failed", formatted, StringComparison.Ordinal);
        Assert.Contains("statusCounts:", formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_DirectorySummary_UsesDirectoryHeading()
    {
        var formatted = LogReportFormatter.Format(
        [
            LogSummaryBuilder.ParseAndSummarizeDirectory(GetTestDataDirectoryPath())
        ]);

        Assert.Contains("Directory: TestData", formatted, StringComparison.Ordinal);
        Assert.Contains("declaredExecuted: 8", formatted, StringComparison.Ordinal);
        Assert.Contains("parsedResults: 8", formatted, StringComparison.Ordinal);
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
        Assert.Equal(17, parsedException.LineNumber);
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
    public void Format_IncludesParsedExceptionDetailsAndExceptions()
    {
        var formatted = LogReportFormatter.Format(
        [
            LogSummaryBuilder.ParseAndSummarize(GetExceptionFixturePath())
        ]);

        Assert.Contains("totalEntriesWithExceptions: 3", formatted, StringComparison.Ordinal);
        Assert.Contains("totalResults: 4", formatted, StringComparison.Ordinal);
        Assert.Contains("type: Broiler.JavaScript.Runtime.JSException", formatted, StringComparison.Ordinal);
        Assert.Contains("context: InitializeFactories", formatted, StringComparison.Ordinal);
        Assert.Contains("context: GetDate", formatted, StringComparison.Ordinal);
        Assert.Contains("message: Cannot get property set of undefined", formatted, StringComparison.Ordinal);
        Assert.Contains("lineNumber: 17", formatted, StringComparison.Ordinal);
        Assert.Contains("exceptions:", formatted, StringComparison.Ordinal);
        Assert.Contains("path: test/annexB/alpha.js", formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void ParseAndSummarize_IncludesAllExceptionsInsteadOfTruncatingMatches()
    {
        using var fixture = TempLogFile.Create("""
        {
          "suiteRef": "fixture-many-exceptions",
          "broilerDll": "fixture/BroilerJS.dll",
          "executed": 5,
          "passed": 0,
          "failed": 5,
          "skipped": 0,
          "results": [
            {
              "path": "test/annexB/alpha.js",
              "status": "failed",
              "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Cannot get property set of undefined\nat InitializeFactories in /repo/JSValueCoreExtensions.cs:line 17\n"
            },
            {
              "path": "test/annexB/beta.js",
              "status": "failed",
              "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Cannot get property set of undefined\nat InitializeFactories in /repo/JSValueCoreExtensions.cs:line 17\n"
            },
            {
              "path": "test/annexB/gamma.js",
              "status": "failed",
              "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Cannot get property set of undefined\nat InitializeFactories in /repo/JSValueCoreExtensions.cs:line 17\n"
            },
            {
              "path": "test/annexB/delta.js",
              "status": "failed",
              "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Cannot get property set of undefined\nat InitializeFactories in /repo/JSValueCoreExtensions.cs:line 17\n"
            },
            {
              "path": "test/annexB/epsilon.js",
              "status": "failed",
              "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Cannot get property set of undefined\nat InitializeFactories in /repo/JSValueCoreExtensions.cs:line 17\n"
            }
          ]
        }
        """);

        var summary = LogSummaryBuilder.ParseAndSummarize(fixture.Path);

        var typeGroup = Assert.Single(
            summary.ExceptionSummary.TypeGroups,
            group => group.Type == "Broiler.JavaScript.Runtime.JSException");
        var contextGroup = Assert.Single(
            summary.ExceptionSummary.ContextGroups,
            group => group.Type == "Broiler.JavaScript.Runtime.JSException"
                && group.Context == "InitializeFactories");
        var messageGroup = Assert.Single(
            summary.ExceptionSummary.MessageGroups,
            group => group.Message == "Cannot get property set of undefined");

        Assert.Equal(5, typeGroup.Exceptions.Count);
        Assert.Equal(5, contextGroup.Exceptions.Count);
        Assert.Equal(5, messageGroup.Exceptions.Count);
        Assert.Equal(
            [
                "test/annexB/alpha.js",
                "test/annexB/beta.js",
                "test/annexB/delta.js",
                "test/annexB/epsilon.js",
                "test/annexB/gamma.js"
            ],
            typeGroup.Exceptions.Select(exception => exception.Path).ToArray());
    }

    [Fact]
    public void Format_IncludesEveryExceptionEntryForLargeGroups()
    {
        using var fixture = TempLogFile.Create("""
        {
          "suiteRef": "fixture-many-exceptions",
          "broilerDll": "fixture/BroilerJS.dll",
          "executed": 5,
          "passed": 0,
          "failed": 5,
          "skipped": 0,
          "results": [
            {
              "path": "test/annexB/alpha.js",
              "status": "failed",
              "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Cannot get property set of undefined\nat InitializeFactories in /repo/JSValueCoreExtensions.cs:line 17\n"
            },
            {
              "path": "test/annexB/beta.js",
              "status": "failed",
              "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Cannot get property set of undefined\nat InitializeFactories in /repo/JSValueCoreExtensions.cs:line 17\n"
            },
            {
              "path": "test/annexB/gamma.js",
              "status": "failed",
              "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Cannot get property set of undefined\nat InitializeFactories in /repo/JSValueCoreExtensions.cs:line 17\n"
            },
            {
              "path": "test/annexB/delta.js",
              "status": "failed",
              "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Cannot get property set of undefined\nat InitializeFactories in /repo/JSValueCoreExtensions.cs:line 17\n"
            },
            {
              "path": "test/annexB/epsilon.js",
              "status": "failed",
              "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Cannot get property set of undefined\nat InitializeFactories in /repo/JSValueCoreExtensions.cs:line 17\n"
            }
          ]
        }
        """);

        var formatted = LogReportFormatter.Format(
        [
            LogSummaryBuilder.ParseAndSummarize(fixture.Path)
        ]);

        Assert.Contains("path: test/annexB/alpha.js", formatted, StringComparison.Ordinal);
        Assert.Contains("path: test/annexB/beta.js", formatted, StringComparison.Ordinal);
        Assert.Contains("path: test/annexB/gamma.js", formatted, StringComparison.Ordinal);
        Assert.Contains("path: test/annexB/delta.js", formatted, StringComparison.Ordinal);
        Assert.Contains("path: test/annexB/epsilon.js", formatted, StringComparison.Ordinal);
        Assert.DoesNotContain("examples:", formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatJson_SerializesStructuredReport()
    {
        var json = LogReportFormatter.FormatJson(
        [
            LogSummaryBuilder.ParseAndSummarize(GetFixturePath())
        ]);

        Assert.Contains("\"outputFormat\": \"json\"", json, StringComparison.Ordinal);
        Assert.Contains("\"kind\": \"file\"", json, StringComparison.Ordinal);
        Assert.Contains("\"name\": \"sample-shard.json\"", json, StringComparison.Ordinal);
        Assert.Contains("\"declaredExecuted\": 4", json, StringComparison.Ordinal);
        Assert.Contains("\"statusGroups\"", json, StringComparison.Ordinal);
        Assert.Contains("\"pathGroups\"", json, StringComparison.Ordinal);
        Assert.Contains("\"exceptionSummary\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatJson_IncludesExceptionLineNumbers()
    {
        var json = LogReportFormatter.FormatJson(
        [
            LogSummaryBuilder.ParseAndSummarize(GetExceptionFixturePath())
        ]);

        Assert.Contains("\"lineNumber\": 17", json, StringComparison.Ordinal);
        Assert.Contains("\"lineNumber\": 99", json, StringComparison.Ordinal);
        Assert.Contains("\"lineNumber\": 42", json, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(new[] { "--output", "json", "sample.json" }, "json", new[] { "sample.json" })]
    [InlineData(new[] { "--output=json", "sample.json" }, "json", new[] { "sample.json" })]
    [InlineData(new[] { "-o", "text", "sample.json" }, "text", new[] { "sample.json" })]
    public void ParseOptions_ReadsSupportedOutputSyntaxes(string[] args, string expectedOutput, string[] expectedInputs)
    {
        var options = Program.ParseOptions(args);

        Assert.Equal(expectedOutput, options.OutputFormat);
        Assert.Equal(expectedInputs, options.Inputs);
    }

    [Theory]
    [InlineData(new[] { "--type", "Broiler.JavaScript.Runtime.JSException", "sample.json" }, "Broiler.JavaScript.Runtime.JSException", null)]
    [InlineData(new[] { "--type=Broiler.JavaScript.Runtime.JSException", "--context=InitializeFactories", "sample.json" }, "Broiler.JavaScript.Runtime.JSException", "InitializeFactories")]
    [InlineData(new[] { "--context", "GetDate", "sample.json" }, null, "GetDate")]
    public void ParseOptions_ReadsSupportedExceptionFilterSyntaxes(string[] args, string? expectedType, string? expectedContext)
    {
        var options = Program.ParseOptions(args);

        Assert.Equal(expectedType, options.TypeFilter);
        Assert.Equal(expectedContext, options.ContextFilter);
    }

    [Theory]
    [InlineData(new[] { "--message", "property set", "sample.json" }, "property set")]
    [InlineData(new[] { "--message=Unexpected parser state", "sample.json" }, "Unexpected parser state")]
    public void ParseOptions_ReadsSupportedMessageFilterSyntaxes(string[] args, string expectedMessage)
    {
        var options = Program.ParseOptions(args);

        Assert.Equal(expectedMessage, options.MessageFilter);
    }

    [Theory]
    [MemberData(nameof(GetMostCommonProblemFlagArgs))]
    public void ParseOptions_ReadsMostCommonProblemFlag(string[] args)
    {
        var options = Program.ParseOptions(args);

        Assert.True(options.MostCommonProblem);
    }

    [Fact]
    public void ParseOptions_DefaultsToTextWhenOutputIsNotSpecified()
    {
        var options = Program.ParseOptions(["sample.json"]);

        Assert.Equal("text", options.OutputFormat);
        Assert.Equal(["sample.json"], options.Inputs);
        Assert.Null(options.TypeFilter);
        Assert.Null(options.ContextFilter);
        Assert.Null(options.MessageFilter);
        Assert.False(options.MostCommonProblem);
    }

    [Fact]
    public void ParseOptions_RejectsCombiningMostCommonProblemWithFilters()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            Program.ParseOptions(["--most-common-problem", "--type", "System.Exception", "sample.json"]));

        Assert.Contains("--most-common-problem", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatFilteredExceptions_SuppressesSummariesAndShowsOnlyMatches()
    {
        var formatted = LogReportFormatter.FormatFilteredExceptions(
            [
                LogSummaryBuilder.ParseAndSummarize(GetExceptionFixturePath())
            ],
            "Broiler.JavaScript.Runtime.JSException",
            "InitializeFactories");

        Assert.Contains("Filters:", formatted, StringComparison.Ordinal);
        Assert.Contains("type: Broiler.JavaScript.Runtime.JSException", formatted, StringComparison.Ordinal);
        Assert.Contains("context: InitializeFactories", formatted, StringComparison.Ordinal);
        Assert.Contains("Matches:", formatted, StringComparison.Ordinal);
        Assert.Contains("path: test/annexB/alpha.js", formatted, StringComparison.Ordinal);
        Assert.DoesNotContain("Totals:", formatted, StringComparison.Ordinal);
        Assert.DoesNotContain("Status groups:", formatted, StringComparison.Ordinal);
        Assert.DoesNotContain("Path groups (depth 4):", formatted, StringComparison.Ordinal);
        Assert.DoesNotContain("Exception summary:", formatted, StringComparison.Ordinal);
        Assert.DoesNotContain("path: test/date/beta.js", formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateFilteredExceptionReport_FiltersByPartialMessageMatch()
    {
        var report = LogReportFormatter.CreateFilteredExceptionReport(
            [
                LogSummaryBuilder.ParseAndSummarize(GetExceptionFixturePath())
            ],
            outputFormat: "json",
            typeFilter: null,
            contextFilter: null,
            messageFilter: "property set");

        Assert.Equal("property set", report.Filters.Message);

        var match = Assert.Single(report.Matches);
        Assert.Equal(
            [
                "test/annexB/alpha.js",
                "test/annexB/beta.js"
            ],
            match.Exceptions.Select(exception => exception.Path).ToArray());
        Assert.All(match.Exceptions, exception => Assert.Equal("Cannot get property set of undefined", exception.Message));
        Assert.All(match.Exceptions, exception => Assert.True(exception.LineNumber is 17 or 99));
    }

    [Fact]
    public void FormatFilteredExceptionsJson_SuppressesSummariesProperty()
    {
        var json = LogReportFormatter.FormatFilteredExceptionsJson(
            [
                LogSummaryBuilder.ParseAndSummarize(GetExceptionFixturePath())
            ],
            "Broiler.JavaScript.Runtime.JSException",
            "InitializeFactories",
            "property set");

        Assert.Contains("\"outputFormat\": \"json\"", json, StringComparison.Ordinal);
        Assert.Contains("\"filters\"", json, StringComparison.Ordinal);
        Assert.Contains("\"message\": \"property set\"", json, StringComparison.Ordinal);
        Assert.Contains("\"matches\"", json, StringComparison.Ordinal);
        Assert.Contains("\"exceptions\"", json, StringComparison.Ordinal);
        Assert.Contains("\"lineNumber\": 17", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"summaries\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void FindMostCommonProblem_SelectsMostCommonTypeContextAndMessage()
    {
        using var fixture = TempLogFile.Create("""
        {
          "suiteRef": "fixture-most-common-problem",
          "broilerDll": "fixture/BroilerJS.dll",
          "executed": 5,
          "passed": 0,
          "failed": 5,
          "skipped": 0,
          "results": [
            {
              "path": "test/annexB/alpha.js",
              "status": "failed",
              "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Common failure\nat Throw in /repo/JSException.cs:line 114\n"
            },
            {
              "path": "test/annexB/beta.js",
              "status": "failed",
              "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Common failure\nat Throw in /repo/JSException.cs:line 114\n"
            },
            {
              "path": "test/annexB/gamma.js",
              "status": "failed",
              "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Less common failure\nat Throw in /repo/JSException.cs:line 120\n"
            },
            {
              "path": "test/annexB/delta.js",
              "status": "failed",
              "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Different context failure\nat Handle in /repo/JSException.cs:line 140\n"
            },
            {
              "path": "test/annexB/epsilon.js",
              "status": "failed",
              "stderr": "Unhandled exception. System.InvalidOperationException: Other failure\nat Throw in /repo/Program.cs:line 10\n"
            }
          ]
        }
        """);

        var summary = LogSummaryBuilder.ParseAndSummarize(fixture.Path);
        var problem = LogSummaryBuilder.FindMostCommonProblem(summary.LogRun.Results);

        Assert.NotNull(problem);
        Assert.Equal("Broiler.JavaScript.Runtime.JSException", problem.Type);
        Assert.Equal("Throw", problem.Context);
        Assert.Equal("Common failure", problem.Message);
        Assert.Equal(2, problem.Count);
        Assert.Equal(0.4, problem.OccurrenceRate);
        Assert.Equal("test/annexB/alpha.js", problem.Example.Path);
        Assert.Equal(114, problem.Example.LineNumber);
        Assert.Equal(
            [
                "test/annexB/alpha.js",
                "test/annexB/beta.js"
            ],
            problem.Occurrences.Select(occurrence => occurrence.Path).ToArray());
    }

    [Fact]
    public void FormatMostCommonProblem_EmitsGithubIssueMarkdown()
    {
        using var fixture = TempLogFile.Create("""
        {
          "suiteRef": "fixture-most-common-problem",
          "broilerDll": "fixture/BroilerJS.dll",
          "executed": 5,
          "passed": 0,
          "failed": 5,
          "skipped": 0,
          "results": [
            {
              "path": "test/annexB/alpha.js",
              "status": "failed",
              "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Common failure\nat Throw in /repo/JSException.cs:line 114\n"
            },
            {
              "path": "test/annexB/beta.js",
              "status": "failed",
              "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Common failure\nat Throw in /repo/JSException.cs:line 114\n"
            },
            {
              "path": "test/annexB/gamma.js",
              "status": "failed",
              "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Less common failure\nat Throw in /repo/JSException.cs:line 120\n"
            },
            {
              "path": "test/annexB/delta.js",
              "status": "failed",
              "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Different context failure\nat Handle in /repo/JSException.cs:line 140\n"
            },
            {
              "path": "test/annexB/epsilon.js",
              "status": "failed",
              "stderr": "Unhandled exception. System.InvalidOperationException: Other failure\nat Throw in /repo/Program.cs:line 10\n"
            }
          ]
        }
        """);

        var formatted = LogReportFormatter.FormatMostCommonProblem(
        [
            LogSummaryBuilder.ParseAndSummarize(fixture.Path)
        ]);

        Assert.Contains("### Description", formatted, StringComparison.Ordinal);
        Assert.Contains("Most common exception detected in recent logs.", formatted, StringComparison.Ordinal);
        Assert.Contains("- **Exception type:** Broiler.JavaScript.Runtime.JSException", formatted, StringComparison.Ordinal);
        Assert.Contains("- **Line number:** 114", formatted, StringComparison.Ordinal);
        Assert.Contains("- **Context:** Throw", formatted, StringComparison.Ordinal);
        Assert.Contains("- **Message:** Common failure", formatted, StringComparison.Ordinal);
        Assert.Contains("- **Sample filenames/paths:**", formatted, StringComparison.Ordinal);
        Assert.Contains("  - test/annexB/alpha.js", formatted, StringComparison.Ordinal);
        Assert.Contains("  - test/annexB/beta.js", formatted, StringComparison.Ordinal);
        Assert.DoesNotContain("count:", formatted, StringComparison.Ordinal);
        Assert.DoesNotContain("logLine:", formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatMostCommonProblem_LimitsSamplePathsToTwentyUniqueFiles()
    {
        var results = Enumerable.Range(1, 25)
            .Select(index => $$"""
            {
              "path": "test/generated/file-{{index:D2}}.js",
              "status": "failed",
              "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Common failure\nat Throw in /repo/JSException.cs:line 114\n"
            }
            """)
            .Concat(
            [
                """
                {
                  "path": "test/generated/file-01.js",
                  "status": "failed",
                  "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Common failure\nat Throw in /repo/JSException.cs:line 114\n"
                }
                """
            ]);

        using var fixture = TempLogFile.Create($$"""
        {
          "suiteRef": "fixture-most-common-problem-many-files",
          "broilerDll": "fixture/BroilerJS.dll",
          "executed": 26,
          "passed": 0,
          "failed": 26,
          "skipped": 0,
          "results": [
            {{string.Join(",\n", results)}}
          ]
        }
        """);

        var formatted = LogReportFormatter.FormatMostCommonProblem(
        [
            LogSummaryBuilder.ParseAndSummarize(fixture.Path)
        ]);

        Assert.Equal(20, formatted.Split('\n').Count(line => line.StartsWith("  - test/generated/file-", StringComparison.Ordinal)));
        Assert.Contains("  - test/generated/file-01.js", formatted, StringComparison.Ordinal);
        Assert.Contains("  - test/generated/file-20.js", formatted, StringComparison.Ordinal);
        Assert.DoesNotContain("  - test/generated/file-21.js", formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatMostCommonProblemJson_SerializesStructuredProblemReport()
    {
        using var fixture = TempLogFile.Create("""
        {
          "suiteRef": "fixture-most-common-problem",
          "broilerDll": "fixture/BroilerJS.dll",
          "executed": 5,
          "passed": 0,
          "failed": 5,
          "skipped": 0,
          "results": [
            {
              "path": "test/annexB/alpha.js",
              "status": "failed",
              "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Common failure\nat Throw in /repo/JSException.cs:line 114\n"
            },
            {
              "path": "test/annexB/beta.js",
              "status": "failed",
              "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Common failure\nat Throw in /repo/JSException.cs:line 114\n"
            },
            {
              "path": "test/annexB/gamma.js",
              "status": "failed",
              "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Less common failure\nat Throw in /repo/JSException.cs:line 120\n"
            },
            {
              "path": "test/annexB/delta.js",
              "status": "failed",
              "stderr": "Unhandled exception. Broiler.JavaScript.Runtime.JSException: Different context failure\nat Handle in /repo/JSException.cs:line 140\n"
            },
            {
              "path": "test/annexB/epsilon.js",
              "status": "failed",
              "stderr": "Unhandled exception. System.InvalidOperationException: Other failure\nat Throw in /repo/Program.cs:line 10\n"
            }
          ]
        }
        """);

        var json = LogReportFormatter.FormatMostCommonProblemJson(
        [
            LogSummaryBuilder.ParseAndSummarize(fixture.Path)
        ]);

        Assert.Contains("\"outputFormat\": \"json\"", json, StringComparison.Ordinal);
        Assert.Contains("\"problem\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\": \"Broiler.JavaScript.Runtime.JSException\"", json, StringComparison.Ordinal);
        Assert.Contains("\"context\": \"Throw\"", json, StringComparison.Ordinal);
        Assert.Contains("\"message\": \"Common failure\"", json, StringComparison.Ordinal);
        Assert.Contains("\"count\": 2", json, StringComparison.Ordinal);
        Assert.Contains("\"lineNumber\": 114", json, StringComparison.Ordinal);
        Assert.Contains("\"occurrences\"", json, StringComparison.Ordinal);
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

    public static IEnumerable<object[]> GetMostCommonProblemFlagArgs()
    {
        yield return new object[] { new[] { "--most-common-problem", "sample.json" } };
        yield return new object[] { new[] { "--most-common", "--output", "json", "sample.json" } };
    }

    private static string GetTestDataDirectoryPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "TestData");
    }

    private static string GetExceptionFixturePath()
    {
        return Path.Combine(AppContext.BaseDirectory, "TestData", "sample-exceptions.json");
    }

    private sealed class TempLogFile(string path) : IDisposable
    {
        public string Path { get; } = path;

        public static TempLogFile Create(string content)
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
            File.WriteAllText(path, content);
            return new TempLogFile(path);
        }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
