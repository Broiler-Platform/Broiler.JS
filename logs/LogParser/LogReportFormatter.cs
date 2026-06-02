using System.Text;
using System.Text.Json;

namespace LogParser;

/// <summary>
/// Formats file and group summaries for console output.
/// </summary>
public static class LogReportFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string Format(IEnumerable<LogFileSummary> fileSummaries)
    {
        var report = CreateReport(fileSummaries, outputFormat: "text");
        var builder = new StringBuilder();

        for (var i = 0; i < report.Summaries.Count; i++)
        {
            if (i > 0)
            {
                builder.AppendLine();
            }

            AppendFileSummary(builder, report.Summaries[i]);
        }

        return builder.ToString().TrimEnd();
    }

    public static string FormatJson(IEnumerable<LogFileSummary> fileSummaries)
    {
        return JsonSerializer.Serialize(CreateReport(fileSummaries, outputFormat: "json"), JsonOptions);
    }

    public static string FormatMostCommonProblem(IEnumerable<LogFileSummary> fileSummaries)
    {
        var report = CreateMostCommonProblemReport(fileSummaries, outputFormat: "text");
        var builder = new StringBuilder();

        if (report.Problem is null)
        {
            return "### Description\nMost common exception detected in recent logs.\n\n- No parsed exceptions were found.";
        }

        builder.AppendLine("### Description");
        builder.AppendLine("Most common exception detected in recent logs.");
        builder.AppendLine();
        AppendMostCommonProblem(builder, report.Problem);

        return builder.ToString().TrimEnd();
    }

    public static string FormatMostCommonProblemJson(IEnumerable<LogFileSummary> fileSummaries)
    {
        return JsonSerializer.Serialize(CreateMostCommonProblemReport(fileSummaries, outputFormat: "json"), JsonOptions);
    }

    public static string FormatMostCommonProblems(IEnumerable<LogFileSummary> fileSummaries)
    {
        var report = CreateMostCommonProblemsReport(fileSummaries, outputFormat: "text");
        var builder = new StringBuilder();

        if (report.Problems.Count == 0)
        {
            return "### Description\nMost common exceptions detected in recent logs.\n\n- No parsed exceptions were found.";
        }

        builder.AppendLine("### Description");
        builder.AppendLine("Three most common exceptions detected in recent logs.");

        for (var i = 0; i < report.Problems.Count; i++)
        {
            builder.AppendLine();
            builder.AppendLine($"#### Problem {i + 1}");
            AppendMostCommonProblem(builder, report.Problems[i]);
        }

        return builder.ToString().TrimEnd();
    }

    public static string FormatMostCommonProblemsJson(IEnumerable<LogFileSummary> fileSummaries)
    {
        return JsonSerializer.Serialize(CreateMostCommonProblemsReport(fileSummaries, outputFormat: "json"), JsonOptions);
    }

    public static string FormatHighestImpactProblem(IEnumerable<LogFileSummary> fileSummaries)
    {
        var report = CreateHighestImpactProblemReport(fileSummaries, outputFormat: "text");
        var builder = new StringBuilder();

        if (report.Problem is null)
        {
            return "### Description\nHighest-impact exception detected in recent logs.\n\n- No parsed exceptions were found.";
        }

        builder.AppendLine("### Description");
        builder.AppendLine("Highest-impact exception detected in recent logs.");
        builder.AppendLine();
        builder.AppendLine($"- **Exception type:** {report.Problem.Type}");
        builder.AppendLine($"- **Line number:** {FormatLineNumber(report.Problem.Example.LineNumber)}");
        builder.AppendLine($"- **Context:** {report.Problem.Context}");
        builder.AppendLine($"- **Message:** {report.Problem.Message}");
        builder.AppendLine(
            $"- **Impact score:** {report.Problem.ImpactScore.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)} "
            + $"(spans {report.Problem.DistinctPathBucketCount} distinct path bucket(s), {report.Problem.Count} occurrence(s))");
        builder.AppendLine("- **Sample filenames/paths:**");

        var samplePaths = report.Problem.Occurrences
            .Select(occurrence => occurrence.Path)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToArray();

        if (samplePaths.Length == 0)
        {
            builder.AppendLine("  - (no sample files available)");
        }
        else
        {
            foreach (var samplePath in samplePaths)
            {
                builder.AppendLine($"  - {samplePath}");
            }
        }

        return builder.ToString().TrimEnd();
    }

    public static string FormatHighestImpactProblemJson(IEnumerable<LogFileSummary> fileSummaries)
    {
        return JsonSerializer.Serialize(CreateHighestImpactProblemReport(fileSummaries, outputFormat: "json"), JsonOptions);
    }

    public static string FormatFilteredExceptions(
        IEnumerable<LogFileSummary> fileSummaries,
        string? typeFilter,
        string? contextFilter,
        string? messageFilter = null)
    {
        var report = CreateFilteredExceptionReport(fileSummaries, outputFormat: "text", typeFilter, contextFilter, messageFilter);
        var builder = new StringBuilder();

        builder.AppendLine("Filters:");
        builder.AppendLine($"  type: {report.Filters.Type ?? "(any)"}");
        builder.AppendLine($"  context: {report.Filters.Context ?? "(any)"}");
        builder.AppendLine($"  message: {report.Filters.Message ?? "(any)"}");
        builder.AppendLine("Matches:");

        if (report.Matches.Count == 0)
        {
            builder.AppendLine("  []");
            return builder.ToString().TrimEnd();
        }

        foreach (var match in report.Matches)
        {
            builder.AppendLine($"  {(match.Source.Kind == "directory" ? "Directory" : "File")}: {match.Source.Name}");
            builder.AppendLine($"    path: {match.Source.Path}");
            builder.AppendLine("    exceptions:");

            foreach (var exception in match.Exceptions)
            {
                builder.AppendLine("      -");
                builder.AppendLine($"        path: {exception.Path}");
                builder.AppendLine($"        type: {exception.Type}");
                builder.AppendLine($"        context: {exception.Context ?? "(unknown context)"}");
                builder.AppendLine($"        message: {exception.Message}");
                builder.AppendLine($"        lineNumber: {FormatLineNumber(exception.LineNumber)}");
                builder.AppendLine($"        logLine: {exception.LogLine}");
            }
        }

        return builder.ToString().TrimEnd();
    }

    public static string FormatFilteredExceptionsJson(
        IEnumerable<LogFileSummary> fileSummaries,
        string? typeFilter,
        string? contextFilter,
        string? messageFilter = null)
    {
        return JsonSerializer.Serialize(
            CreateFilteredExceptionReport(fileSummaries, outputFormat: "json", typeFilter, contextFilter, messageFilter),
            JsonOptions);
    }

    internal static LogReport CreateReport(IEnumerable<LogFileSummary> fileSummaries, string outputFormat)
    {
        return new LogReport
        {
            OutputFormat = outputFormat,
            Summaries = fileSummaries
                .Select(summary => new LogReportSummary
                {
                    Source = new LogReportSource
                    {
                        Kind = summary.IsDirectorySummary ? "directory" : "file",
                        Name = GetDisplayName(summary),
                        Path = summary.FilePath
                    },
                    Metadata = new LogReportMetadata
                    {
                        SuiteRef = summary.LogRun.SuiteRef,
                        BroilerDll = summary.LogRun.BroilerDll,
                        BucketDepth = summary.BucketDepth
                    },
                    Totals = new LogReportTotals
                    {
                        DeclaredExecuted = summary.LogRun.Executed,
                        Passed = summary.LogRun.Passed,
                        Failed = summary.LogRun.Failed,
                        Skipped = summary.LogRun.Skipped,
                        ParsedResults = summary.LogRun.Results.Length
                    },
                    StatusGroups = summary.StatusGroups,
                    PathGroups = summary.PathGroups,
                    ExceptionSummary = summary.ExceptionSummary
                })
                .ToArray()
        };
    }

    internal static FilteredExceptionReport CreateFilteredExceptionReport(
        IEnumerable<LogFileSummary> fileSummaries,
        string outputFormat,
        string? typeFilter,
        string? contextFilter,
        string? messageFilter = null)
    {
        return new FilteredExceptionReport
        {
            OutputFormat = outputFormat,
            Filters = new FilteredExceptionFilters
            {
                Type = typeFilter,
                Context = contextFilter,
                Message = messageFilter
            },
            Matches = fileSummaries
                .Select(summary => new FilteredExceptionMatch
                {
                    Source = new LogReportSource
                    {
                        Kind = summary.IsDirectorySummary ? "directory" : "file",
                        Name = GetDisplayName(summary),
                        Path = summary.FilePath
                    },
                    Exceptions = summary.LogRun.Results
                        .Where(entry => entry.Exception is not null && MatchesExceptionFilter(entry.Exception!, typeFilter, contextFilter, messageFilter))
                        .OrderBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(entry => entry.Exception!.Type, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(entry => entry.Exception!.Context ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                        .Select(entry => new LoggedException
                        {
                            Path = entry.Path,
                            Type = entry.Exception!.Type,
                            Message = entry.Exception.Message,
                            Context = entry.Exception.Context,
                            LineNumber = entry.Exception.LineNumber,
                            LogLine = entry.Exception.LogLine
                        })
                        .ToArray()
                })
                .Where(match => match.Exceptions.Count > 0)
                .ToArray()
        };
    }

    internal static MostCommonProblemReport CreateMostCommonProblemReport(
        IEnumerable<LogFileSummary> fileSummaries,
        string outputFormat)
    {
        var summaries = fileSummaries.ToArray();
        return new MostCommonProblemReport
        {
            OutputFormat = outputFormat,
            Problem = LogSummaryBuilder.FindMostCommonProblem(summaries.SelectMany(summary => summary.LogRun.Results))
        };
    }

    internal static MostCommonProblemsReport CreateMostCommonProblemsReport(
        IEnumerable<LogFileSummary> fileSummaries,
        string outputFormat)
    {
        var summaries = fileSummaries.ToArray();
        return new MostCommonProblemsReport
        {
            OutputFormat = outputFormat,
            Problems = LogSummaryBuilder.FindMostCommonProblems(summaries.SelectMany(summary => summary.LogRun.Results))
        };
    }

    internal static HighestImpactProblemReport CreateHighestImpactProblemReport(
        IEnumerable<LogFileSummary> fileSummaries,
        string outputFormat)
    {
        var summaries = fileSummaries.ToArray();
        var bucketDepth = summaries.Length > 0 ? summaries[0].BucketDepth : 4;
        return new HighestImpactProblemReport
        {
            OutputFormat = outputFormat,
            Problem = LogSummaryBuilder.FindHighestImpactProblem(
                summaries.SelectMany(summary => summary.LogRun.Results),
                bucketDepth)
        };
    }

    private static void AppendFileSummary(StringBuilder builder, LogReportSummary summary)
    {
        builder.AppendLine($"{(summary.Source.Kind == "directory" ? "Directory" : "File")}: {summary.Source.Name}");
        builder.AppendLine("  Source:");
        builder.AppendLine($"    kind: {summary.Source.Kind}");
        builder.AppendLine($"    path: {summary.Source.Path}");
        builder.AppendLine("  Metadata:");
        builder.AppendLine($"    suiteRef: {summary.Metadata.SuiteRef}");
        builder.AppendLine($"    broilerDll: {summary.Metadata.BroilerDll}");
        builder.AppendLine($"    bucketDepth: {summary.Metadata.BucketDepth}");
        builder.AppendLine("  Totals:");
        builder.AppendLine($"    declaredExecuted: {summary.Totals.DeclaredExecuted}");
        builder.AppendLine($"    passed: {summary.Totals.Passed}");
        builder.AppendLine($"    failed: {summary.Totals.Failed}");
        builder.AppendLine($"    skipped: {summary.Totals.Skipped}");
        builder.AppendLine($"    parsedResults: {summary.Totals.ParsedResults}");

        AppendGroupSection(builder, "Status groups", summary.StatusGroups);
        AppendGroupSection(builder, $"Path groups (depth {summary.Metadata.BucketDepth})", summary.PathGroups);
        AppendExceptionSection(builder, summary.ExceptionSummary, summary.Totals.ParsedResults);
    }

    private static void AppendGroupSection(StringBuilder builder, string title, IEnumerable<LogGroupSummary> groups)
    {
        builder.AppendLine($"  {title}:");
        foreach (var group in groups)
        {
            builder.AppendLine("    -");
            builder.AppendLine($"      key: {group.Key}");
            builder.AppendLine($"      count: {group.Count}");
            builder.AppendLine($"      statusCounts: {FormatStatusCounts(group.StatusCounts)}");
            builder.AppendLine(
                $"      stdoutLength: min={group.MinStdoutLength}, avg={group.AverageStdoutLength:F1}, max={group.MaxStdoutLength}");
            builder.AppendLine(
                $"      stderrLength: min={group.MinStderrLength}, avg={group.AverageStderrLength:F1}, max={group.MaxStderrLength}");
            builder.AppendLine("      notableEntries:");

            foreach (var notableEntry in group.NotableEntries)
            {
                builder.AppendLine("        -");
                builder.AppendLine($"          path: {notableEntry.Path}");
                builder.AppendLine($"          status: {notableEntry.Status}");
                builder.AppendLine($"          stdoutLength: {notableEntry.StdoutLength}");
                builder.AppendLine($"          stderrLength: {notableEntry.StderrLength}");
            }

            if (group.NotableEntries.Count == 0)
            {
                builder.AppendLine("        []");
            }
        }
    }

    private static void AppendMostCommonProblem(StringBuilder builder, MostCommonProblemMatch problem)
    {
        builder.AppendLine($"- **Exception type:** {problem.Type}");
        builder.AppendLine($"- **Line number:** {FormatLineNumber(problem.Example.LineNumber)}");
        builder.AppendLine($"- **Context:** {problem.Context}");
        builder.AppendLine($"- **Message:** {problem.Message}");
        builder.AppendLine("- **Sample filenames/paths:**");

        var samplePaths = problem.Occurrences
            .Select(occurrence => occurrence.Path)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToArray();

        if (samplePaths.Length == 0)
        {
            builder.AppendLine("  - (no sample files available)");
            return;
        }

        foreach (var samplePath in samplePaths)
        {
            builder.AppendLine($"  - {samplePath}");
        }
    }

    private static string FormatStatusCounts(IReadOnlyDictionary<string, int> statusCounts)
    {
        return string.Join(", ", statusCounts.Select(pair => $"{pair.Key}={pair.Value}"));
    }

    private static void AppendExceptionSection(
        StringBuilder builder,
        ExceptionAnalysisSummary summary,
        int totalResultCount)
    {
        builder.AppendLine("  Exception summary:");
        builder.AppendLine($"    totalEntriesWithExceptions: {summary.TotalEntriesWithExceptions}");
        builder.AppendLine($"    totalResults: {totalResultCount}");
        builder.AppendLine($"    occurrenceRate: {summary.OccurrenceRate:P1}");

        if (summary.TotalEntriesWithExceptions == 0)
        {
            builder.AppendLine("    typeGroups:");
            builder.AppendLine("      []");
            builder.AppendLine("    contextGroups:");
            builder.AppendLine("      []");
            builder.AppendLine("    messageGroups:");
            builder.AppendLine("      []");
            builder.AppendLine("    suggestedPatterns:");
            builder.AppendLine("      []");
            return;
        }

        builder.AppendLine("    typeGroups:");
        foreach (var group in summary.TypeGroups)
        {
            builder.AppendLine("      -");
            builder.AppendLine($"        type: {group.Type}");
            builder.AppendLine($"        count: {group.Count}");
            builder.AppendLine($"        occurrenceRate: {group.OccurrenceRate:P1}");
            builder.AppendLine($"        distinctMessageCount: {group.DistinctMessageCount}");
            builder.AppendLine("        exceptions:");

            foreach (var entry in group.Exceptions)
            {
                builder.AppendLine("          -");
                builder.AppendLine($"            path: {entry.Path}");
                builder.AppendLine($"            type: {entry.Type}");
                builder.AppendLine($"            context: {entry.Context ?? "(unknown context)"}");
                builder.AppendLine($"            message: {entry.Message}");
                builder.AppendLine($"            lineNumber: {FormatLineNumber(entry.LineNumber)}");
                builder.AppendLine($"            logLine: {entry.LogLine}");
            }

            if (group.Exceptions.Count == 0)
            {
                builder.AppendLine("          []");
            }
        }

        builder.AppendLine("    contextGroups:");
        foreach (var group in summary.ContextGroups)
        {
            builder.AppendLine("      -");
            builder.AppendLine($"        type: {group.Type}");
            builder.AppendLine($"        context: {group.Context}");
            builder.AppendLine($"        count: {group.Count}");
            builder.AppendLine($"        occurrenceRate: {group.OccurrenceRate:P1}");
            builder.AppendLine($"        distinctMessageCount: {group.DistinctMessageCount}");
            builder.AppendLine("        exceptions:");

            foreach (var entry in group.Exceptions)
            {
                builder.AppendLine("          -");
                builder.AppendLine($"            path: {entry.Path}");
                builder.AppendLine($"            type: {entry.Type}");
                builder.AppendLine($"            context: {entry.Context ?? "(unknown context)"}");
                builder.AppendLine($"            message: {entry.Message}");
                builder.AppendLine($"            lineNumber: {FormatLineNumber(entry.LineNumber)}");
                builder.AppendLine($"            logLine: {entry.LogLine}");
            }

            if (group.Exceptions.Count == 0)
            {
                builder.AppendLine("          []");
            }
        }

        builder.AppendLine("    messageGroups:");
        foreach (var group in summary.MessageGroups)
        {
            builder.AppendLine("      -");
            builder.AppendLine($"        message: {group.Message}");
            builder.AppendLine($"        count: {group.Count}");
            builder.AppendLine($"        occurrenceRate: {group.OccurrenceRate:P1}");
            builder.AppendLine("        exceptions:");

            foreach (var entry in group.Exceptions)
            {
                builder.AppendLine("          -");
                builder.AppendLine($"            path: {entry.Path}");
                builder.AppendLine($"            type: {entry.Type}");
                builder.AppendLine($"            context: {entry.Context ?? "(unknown context)"}");
                builder.AppendLine($"            message: {entry.Message}");
                builder.AppendLine($"            lineNumber: {FormatLineNumber(entry.LineNumber)}");
                builder.AppendLine($"            logLine: {entry.LogLine}");
            }

            if (group.Exceptions.Count == 0)
            {
                builder.AppendLine("          []");
            }
        }

        builder.AppendLine("    suggestedPatterns:");
        foreach (var pattern in summary.SuggestedPatterns)
        {
            builder.AppendLine($"      - {pattern}");
        }

        if (summary.SuggestedPatterns.Count == 0)
        {
            builder.AppendLine("      []");
        }
    }

    private static string GetDisplayName(LogFileSummary summary)
    {
        var trimmedPath = summary.FilePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fileName = Path.GetFileName(trimmedPath);
        return string.IsNullOrEmpty(fileName) ? trimmedPath : fileName;
    }

    private static bool MatchesExceptionFilter(ParsedException exception, string? typeFilter, string? contextFilter, string? messageFilter)
    {
        if (!string.IsNullOrEmpty(typeFilter)
            && !string.Equals(exception.Type, typeFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(contextFilter)
            && !string.Equals(exception.Context ?? string.Empty, contextFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(messageFilter)
            && exception.Message.IndexOf(messageFilter, StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        return true;
    }

    private static string FormatLineNumber(int? lineNumber)
    {
        return lineNumber?.ToString() ?? "(unknown)";
    }
}
