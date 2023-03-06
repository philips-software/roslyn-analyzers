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

			//this is an error, unless the class contains an "AssemblyInitialize" within it.  If it does, it _must_ be static.
			var hasAttributeThatMustBeStatic = CheckForStaticAttributesOnMethods(context, classDeclaration);
			var isClassStatic = classDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);

			if (hasAttributeThatMustBeStatic != isClassStatic)
			{
				Location location = classDeclaration.Identifier.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(Rule, location, classDeclaration.Identifier));
			}
		}

		private bool CheckForStaticAttributesOnMethods(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDeclaration)
		{
			foreach (MethodDeclarationSyntax method in classDeclaration.Members.OfType<MethodDeclarationSyntax>())
			{
				if (!method.Modifiers.Any(SyntaxKind.StaticKeyword))
				{
					continue;
				}

				if (AttributeHelper.HasAnyAttribute(method.AttributeLists, context, MsTestFrameworkDefinitions.AssemblyInitializeAttribute, MsTestFrameworkDefinitions.AssemblyCleanupAttribute))
				{
					return true;
				}
			}

			return false;
		}
	}
}
