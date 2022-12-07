// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ExpectedExceptionAttributeAnalyzer : DiagnosticAnalyzer
	{
		public const string MessageFormat = @"Tests may not use the ExpectedException attribute. Use the AssertEx.Throws method instead.";
		private const string Title = @"ExpectedException attribute not allowed";
		private const string Description = @"The [ExpectedException()] attribute does not have line number granularity and trips the debugger anyway.  Use AssertEx.Throws() instead.";
		private const string Category = Categories.Maintainability;

		private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.ExpectedExceptionAttribute), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(startContext =>
			{
				if (startContext.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.ExpectedExceptionAttribute") == null)
				{
					return;
				}

				startContext.RegisterSyntaxNodeAction(Analyze, SyntaxKind.AttributeList);
			});
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			AttributeListSyntax attributesNode = (AttributeListSyntax)context.Node;

			foreach (AttributeSyntax attribute in attributesNode.Attributes)
			{
				if (attribute.Name.ToString().Contains(@"ExpectedException"))
				{
					Diagnostic diagnostic = Diagnostic.Create(Rule, attribute.GetLocation());
					context.ReportDiagnostic(diagnostic);
					return;
				}
			}
		}
	}
}
