﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class TestMethodNameAnalyzer : SingleDiagnosticAnalyzer<AttributeListSyntax, TestMethodNameSyntaxNodeAction>
	{
		public const string MessageFormat = @"Test Method must not start with '{0}'";
		private const string Title = @"Test Method names unhelpful prefix'";
		private const string Description = @"Test Method names must not start with 'Test', 'Ensure', or 'Verify'. Otherwise, they are more difficult to find in sorted lists in Test Explorer.";

		public TestMethodNameAnalyzer()
			: base(DiagnosticId.TestMethodName, Title, MessageFormat, Description, Categories.Naming)
		{
			FullyQualifiedMetaDataName = "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute";
		}
	}
	public class TestMethodNameSyntaxNodeAction : SyntaxNodeAction<AttributeListSyntax>
	{
		private const string TestLiteral = @"Test";
		private const string EnsureLiteral = @"Ensure";
		private const string VerifyLiteral = @"Verify";

		public override void Analyze()
		{
			// Only interested in TestMethod attributes
			if(Node.Attributes.All(attr => attr.Name.ToString() != @"TestMethod"))
			{
				return;
			}

			SyntaxNode methodNode = Node.Parent;

			// Confirm this is actually a method...
			if (methodNode.Kind() != SyntaxKind.MethodDeclaration)
			{
				return;
			}

			string invalidPrefix = string.Empty;
			foreach (SyntaxToken token in methodNode.ChildTokens())
			{
				if (token.Kind() == SyntaxKind.IdentifierToken)
				{
					if (token.ValueText.StartsWith(TestLiteral))
					{
						invalidPrefix = TestLiteral;
					}
					else if (token.ValueText.StartsWith(EnsureLiteral))
					{
						invalidPrefix = EnsureLiteral;
					}
					else if (token.ValueText.StartsWith(VerifyLiteral))
					{
						invalidPrefix = VerifyLiteral;
					}

					if (!string.IsNullOrEmpty(invalidPrefix))
					{
						var location = token.GetLocation();
						ReportDiagnostic(location);
						return;
					}
				}
			}
		}
	}
}
