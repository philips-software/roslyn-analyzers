﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NoNestedStringFormatsAnalyzer : DiagnosticAnalyzerBase
	{
		private const string NestedStringFormatTitle = @"Don't nest string.Format (or similar) methods";
		private const string NestedStringFormatMessageFormat = @"Don't nest a call to {0} inside a call to {1}";
		private const string NestedStringFormatDescription = NestedStringFormatTitle;

		private const string UnnecessaryStringFormatTitle = @"Don't call string.Format unnecessarily";
		private const string UnnecessaryStringFormatMessageFormat = UnnecessaryStringFormatTitle;
		private const string UnnecessaryStringFormatDescription = UnnecessaryStringFormatTitle;

		private const string Category = Categories.Maintainability;

		private readonly Regex _formatRegex = new(@"^\{\d+\}$", RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromSeconds(1));

		private static readonly DiagnosticDescriptor NestedRule = new(DiagnosticId.NoNestedStringFormats.ToId(), NestedStringFormatTitle, NestedStringFormatMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: NestedStringFormatDescription);
		private static readonly DiagnosticDescriptor UnnecessaryRule = new(DiagnosticId.NoUnnecessaryStringFormats.ToId(), UnnecessaryStringFormatTitle, UnnecessaryStringFormatMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: UnnecessaryStringFormatDescription);

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			context.RegisterOperationAction(OnInvocation, OperationKind.Invocation);
			context.RegisterOperationAction(OnInterpolatedString, OperationKind.InterpolatedString);
		}

		private void OnInterpolatedString(OperationAnalysisContext operationContext)
		{
			var interpolation = (IInterpolatedStringOperation)operationContext.Operation;

			if (interpolation.Parts.Length != 1)
			{
				return;
			}

			IInterpolatedStringContentOperation onlyInterpolation = interpolation.Parts[0];

			if (
				onlyInterpolation is not IInterpolatedStringTextOperation and
				IInterpolationOperation interpolationOperation)
			{
				if (interpolationOperation.FormatString is not null)
				{
					//has a format, ignore
					return;
				}

				ITypeSymbol resultType = interpolationOperation.Expression.Type;

				if (resultType is null)
				{
					return;
				}

				if (resultType.SpecialType != SpecialType.System_String)
				{
					return;
				}

				if (IsFormattableStringMethodArgument(interpolationOperation))
				{
					return;
				}

				Location location = interpolation.Syntax.GetLocation();
				operationContext.ReportDiagnostic(Diagnostic.Create(UnnecessaryRule, location));
			}
		}

		private void OnInvocation(OperationAnalysisContext operationContext)
		{
			var invocation = (IInvocationOperation)operationContext.Operation;

			if (!IsStringFormatMethod(invocation.TargetMethod, out ITypeSymbol returnType, out var formatStringParameterIndex))
			{
				return;
			}

			IOperation argument = invocation.Arguments[formatStringParameterIndex].Value;

			switch (argument.Kind)
			{
				case OperationKind.Invocation:
					if (CheckForNestedStringFormat(operationContext, invocation, (IInvocationOperation)argument))
					{
						return;
					}
					break;
				case OperationKind.InterpolatedString:
					Location location = argument.Syntax.GetLocation();
					var displayString = invocation.TargetMethod.ToDisplayString();
					operationContext.ReportDiagnostic(Diagnostic.Create(NestedRule, location, "an interpolated string", displayString));
					break;
			}

			if (returnType.SpecialType is SpecialType.System_String or SpecialType.System_Void)
			{
				CheckParameters(operationContext, invocation, formatStringParameterIndex, returnType, argument);
			}
		}

		private void CheckParameters(OperationAnalysisContext operationContext, IInvocationOperation invocation,
			int formatStringParameterIndex, ITypeSymbol returnType, IOperation argument)
		{
			IOperation paramsArguments = invocation.Arguments[formatStringParameterIndex + 1].Value;

			if (paramsArguments is IArrayCreationOperation arrayCreation)
			{
				if (arrayCreation.Initializer.ElementValues.IsEmpty && returnType.SpecialType == SpecialType.System_String)
				{
					//string format with no arguments
					Location location = argument.Syntax.GetLocation();
					operationContext.ReportDiagnostic(Diagnostic.Create(UnnecessaryRule, location));
					return;
				}

				CheckStringLiteral(operationContext, argument, arrayCreation);
			}
			else if (
				paramsArguments is IConversionOperation conversion &&
				argument.Kind == OperationKind.Literal &&
				argument.Type.SpecialType == SpecialType.System_String &&
				((string)argument.ConstantValue.Value) == "{0}" &&
				conversion.Operand.Type.SpecialType == SpecialType.System_String)
			{
				Location location = argument.Syntax.GetLocation();
				operationContext.ReportDiagnostic(Diagnostic.Create(UnnecessaryRule, location));
			}
		}

		private void CheckStringLiteral(OperationAnalysisContext operationContext, IOperation argument,
			IArrayCreationOperation arrayCreation)
		{
			if (argument.Kind == OperationKind.Literal && argument.Type.SpecialType == SpecialType.System_String)
			{
				var formatValue = (string)argument.ConstantValue.Value;

				if (_formatRegex.IsMatch(formatValue))
				{
					if (arrayCreation.Initializer.ElementValues.Length == 0)
					{
						//string format ala string.format("{0}")
						Location location = argument.Syntax.GetLocation();
						operationContext.ReportDiagnostic(Diagnostic.Create(UnnecessaryRule, location));
						return;
					}

					if (ArrayContainsString(0, arrayCreation))
					{
						//string format ala string.format("{0}", 3)
						Location location = argument.Syntax.GetLocation();
						operationContext.ReportDiagnostic(Diagnostic.Create(UnnecessaryRule, location));
					}
				}
			}
		}

		private bool ArrayContainsString(int index, IArrayCreationOperation arrayCreation)
		{
			if (arrayCreation.Initializer.ElementValues.Length <= index)
			{
				return false;
			}

			if (arrayCreation.Initializer.ElementValues[index].Type.SpecialType == SpecialType.System_String)
			{
				return true;
			}

			if (arrayCreation.Initializer.ElementValues[index] is IConversionOperation conversionOperation)
			{
				ITypeSymbol type;

				if (conversionOperation.Operand is ILiteralOperation literal)
				{
					type = literal.Type;
				}
				else if (conversionOperation.Operand is IPropertyReferenceOperation property)
				{
					type = property.Type;
				}
				else if (conversionOperation.Operand is IFieldReferenceOperation field)
				{
					type = field.Type;
				}
				else if (conversionOperation.Operand is IMethodReferenceOperation method)
				{
					type = method.Type;
				}
				else if (conversionOperation.Operand is IParameterReferenceOperation parameter)
				{
					type = parameter.Type;
				}
				else if (conversionOperation.Operand is ILocalReferenceOperation local)
				{
					type = local.Type;
				}
				else if (conversionOperation.Operand is IInvocationOperation invocation)
				{
					type = invocation.Type;
				}
				else
				{
					return false;
				}

				return type != null && type.SpecialType == SpecialType.System_String;
			}

			return false;
		}

		private bool CheckForNestedStringFormat(OperationAnalysisContext operationContext, IInvocationOperation target, IInvocationOperation argument)
		{
			IMethodSymbol targetMethod = argument.TargetMethod;

			if (!IsStringFormatMethod(targetMethod, out _, out _))
			{
				return false;
			}

			Location location = argument.Syntax.GetLocation();
			var displayString = targetMethod.ToDisplayString();
			var targetDisplayString = target.TargetMethod.ToDisplayString();
			operationContext.ReportDiagnostic(Diagnostic.Create(NestedRule, location, displayString, targetDisplayString));
			return true;
		}

		private bool IsStringFormatMethod(IMethodSymbol targetMethod, out ITypeSymbol returnType, out int formatStringParameterIndex)
		{
			returnType = targetMethod.ReturnType;

			if (targetMethod.Name == "Format" && targetMethod.ContainingType != null && targetMethod.ContainingType.SpecialType == SpecialType.System_String)
			{
				formatStringParameterIndex = 0;
				return true;
			}

			formatStringParameterIndex = -1;
			if (targetMethod.Parameters.IsDefaultOrEmpty)
			{
				return false;
			}

			if (targetMethod.Parameters.Length < 2)
			{
				return false;
			}

			IParameterSymbol possibleArgsParameter = targetMethod.Parameters[targetMethod.Parameters.Length - 1];

			formatStringParameterIndex = targetMethod.Parameters.Length - 2;
			IParameterSymbol possibleFormatStringParameter = targetMethod.Parameters[formatStringParameterIndex];

			if (possibleFormatStringParameter.Type.SpecialType != SpecialType.System_String)
			{
				return false;
			}

			if (possibleArgsParameter.Type.Kind != SymbolKind.ArrayType)
			{
				return false;
			}

			ITypeSymbol arrayType = ((IArrayTypeSymbol)possibleArgsParameter.Type).ElementType;

			return arrayType.SpecialType == SpecialType.System_Object;
		}

		private static bool IsFormattableStringMethodArgument(IInterpolationOperation interpolationOperation)
		{
			IOperation parent = interpolationOperation.Parent;
			while (parent != null)
			{
				if (parent is IInvocationOperation invocation)
				{
					IMethodSymbol method = invocation.TargetMethod;
					return method.Parameters.Any(p => p.Type.Name == "FormattableString");
				}

				parent = parent.Parent;
			}

			return false;
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(NestedRule, UnnecessaryRule);
	}
}
