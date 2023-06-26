// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Philips.CodeAnalysis.Common
{
	public class DataFlowHelper
	{
		public DataFlowAnalysis GetDataFlowAnalysis(SemanticModel semanticModel, BaseMethodDeclarationSyntax methodDeclaration)
		{
			DataFlowAnalysis flow;
			BlockSyntax body = methodDeclaration.Body;

			if (body != null)
			{
				if (!body.Statements.Any())
				{
					return null;
				}

				StatementSyntax firstStatement = body.Statements.First();
				StatementSyntax lastStatement = body.Statements.Last();
				flow = semanticModel.AnalyzeDataFlow(firstStatement, lastStatement);
			}
			else if (methodDeclaration.ExpressionBody != null)
			{
				flow = semanticModel.AnalyzeDataFlow(methodDeclaration.ExpressionBody.Expression);
			}
			else
			{
				return null;
			}

			return flow;
		}
	}
}
