using Microsoft.Build.Logging.StructuredLogger;

namespace Philips.CodeAnalysis.AnalyzerPerformance
{
	public static class BinaryLogReadBuild
	{
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
		}

		private static void AnalyzerItems(Folder namedAnalyzerPackageFolder)
		{
			foreach (BaseNode analyzerMessage in namedAnalyzerPackageFolder.Children)
			{
				if (analyzerMessage is Item item)
				{
					string[] analyzerAndId = item.Name.Split(" ");
					string id = analyzerAndId[1].Substring(1, analyzerAndId[1].Length - 2);
					Console.WriteLine($"| {id} | {analyzerAndId[0]} | {item.Text} |");
				}
			}
		}
	}
}
