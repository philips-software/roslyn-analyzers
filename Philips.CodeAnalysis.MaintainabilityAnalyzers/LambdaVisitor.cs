// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	/// <summary>
	/// 
	/// </summary>
	public class LambdaVisitor : CSharpSyntaxWalker
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		#endregion

		#region Public Interface

		public LambdaVisitor() : base(SyntaxWalkerDepth.Node)
		{ }

		public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
		{
			base.VisitSimpleLambdaExpression(node);

			Lambdas.Add(node);
		}

		public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
		{
			base.VisitParenthesizedLambdaExpression(node);

			Lambdas.Add(node);
		}

		public List<LambdaExpressionSyntax> Lambdas { get; } = new List<LambdaExpressionSyntax>();

		#endregion
	}
}
