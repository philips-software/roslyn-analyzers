// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	/// <summary>
	/// Count the different operators in a class.
	/// </summary>
	public class OperatorsVisitor : CSharpSyntaxWalker
	{
		public OperatorsVisitor() : base(SyntaxWalkerDepth.Node)
		{
			PlusCount = 0;
			MinusCount = 0;
		}

		public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node)
		{
			base.VisitOperatorDeclaration(node);
			switch(node.OperatorToken.Kind())
			{
				case SyntaxKind.PlusToken:
					PlusCount += 1;
					break;
				case SyntaxKind.MinusToken:
					MinusCount += 1;
					break;
				case SyntaxKind.AsteriskToken:
					MultiplyCount += 1;
					break;
				case SyntaxKind.SlashToken:
					DivideCount += 1;
					break;
			}
		}

		public int PlusCount { get; private set; }

		public int MinusCount { get; private set; }

		public int MultiplyCount { get; private set; }

		public int DivideCount { get; private set; }
	}
}
