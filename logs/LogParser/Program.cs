namespace LogParser;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            var summaryInputs = ResolveSummaryInputs(args);
            if (summaryInputs.Count == 0)
            {
                Console.Error.WriteLine("No log files were found to summarize.");
                return 1;
            }

            var summaries = summaryInputs
                .Select(SummarizeInput)
                .ToArray();

            Console.WriteLine(LogReportFormatter.Format(summaries));
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to summarize logs: {ex.Message}");
            return 1;
        }
    }

    internal static IReadOnlyList<SummaryInput> ResolveSummaryInputs(IEnumerable<string> inputs)
    {
        var providedInputs = inputs.ToArray();
        if (providedInputs.Length == 0)
        {
            return Directory
                .GetFiles(GetDefaultLogsDirectory(), "*.json", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(path => new SummaryInput(path, IsDirectory: false))
                .ToArray();
        }

        return providedInputs
            .Select(ResolveSummaryInput)
            .GroupBy(input => input.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(input => input.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static LogFileSummary SummarizeInput(SummaryInput input)
    {
        return input.IsDirectory
            ? LogSummaryBuilder.ParseAndSummarizeDirectory(input.Path)
            : LogSummaryBuilder.ParseAndSummarize(input.Path);
    }

    private static SummaryInput ResolveSummaryInput(string input)
    {
        var fullPath = Path.GetFullPath(input);
        if (File.Exists(fullPath))
        {
            return new SummaryInput(fullPath, IsDirectory: false);
        }

        if (Directory.Exists(fullPath))
        {
            return new SummaryInput(fullPath, IsDirectory: true);
        }

        throw new FileNotFoundException($"Input path '{input}' does not exist.", fullPath);
    }

    private static string GetDefaultLogsDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "shard-7.json")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the logs directory automatically. Pass one or more log file paths explicitly.");
    }

    internal readonly record struct SummaryInput(string Path, bool IsDirectory);
}
