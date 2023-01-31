﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Build.Logging.StructuredLogger;

namespace Philips.CodeAnalysis.AnalyzerPerformance
{
	[ExcludeFromCodeCoverage]
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

			_records.Sort();
			foreach (var record in _records)
			{
				Console.WriteLine($"| {record.Id} | {record.Package} | {record.Analyzer} | {record.DisplayTime} |");
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
			if (other is AnalyzerPerfRecord otherRecord)
			{
				if (Time.CompareTo(otherRecord.Time) != 0)
				{
					return Time.CompareTo(otherRecord.Time) * -1;
				}
				return Id.CompareTo(otherRecord.Id);
			}
			throw new ArgumentException($"Object is not a {nameof(AnalyzerPerfRecord)}");
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
	}
}
