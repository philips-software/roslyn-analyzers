using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NoNestedStringFormatsAnalyzer : DiagnosticAnalyzer
	{
		#region Non-Public Data Members

		private const string NestedStringFormatTitle = @"Don't nest string.Format (or similar) methods";
		private const string NestedStringFormatMessageFormat = @"Don't nest a call to {0} inside a call to {1}";
		private const string NestedStringFormatDescription = @"Don't nest string.Format (or similar) methods";

		private const string UnnecessaryStringFormatTitle = @"Don't call string.Format unnecessarily";
		private const string UnnecessaryStringFormatMessageFormat = UnnecessaryStringFormatTitle;
		private const string UnnecessaryStringFormatDescription = UnnecessaryStringFormatTitle;

		private const string Category = Categories.Maintainability;

		private readonly Regex _formatRegex = new Regex(@"^\{\d+\}$", RegexOptions.Compiled);

		#endregion

		#region Non-Public Properties/Methods

		private static DiagnosticDescriptor NestedRule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.NoNestedStringFormats), NestedStringFormatTitle, NestedStringFormatMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: NestedStringFormatDescription);
		private static DiagnosticDescriptor UnnecessaryRule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.NoUnnecessaryStringFormats), UnnecessaryStringFormatTitle, UnnecessaryStringFormatMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: false, description: UnnecessaryStringFormatDescription);

		private void Analyze(CompilationStartAnalysisContext context)
		{
			context.RegisterOperationBlockStartAction(compilationContext =>
			{
				compilationContext.RegisterOperationAction(OnInvocation, OperationKind.Invocation);
				compilationContext.RegisterOperationAction(OnInterpolatedString, OperationKind.InterpolatedString);
			});
		}

		private void OnInterpolatedString(OperationAnalysisContext operationContext)
		{
			var interpolation = (IInterpolatedStringOperation)operationContext.Operation;

			if (interpolation.Parts.Length != 1)
			{
				return;
			}

			var onlyInterpolation = interpolation.Parts[0];

			if (onlyInterpolation is IInterpolatedStringTextOperation textOperation)
			{
			}
			else if (onlyInterpolation is IInterpolationOperation interpolationOperation)
			{
				if (!(interpolationOperation.FormatString is null))
				{
					//has a format, ignore
					return;
				}

				var resultType = interpolationOperation.Expression.Type;

				if (resultType is null)
				{
					return;
				}

				if (resultType.SpecialType != SpecialType.System_String)
				{
					return;
				}

				operationContext.ReportDiagnostic(Diagnostic.Create(UnnecessaryRule, interpolation.Syntax.GetLocation()));
			}

		}

		private void OnInvocation(OperationAnalysisContext operationContext)
		{
			var invocation = (IInvocationOperation)operationContext.Operation;

			if (!IsStringFormatMethod(invocation.TargetMethod, out ITypeSymbol returnType, out int formatStringParameterIndex))
			{
				return;
			}

			var argument = invocation.Arguments[formatStringParameterIndex].Value;

			switch (argument.Kind)
			{
				case OperationKind.Invocation:
					if (CheckForNestedStringFormat(operationContext, invocation, (IInvocationOperation)argument))
					{
						return;
					}
					break;
				case OperationKind.InterpolatedString:
					operationContext.ReportDiagnostic(Diagnostic.Create(NestedRule, argument.Syntax.GetLocation(), "an interpolated string", invocation.TargetMethod.ToDisplayString()));
					break;
			}

			if (returnType.SpecialType == SpecialType.System_String || returnType.SpecialType == SpecialType.System_Void)
			{
				var paramsArguments = invocation.Arguments[formatStringParameterIndex + 1].Value;

				if (paramsArguments is IArrayCreationOperation arrayCreation)
				{
					if (arrayCreation.Initializer.ElementValues.IsEmpty && returnType.SpecialType == SpecialType.System_String)
					{
						//string format with no arguments
						operationContext.ReportDiagnostic(Diagnostic.Create(UnnecessaryRule, argument.Syntax.GetLocation()));
						return;
					}

					if (argument.Kind == OperationKind.Literal && argument.Type.SpecialType == SpecialType.System_String)
					{
						string formatValue = (string)argument.ConstantValue.Value;

						if (_formatRegex.IsMatch(formatValue))
						{
							if (arrayCreation.Initializer.ElementValues.Length == 0)
							{
								//string format ala string.format("{0}");
								operationContext.ReportDiagnostic(Diagnostic.Create(UnnecessaryRule, argument.Syntax.GetLocation()));
								return;
							}

							if (ArrayContainsString(0, arrayCreation))
							{
								//string format ala string.format("{0}", 3);
								operationContext.ReportDiagnostic(Diagnostic.Create(UnnecessaryRule, argument.Syntax.GetLocation()));
								return;
							}
						}
					}
				}
				else if (paramsArguments is IConversionOperation conversion)
				{
					if (argument.Kind == OperationKind.Literal && argument.Type.SpecialType == SpecialType.System_String)
					{
						if (((string)argument.ConstantValue.Value) == "{0}" && conversion.Operand.Type.SpecialType == SpecialType.System_String)
						{
							operationContext.ReportDiagnostic(Diagnostic.Create(UnnecessaryRule, argument.Syntax.GetLocation()));
							return;
						}
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
			var targetMethod = argument.TargetMethod;

			if (!IsStringFormatMethod(targetMethod, out _, out _))
			{
				return false;
			}

			operationContext.ReportDiagnostic(Diagnostic.Create(NestedRule, argument.Syntax.GetLocation(), targetMethod.ToDisplayString(), target.TargetMethod.ToDisplayString()));
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

			var possibleArgsParameter = targetMethod.Parameters[targetMethod.Parameters.Length - 1];

			formatStringParameterIndex = targetMethod.Parameters.Length - 2;
			var possibleFormatStringParameter = targetMethod.Parameters[formatStringParameterIndex];

			if (possibleFormatStringParameter.Type.SpecialType != SpecialType.System_String)
			{
				return false;
			}

			if (possibleArgsParameter.Type.Kind != SymbolKind.ArrayType)
			{
				return false;
			}

			var arrayType = ((IArrayTypeSymbol)possibleArgsParameter.Type).ElementType;

			if (arrayType.SpecialType != SpecialType.System_Object)
			{
				return false;
			}

			return true;
		}

		#endregion

		#region Public Interface

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(NestedRule, UnnecessaryRule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterCompilationStartAction(Analyze);
		}

		#endregion
	}
}
