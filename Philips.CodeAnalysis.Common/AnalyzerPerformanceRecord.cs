﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Philips.CodeAnalysis.Common
{
	public sealed class AnalyzerPerformanceRecord : IComparable<AnalyzerPerformanceRecord>
	{
		public static AnalyzerPerformanceRecord TryParse(string name, string text)
		{
			var analyzerAndId = name.Split(' ');
			var id = analyzerAndId[1].Substring(1, analyzerAndId[1].Length - 2);

			var analyzerParts = analyzerAndId[0].Split('.');

			var timeParts = text.Split(' ');
			if (timeParts.Length == 0 || !double.TryParse(timeParts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var time))
			{
				return null;
			}
			if (timeParts[1] == "s")
			{
				time *= 1000;
			}

			Console.WriteLine($"Name: {name} ; Text: {text}");
			Console.WriteLine($"id: {id} ; Package: {analyzerParts[2]} ; Analyzer: {analyzerParts[analyzerParts.Length - 1]} ; DisplayTime: {text} ; Time: {time}");

			AnalyzerPerformanceRecord record = new()
			{
				Id = id,
				Package = analyzerParts[2],
				Analyzer = analyzerParts[analyzerParts.Length - 1],
				DisplayTime = text,
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
