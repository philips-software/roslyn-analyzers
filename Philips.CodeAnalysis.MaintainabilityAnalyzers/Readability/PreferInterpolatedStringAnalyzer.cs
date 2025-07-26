// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
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

		private const string Category = Categories.Readability;

		private static readonly DiagnosticDescriptor Rule = new(
			DiagnosticId.PreferInterpolatedString.ToId(),
			Title,
			MessageFormat,
			Category,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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

			if (!CanConvertToInterpolatedString(invocation))
			{
				return;
			}

			Location location = invocation.Syntax.GetLocation();
			operationContext.ReportDiagnostic(Diagnostic.Create(Rule, location));
		}

		private bool IsStringFormatMethod(IMethodSymbol targetMethod)
		{
			return targetMethod.Name == "Format" &&
				   targetMethod.ContainingType != null &&
				   targetMethod.ContainingType.SpecialType == SpecialType.System_String;
		}

		private bool CanConvertToInterpolatedString(IInvocationOperation invocation)
		{
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

			var hasPlaceholders = formatString.Contains("{") && formatString.Contains("}");
			if (!hasPlaceholders)
			{
				return false;
			}

			return invocation.Arguments.Length > 1;
		}
	}
}
