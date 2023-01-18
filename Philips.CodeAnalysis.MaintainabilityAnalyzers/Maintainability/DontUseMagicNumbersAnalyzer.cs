// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class DontUseMagicNumbersAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Don't use magic numbers";
		private const string MessageFormat = @"Don't use magic numbers";
		private const string Description = @"Don't use magic number, define them as constant instead.";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.DontUseMagicNumbers),
			Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
			description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.NumericLiteralExpression);
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			var literal = (LiteralExpressionSyntax)context.Node;

			GeneratedCodeDetector detector = new();
			if (detector.IsGeneratedCode(context))
			{
				return;
			}

			if (!literal.Token.IsKind(SyntaxKind.NumericLiteralToken))
			{
				return;
			}

			if (IsAllowedNumber(literal.Token.Text))
			{
				return;
			}

			// The magic number should be defined in a static field.
			var field = literal.Ancestors().OfType<FieldDeclarationSyntax>().FirstOrDefault();
			if (field == null || !IsStaticOrConst(field))
			{
				context.ReportDiagnostic(Diagnostic.Create(Rule, literal.GetLocation()));
			}
		}

		private static bool IsAllowedNumber(string text)
		{
			long parsed = long.MaxValue;
			string trimmed = text.TrimEnd('f', 'd');
			if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long integer))
			{
				parsed = integer;
			}
			else if(double.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
			{
				double rounded = (long)value;
				if (Math.Abs(rounded - value) < double.Epsilon)
				{
					parsed = (long)value;
				}
			}

			return 
				parsed is 0 or 90 or 180 or 270 or 360 ||
				IsPowerOf(parsed, 2) ||
				IsPowerOf(parsed, 10);
		}

		private static bool IsPowerOf(long nut, int bas)
		{
			long current = 1L;
			while (nut > current)
			{
				current *= bas;
			}

			return nut == current;
		}

		private static bool IsStaticOrConst(FieldDeclarationSyntax field)
		{
			bool isStatic = field.Modifiers.IndexOf(SyntaxKind.StaticKeyword) >= 0;
			bool isConst = field.Modifiers.IndexOf(SyntaxKind.ConstKeyword) >= 0;
			return isStatic || isConst;
		}
	}
}
