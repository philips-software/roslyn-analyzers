// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.Test.Helpers;

namespace Philips.CodeAnalysis.Test.Common
{
	[TestClass]
	public class AttributeHelperTest
	{
		private readonly CodeFixHelper _helper = new();

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void HasAttributeReturnsFalseForEmptyAttributeList()
		{
			// Arrange
			var testCode = "public void TestMethod() { }";
			var memberDeclaration = SyntaxFactory.ParseMemberDeclaration(testCode) as MethodDeclarationSyntax;
			var testMethodAttribute = new AttributeDefinition("TestMethod", "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute");

			// Act
			// Using function overload that only checks syntax, not semantic model
			var hasAttribute = _helper.ForAttributes.HasAttribute(memberDeclaration.AttributeLists, () => null, "TestMethod", null, out _, out _);

			// Assert
			Assert.IsFalse(hasAttribute);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void HasAttributeReturnsTrueForMatchingAttributeName()
		{
			// Arrange
			var testCode = "[TestMethod] public void TestMethod() { }";
			var memberDeclaration = SyntaxFactory.ParseMemberDeclaration(testCode) as MethodDeclarationSyntax;

			// Act
			// Test name-only matching (fullName = null bypasses semantic model check)
			var hasAttribute = _helper.ForAttributes.HasAttribute(memberDeclaration.AttributeLists, () => null, "TestMethod", null, out _, out _);

			// Assert
			Assert.IsTrue(hasAttribute);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void HasAttributeReturnsFalseForNonMatchingAttributeName()
		{
			// Arrange
			var testCode = "[TestMethod] public void TestMethod() { }";
			var memberDeclaration = SyntaxFactory.ParseMemberDeclaration(testCode) as MethodDeclarationSyntax;

			// Act
			// Test name-only matching (fullName = null bypasses semantic model check)
			var hasAttribute = _helper.ForAttributes.HasAttribute(memberDeclaration.AttributeLists, () => null, "DataRow", null, out _, out _);

			// Assert
			Assert.IsFalse(hasAttribute);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void IsAttributeReturnsTrueForMatchingAttributeName()
		{
			// Arrange
			var testCode = "[TestMethod] public void TestMethod() { }";
			var memberDeclaration = SyntaxFactory.ParseMemberDeclaration(testCode) as MethodDeclarationSyntax;
			AttributeSyntax attribute = memberDeclaration.AttributeLists.First().Attributes.First();

			// Act
			// Test name-only matching (fullName = null bypasses semantic model check)
			var isAttribute = _helper.ForAttributes.IsAttribute(attribute, () => null, "TestMethod", null, out _, out _);

			// Assert
			Assert.IsTrue(isAttribute);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void IsAttributeReturnsFalseForNonMatchingAttributeName()
		{
			// Arrange
			var testCode = "[TestMethod] public void TestMethod() { }";
			var memberDeclaration = SyntaxFactory.ParseMemberDeclaration(testCode) as MethodDeclarationSyntax;
			AttributeSyntax attribute = memberDeclaration.AttributeLists.First().Attributes.First();

			// Act
			// Test name-only matching (fullName = null bypasses semantic model check)
			var isAttribute = _helper.ForAttributes.IsAttribute(attribute, () => null, "DataRow", null, out _, out _);

			// Assert
			Assert.IsFalse(isAttribute);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void HasAttributeWithoutSemanticModelDoesNotExtractArguments()
		{
			// Arrange
			var testCode = @"[TestCategory(""Unit"")] public void TestMethod() { }";
			var memberDeclaration = SyntaxFactory.ParseMemberDeclaration(testCode) as MethodDeclarationSyntax;

			// Act
			// Test name-only matching - when fullName is null, argument extraction is not performed
			var hasAttribute = _helper.ForAttributes.HasAttribute(memberDeclaration.AttributeLists, () => null, "TestCategory", null, out _, out AttributeArgumentSyntax argument);

			// Assert
			Assert.IsTrue(hasAttribute);
			// When fullName is null (bypassing semantic model), argument is not extracted
			Assert.IsNull(argument);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void HasAttributeExtractsArgumentsFromSyntax()
		{
			// Arrange
			var testCode = @"[TestCategory(""Unit"")] public void TestMethod() { }";
			var memberDeclaration = SyntaxFactory.ParseMemberDeclaration(testCode) as MethodDeclarationSyntax;
			AttributeSyntax attribute = memberDeclaration.AttributeLists.First().Attributes.First();

			// Act
			// Test direct syntax-level argument extraction 
			AttributeArgumentSyntax argument = attribute.ArgumentList?.Arguments.FirstOrDefault();

			// Assert
			Assert.IsNotNull(argument);
			Assert.AreEqual("\"Unit\"", argument.Expression.ToString());
		}
	}
}