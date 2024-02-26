// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Philips.CodeAnalysis.Common
{
	public sealed class AnalyzerPerformanceRecord : IComparable<AnalyzerPerformanceRecord>
	{
		public static AnalyzerPerformanceRecord TryParse(string name)
		{
			var analyzerAndId = name.Split(' ');
			var id = analyzerAndId[1].Substring(1, analyzerAndId[1].Length - 2);

			var analyzerParts = analyzerAndId[0].Split('.');
			var package = analyzerParts[2];
			var analyzer = analyzerParts.Last();

			var timePart = analyzerAndId[analyzerAndId.Length - 2];
			var seconds = analyzerAndId[analyzerAndId.Length - 1];

			if (!double.TryParse(timePart, NumberStyles.Any, CultureInfo.InvariantCulture, out var time))
			{
				return null;
			}
			if (seconds == "s")
			{
				time *= 1000;
			}

			var displayTime = (time < 1000) ? $"{(int)time} ms" : $"{time / 1000} s";

			AnalyzerPerformanceRecord record = new()
			{
				Id = id,
				Package = package,
				Analyzer = analyzer,
				DisplayTime = displayTime,
				Time = (int)time
			};
			return record;
		}

		public string Id { get; init; }
		public string Package { get; init; }
		public string Analyzer { get; init; }
		public string DisplayTime { get; init; }
		public int Time { get; init; }

		public int CompareTo(AnalyzerPerformanceRecord other)
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

		public static bool operator ==(AnalyzerPerformanceRecord left, AnalyzerPerformanceRecord right)
		{
			if (left is null)
			{
				return right is null;
			}
			return left.CompareTo(right) == 0;
		}

		public static bool operator !=(AnalyzerPerformanceRecord left, AnalyzerPerformanceRecord right)
		{
			if (left is null)
			{
				return right is not null;
			}
			return left.CompareTo(right) != 0;
		}

		public static bool operator <(AnalyzerPerformanceRecord left, AnalyzerPerformanceRecord right)
		{
			return left.CompareTo(right) < 0;
		}

		public static bool operator >(AnalyzerPerformanceRecord left, AnalyzerPerformanceRecord right)
		{
			return left.CompareTo(right) > 0;
		}

		public static bool operator <=(AnalyzerPerformanceRecord left, AnalyzerPerformanceRecord right)
		{
			return left.CompareTo(right) <= 0;
		}

		public static bool operator >=(AnalyzerPerformanceRecord left, AnalyzerPerformanceRecord right)
		{
			return left.CompareTo(right) >= 0;
		}

		public override bool Equals(object obj)
		{
			return this == (obj as AnalyzerPerformanceRecord);
		}

		public override int GetHashCode()
		{
			return RuntimeHelpers.GetHashCode(this);
		}
	}
}
