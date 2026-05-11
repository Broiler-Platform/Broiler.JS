namespace LogParser;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            var inputPaths = ResolveInputPaths(args);
            if (inputPaths.Count == 0)
            {
                Console.Error.WriteLine("No log files were found to summarize.");
                return 1;
            }

            var summaries = inputPaths
                .Select(path => LogSummaryBuilder.ParseAndSummarize(path))
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

    internal static IReadOnlyList<string> ResolveInputPaths(IEnumerable<string> inputs)
    {
        var resolved = inputs
            .SelectMany(ResolveInputPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return resolved.Length > 0
            ? resolved
            : Directory
                .GetFiles(GetDefaultLogsDirectory(), "*.json", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }

    private static IEnumerable<string> ResolveInputPath(string input)
    {
        var fullPath = Path.GetFullPath(input);
        if (File.Exists(fullPath))
        {
            yield return fullPath;
            yield break;
        }

        if (Directory.Exists(fullPath))
        {
            foreach (var file in Directory.GetFiles(fullPath, "*.json", SearchOption.TopDirectoryOnly)
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                yield return file;
            }

            yield break;
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
}
