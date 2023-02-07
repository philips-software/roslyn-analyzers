// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;
using LanguageExt;

using static LanguageExt.Prelude;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class TestMethodNameAnalyzer : SingleDiagnosticAnalyzer<AttributeListSyntax, TestMethodNameSyntaxNodeAction>
	{
		public const string MessageFormat = @"Test Method must not start with '{0}'";
		private const string Title = @"Test Method names unhelpful prefix";
		private const string Description = @"Test Method names must not start with 'Test', 'Ensure', or 'Verify'. Otherwise, they are more difficult to find in sorted lists in Test Explorer.";
		public TestMethodNameAnalyzer()
			: base(DiagnosticId.TestMethodName, Title, MessageFormat, Description, Categories.Naming)
		{
			FullyQualifiedMetaDataName = "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute";
		}
	}
	public class TestMethodNameSyntaxNodeAction : SyntaxNodeAction<AttributeListSyntax>
	{
		private readonly List<string> PrefixChecks = new()
		{
				StringConstants.TestAttributeName,
				StringConstants.EnsureAttributeName,
				StringConstants.VerifyAttributeName,
			};

		public override IEnumerable<Diagnostic> Analyze()
		{
			if (Node.Attributes.Any(attr => attr.Name.ToString() == @"TestMethod"))
			{
				return Optional(Node.Parent)
					.Filter(methodName => methodName.Kind() == SyntaxKind.MethodDeclaration)
					.SelectMany(methodName => methodName.ChildTokens())
					.SelectMany(token =>
						PrefixChecks.FindAll(token.ValueText.StartsWith)
						.Select((invalidPrefix) => PrepareDiagnostic(token.GetLocation(), invalidPrefix))
					);
			}
			return Option<Diagnostic>.None;
		}
	}
}
