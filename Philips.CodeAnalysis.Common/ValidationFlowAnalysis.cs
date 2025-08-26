
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Philips.CodeAnalysis.Common
{
	/// <summary>
	/// Analyze the flow of input variables to output variables. Useful for input validation checks.
	/// </summary>
	public class ValidationFlowAnalysis
	{
		private readonly HashSet<string> _connectedToReturn;
		private readonly List<Tuple<string, string>> _aliases;

		public ValidationFlowAnalysis(BaseMethodDeclarationSyntax method)
		{
			_connectedToReturn = [];
			_aliases = [];
			FindConnected(method);
		}

		/// <summary>
		/// Gets a list of all input parameters that are directly influencing one of the return variables.
		/// </summary>
		public ImmutableList<string> ConnectedToReturn => _connectedToReturn.ToImmutableList();

		private void FindConnected(BaseMethodDeclarationSyntax method)
		{
			var parameters = method.ParameterList.Parameters.Select(para => para.Identifier.Text).ToList();
			AddAssignments(method, parameters);
			IEnumerable<string> returnVariable = FindReturnVariable(method).Select(ret => ret.Identifier.Text);
			var returnedNames = new HashSet<string>(returnVariable);
			foreach (var token in parameters)
			{
				if (returnedNames.Contains(token))
				{
					_ = _connectedToReturn.Add(token);
					break;
				}

				List<string> needle = GetAliases(token);

			}
		}

		private List<string> GetAliases(string needle)
		{
			var foundList = new List<string>();
			foreach (Tuple<string, string> pair in _aliases)
			{
				if (pair.Item1 == needle)
				{
					foundList.Add(pair.Item2);
				}
			}

			return foundList;
		}

		private void AddAssignments(BaseMethodDeclarationSyntax method, List<string> parameters)
		{
			IEnumerable<SyntaxNode> declarators = method.DescendantNodes().Where(node => node.IsKind(SyntaxKind.VariableDeclarator));
			foreach (VariableDeclaratorSyntax declarator in declarators)
			{
				var assignedFrom = FindReferencedVariable(declarator.Initializer?.Value)?.Identifier.Text;
				if (!string.IsNullOrEmpty(assignedFrom))
				{
					var assignedTo = declarator.Identifier.Text;
					if (parameters.Contains(assignedFrom))
					{
						_aliases.Add(new Tuple<string, string>(assignedTo, assignedFrom));
					}
				}
			}
		}

		private List<IdentifierNameSyntax> FindReturnVariable(BaseMethodDeclarationSyntax method)
		{
			IEnumerable<SyntaxNode> statements = method.Body?.DescendantNodes().Where(node => node.IsKind(SyntaxKind.ReturnStatement));
			return statements?.Select(statement => FindReferencedVariable(((ReturnStatementSyntax)statement).Expression)).ToList();
		}

		private IdentifierNameSyntax FindReferencedVariable(ExpressionSyntax expression)
		{
			if (expression is IdentifierNameSyntax identifierName)
			{
				return identifierName;
			}

			return null;
		}
	}
}
