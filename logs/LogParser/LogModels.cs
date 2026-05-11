namespace LogParser;

/// <summary>
/// Represents a test262 shard log file.
/// </summary>
public sealed class LogRun
{
    public string SuiteRef { get; set; } = string.Empty;
    public string BroilerDll { get; set; } = string.Empty;
    public int Executed { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public LogEntry[] Results { get; set; } = [];
}

/// <summary>
/// Represents a single log entry in the shard output.
/// </summary>
public sealed class LogEntry
{
    public string Path { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Stdout { get; set; }
    public string? Stderr { get; set; }
    public ParsedException? Exception { get; set; }

    public int StdoutLength => Stdout?.Length ?? 0;

    public int StderrLength => Stderr?.Length ?? 0;
}

/// <summary>
/// Structured exception details extracted from a log line and its stack trace context.
/// </summary>
public sealed class ParsedException
{
    public required string Type { get; init; }
    public required string Message { get; init; }
    public string? Context { get; init; }
    public required string LogLine { get; init; }
}

/// <summary>
/// Stores a parsed file and the summaries derived from it.
/// </summary>
public sealed class LogFileSummary
{
    public required string FilePath { get; init; }
    public required bool IsDirectorySummary { get; init; }
    public required LogRun LogRun { get; init; }
    public required IReadOnlyList<LogGroupSummary> StatusGroups { get; init; }
    public required IReadOnlyList<LogGroupSummary> PathGroups { get; init; }
    public required ExceptionAnalysisSummary ExceptionSummary { get; init; }
    public required int BucketDepth { get; init; }
}

/// <summary>
/// Aggregated statistics for a group of log entries.
/// </summary>
public sealed class LogGroupSummary
{
    public required string Key { get; init; }
    public required int Count { get; init; }
    public required IReadOnlyDictionary<string, int> StatusCounts { get; init; }
    public required int MinStdoutLength { get; init; }
    public required int MaxStdoutLength { get; init; }
    public required double AverageStdoutLength { get; init; }
    public required int MinStderrLength { get; init; }
    public required int MaxStderrLength { get; init; }
    public required double AverageStderrLength { get; init; }
    public required IReadOnlyList<NotableLogEntry> NotableEntries { get; init; }
}

/// <summary>
/// A representative entry surfaced in the summary because it stands out within its group.
/// </summary>
public sealed class NotableLogEntry
{
    public required string Path { get; init; }
    public required string Status { get; init; }
    public required int StdoutLength { get; init; }
    public required int StderrLength { get; init; }
}

/// <summary>
/// Aggregated exception statistics derived from parsed log entries.
/// </summary>
public sealed class ExceptionAnalysisSummary
{
    public required int TotalEntriesWithExceptions { get; init; }
    public required double OccurrenceRate { get; init; }
    public required IReadOnlyList<ExceptionTypeSummary> TypeGroups { get; init; }
    public required IReadOnlyList<ExceptionContextSummary> ContextGroups { get; init; }
    public required IReadOnlyList<ExceptionMessageSummary> MessageGroups { get; init; }
    public required IReadOnlyList<string> SuggestedPatterns { get; init; }
}

/// <summary>
/// Aggregated statistics for a specific exception type.
/// </summary>
public sealed class ExceptionTypeSummary
{
    public required string Type { get; init; }
    public required int Count { get; init; }
    public required double OccurrenceRate { get; init; }
    public required int DistinctMessageCount { get; init; }
    public required IReadOnlyList<ExceptionExample> Examples { get; init; }
}

/// <summary>
/// Aggregated statistics for a specific exception type within a parsed method or function context.
/// </summary>
public sealed class ExceptionContextSummary
{
    public required string Type { get; init; }
    public required string Context { get; init; }
    public required int Count { get; init; }
    public required double OccurrenceRate { get; init; }
    public required int DistinctMessageCount { get; init; }
    public required IReadOnlyList<ExceptionExample> Examples { get; init; }
}

/// <summary>
/// Aggregated statistics for a specific exception message.
/// </summary>
public sealed class ExceptionMessageSummary
{
    public required string Message { get; init; }
    public required int Count { get; init; }
    public required double OccurrenceRate { get; init; }
    public required IReadOnlyList<ExceptionExample> Examples { get; init; }
}

/// <summary>
/// A representative parsed exception surfaced in the summary.
/// </summary>
public sealed class ExceptionExample
{
    public required string Path { get; init; }
    public required string Type { get; init; }
    public required string Message { get; init; }
    public string? Context { get; init; }
    public required string LogLine { get; init; }
}
