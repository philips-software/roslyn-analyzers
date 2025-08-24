// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	/// <summary>
	/// Diagnostic for inconsistent number of operators in a class.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AlignOperatorsCountAnalyzer : DiagnosticAnalyzerBase
	{
		private const string Category = Categories.Maintainability;
		private const string Plus = "+";

		private static readonly DiagnosticDescriptor IncrementAndDecrementRule =
			GenerateRule("++", "--", DiagnosticId.AlignNumberOfIncrementAndDecrementOperators);

		private static readonly DiagnosticDescriptor PlusMinusRule =
			GenerateRule(Plus, "-", DiagnosticId.AlignNumberOfPlusAndMinusOperators);

		private static readonly DiagnosticDescriptor MultiplyDivideRule =
			GenerateRule("*", "/", DiagnosticId.AlignNumberOfMultiplyAndDivideOperators);

		private static readonly DiagnosticDescriptor GreaterLessThanRule =
			GenerateRule(">", "<", DiagnosticId.AlignNumberOfGreaterAndLessThanOperators);

		private static readonly DiagnosticDescriptor GreaterLessThanOrEqualRule =
			GenerateRule(">=", "<=", DiagnosticId.AlignNumberOfGreaterAndLessThanOrEqualOperators);

		private static readonly DiagnosticDescriptor ShiftRightAndLeftRule =
			GenerateRule(">>", "<<", DiagnosticId.AlignNumberOfShiftRightAndLeftOperators);

		private static readonly DiagnosticDescriptor PlusAndEqualRule =
			GenerateRule(Plus, "==", DiagnosticId.AlignNumberOfPlusAndEqualOperators);

		private static DiagnosticDescriptor GenerateRule(string first, string second, DiagnosticId diagnosticId)
		{
			return new(
				diagnosticId.ToId(),
				$"Align number of {first} and {second} operators.",
				$"Align number of {first} and {second} operators.",
				Category,
				DiagnosticSeverity.Error,
				isEnabledByDefault: false,
				description: $"Overload the {second} operator, when you overload the {first} operator, as they are often used in combination with each other."
			);
		}

		/// <summary>
		/// <inheritdoc cref="DiagnosticAnalyzer"/>
		/// </summary>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create(IncrementAndDecrementRule, PlusMinusRule, MultiplyDivideRule, GreaterLessThanRule, GreaterLessThanOrEqualRule, ShiftRightAndLeftRule, PlusAndEqualRule);

		/// <summary>
		/// <inheritdoc cref="DiagnosticAnalyzer"/>
		/// </summary>
		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(AnalyzeType, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
		}

		private void AnalyzeType(SyntaxNodeAnalysisContext context)
		{
			if (Helper.ForGeneratedCode.IsGeneratedCode(context))
			{
				return;
			}

			var typeDeclaration = (TypeDeclarationSyntax)context.Node;

			OperatorsVisitor visitor = new();
			visitor.Visit(typeDeclaration);

			AnalyzeVisitor(context, visitor, typeDeclaration.Identifier);
		}

		private void AnalyzeVisitor(SyntaxNodeAnalysisContext context, OperatorsVisitor visitor, SyntaxToken identifier)
		{
			if (visitor.IncrementCount != visitor.DecrementCount)
			{
				Location location = identifier.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(IncrementAndDecrementRule, location));
			}

			if (visitor.PlusCount != visitor.MinusCount)
			{
				Location location = identifier.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(PlusMinusRule, location));
			}

			if (visitor.MultiplyCount != visitor.DivideCount)
			{
				Location location = identifier.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(MultiplyDivideRule, location));
			}

			if (visitor.LessThanCount != visitor.GreaterThanCount)
			{
				Location location = identifier.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(GreaterLessThanRule, location));
			}

			if (visitor.LessThanOrEqualCount != visitor.GreaterThanOrEqualCount)
			{
				Location location = identifier.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(GreaterLessThanOrEqualRule, location));
			}

			if (visitor.ShiftLeftCount != visitor.ShiftRightCount)
			{
				Location location = identifier.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(ShiftRightAndLeftRule, location));
			}

			if ((visitor.PlusCount > 0 || visitor.MinusCount > 0) && visitor.PlusCount != visitor.EqualCount)
			{
				Location location = identifier.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(PlusAndEqualRule, location));
			}
		}
	}
}
