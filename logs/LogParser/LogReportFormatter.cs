using System.Text;

namespace LogParser;

/// <summary>
/// Formats file and group summaries for console output.
/// </summary>
public static class LogReportFormatter
{
    public static string Format(IEnumerable<LogFileSummary> fileSummaries)
    {
        var summaries = fileSummaries.ToArray();
        var builder = new StringBuilder();

        for (var i = 0; i < summaries.Length; i++)
        {
            if (i > 0)
            {
                builder.AppendLine();
            }

            AppendFileSummary(builder, summaries[i]);
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendFileSummary(StringBuilder builder, LogFileSummary summary)
    {
        builder.AppendLine($"{(summary.IsDirectorySummary ? "Directory" : "File")}: {GetDisplayName(summary)}");
        builder.AppendLine($"  SuiteRef: {summary.LogRun.SuiteRef}");
        builder.AppendLine(
            $"  Totals: declared executed={summary.LogRun.Executed}, passed={summary.LogRun.Passed}, failed={summary.LogRun.Failed}, skipped={summary.LogRun.Skipped}; parsed results={summary.LogRun.Results.Length}");

        AppendGroupSection(builder, "Status groups", summary.StatusGroups);
        AppendGroupSection(builder, $"Path groups (depth {summary.BucketDepth})", summary.PathGroups);
        AppendExceptionSection(builder, summary.ExceptionSummary, summary.LogRun.Results.Length);
    }

    private static void AppendGroupSection(StringBuilder builder, string title, IEnumerable<LogGroupSummary> groups)
    {
        builder.AppendLine($"  {title}:");
        foreach (var group in groups)
        {
            builder.AppendLine(
                $"    - {group.Key}: count={group.Count}, statuses=[{FormatStatusCounts(group.StatusCounts)}], stdout(len min/avg/max)={group.MinStdoutLength}/{group.AverageStdoutLength:F1}/{group.MaxStdoutLength}, stderr(len min/avg/max)={group.MinStderrLength}/{group.AverageStderrLength:F1}/{group.MaxStderrLength}");

            foreach (var notableEntry in group.NotableEntries)
            {
                builder.AppendLine(
                    $"      notable: {notableEntry.Path} [{notableEntry.Status}] stdout={notableEntry.StdoutLength}, stderr={notableEntry.StderrLength}");
            }
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
        builder.AppendLine(
            $"    - entries with parsed exceptions={summary.TotalEntriesWithExceptions}/{totalResultCount} ({summary.OccurrenceRate:P1})");

        if (summary.TotalEntriesWithExceptions == 0)
        {
            builder.AppendLine("    - no exception lines were detected");
            return;
        }

        builder.AppendLine("    - by type:");
        foreach (var group in summary.TypeGroups)
        {
            builder.AppendLine(
                $"      - {group.Type}: count={group.Count}, rate={group.OccurrenceRate:P1}, distinct messages={group.DistinctMessageCount}");

            foreach (var example in group.Examples)
            {
                builder.AppendLine(
                    $"        example: {example.Path} => type={example.Type}, context={example.Context ?? "(unknown context)"}, message={example.Message}, log line=\"{example.LogLine}\"");
            }
        }

        builder.AppendLine("    - by type and context:");
        foreach (var group in summary.ContextGroups)
        {
            builder.AppendLine(
                $"      - {group.Type} in {group.Context}: count={group.Count}, rate={group.OccurrenceRate:P1}, distinct messages={group.DistinctMessageCount}");

            foreach (var example in group.Examples)
            {
                builder.AppendLine(
                    $"        example: {example.Path} => type={example.Type}, context={example.Context ?? "(unknown context)"}, message={example.Message}, log line=\"{example.LogLine}\"");
            }
        }

        builder.AppendLine("    - by message:");
        foreach (var group in summary.MessageGroups)
        {
            builder.AppendLine(
                $"      - {group.Message}: count={group.Count}, rate={group.OccurrenceRate:P1}");

            foreach (var example in group.Examples)
            {
                builder.AppendLine(
                    $"        example: {example.Path} => type={example.Type}, context={example.Context ?? "(unknown context)"}, message={example.Message}, log line=\"{example.LogLine}\"");
            }
        }

        builder.AppendLine("    - suggested patterns:");
        foreach (var pattern in summary.SuggestedPatterns)
        {
            builder.AppendLine($"      - {pattern}");
        }
    }

    private static string GetDisplayName(LogFileSummary summary)
    {
        var trimmedPath = summary.FilePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fileName = Path.GetFileName(trimmedPath);
        return string.IsNullOrEmpty(fileName) ? trimmedPath : fileName;
    }
}
