// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.Test.Helpers;

namespace Philips.CodeAnalysis.Test.Common
{
	[TestClass]
	public class AnalyzerPerformanceRecordTest
	{
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ParsedCorrectly()
		{
			// Arrange
			var expectedAnalyzer = "DummyAnalyzer";
			var expectedId = "DummyId";
			var expectedTime = 25.0d;
			var testName = $"Philips.CodeAnalysis.{expectedAnalyzer} \"{expectedId}\"";
			var testText = $"{expectedTime} s";

			// Act
			var actual = AnalyzerPerformanceRecord.TryParse(testName, testText);

			// Assert
			Assert.AreEqual(expectedAnalyzer, actual.Analyzer);
			Assert.AreEqual(expectedId, actual.Id);
			Assert.AreEqual(expectedTime * 1000, actual.Time);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void SortsLargestTimeFirst()
		{
			// Arrange
			var list = new List<AnalyzerPerformanceRecord>(2)
			{
				new AnalyzerPerformanceRecord() { Time = 42 },
				new AnalyzerPerformanceRecord() { Time = 2 }
			};

			// Act
			list.Sort();

			// Assert
			Assert.AreEqual(42, list[0].Time);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CompareLargestThan()
		{
			// Arrange
			var quickest = new AnalyzerPerformanceRecord() { Time = 2 };
			var longest = new AnalyzerPerformanceRecord() { Time = 42 };
			AnalyzerPerformanceRecord quick = quickest;

			// Assert
			Assert.AreEqual(true, quickest > longest);
			Assert.AreEqual(true, quickest >= longest);
			Assert.AreEqual(true, quickest >= quick);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CompareSmallerThan()
		{
			// Arrange
			var quickest = new AnalyzerPerformanceRecord() { Time = 2 };
			var longest = new AnalyzerPerformanceRecord() { Time = 42 };
			AnalyzerPerformanceRecord quick = quickest;

			// Assert
			Assert.AreEqual(true, longest < quickest);
			Assert.AreEqual(true, longest <= quickest);
			Assert.AreEqual(true, quickest <= quick);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CompareEqual()
		{
			// Arrange
			var quickest = new AnalyzerPerformanceRecord() { Time = 2 };
			var quick = new AnalyzerPerformanceRecord() { Time = 2 };

			// Assert
			Assert.AreEqual(true, quick == quickest);
			Assert.AreEqual(true, quick.Equals(quickest));
			Assert.AreNotEqual(quick.GetHashCode(), quickest.GetHashCode());
			Assert.AreEqual(0, quick.CompareTo(quickest));
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CompareNotEqual()
		{
			// Arrange
			var quickest = new AnalyzerPerformanceRecord() { Time = 2 };
			var longest = new AnalyzerPerformanceRecord() { Time = 42 };

			// Assert
			Assert.AreEqual(true, longest != quickest);
			Assert.AreEqual(false, longest.Equals(quickest));
			Assert.AreEqual(-1, longest.CompareTo(quickest));
			Assert.AreEqual(1, quickest.CompareTo(longest));
			Assert.AreNotEqual(longest.GetHashCode(), quickest.GetHashCode());
		}
	}
}
