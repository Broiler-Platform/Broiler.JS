namespace LogParser
{
    internal class Program
    {
        public class Rootobject
        {
            public string suiteRef { get; set; }
            public string broilerDll { get; set; }
            public object[] requestedPaths { get; set; }
            public string[] expandedPaths { get; set; }
            public string selectionMode { get; set; }
            public int candidateCount { get; set; }
            public int selectedCountBeforeSharding { get; set; }
            public int shardCount { get; set; }
            public int shardIndex { get; set; }
            public int executed { get; set; }
            public int passed { get; set; }
            public int failed { get; set; }
            public int skipped { get; set; }
            public Result[] results { get; set; }
        }

        public class Result
        {
            public string path { get; set; }
            public string status { get; set; }
            public string stdout { get; set; }
            public string stderr { get; set; }
        }

        static void Main(string[] args)
        {
        }
    }
}
