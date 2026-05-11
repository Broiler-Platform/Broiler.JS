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
        builder.AppendLine($"File: {Path.GetFileName(summary.FilePath)}");
        builder.AppendLine($"  SuiteRef: {summary.LogRun.SuiteRef}");
        builder.AppendLine(
            $"  Totals: declared executed={summary.LogRun.Executed}, passed={summary.LogRun.Passed}, failed={summary.LogRun.Failed}, skipped={summary.LogRun.Skipped}; parsed results={summary.LogRun.Results.Length}");

        AppendGroupSection(builder, "Status groups", summary.StatusGroups);
        AppendGroupSection(builder, $"Path groups (depth {summary.BucketDepth})", summary.PathGroups);
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
}
