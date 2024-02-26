// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.Build.Logging.StructuredLogger;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.AnalyzerPerformance
{
	public static class Program
	{
		private static readonly List<AnalyzerPerformanceRecord> Records = new();
		private static string _filter = string.Empty;
		private const int MaxPackageNameLength = 24;
		private const int MaxAnalyzerNameLength = 45;
		private const string Ellipsis = "...";

		public static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.Error.WriteLine(@"Please specify a .binlog file.");
			}
			if (args.Length == 2)
			{
				_filter = args[1];
			}

			Build buildRoot = BinaryLog.ReadBuild(args[0]);
			BuildAnalyzer.AnalyzeBuild(buildRoot);

			foreach (BaseNode node in buildRoot.Children)
			{
				if (node is NamedNode { Name: @"Analyzer Summary" } namedNode)
				{
					AnalyzePackages(namedNode);
				}
			}
		}

		private static void AnalyzePackages(NamedNode namedNode)
		{
			foreach (BaseNode analyzerPackageNode in namedNode.Children)
			{
				if (analyzerPackageNode is Folder namedAnalyzerPackageFolder &&
					(string.IsNullOrEmpty(_filter) || namedAnalyzerPackageFolder.Name.Contains(_filter)))
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

			Records.Sort();
			foreach (AnalyzerPerformanceRecord record in Records)
			{
				var package = record.Package != null ? record.Package.Length > MaxPackageNameLength ? record.Package[..MaxPackageNameLength] + Ellipsis : record.Package : "...";
				var analyzer = record.Analyzer.Length > MaxAnalyzerNameLength ? record.Analyzer[..MaxAnalyzerNameLength] + Ellipsis : record.Analyzer;
				Console.WriteLine($"| {record.Id} | {package} | {analyzer} | {record.DisplayTime} |");
			}
		}

		private static void AnalyzerItems(Folder namedAnalyzerPackageFolder)
		{
			foreach (BaseNode analyzerMessage in namedAnalyzerPackageFolder.Children)
			{
				if (analyzerMessage is Item item)
				{
					var record = AnalyzerPerformanceRecord.TryParse(item.Name, item.Text);
					Records.Add(record);
				}
			}
		}
	}
}
