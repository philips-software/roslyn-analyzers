﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation;

namespace Philips.CodeAnalysis.Test.Maintainability.Documentation
{
	[TestClass]
	public class NamespaceIgnoringComparerTest
	{

		[DataTestMethod]
		[DataRow("System.Exception", "System.Exception"),
		 DataRow("System.Exception", "Exception"),
		 DataRow("System.IO.IOException", "IOException"),
		 DataRow("IO.IOException", "System.IO.IOException")]
		public void ComparingEquivalentShouldReturnZero(string left, string right)
		{
			// Arrange
			var comparer = new NamespaceIgnoringComparer();
			// Act
			int compareResult = comparer.Compare(left, right);
			bool equalsResult = comparer.Equals(left, right);
			// Assert
			Assert.AreEqual(0, compareResult);
			Assert.IsTrue(equalsResult);
		}

		[DataTestMethod]
		[DataRow(null, "System.Exception"),
		 DataRow("System.Exception", null),
		 DataRow("System.IO.IOException", ""),
		 DataRow("", "System.IO.IOException")]
		public void InvalidInputShouldReturnNonZero(string left, string right)
		{
			// Arrange
			var comparer = new NamespaceIgnoringComparer();
			// Act
			bool equalsResult = comparer.Equals(left, right);
			// Assert
			Assert.IsFalse(equalsResult);
		}

		[DataTestMethod]
		[DataRow("System.Exception"),
		 DataRow("System.IO.IOException"),
		 DataRow("")]
		public void GetHashCodeShouldReturnSameAsInput(string input)
		{
			// Arrange
			var comparer = new NamespaceIgnoringComparer();
			var expected = input.GetHashCode();
			// Act
			int actual = comparer.GetHashCode(input);
			// Assert
			Assert.AreEqual(expected, actual);
		}
	}
}
