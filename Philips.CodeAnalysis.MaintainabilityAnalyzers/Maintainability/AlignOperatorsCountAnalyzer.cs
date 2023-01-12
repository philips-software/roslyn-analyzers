// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	/// <summary>
	/// Diagnostic for inconsistent number of operators in a class.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AlignOperatorsCountAnalyzer : DiagnosticAnalyzer
	{
		private const string PlusMinusTitle = "Align number of + and - operators.";
		private const string PlusMinusMessage = "Align number of + and - operators.";
		private const string PlusMinusDescription = 
			"A class should have the same number of + as - operators.";
		private const string MultiplyDivideTitle = "Align number of * and / operators.";
		private const string MultiplyDivideMessage = "Align number of * and / operators.";
		private const string MultiplyDivideDescription =
			"A class should have the same number of * as / operators.";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor PlusMinusRule =
			new(
				Helper.ToDiagnosticId(DiagnosticIds.AlignNumberOfPlusAndMinusOperators),
				PlusMinusTitle,
				PlusMinusMessage,
				Category,
				DiagnosticSeverity.Error,
				isEnabledByDefault: true,
				description: PlusMinusDescription
			);
		private static readonly DiagnosticDescriptor MultiplyDivideRule =
			new(
				Helper.ToDiagnosticId(DiagnosticIds.AlignNumberOfMultiplyAndDivideOperators),
				MultiplyDivideTitle,
				MultiplyDivideMessage,
				Category,
				DiagnosticSeverity.Error,
				isEnabledByDefault: true,
				description: MultiplyDivideDescription
			);

		/// <summary>
		/// <inheritdoc cref="DiagnosticAnalyzer"/>
		/// </summary>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create(PlusMinusRule, MultiplyDivideRule);

		/// <summary>
		/// <inheritdoc cref="DiagnosticAnalyzer"/>
		/// </summary>
		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			ClassDeclarationSyntax classDeclaration = (ClassDeclarationSyntax)context.Node;

			OperatorsVisitor visitor = new();
			visitor.Visit(classDeclaration);

			if(visitor.PlusCount != visitor.MinusCount)
			{
				var location = classDeclaration.Identifier.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(PlusMinusRule, location));
			}

			if(visitor.MultiplyCount != visitor.DivideCount)
			{
				var location = classDeclaration.Identifier.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(MultiplyDivideRule, location));
			}
		}
	}
}
