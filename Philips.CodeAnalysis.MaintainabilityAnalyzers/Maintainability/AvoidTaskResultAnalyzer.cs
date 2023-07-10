// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidTaskResultAnalyzer : DiagnosticAnalyzerBase
	{
		private const string Title = @"Avoid Task.Result";
		public const string MessageFormat = @"Methods may not call Result on a Task.";
		private const string Description = @"To avoid deadlocks, methods may not call Result on a Task.";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(DiagnosticId.AvoidTaskResult.ToId(), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		private const string ContainingNamespace = @"Tasks";
		private const string ContainingType = @"Task";
		private const string Identifier = @"Result";

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			if (context.Compilation.GetTypeByMetadataName(StringConstants.TaskFullyQualifiedName) == null)
			{
				return;
			}
			context.RegisterOperationAction(Analyze, OperationKind.PropertyReference);
		}

		private void Analyze(OperationAnalysisContext context)
		{
			var propertyReference = (IPropertyReferenceOperation)context.Operation;


			if (propertyReference.Syntax is not MemberAccessExpressionSyntax propertySyntax)
			{
				return;
			}

			if (propertySyntax.Name.Identifier.ValueText == Identifier)
			{
				IPropertySymbol propertySymbol = propertyReference.Property;
				if (propertySymbol.ContainingNamespace.Name == ContainingNamespace &&
					propertySymbol.ContainingType.Name.Contains(ContainingType))
				{
					Location location = propertySyntax.Name.GetLocation();
					var diagnostic = Diagnostic.Create(Rule, location);
					context.ReportDiagnostic(diagnostic);
				}
			}
		}
	}
}
