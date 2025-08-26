// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.SecurityAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class InputValidationAnalyzer : DiagnosticAnalyzerBase
	{
		private const string Title = @"Regex needs a timeout";
		public const string MessageFormat = @"When constructing a new Regex instance, provide a timeout.";
		private const string Description = @"When constructing a new Regex instance, provide a timeout (or `RegexOptions.NonBacktracking` in .NET 7 and higher) as this can facilitate denial-of-serice attacks.";
		private const string Category = Categories.Security;

		private static readonly Dictionary<string, ValidationSinkInfo> SinkMemberInfos = [];
		private readonly List<SyntaxNode> validationSinks = [];

		public static readonly DiagnosticDescriptor Rule = new(
			DiagnosticId.InputValidation.ToId(),
			Title, MessageFormat, Category,
			DiagnosticSeverity.Error, isEnabledByDefault: false, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
			context.RegisterSyntaxNodeAction(AnalyzeDeclaration, SyntaxKind.MethodDeclaration);
			context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
		}

		private void AnalyzeDeclaration(SyntaxNodeAnalysisContext context)
		{
			var method = (BaseMethodDeclarationSyntax)context.Node;
			IEnumerable<ReturnStatementSyntax> returns = method.Body.DescendantNodes().Where(node => node.IsKind(SyntaxKind.ReturnStatement)).Cast<ReturnStatementSyntax>();
			SeparatedSyntaxList<ParameterSyntax> parameters = method.ParameterList.Parameters;
		}

		private void AnalyzeMethod(SymbolAnalysisContext context)
		{
			var methodSymbol = (IMethodSymbol)context.Symbol;
			if (
				(methodSymbol.ReturnType.SpecialType != SpecialType.System_String) ||
				!methodSymbol.Parameters.Any(parameter => parameter.RefKind == RefKind.Out && parameter.Type.SpecialType == SpecialType.System_String))
			{
				return;
			}

		}

		protected override GeneratedCodeAnalysisFlags GetGeneratedCodeAnalysisFlags()
		{
			return GeneratedCodeAnalysisFlags.Analyze;
		}

		private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
		{
			var invocation = (InvocationExpressionSyntax)context.Node;
			if (Helper.ForTests.IsInTestClass(context))
			{
				return;
			}

			if (invocation.Expression is MemberAccessExpressionSyntax expression)
			{
				if (SinkMemberInfos.TryGetValue(expression.Name.Identifier.Text, out ValidationSinkInfo info))
				{
					PreprocessingSymbolInfo symbolInfo = context.SemanticModel.GetPreprocessingSymbolInfo(expression.Name);
					if (symbolInfo.IsDefined)
					{
						if (info.IsMatch(symbolInfo.Symbol!))
						{
							ArgumentSyntax argument = invocation.ArgumentList.Arguments[info.ArgumentIndex];
							AnalyzeArgument(context, argument);
							validationSinks.Add(argument);
						}
					}
				}
			}
		}

		private void AnalyzeArgument(SyntaxNodeAnalysisContext context, ArgumentSyntax argument)
		{
			DataFlowAnalysis analysis = context.SemanticModel.AnalyzeDataFlow(argument.Expression);
			SyntaxNode originalNode = analysis.DataFlowsIn[0].DeclaringSyntaxReferences[0].GetSyntax();


			var argumentVariableName = GetVariableNameOfArgument(argument);
			SyntaxNode block = argument.FirstAncestorOrSelf<SyntaxNode>(node => node.IsKind(SyntaxKind.Block));
			IEnumerable<SyntaxNode> declaration = block?.DescendantNodes().Where(child => child.IsKind(SyntaxKind.VariableDeclaration));
			IEnumerable<SyntaxNode> calls = block?.DescendantNodes(child =>
				child.IsKind(SyntaxKind.Argument) &&
				GetVariableNameOfArgument(child as ArgumentSyntax) == argumentVariableName);
		}

		private string GetVariableNameOfArgument(ArgumentSyntax argument)
		{
			return (argument?.Expression as IdentifierNameSyntax)?.Identifier.Text;
		}
	}

	internal sealed class ValidationSinkInfo
	{
		private readonly string _namespace;
		private readonly string _typeName;

		public ValidationSinkInfo(string ns, string type, string method, int index)
		{
			_namespace = ns;
			_typeName = type;
			MethodName = method;
			ArgumentIndex = index;
		}

		public string MethodName { get; }

		public int ArgumentIndex { get; }

		public bool IsMatch(IPreprocessingSymbol symbol)
		{
			return
				string.CompareOrdinal(symbol.ContainingNamespace.Name, _namespace) == 0 &&
				string.CompareOrdinal(symbol.ContainingType.Name, _typeName) == 0;
		}
	}
}
