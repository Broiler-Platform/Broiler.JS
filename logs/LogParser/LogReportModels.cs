namespace LogParser;

/// <summary>
/// Structured report emitted by the log parser.
/// </summary>
public sealed class LogReport
{
    public required string OutputFormat { get; init; }
    public required IReadOnlyList<LogReportSummary> Summaries { get; init; }
}

/// <summary>
/// Structured report emitted when exception filters are active.
/// </summary>
public sealed class FilteredExceptionReport
{
    public required string OutputFormat { get; init; }
    public required FilteredExceptionFilters Filters { get; init; }
    public required IReadOnlyList<FilteredExceptionMatch> Matches { get; init; }
}

/// <summary>
/// Structured summary for a parsed file or directory.
/// </summary>
public sealed class LogReportSummary
{
    public required LogReportSource Source { get; init; }
    public required LogReportMetadata Metadata { get; init; }
    public required LogReportTotals Totals { get; init; }
    public required IReadOnlyList<LogGroupSummary> StatusGroups { get; init; }
    public required IReadOnlyList<LogGroupSummary> PathGroups { get; init; }
    public required ExceptionAnalysisSummary ExceptionSummary { get; init; }
}

/// <summary>
/// Active exception filters applied to a report.
/// </summary>
public sealed class FilteredExceptionFilters
{
    public string? Type { get; init; }
    public string? Context { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// Filtered exception matches for a single source file or directory.
/// </summary>
public sealed class FilteredExceptionMatch
{
    public required LogReportSource Source { get; init; }
    public required IReadOnlyList<LoggedException> Exceptions { get; init; }
}

/// <summary>
/// Identifies where a parsed summary came from.
/// </summary>
public sealed class LogReportSource
{
    public required string Kind { get; init; }
    public required string Name { get; init; }
    public required string Path { get; init; }
}

/// <summary>
/// File-level metadata retained in the structured report.
/// </summary>
public sealed class LogReportMetadata
{
    public required string SuiteRef { get; init; }
    public required string BroilerDll { get; init; }
    public required int BucketDepth { get; init; }
}

/// <summary>
/// Totals retained in the structured report.
/// </summary>
public sealed class LogReportTotals
{
    public required int DeclaredExecuted { get; init; }
    public required int Passed { get; init; }
    public required int Failed { get; init; }
    public required int Skipped { get; init; }
    public required int ParsedResults { get; init; }
}
