// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class TestClassMustBePublicAnalyzer : TestClassDiagnosticAnalyzer
	{
		private const string Title = @"[TestClass] must be a public instance class";
		public static readonly string MessageFormat = @"'{0}' is not a public instance class";
		private const string Description = @"";
		private const string Category = Categories.MsTest;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticId.TestClassesMustBePublic),
												Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		protected override void OnTestClass(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDeclaration)
		{
			if (!classDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword))
			{
				Location location = classDeclaration.Identifier.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(Rule, location, classDeclaration.Identifier));
				return;
			}
		}
	}
}
