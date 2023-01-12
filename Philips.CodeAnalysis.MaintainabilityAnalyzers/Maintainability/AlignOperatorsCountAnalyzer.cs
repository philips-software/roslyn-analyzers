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
		private const string TitleFormat = "Align number of {0} and {1} operators.";
		private const string MessageFormat = "Align number of {0} and {1} operators.";
		private const string DescriptionFormat = 
			"A class should have the same number of {0} as {1} operators.";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor IncrementAndDecrementRule =
			GenerateRule("++", "--", DiagnosticIds.AlignNumberOfIncrementAndDecrementOperators);

		private static readonly DiagnosticDescriptor PlusMinusRule =
			GenerateRule("+", "-", DiagnosticIds.AlignNumberOfPlusAndMinusOperators);

		private static readonly DiagnosticDescriptor MultiplyDivideRule =
			GenerateRule("*", "/", DiagnosticIds.AlignNumberOfMultiplyAndDivideOperators);

		private static readonly DiagnosticDescriptor GreaterLessThanRule =
			GenerateRule(">", "<", DiagnosticIds.AlignNumberOfGreaterAndLessThanOperators);

		private static readonly DiagnosticDescriptor GreaterLessThanOrEqualRule =
			GenerateRule(">=", "<=", DiagnosticIds.AlignNumberOfGreaterAndLessThanOrEqualOperators);

		private static readonly DiagnosticDescriptor ShiftRightAndLeftRule =
			GenerateRule(">>", "<<", DiagnosticIds.AlignNumberOfShiftRightAndLeftOperators);

		private static DiagnosticDescriptor GenerateRule(string first, string second, DiagnosticIds diagnosticId)
		{
			return new(
				Helper.ToDiagnosticId(diagnosticId),
				string.Format(TitleFormat, first, second),
				string.Format(MessageFormat, first, second),
				Category,
				DiagnosticSeverity.Error,
				isEnabledByDefault: true,
				description: string.Format(DescriptionFormat, first, second)
			);
		}

		/// <summary>
		/// <inheritdoc cref="DiagnosticAnalyzer"/>
		/// </summary>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create(IncrementAndDecrementRule, PlusMinusRule, MultiplyDivideRule, GreaterLessThanRule, GreaterLessThanOrEqualRule, ShiftRightAndLeftRule);

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

			if (visitor.IncrementCount != visitor.DecrementCount)
			{
				var location = classDeclaration.Identifier.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(IncrementAndDecrementRule, location));
			}

			if (visitor.PlusCount != visitor.MinusCount)
			{
				var location = classDeclaration.Identifier.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(PlusMinusRule, location));
			}

			if (visitor.MultiplyCount != visitor.DivideCount)
			{
				var location = classDeclaration.Identifier.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(MultiplyDivideRule, location));
			}

			if (visitor.LessThanCount != visitor.GreaterThanCount)
			{
				var location = classDeclaration.Identifier.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(GreaterLessThanRule, location));
			}

			if (visitor.LessThanOrEqualCount != visitor.GreaterThanOrEqualCount)
			{
				var location = classDeclaration.Identifier.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(GreaterLessThanOrEqualRule, location));
			}

			if (visitor.ShiftLeftCount != visitor.ShiftRightCount)
			{
				var location = classDeclaration.Identifier.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(ShiftRightAndLeftRule, location));
			}
		}
	}
}
