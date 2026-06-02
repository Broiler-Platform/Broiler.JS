using System.Text.Json;
using System.Text.RegularExpressions;

namespace LogParser;

/// <summary>
/// Parses shard logs and produces reusable group summaries.
/// </summary>
public static class LogSummaryBuilder
{
    private const string UnknownStatus = "unknown";
    private const string AllPathsBucket = "(all paths)";
    private const string UnhandledExceptionPrefix = "Unhandled exception. ";
    private const string UnknownContext = "(unknown context)";
    // Matches common .NET and JavaScript stack frames such as:
    //   at InitializeFactories in /repo/File.cs:line 17
    //   at MyClass.Method() in /repo/File.cs:line 17
    //   at Compile:/tmp/script.js:206,1
    // The "method" group captures the method/function token after "at " and trims any
    // trailing whitespace. When parentheses are present, the captured value stops before
    // the opening parenthesis so signatures and locations are not included in the context.
    private static readonly Regex StackFrameContextRegex = new(
        @"^\s*at\s+(?<method>.+?)(?:\s+in\s+|\(|:|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex DotNetStackFrameLineNumberRegex = new(
        @"\bline\s+(?<line>\d+)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex ScriptStackFrameLineNumberRegex = new(
        @":(?<line>\d+)(?:,\d+)?\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static LogRun ParseFile(string path)
    {
        using var stream = File.OpenRead(path);
        var parsed = JsonSerializer.Deserialize<LogRun>(stream, JsonOptions)
            ?? throw new InvalidOperationException($"Log file '{path}' is empty or invalid.");

        return Normalize(parsed);
    }

    public static LogFileSummary ParseAndSummarize(string path, int bucketDepth = 4, int notableEntryLimit = 3)
    {
        return SummarizeLogRun(
            Path.GetFullPath(path),
            ParseFile(path),
            isDirectorySummary: false,
            bucketDepth,
            notableEntryLimit);
    }

    public static LogFileSummary ParseAndSummarizeDirectory(string path, int bucketDepth = 4, int notableEntryLimit = 3)
    {
        var fullPath = Path.GetFullPath(path);
        var filePaths = Directory
            .GetFiles(fullPath, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(filePath => filePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (filePaths.Length == 0)
        {
            throw new InvalidOperationException($"Directory '{path}' does not contain any .json log files.");
        }

        return SummarizeLogRun(
            fullPath,
            CombineLogRuns(filePaths.Select(ParseFile)),
            isDirectorySummary: true,
            bucketDepth,
            notableEntryLimit);
    }

    private static LogFileSummary SummarizeLogRun(
        string path,
        LogRun logRun,
        bool isDirectorySummary,
        int bucketDepth,
        int notableEntryLimit)
    {
        return new LogFileSummary
        {
            FilePath = path,
            IsDirectorySummary = isDirectorySummary,
            LogRun = logRun,
            BucketDepth = bucketDepth,
            StatusGroups = SummarizeGroups(logRun.Results, entry => entry.Status, notableEntryLimit),
            PathGroups = SummarizeGroups(logRun.Results, entry => GetPathBucket(entry.Path, bucketDepth), notableEntryLimit),
            ExceptionSummary = SummarizeExceptions(logRun.Results)
        };
    }

    private static LogRun CombineLogRuns(IEnumerable<LogRun> logRuns)
    {
        var runs = logRuns.ToArray();
        return new LogRun
        {
            SuiteRef = CombineMetadata(runs.Select(run => run.SuiteRef)),
            BroilerDll = CombineMetadata(runs.Select(run => run.BroilerDll)),
            Executed = runs.Sum(run => run.Executed),
            Passed = runs.Sum(run => run.Passed),
            Failed = runs.Sum(run => run.Failed),
            Skipped = runs.Sum(run => run.Skipped),
            Results = runs.SelectMany(run => run.Results).ToArray()
        };
    }

    public static IReadOnlyList<LogGroupSummary> SummarizeGroups(
        IEnumerable<LogEntry> entries,
        Func<LogEntry, string> groupKeySelector,
        int notableEntryLimit = 3)
    {
        return entries
            .GroupBy(groupKeySelector, StringComparer.OrdinalIgnoreCase)
            .Select(group => SummarizeGroup(group, notableEntryLimit))
            .OrderByDescending(group => group.Count)
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string GetPathBucket(string path, int depth)
    {
        if (depth <= 0)
        {
            return AllPathsBucket;
        }

        var segments = path
            .Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return segments.Length <= depth
            ? path
            : string.Join('/', segments.Take(depth));
    }

    public static ExceptionAnalysisSummary SummarizeExceptions(IEnumerable<LogEntry> entries)
    {
        var allEntries = entries.ToArray();
        var exceptionEntries = allEntries
            .Where(entry => entry.Exception is not null)
            .Select(entry => (
                Entry: entry,
                Exception: entry.Exception!,
                LoggedException: new LoggedException
                {
                    Path = entry.Path ?? string.Empty,
                    Type = entry.Exception!.Type,
                    Message = entry.Exception.Message,
                    Context = entry.Exception.Context,
                    LineNumber = entry.Exception.LineNumber,
                    LogLine = entry.Exception.LogLine
                }))
            .ToArray();

        var totalEntryCount = allEntries.Length;
        var totalExceptionCount = exceptionEntries.Length;

        return new ExceptionAnalysisSummary
        {
            TotalEntriesWithExceptions = totalExceptionCount,
            OccurrenceRate = totalEntryCount == 0 ? 0 : totalExceptionCount / (double)totalEntryCount,
            TypeGroups = exceptionEntries
                .GroupBy(item => item.Exception.Type, StringComparer.OrdinalIgnoreCase)
                .Select(group => new ExceptionTypeSummary
                {
                    Type = group.Key,
                    Count = group.Count(),
                    OccurrenceRate = totalEntryCount == 0 ? 0 : group.Count() / (double)totalEntryCount,
                    DistinctMessageCount = group
                        .Select(item => item.Exception.Message)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count(),
                    Exceptions = SelectExceptions(group)
                })
                .OrderByDescending(group => group.Count)
                .ThenBy(group => group.Type, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            ContextGroups = exceptionEntries
                .GroupBy(item => item.Exception.Type, StringComparer.OrdinalIgnoreCase)
                .SelectMany(typeGroup => typeGroup
                    .GroupBy(item => item.Exception.Context ?? UnknownContext, StringComparer.OrdinalIgnoreCase)
                    .Select(group => new ExceptionContextSummary
                    {
                        Type = typeGroup.Key,
                        Context = group.Key,
                        Count = group.Count(),
                        OccurrenceRate = totalEntryCount == 0 ? 0 : group.Count() / (double)totalEntryCount,
                        DistinctMessageCount = group
                            .Select(item => item.Exception.Message)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .Count(),
                        Exceptions = SelectExceptions(group)
                    }))
                .OrderByDescending(group => group.Count)
                .ThenBy(group => group.Type, StringComparer.OrdinalIgnoreCase)
                .ThenBy(group => group.Context, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            MessageGroups = exceptionEntries
                .GroupBy(item => item.Exception.Message, StringComparer.OrdinalIgnoreCase)
                .Select(group => new ExceptionMessageSummary
                {
                    Message = group.Key,
                    Count = group.Count(),
                    OccurrenceRate = totalEntryCount == 0 ? 0 : group.Count() / (double)totalEntryCount,
                    Exceptions = SelectExceptions(group)
                })
                .OrderByDescending(group => group.Count)
                .ThenBy(group => group.Message, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            SuggestedPatterns = BuildSuggestedPatterns(exceptionEntries, totalExceptionCount)
        };
    }

    public static MostCommonProblemMatch? FindMostCommonProblem(IEnumerable<LogEntry> entries)
    {
        var allEntries = entries.ToArray();
        var exceptionSummary = SummarizeExceptions(allEntries);
        var mostCommonType = exceptionSummary.TypeGroups.FirstOrDefault();
        if (mostCommonType is null)
        {
            return null;
        }

        var mostCommonContext = exceptionSummary.ContextGroups
            .Where(group => string.Equals(group.Type, mostCommonType.Type, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(group => group.Count)
            .ThenBy(group => group.Context, StringComparer.OrdinalIgnoreCase)
            .First();

        var matchingOccurrences = allEntries
            .Where(entry => entry.Exception is not null
                && string.Equals(entry.Exception.Type, mostCommonType.Type, StringComparison.OrdinalIgnoreCase)
                && string.Equals(entry.Exception.Context ?? UnknownContext, mostCommonContext.Context, StringComparison.OrdinalIgnoreCase))
            .Select(entry => new LoggedException
            {
                Path = entry.Path,
                Type = entry.Exception!.Type,
                Message = entry.Exception.Message,
                Context = entry.Exception.Context,
                LineNumber = entry.Exception.LineNumber,
                LogLine = entry.Exception.LogLine
            })
            .GroupBy(exception => exception.Message, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderBy(exception => exception.Path, StringComparer.OrdinalIgnoreCase)
                .ThenBy(exception => exception.LineNumber ?? int.MaxValue)
                .ThenBy(exception => exception.LogLine, StringComparer.OrdinalIgnoreCase)
                .ToArray())
            .First();

        return new MostCommonProblemMatch
        {
            Type = mostCommonType.Type,
            Context = mostCommonContext.Context,
            Message = matchingOccurrences[0].Message,
            Count = matchingOccurrences.Length,
            OccurrenceRate = allEntries.Length == 0 ? 0 : matchingOccurrences.Length / (double)allEntries.Length,
            Example = matchingOccurrences[0],
            Occurrences = matchingOccurrences
        };
    }

    public static IReadOnlyList<MostCommonProblemMatch> FindMostCommonProblems(
        IEnumerable<LogEntry> entries,
        int limit = 3)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "Limit must be greater than zero.");
        }

        var allEntries = entries.ToArray();
        var exceptionEntries = allEntries
            .Where(entry => entry.Exception is not null)
            .Select(entry => (Entry: entry, Exception: entry.Exception!))
            .ToArray();

        if (exceptionEntries.Length == 0)
        {
            return [];
        }

        return exceptionEntries
            .GroupBy(item => item.Exception.Type, StringComparer.OrdinalIgnoreCase)
            .SelectMany(typeGroup => typeGroup
                .GroupBy(item => item.Exception.Context ?? UnknownContext, StringComparer.OrdinalIgnoreCase)
                .SelectMany(contextGroup => contextGroup
                    .GroupBy(item => item.Exception.Message, StringComparer.OrdinalIgnoreCase)
                    .Select(messageGroup => CreateMostCommonProblemMatch(
                        allEntries.Length,
                        typeGroup.Key,
                        contextGroup.Key,
                        messageGroup))))
            .OrderByDescending(problem => problem.Count)
            .ThenBy(problem => problem.Type, StringComparer.OrdinalIgnoreCase)
            .ThenBy(problem => problem.Context, StringComparer.OrdinalIgnoreCase)
            .ThenBy(problem => problem.Message, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToArray();
    }

    /// <summary>
    /// Path-area weight used to estimate the relative impact of a failure on
    /// the overall test surface.  The weights are intentionally simple proxy
    /// signals — higher weight means the area is closer to core language or
    /// built-in semantics, so a failure there is treated as more impactful
    /// than the same failure in an annex or staging area.
    /// </summary>
    /// <remarks>
    /// Weights:
    /// <list type="bullet">
    /// <item><description><c>test/language/</c> → 3.0 (core language semantics)</description></item>
    /// <item><description><c>test/built-ins/</c> → 2.0 (built-in objects)</description></item>
    /// <item><description><c>test/intl/</c> → 2.0 (built-in internationalization)</description></item>
    /// <item><description><c>test/harness/</c> → 1.5 (harness scaffolding)</description></item>
    /// <item><description><c>test/staging/</c> → 1.5 (in-flight proposals)</description></item>
    /// <item><description><c>test/annexB/</c> → 1.0 (legacy/optional features)</description></item>
    /// <item><description>everything else → 1.0</description></item>
    /// </list>
    /// </remarks>
    public static double GetAreaWeight(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return 1.0;
        }

        var normalized = path.Replace('\\', '/');
        if (normalized.StartsWith("test/language/", StringComparison.OrdinalIgnoreCase))
        {
            return 3.0;
        }

        if (normalized.StartsWith("test/built-ins/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("test/intl/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("test/intl402/", StringComparison.OrdinalIgnoreCase))
        {
            return 2.0;
        }

        if (normalized.StartsWith("test/harness/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("test/staging/", StringComparison.OrdinalIgnoreCase))
        {
            return 1.5;
        }

        return 1.0;
    }

    /// <summary>
    /// Returns the highest-impact problem after grouping parsed exceptions by
    /// (type, context) and (within that) by message, scoring each group by
    /// <see cref="GetAreaWeight"/> summed across occurrences and multiplied by
    /// the number of distinct path buckets the group spans.
    /// </summary>
    /// <remarks>
    /// The impact score for a group is computed as
    /// <c>sum(GetAreaWeight(path)) * max(1, distinctPathBucketCount)</c>.
    /// Higher-weight test areas and broader spread across the test suite both
    /// increase the score, so a failure that hits many core-language paths
    /// outranks a more frequent failure concentrated in a single legacy
    /// directory.  Ties are broken by raw count (desc), then type / context /
    /// message (asc) to keep the selection deterministic.
    /// </remarks>
    public static HighestImpactProblemMatch? FindHighestImpactProblem(
        IEnumerable<LogEntry> entries,
        int bucketDepth = 4)
    {
        var allEntries = entries.ToArray();
        var exceptionItems = allEntries
            .Where(entry => entry.Exception is not null)
            .Select(entry => new LoggedException
            {
                Path = entry.Path,
                Type = entry.Exception!.Type,
                Message = entry.Exception.Message,
                Context = entry.Exception.Context,
                LineNumber = entry.Exception.LineNumber,
                LogLine = entry.Exception.LogLine
            })
            .ToArray();

        if (exceptionItems.Length == 0)
        {
            return null;
        }

        var bestTypeContext = exceptionItems
            .GroupBy(item => (
                Type: item.Type,
                Context: item.Context ?? UnknownContext),
                TypeContextComparer.Instance)
            .Select(group => new
            {
                group.Key.Type,
                group.Key.Context,
                Score = ComputeImpactScore(group, bucketDepth, out var distinctBuckets),
                DistinctBuckets = distinctBuckets,
                Items = group.ToArray()
            })
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.Items.Length)
            .ThenBy(candidate => candidate.Type, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Context, StringComparer.OrdinalIgnoreCase)
            .First();

        var bestMessage = bestTypeContext.Items
            .GroupBy(item => item.Message, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                Message = group.Key,
                Score = ComputeImpactScore(group, bucketDepth, out _),
                Items = group
                    .OrderBy(item => item.Path, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(item => item.LineNumber ?? int.MaxValue)
                    .ThenBy(item => item.LogLine, StringComparer.OrdinalIgnoreCase)
                    .ToArray()
            })
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.Items.Length)
            .ThenBy(candidate => candidate.Message, StringComparer.OrdinalIgnoreCase)
            .First();

        return new HighestImpactProblemMatch
        {
            Type = bestTypeContext.Type,
            Context = bestTypeContext.Context,
            Message = bestMessage.Message,
            Count = bestMessage.Items.Length,
            OccurrenceRate = allEntries.Length == 0 ? 0 : bestMessage.Items.Length / (double)allEntries.Length,
            ImpactScore = bestTypeContext.Score,
            DistinctPathBucketCount = bestTypeContext.DistinctBuckets,
            Example = bestMessage.Items[0],
            Occurrences = bestMessage.Items
        };
    }

    private static double ComputeImpactScore(
        IEnumerable<LoggedException> exceptions,
        int bucketDepth,
        out int distinctBucketCount)
    {
        var materialized = exceptions as IReadOnlyCollection<LoggedException> ?? exceptions.ToArray();
        var weightSum = materialized.Sum(item => GetAreaWeight(item.Path));
        distinctBucketCount = materialized
            .Select(item => GetPathBucket(item.Path ?? string.Empty, bucketDepth))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var breadth = Math.Max(1, distinctBucketCount);
        return weightSum * breadth;
    }

    private sealed class TypeContextComparer : IEqualityComparer<(string Type, string Context)>
    {
        public static readonly TypeContextComparer Instance = new();

        public bool Equals((string Type, string Context) x, (string Type, string Context) y)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(x.Type, y.Type)
                && StringComparer.OrdinalIgnoreCase.Equals(x.Context, y.Context);
        }

        public int GetHashCode((string Type, string Context) obj)
        {
            return HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Type),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Context));
        }
    }

    private static LogGroupSummary SummarizeGroup(IGrouping<string, LogEntry> group, int notableEntryLimit)
    {
        var entries = group.ToArray();
        var stdoutLengths = entries.Select(entry => entry.StdoutLength).ToArray();
        var stderrLengths = entries.Select(entry => entry.StderrLength).ToArray();
        var notableEntries = entries
            .OrderByDescending(entry => Math.Max(entry.StdoutLength, entry.StderrLength))
            .ThenBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(0, notableEntryLimit))
            .Select(entry => new NotableLogEntry
            {
                Path = entry.Path,
                Status = entry.Status,
                StdoutLength = entry.StdoutLength,
                StderrLength = entry.StderrLength
            })
            .ToArray();

        return new LogGroupSummary
        {
            Key = group.Key,
            Count = entries.Length,
            StatusCounts = entries
                .GroupBy(entry => entry.Status, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(statusGroup => statusGroup.Count())
                .ThenBy(statusGroup => statusGroup.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(statusGroup => statusGroup.Key, statusGroup => statusGroup.Count(), StringComparer.OrdinalIgnoreCase),
            MinStdoutLength = stdoutLengths.Min(),
            MaxStdoutLength = stdoutLengths.Max(),
            AverageStdoutLength = stdoutLengths.Average(),
            MinStderrLength = stderrLengths.Min(),
            MaxStderrLength = stderrLengths.Max(),
            AverageStderrLength = stderrLengths.Average(),
            NotableEntries = notableEntries
        };
    }

    private static LogRun Normalize(LogRun parsed)
    {
        return new LogRun
        {
            SuiteRef = parsed.SuiteRef ?? string.Empty,
            BroilerDll = parsed.BroilerDll ?? string.Empty,
            Executed = parsed.Executed,
            Passed = parsed.Passed,
            Failed = parsed.Failed,
            Skipped = parsed.Skipped,
            Results = parsed.Results?
                .Select(entry => new LogEntry
                {
                    Path = entry.Path ?? string.Empty,
                    Status = string.IsNullOrWhiteSpace(entry.Status) ? UnknownStatus : entry.Status,
                    Stdout = entry.Stdout,
                    Stderr = entry.Stderr,
                    Exception = TryParseException(entry.Stderr) ?? TryParseException(entry.Stdout)
                })
                .ToArray()
                ?? []
        };
    }

    private static IReadOnlyList<LoggedException> SelectExceptions(
        IEnumerable<(LogEntry Entry, ParsedException Exception, LoggedException LoggedException)> entries)
    {
        return entries
            .OrderBy(item => item.Entry.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Exception.Message, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.LoggedException)
            .ToArray();
    }

    private static IReadOnlyList<string> BuildSuggestedPatterns(
        IEnumerable<(LogEntry Entry, ParsedException Exception, LoggedException LoggedException)> exceptionEntries,
        int totalExceptionCount)
    {
        if (totalExceptionCount == 0)
        {
            return [];
        }

        var repeatedMessages = exceptionEntries
            .GroupBy(item => item.Exception.Message, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Select(group =>
                $"Repeated message \"{group.Key}\" appears {group.Count()} times ({group.Count() / (double)totalExceptionCount:P1} of exception entries).")
            .ToArray();

        if (repeatedMessages.Length > 0)
        {
            return repeatedMessages;
        }

        var repeatedTypes = exceptionEntries
            .GroupBy(item => item.Exception.Type, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Select(group =>
                $"Exception type \"{group.Key}\" appears {group.Count()} times across {group.Select(item => item.Exception.Message).Distinct(StringComparer.OrdinalIgnoreCase).Count()} distinct messages.")
            .ToArray();

        if (repeatedTypes.Length > 0)
        {
            return repeatedTypes;
        }

        return exceptionEntries
            .GroupBy(item => item.Exception.Type, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Select(group =>
                $"Most frequent exception type so far is \"{group.Key}\" with {group.Count()} occurrence(s); compare its message and stack examples first.")
            .ToArray();
    }

    private static ParsedException? TryParseException(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var lineIndex = 0;
        foreach (var line in lines)
        {
            if (TryParseExceptionLine(line, out var exceptionType, out var exceptionMessage))
            {
                var stackFrame = TryParseExceptionFrame(lines, lineIndex + 1);
                return new ParsedException
                {
                    Type = exceptionType,
                    Message = exceptionMessage,
                    Context = stackFrame.Context,
                    LineNumber = stackFrame.LineNumber,
                    LogLine = line
                };
            }

            lineIndex++;
        }

        return null;
    }

    private static MostCommonProblemMatch CreateMostCommonProblemMatch(
        int totalEntryCount,
        string type,
        string context,
        IEnumerable<(LogEntry Entry, ParsedException Exception)> entries)
    {
        var occurrences = entries
            .Select(item => new LoggedException
            {
                Path = item.Entry.Path,
                Type = item.Exception.Type,
                Message = item.Exception.Message,
                Context = item.Exception.Context,
                LineNumber = item.Exception.LineNumber,
                LogLine = item.Exception.LogLine
            })
            .OrderBy(exception => exception.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(exception => exception.LineNumber ?? int.MaxValue)
            .ThenBy(exception => exception.LogLine, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new MostCommonProblemMatch
        {
            Type = type,
            Context = context,
            Message = occurrences[0].Message,
            Count = occurrences.Length,
            OccurrenceRate = totalEntryCount == 0 ? 0 : occurrences.Length / (double)totalEntryCount,
            Example = occurrences[0],
            Occurrences = occurrences
        };
    }

    /// <summary>
    /// Extracts stack frame details from the first recognizable stack frame that follows a parsed exception
    /// line, tolerating intervening wrapper lines until a stack frame is found.
    /// </summary>
    /// <param name="lines">All non-empty log lines from the captured output.</param>
    /// <param name="startIndex">The index immediately after the line that contained the exception header.</param>
    /// <returns>The parsed frame details, or empty details when no stack frame is present.</returns>
    private static ExceptionFrameDetails TryParseExceptionFrame(IReadOnlyList<string> lines, int startIndex)
    {
        for (var i = startIndex; i < lines.Count; i++)
        {
            var match = StackFrameContextRegex.Match(lines[i]);
            if (match.Success)
            {
                var context = match.Groups["method"].Value.Trim();
                if (!string.IsNullOrWhiteSpace(context))
                {
                    return new ExceptionFrameDetails(context, TryParseLineNumber(lines[i]));
                }
            }
        }

        return new ExceptionFrameDetails(null, null);
    }

    private static int? TryParseLineNumber(string stackFrame)
    {
        var lineMatch = DotNetStackFrameLineNumberRegex.Match(stackFrame);
        if (!lineMatch.Success)
        {
            lineMatch = ScriptStackFrameLineNumberRegex.Match(stackFrame);
        }

        return lineMatch.Success && int.TryParse(lineMatch.Groups["line"].Value, out var lineNumber)
            ? lineNumber
            : null;
    }

    private static bool TryParseExceptionLine(string line, out string exceptionType, out string exceptionMessage)
    {
        var candidate = line.Trim();
        if (candidate.StartsWith(UnhandledExceptionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            candidate = candidate[UnhandledExceptionPrefix.Length..].Trim();
        }

        var separatorIndex = candidate.IndexOf(": ", StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            exceptionType = string.Empty;
            exceptionMessage = string.Empty;
            return false;
        }

        var typeCandidate = candidate[..separatorIndex].Trim();
        if (!LooksLikeExceptionType(typeCandidate))
        {
            exceptionType = string.Empty;
            exceptionMessage = string.Empty;
            return false;
        }

        exceptionType = typeCandidate;
        exceptionMessage = candidate[(separatorIndex + 2)..].Trim();
        if (string.IsNullOrEmpty(exceptionMessage))
        {
            exceptionMessage = "(no message)";
        }

        return true;
    }

    private static bool LooksLikeExceptionType(string typeCandidate)
    {
        if (string.IsNullOrWhiteSpace(typeCandidate) || typeCandidate.Any(char.IsWhiteSpace))
        {
            return false;
        }

        return typeCandidate.EndsWith("Exception", StringComparison.Ordinal)
            || typeCandidate.EndsWith("Error", StringComparison.Ordinal)
            || typeCandidate.Contains('.', StringComparison.Ordinal);
    }

    private static string CombineMetadata(IEnumerable<string> values)
    {
        var distinctValues = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return distinctValues.Length switch
        {
            0 => string.Empty,
            1 => distinctValues[0],
            _ => string.Join(", ", distinctValues)
        };
    }

    private readonly record struct ExceptionFrameDetails(string? Context, int? LineNumber);
}
