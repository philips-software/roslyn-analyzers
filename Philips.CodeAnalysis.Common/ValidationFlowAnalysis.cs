
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Philips.CodeAnalysis.Common
{
	public class ValidationFlowAnalysis
	{
		private readonly HashSet<string> _connectedToReturn;

		public ValidationFlowAnalysis(BaseMethodDeclarationSyntax method)
		{
			_connectedToReturn = [];
			IEnumerable<SyntaxToken> parameters = method.ParameterList.Parameters.Select(para => para.Identifier);
			List<IdentifierNameSyntax> returnVariable = FindReturnVariable(method);
			FindConnected(parameters, returnVariable);
		}

		/// <summary>
		/// Gets a list of all input parameters that are directly influencing one of the return variables.
		/// </summary>
		public ImmutableList<string> ConnectedToReturn => _connectedToReturn.ToImmutableList();

		private void FindConnected(IEnumerable<SyntaxToken> tokens, List<IdentifierNameSyntax> returnedVariables)
		{
			var returnedNames = new HashSet<string>();
			foreach (IdentifierNameSyntax ret in returnedVariables)
			{
				_ = returnedNames.Add(ret.Identifier.Text);
			}
			foreach (SyntaxToken token in tokens)
			{
				if (returnedNames.Contains(token.Text))
				{
					_ = _connectedToReturn.Add(token.Text);
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
