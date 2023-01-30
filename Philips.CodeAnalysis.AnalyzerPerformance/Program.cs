using Microsoft.Build.Logging.StructuredLogger;

namespace Philips.CodeAnalysis.AnalyzerPerformance
{
	public static class Program
	{
		private static readonly List<AnalyzerPerfRecord> _records = new();

		public static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.Error.WriteLine(@"Please specify a .binlog file.");
			}

			Build buildRoot = BinaryLog.ReadBuild(args[0]);
			BuildAnalyzer.AnalyzeBuild(buildRoot);

			foreach (BaseNode node in buildRoot.Children)
			{
				if (node is NamedNode namedNode && namedNode.Name == @"Analyzer Summary")
				{
					AnalyzePackages(namedNode);
				}
			}
		}

		private static void AnalyzePackages(NamedNode namedNode)
		{
			foreach (BaseNode analyzerPackageNode in namedNode.Children)
			{
				if (analyzerPackageNode is Folder namedAnalyzerPackageFolder && namedAnalyzerPackageFolder.Name.Contains(@"Philips.CodeAnalysis"))
				{
					AnalyzerItems(namedAnalyzerPackageFolder);
				}
			}
			OutputResults();
		}

		private static void OutputResults()
		{
			Console.WriteLine(@"### Analyzer Performance");
			Console.WriteLine(@"| Id | Package | Analyzer | Time |");
			Console.WriteLine(@"| -- | ------- | -------- | ---- |");
			foreach (var record in _records)
			{
				Console.WriteLine($"| {record.Id} | {record.Package} | {record.Analyzer} | {record.Time} |");
			}
		}

		private static void AnalyzerItems(Folder namedAnalyzerPackageFolder)
		{
			foreach (BaseNode analyzerMessage in namedAnalyzerPackageFolder.Children)
			{
				if (analyzerMessage is Item item)
				{
					string[] analyzerAndId = item.Name.Split(" ");
					string id = analyzerAndId[1].Substring(1, analyzerAndId[1].Length - 2);

					string[] analyzerParts = analyzerAndId[0].Split(".");

					AnalyzerPerfRecord record = new()
					{
						Id = id,
						Package = analyzerParts[2],
						Analyzer = analyzerParts[analyzerParts.Length - 1],
						DisplayTime = item.Text
					};
					_records.Add(record);
				}
			}
		}
	}

	internal record AnalyzerPerfRecord
	{
		public string Id { get; init; }
		public string Package { get; init; }
		public string Analyzer { get; init; }
		public string DisplayTime { get; init; }
		public int Time { get; init; }
	}
}
