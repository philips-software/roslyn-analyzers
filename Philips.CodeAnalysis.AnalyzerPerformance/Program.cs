// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Build.Logging;
using Microsoft.Build.Logging.StructuredLogger;

namespace Philips.CodeAnalysis.AnalyzerPerformance
{
	[ExcludeFromCodeCoverage]
	public static class Program
	{
		private static readonly List<AnalyzerPerfRecord> _records = new();
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

			_records.Sort();
			foreach (var record in _records)
			{
				string package = record.Package.Length > MaxPackageNameLength ? record.Package.Substring(0, MaxPackageNameLength) + Ellipsis : record.Package;
				string analyzer = record.Analyzer.Length > MaxAnalyzerNameLength ? record.Analyzer.Substring(0, MaxAnalyzerNameLength) + Ellipsis : record.Analyzer;
				Console.WriteLine($"| {record.Id} | {package} | {analyzer} | {record.DisplayTime} |");
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

					string[] timeParts = item.Text.Split(" ");
					double time = double.Parse(timeParts[0]);
					if (timeParts[1] == "s")
					{
						time *= 1000;
					}

					AnalyzerPerfRecord record = new()
					{
						Id = id,
						Package = analyzerParts[2],
						Analyzer = analyzerParts[analyzerParts.Length - 1],
						DisplayTime = item.Text,
						Time = (int)time
					};
					_records.Add(record);
				}
			}
		}
	}

	[ExcludeFromCodeCoverage]
	internal class AnalyzerPerfRecord : IComparable<AnalyzerPerfRecord>
	{
		public string Id { get; init; }
		public string Package { get; init; }
		public string Analyzer { get; init; }
		public string DisplayTime { get; init; }
		public int Time { get; init; }

		public int CompareTo(AnalyzerPerfRecord other)
		{
			if (other == null)
			{
				return 1;
			}
			if (Time.CompareTo(other.Time) != 0)
			{
				return Time.CompareTo(other.Time) * -1;
			}
			return StringComparer.Ordinal.Compare(Id, other.Id);
		}

		public static bool operator ==(AnalyzerPerfRecord left, AnalyzerPerfRecord right)
		{
			if (left is null)
			{
				return right is null;
			}
			return left.CompareTo(right) == 0;
		}

		public static bool operator !=(AnalyzerPerfRecord left, AnalyzerPerfRecord right)
		{
			if (left is null)
			{
				return right is not null;
			}
			return left.CompareTo(right) != 0;
		}

		public static bool operator <(AnalyzerPerfRecord left, AnalyzerPerfRecord right)
		{
			return left.CompareTo(right) < 0;
		}

		public static bool operator >(AnalyzerPerfRecord left, AnalyzerPerfRecord right)
		{
			return left.CompareTo(right) > 0;
		}

		public static bool operator <=(AnalyzerPerfRecord left, AnalyzerPerfRecord right)
		{
			return left.CompareTo(right) <= 0;
		}

		public static bool operator >=(AnalyzerPerfRecord left, AnalyzerPerfRecord right)
		{
			return left.CompareTo(right) >= 0;
		}

		public override bool Equals(object obj)
		{
			return this == (obj as AnalyzerPerfRecord);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Id, Package, Analyzer, DisplayTime, Time);
		}
	}
}
