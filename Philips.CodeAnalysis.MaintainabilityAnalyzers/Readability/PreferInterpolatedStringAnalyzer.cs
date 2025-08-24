// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class PreferInterpolatedStringAnalyzer : DiagnosticAnalyzerBase
	{
		[Flags]
		private enum ConversionResult
		{
			None = 0,
			CanConvert = 1,
			IsUnnecessary = 2
		}
		private const string Title = @"Prefer interpolated strings over string.Format";
		private const string MessageFormat = @"Replace string.Format with interpolated string for better readability";
		private const string Description = @"Interpolated strings are more readable and less error prone than string.Format";

		private const string UnnecessaryTitle = @"Unnecessary call to string.Format";
		private const string UnnecessaryMessageFormat = @"Remove unnecessary call to string.Format";
		private const string UnnecessaryDescription = @"string.Format calls with no placeholders are unnecessary";

		private const string Category = Categories.Readability;

		private static readonly DiagnosticDescriptor Rule = new(
			DiagnosticId.PreferInterpolatedString.ToId(),
			Title,
			MessageFormat,
			Category,
			DiagnosticSeverity.Error,
			isEnabledByDefault: false,
			description: Description);

		private static readonly DiagnosticDescriptor UnnecessaryRule = new(
			DiagnosticId.PreferInterpolatedString.ToId(),
			UnnecessaryTitle,
			UnnecessaryMessageFormat,
			Category,
			DiagnosticSeverity.Error,
			isEnabledByDefault: false,
			description: UnnecessaryDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, UnnecessaryRule);

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			context.RegisterOperationAction(OnInvocation, OperationKind.Invocation);
		}

		private void OnInvocation(OperationAnalysisContext operationContext)
		{
			var invocation = (IInvocationOperation)operationContext.Operation;

			if (!IsStringFormatMethod(invocation.TargetMethod))
			{
				return;
			}

			ConversionResult result = CanConvertToInterpolatedString(invocation);
			if (result == ConversionResult.None)
			{
				return;
			}

			Location location = invocation.Syntax.GetLocation();
			if (result.HasFlag(ConversionResult.IsUnnecessary))
			{
				operationContext.ReportDiagnostic(Diagnostic.Create(UnnecessaryRule, location));
			}
			else
			{
				operationContext.ReportDiagnostic(Diagnostic.Create(Rule, location));
			}
		}

		private bool IsStringFormatMethod(IMethodSymbol targetMethod)
		{
			return targetMethod.Name == "Format" &&
				   targetMethod.ContainingType != null &&
				   targetMethod.ContainingType.SpecialType == SpecialType.System_String;
		}

		private ConversionResult CanConvertToInterpolatedString(IInvocationOperation invocation)
		{
			if (invocation.Arguments.Length < 1)
			{
				return ConversionResult.None;
			}

			IOperation formatStringArgument = invocation.Arguments[0].Value;

			if (formatStringArgument.Kind != OperationKind.Literal ||
				formatStringArgument.Type?.SpecialType != SpecialType.System_String)
			{
				return ConversionResult.None;
			}

			var formatString = (string)formatStringArgument.ConstantValue.Value;

			// Parse the format string to find actual placeholders, ignoring escaped braces
			var placeholderCount = CountFormatPlaceholders(formatString);

			if (placeholderCount == 0)
			{
				// Only flag as unnecessary if there are no braces at all
				// If there are escaped braces, string.Format is needed to produce literal braces
				var hasAnyBraces = formatString.Contains("{") || formatString.Contains("}");
				if (!hasAnyBraces)
				{
					return ConversionResult.CanConvert | ConversionResult.IsUnnecessary;
				}

				// Has escaped braces but no placeholders - don't suggest conversion
				return ConversionResult.None;
			}

			return invocation.Arguments.Length > 1 ? ConversionResult.CanConvert : ConversionResult.None;
		}

		private int CountFormatPlaceholders(string formatString)
		{
			var placeholderCount = 0;
			var i = 0;

			while (i < formatString.Length)
			{
				if (formatString[i] == '{')
				{
					if (i + 1 < formatString.Length && formatString[i + 1] == '{')
					{
						// Escaped brace {{, skip both characters
						i += 2;
						continue;
					}

					// Look for closing brace
					var j = i + 1;
					while (j < formatString.Length && formatString[j] != '}')
					{
						j++;
					}

					if (j < formatString.Length && formatString[j] == '}')
					{
						// Found valid placeholder, check if it's a simple numeric placeholder
						var content = formatString.Substring(i + 1, j - i - 1);

						// Handle format specifiers like {0:N2} by splitting on ':'
						var colonIndex = content.IndexOf(':');
						var indexPart = colonIndex >= 0 ? content.Substring(0, colonIndex) : content;

						if (int.TryParse(indexPart.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
						{
							placeholderCount++;
						}
						i = j + 1;
					}
					else
					{
						i++;
					}
				}
				else if (formatString[i] == '}')
				{
					if (i + 1 < formatString.Length && formatString[i + 1] == '}')
					{
						// Escaped brace }}, skip both characters
						i += 2;
						continue;
					}
					i++;
				}
				else
				{
					i++;
				}
			}

			return placeholderCount;
		}
	}
}