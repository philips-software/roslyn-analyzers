// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.Test.Helpers;

namespace Philips.CodeAnalysis.Test.Common
{
	[TestClass]
	public class HelperTest
	{
		private readonly CodeFixHelper _helper = new();

		[DataTestMethod]
		[DataRow("private static readonly", false),
		 DataRow("private static", false),
		 DataRow("private const", false),
		 DataRow("private", false),
		 DataRow("public static readonly", true),
		 DataRow("public const", true),
		 DataRow("public static", true),
		 DataRow("public readonly", true),
		 DataRow("public", true),
		 DataRow("internal static readonly", true),
		 DataRow("internal static", true),
		 DataRow("internal const", true),
		 DataRow("internal readonly", true),
		 DataRow("internal", true),
		 DataRow("protected static readonly", true),
		 DataRow("protected static", true),
		 DataRow("protected const", true),
		 DataRow("protected readonly", true),
		 DataRow("protected", true),
		 DataRow("protected internal static readonly", true),
		 DataRow("protected internal static", true),
		 DataRow("protected internal const", true),
		 DataRow("protected internal readonly", true),
		 DataRow("protected internal", true)]
		[TestCategory(TestDefinitions.UnitTests)]
		public void IsCallableFromOutsideClassTest(string modifiers, bool isExpectedCallable)
		{
			// Arrange
			var testCode = $"{modifiers} int I;";
			MemberDeclarationSyntax memberDeclaration = SyntaxFactory.ParseMemberDeclaration(testCode);

			// Act
			var isActualCallable = _helper.ForModifiers.IsCallableFromOutsideClass(memberDeclaration);

			// Assert
			Assert.AreEqual(isExpectedCallable, isActualCallable);
		}
	}
}
