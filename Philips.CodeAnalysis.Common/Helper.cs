﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	public class Helper
	{
		public static string ToDiagnosticId(DiagnosticIds id)
		{
			return @"PH" + ((int)id).ToString();
		}

		public string ToPrettyList(IEnumerable<Diagnostic> diagnostics)
		{
			var values = diagnostics.Select(diagnostic => diagnostic.Id);
			return string.Join(", ", values);
		}

		/// <summary>
		/// Checks for the presence of an "autogenerated" comment in the starting trivia for a file
		/// The compiler generates a version of the AssemblyInfo.cs file for certain projects (not named AssemblyInfo.cs), and this is how to pick it up
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public bool HasAutoGeneratedComment(CompilationUnitSyntax node)
		{
			if (node.FindToken(0).IsKind(SyntaxKind.EndOfFileToken))
			{
				return false;
			}

			var first = node.GetLeadingTrivia();

			if (first.Count == 0)
			{
				return false;
			}

			string possibleHeader = first.ToFullString();


			bool isAutogenerated = possibleHeader.Contains(@"<autogenerated />") || possibleHeader.Contains("<auto-generated");

			return isAutogenerated;
		}

		public bool IsLiteralNull(ExpressionSyntax expression)
		{
			return expression is LiteralExpressionSyntax { Token.Text: "null" };
		}

		public bool IsLiteral(ExpressionSyntax expression, SemanticModel semanticModel)
		{
			if (expression is LiteralExpressionSyntax literal)
			{
				Optional<object> literalValue = semanticModel.GetConstantValue(literal);

				return literalValue.HasValue;
			}

			var constant = semanticModel.GetConstantValue(expression);
			return constant.HasValue || IsConstantExpression(expression, semanticModel);
		}

		private bool IsConstantExpression(ExpressionSyntax expression, SemanticModel semanticModel)
		{
			// this assumes you've already checked for literals
			if (expression is MemberAccessExpressionSyntax)
			{
				// return true for member accesses that resolve to a constant e.g. SurveillanceConstants.TrendWidth
				Optional<object> constValue = semanticModel.GetConstantValue(expression);
				return constValue.HasValue;
			}
			else
			{
				if (expression is TypeOfExpressionSyntax typeOfExpression && typeOfExpression.Type is PredefinedTypeSyntax)
				{
					// return true for typeof(<static type>)
					return true;
				}
			}

			return false;
		}

		public bool IsExtensionClass(INamedTypeSymbol declaredSymbol)
		{
			return 
				declaredSymbol is { MightContainExtensionMethods: true } &&
					!declaredSymbol.GetMembers().Any(m =>
						m.Kind == SymbolKind.Method &&
						m.DeclaredAccessibility == Accessibility.Public &&
						!((IMethodSymbol)m).IsExtensionMethod);
		}


		public string GetFileName(string filePath)
		{
			string[] nodes = filePath.Split('/', '\\');
			return nodes[nodes.Length - 1];
		}

		public bool IsAssemblyInfo(SyntaxNodeAnalysisContext context)
		{
			string fileName = GetFileName(context.Node.SyntaxTree.FilePath);

			return fileName.EndsWith("AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase);
		}

		public bool IsInheritingFromClass(INamedTypeSymbol inputType, string classTypeName)
		{
			INamedTypeSymbol type = inputType;
			while (type != null)
			{
				if (type.Name == classTypeName)
				{
					return true;
				}
				type = type.BaseType;
			}

			return false;
		}

		public bool IsUserControl(INamedTypeSymbol type)
		{
			return IsInheritingFromClass(type, @"ContainerControl");
		}

		public bool IsLiteralTrueFalse(ExpressionSyntax expressionSyntax)
		{
			var kind = expressionSyntax.Kind();
			return kind switch
			{
				SyntaxKind.LogicalNotExpression => IsLiteralTrueFalse(((PrefixUnaryExpressionSyntax)expressionSyntax).Operand),//recurse.
				SyntaxKind.TrueLiteralExpression or SyntaxKind.FalseLiteralExpression => true,//literal true/false
				_ => false,
			};
		}


		public IReadOnlyDictionary<string, string> GetUsingAliases(SyntaxNode node)
		{
			var list = new Dictionary<string, string>();
			var root = node.SyntaxTree.GetRoot();
			foreach(var child in root.DescendantNodes(n => n is not TypeDeclarationSyntax).OfType<UsingDirectiveSyntax>())
			{
				if(child.Alias != null)
				{
					var alias = child.Alias.Name.GetFullName(list);
					var name = child.Name.GetFullName(list);
					list.Add(alias, name);
				}
			}
			return list;
		}

		public bool IsCallableFromOutsideClass(MemberDeclarationSyntax method)
		{
			return method.Modifiers.Any(SyntaxKind.PublicKeyword) || method.Modifiers.Any(SyntaxKind.InternalKeyword) || method.Modifiers.Any(SyntaxKind.ProtectedKeyword);
		}
	}
}
