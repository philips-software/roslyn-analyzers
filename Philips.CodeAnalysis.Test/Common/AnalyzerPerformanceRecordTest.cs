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
	}
}
