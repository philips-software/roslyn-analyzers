// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Philips.CodeAnalysis.Common
{
	public sealed class AnalyzerPerformanceRecord : IComparable<AnalyzerPerformanceRecord>
	{
		public static AnalyzerPerformanceRecord TryParse(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				return null;
			}

			var analyzerAndId = name.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
			if (analyzerAndId.Length < 4)
			{
				return null;
			}

			var idPart = analyzerAndId[1];
			if (idPart.Length < 2 || idPart[0] != '"' || idPart[idPart.Length - 1] != '"')
			{
				return null;
			}
			var id = idPart.Substring(1, idPart.Length - 2);

			var analyzerParts = analyzerAndId[0].Split('.');
			if (analyzerParts.Length < 3)
			{
				return null;
			}
			var package = analyzerParts[2];
			var analyzer = analyzerParts[analyzerParts.Length - 1];

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
