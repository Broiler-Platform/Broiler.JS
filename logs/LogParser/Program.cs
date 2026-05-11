namespace LogParser;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            var options = ParseOptions(args);
            var summaryInputs = ResolveSummaryInputs(options.Inputs);
            if (summaryInputs.Count == 0)
            {
                Console.Error.WriteLine("No log files were found to summarize.");
                return 1;
            }

            var summaries = summaryInputs
                .Select(SummarizeInput)
                .ToArray();

            Console.WriteLine(FormatOutput(summaries, options.OutputFormat));
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to summarize logs: {ex.Message}");
            return 1;
        }
    }

    internal static ProgramOptions ParseOptions(IEnumerable<string> args)
    {
        var inputs = new List<string>();
        var outputFormat = "text";
        var pendingOption = string.Empty;

        foreach (var arg in args)
        {
            if (string.Equals(pendingOption, "--output", StringComparison.Ordinal))
            {
                outputFormat = NormalizeOutputFormat(arg);
                pendingOption = string.Empty;
                continue;
            }

            if (arg.StartsWith("--output=", StringComparison.Ordinal))
            {
                outputFormat = NormalizeOutputFormat(arg["--output=".Length..]);
                continue;
            }

            if (string.Equals(arg, "--output", StringComparison.Ordinal)
                || string.Equals(arg, "-o", StringComparison.Ordinal))
            {
                pendingOption = "--output";
                continue;
            }

            if (arg.StartsWith("-o=", StringComparison.Ordinal))
            {
                outputFormat = NormalizeOutputFormat(arg["-o=".Length..]);
                continue;
            }

            inputs.Add(arg);
        }

        if (!string.IsNullOrEmpty(pendingOption))
        {
            throw new ArgumentException("Missing value for --output.");
        }

        return new ProgramOptions(inputs, outputFormat);
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

    private static string FormatOutput(IEnumerable<LogFileSummary> summaries, string outputFormat)
    {
        return outputFormat switch
        {
            "text" => LogReportFormatter.Format(summaries),
            "json" => LogReportFormatter.FormatJson(summaries),
            _ => throw new ArgumentOutOfRangeException(nameof(outputFormat), outputFormat, "Unsupported output format.")
        };
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

    private static string NormalizeOutputFormat(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "text" => "text",
            "json" => "json",
            _ => throw new ArgumentException($"Unsupported output format '{value}'. Expected 'text' or 'json'.")
        };
    }

    internal readonly record struct ProgramOptions(IReadOnlyList<string> Inputs, string OutputFormat);
    internal readonly record struct SummaryInput(string Path, bool IsDirectory);
}
