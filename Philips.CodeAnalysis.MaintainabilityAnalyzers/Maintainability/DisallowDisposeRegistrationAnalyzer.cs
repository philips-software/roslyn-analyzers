// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class DisallowDisposeRegistrationAnalyzer : SingleDiagnosticAnalyzer
	{
		public const string Title = @"Dispose Registration";
		public const string MessageFormat = @"Erroneous registration of an event in a Dispose method.  Did you mean to unregister?";
		public const string Description = @"MyClass.Event += MyHandler is not allowed in a Dispose method.  Should be MyClass.Event -= MyHandler.";

		public DisallowDisposeRegistrationAnalyzer()
			: base(DiagnosticId.DisallowDisposeRegistration, Title, MessageFormat, Description, Categories.Maintainability)
		{ }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.AddAssignmentExpression);
		}

		public void Analyze(SyntaxNodeAnalysisContext context)
		{
			IEnumerable<MethodDeclarationSyntax> ancestorMethods = context.Node.Ancestors().OfType<MethodDeclarationSyntax>();
			if (ancestorMethods.Any())
			{
				MethodDeclarationSyntax parentMethod = ancestorMethods.First();
				if (parentMethod.Identifier.Text == StringConstants.Dispose)
				{
					IMethodSymbol methodSymbol = context.SemanticModel.GetDeclaredSymbol(parentMethod);
					if ((methodSymbol != null) && methodSymbol.ToString().Contains(".Dispose("))
					{
						Location location = context.Node.GetLocation();
						context.ReportDiagnostic(Diagnostic.Create(Rule, location));
					}
				}
			}
		}
	}
}
