// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.Test.Helpers;

namespace Philips.CodeAnalysis.Test.Common
{
	/// <summary>
	/// Test class for methods in <see cref="ValidationFlowAnalysis"/>.
	/// </summary>
	[TestClass]
	public class ValidationFlowAnalysisTest
	{
		private const string SimpleConnectedMethod = @"
public int ConnectedMethod(int a) {
	return a;
}
";


		[DataTestMethod]
		[DataRow(SimpleConnectedMethod, DisplayName = nameof(SimpleConnectedMethod))]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ExpectParameterAToBeConnected(string code)
		{
			// Arrange
			CSharpCompilation compilation = CreateCompilation(code);
			var method = compilation.SyntaxTrees[0].GetRoot().DescendantNodes()
				.First(node => node.IsKind(SyntaxKind.MethodDeclaration)) as BaseMethodDeclarationSyntax;
			// Act
			var analysis = new ValidationFlowAnalysis(method);
			// Assert
			CollectionAssert.Contains(analysis.ConnectedToReturn, "a");
		}

		private static CSharpCompilation CreateCompilation(string code)
		{
			SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(code);
			return CSharpCompilation.Create("Test.dll", new[] { tree });
		}
	}
}
