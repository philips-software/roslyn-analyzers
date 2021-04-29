// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	/// <summary>
	/// Base class for reporting when calling specific methods or properties.
	/// </summary>
	public abstract class MemberInClassAnalyzerBase : DiagnosticAnalyzer
	{
		private readonly string _classId;
		private readonly string _methodId;
		private readonly string[] _argumentIds;
		private readonly bool _expectToCall;

		private ImmutableArray<ISymbol> _memberSymbols;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="className">Fully qualified classname (including namespace).</param>
		/// <param name="methodName">The method or property name.</param>
		protected MemberInClassAnalyzerBase(
			string className,
			string methodName
		)
		{
			_classId = className;
			_methodId = methodName;
			_argumentIds = null;
			_expectToCall = false;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="className">Fully qualified classname (including namespace).</param>
		/// <param name="methodName">The method or property name.</param>
		/// <param name="expected">
		/// If true encourage usage of this overload, otherwise discourage this overload
		/// from being called.
		/// </param>
		/// <param name="arguments">
		/// Method overload selection, based on type of the arguments
		/// </param>
		protected MemberInClassAnalyzerBase(
			string className,
			string methodName,
			bool expected,
			string[] arguments
		)
		{
			_classId = className;
			_methodId = methodName;
			_argumentIds = arguments;
			_expectToCall = expected;
		}

		/// <summary>
		/// Called when a Diagnostic is identified.
		/// </summary>
		/// <param name="context">The analysis context.</param>
		/// <param name="node">The <see cref="SyntaxNode"/> to report a Diagnostic on.</param>
		protected abstract void OnReportDiagnostic(
			SyntaxNodeAnalysisContext context,
			ExpressionSyntax node
		);

		/// <summary>
		/// <inheritdoc cref="DiagnosticAnalyzer"/>
		/// </summary>
		/// <param name="context"></param>
		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction(CompilationStart);
		}

		private void CompilationStart(CompilationStartAnalysisContext context)
		{
			var classSymbol = context.Compilation.GetTypeByMetadataName(_classId);
			// Only continue when the class is accessible for this compilation.
			if (classSymbol != null)
			{
				_memberSymbols = classSymbol.GetMembers(_methodId);
				// Filter overloads
				if (_argumentIds != null)
				{
					var argumentSymbols = _argumentIds.Select(
						attr => context.Compilation.GetTypeByMetadataName(attr)).ToList();
					_memberSymbols = _memberSymbols.Where(
						method =>
						{
							if (method is IMethodSymbol methodSymbol)
							{
								var parameters = methodSymbol.Parameters.ToList();
								return IsSameOverload(argumentSymbols, parameters);
							}

							return false;
						}).ToImmutableArray();
				}

				// Register actions on invocations
				// (Either constructor or accessing a method / property).
				context.RegisterSyntaxNodeAction(
					AnalyzeExpression,
					SyntaxKind.ObjectCreationExpression);
				context.RegisterSyntaxNodeAction(
					AnalyzeExpression,
					SyntaxKind.SimpleMemberAccessExpression);
			}
		}

		private void AnalyzeExpression(SyntaxNodeAnalysisContext context)
		{
			if (context.Node is ExpressionSyntax invocation)
			{
				// Match the invoked symbol.
				var args = GetArguments(invocation);
				var invokeInfo = context.SemanticModel.GetSymbolInfo(context.Node);
				var invokedSymbols = GetSymbols(invokeInfo);
				if (
					!invokedSymbols.IsEmpty &&
					CheckMember(invokedSymbols.First()) &&
					CheckMethodArguments(context, args) != _expectToCall
				)
				{
					OnReportDiagnostic(context, invocation);
				}
			}
		}

		private ImmutableArray<ISymbol> GetSymbols(SymbolInfo info)
		{
			return info.Symbol != null ? ImmutableArray.Create(info.Symbol) : info.CandidateSymbols;
		}

		private ArgumentListSyntax GetArguments(ExpressionSyntax expression)
		{
			ArgumentListSyntax arguments = null;
			if (expression is ObjectCreationExpressionSyntax creation)
			{
				arguments = creation.ArgumentList;
			}
			else if (expression is InvocationExpressionSyntax invocation)
			{
				arguments = invocation.ArgumentList;
			}
			return arguments;
		}

		private bool CheckMember(ISymbol symbol)
		{
			bool result = true;
			if (symbol.ContainingType != null)
			{
				result = _classId.EndsWith(symbol.ContainingType.Name, StringComparison.Ordinal);
			}
			return result && _methodId.Equals(symbol.Name, StringComparison.Ordinal);
		}

		private bool CheckMethodArguments(SyntaxNodeAnalysisContext context, ArgumentListSyntax args)
		{
			bool result = false;
			if (_argumentIds != null && _argumentIds.Any())
			{
				if (args != null)
				{
					// Expect same number of arguments
					result = _argumentIds.Length == args.Arguments.Count;
					if (result)
					{
						// Check the types of the arguments, in same order.
						for (int i = 0; i < _argumentIds.Length; i++)
						{
							var variable = args.Arguments[i].Expression;
							var variableType = ModelExtensions.GetTypeInfo(context.SemanticModel, variable).Type;
							if (variableType != null)
							{
								result &= _argumentIds[i].EndsWith(variableType.Name, StringComparison.Ordinal);
							}
						}
					}
				}
			}
			else
			{
				// Expecting no arguments.
				result = args == null || args.Arguments.Any();
			}
			return result;
		}
		private bool IsSameOverload(
			List<INamedTypeSymbol> expected,
			List<IParameterSymbol> actual
		)
		{
			var areSame = expected.Count == actual.Count;
			if (areSame)
			{
				for (var i = 0; i < expected.Count; i++)
				{
					areSame &= ReferenceEquals(expected[i], actual[i].Type);
				}
			}
			return areSame;
		}
	}
}
