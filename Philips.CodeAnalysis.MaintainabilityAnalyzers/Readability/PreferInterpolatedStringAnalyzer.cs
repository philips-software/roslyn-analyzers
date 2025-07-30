// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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
			isEnabledByDefault: true,
			description: Description);

		private static readonly DiagnosticDescriptor UnnecessaryRule = new(
			DiagnosticId.PreferInterpolatedString.ToId(),
			UnnecessaryTitle,
			UnnecessaryMessageFormat,
			Category,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true,
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

			if (!CanConvertToInterpolatedString(invocation, out var isUnnecessary))
			{
				return;
			}

			Location location = invocation.Syntax.GetLocation();
			if (isUnnecessary)
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

		private bool CanConvertToInterpolatedString(IInvocationOperation invocation, out bool isUnnecessary)
		{
			isUnnecessary = false;
			if (invocation.Arguments.Length < 1)
			{
				return false;
			}

			IOperation formatStringArgument = invocation.Arguments[0].Value;

			if (formatStringArgument.Kind != OperationKind.Literal ||
				formatStringArgument.Type?.SpecialType != SpecialType.System_String)
			{
				return false;
			}

			var formatString = (string)formatStringArgument.ConstantValue.Value;

			if (formatString.Contains(":"))
			{
				return false;
			}

			// Parse the format string to find actual placeholders, ignoring escaped braces
			var placeholderCount = CountFormatPlaceholders(formatString);
			var hasAnyBraces = formatString.Contains("{") || formatString.Contains("}");

			if (placeholderCount == 0)
			{
				// Only flag as unnecessary if there are no braces at all
				// If there are escaped braces, string.Format is needed to produce literal braces
				if (!hasAnyBraces)
				{
					isUnnecessary = true;
					return true;
				}
				else
				{
					// Has escaped braces but no placeholders - don't suggest conversion
					return false;
				}
			}

			return invocation.Arguments.Length > 1;
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
						if (int.TryParse(content.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
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