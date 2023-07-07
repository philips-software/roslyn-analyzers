// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
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
		private Diagnostic Make(string id)
		{
			var descriptor = new DiagnosticDescriptor(id, string.Empty, string.Empty, string.Empty, DiagnosticSeverity.Error, false);
			return Diagnostic.Create(descriptor, null);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ToPrettyListTest()
		{
			Diagnostic diagnostic1 = Make("PH1000");
			Diagnostic diagnostic2 = Make("PH2000");
			Diagnostic diagnostic3 = Make("PH3000");

			Assert.AreEqual("", Helper.ToPrettyList(Array.Empty<Diagnostic>()));
			Assert.AreEqual("PH1000", Helper.ToPrettyList(new Diagnostic[] { diagnostic1 }));
			Assert.AreEqual("PH1000, PH2000", Helper.ToPrettyList(new Diagnostic[] { diagnostic1, diagnostic2 }));
			Assert.AreEqual("PH1000, PH2000, PH3000", Helper.ToPrettyList(new Diagnostic[] { diagnostic1, diagnostic2, diagnostic3 }));
		}

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
			var isActualCallable = memberDeclaration.IsCallableFromOutsideClass();

			// Assert
			Assert.AreEqual(isExpectedCallable, isActualCallable);
		}
	}
}
