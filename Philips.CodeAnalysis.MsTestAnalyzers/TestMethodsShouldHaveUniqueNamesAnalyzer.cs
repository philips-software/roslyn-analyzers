// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class TestMethodsShouldHaveUniqueNamesAnalyzer : TestClassDiagnosticAnalyzer
	{
		private const string Title = @"TestMethods/DataTestMethods should not have the same name";
		public static readonly string MessageFormat = @"Multiple tests named '{0}'";
		private const string Description = @"";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticId.TestMethodsMustHaveUniqueNames),
												Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);


		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		protected override void OnTestClass(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDeclaration)
		{
			HashSet<MethodDeclarationSyntax> testMethods = new();
			foreach (MemberDeclarationSyntax member in classDeclaration.Members)
			{
				if (member is not MethodDeclarationSyntax method || !TestHelper.IsTestMethod(method, context))
				{
					continue;
				}

				testMethods.Add(method);
			}

			HashSet<string> seenNames = new();
			foreach (var method in testMethods.Where(method => !seenNames.Add(method.Identifier.ToString())))
			{
				context.ReportDiagnostic(Diagnostic.Create(Rule, method.Identifier.GetLocation(), method.Identifier));
			}
		}
	}
}
