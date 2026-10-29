// © 2026 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;
using Philips.CodeAnalysis.Test.Helpers;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	[TestClass]
	public class MemberRegionClassifierTest
	{
		private const string Default = "Test Mocks|Non-Public Data Members|Non-Public Properties/Methods|Public Interface|<allowed regions>|Test Methods|Test Classes";

		[TestMethod]
		[DataRow(null, Default)]
		[DataRow("", Default)]
		[DataRow("1234567", Default)]
		[DataRow("12435", "Test Mocks|Non-Public Data Members|Public Interface|Non-Public Properties/Methods|<allowed regions>|Test Methods|Test Classes")]
		[DataRow(" 7 ", "Test Classes|Test Mocks|Non-Public Data Members|Non-Public Properties/Methods|Public Interface|<allowed regions>|Test Methods")]
		[DataRow("11", Default)]
		[DataRow("8", Default)]
		[DataRow("0", Default)]
		[DataRow("1a", Default)]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ParseSectionOrder(string setting, string expected)
		{
			Assert.AreEqual(expected, string.Join("|", MemberRegionClassifier.ParseSectionOrder(setting)));
		}
	}
}
