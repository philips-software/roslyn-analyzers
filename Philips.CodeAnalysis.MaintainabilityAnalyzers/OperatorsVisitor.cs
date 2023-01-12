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
		}

		public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node)
		{
			base.VisitOperatorDeclaration(node);
			switch(node.OperatorToken.Kind())
			{
				case SyntaxKind.PlusPlusToken:
					IncrementCount += 1;
					break;
				case SyntaxKind.MinusMinusToken:
					DecrementCount += 1;
					break;
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
				case SyntaxKind.GreaterThanToken:
					GreaterThanCount += 1;
					break;
				case SyntaxKind.LessThanToken:
					LessThanCount += 1;
					break;
				case SyntaxKind.GreaterThanEqualsToken:
					GreaterThanOrEqualCount += 1;
					break;
				case SyntaxKind.LessThanEqualsToken:
					LessThanOrEqualCount += 1;
					break;
				case SyntaxKind.GreaterThanGreaterThanToken:
					ShiftRightCount += 1;
					break;
				case SyntaxKind.LessThanLessThanToken:
					ShiftLeftCount += 1;
					break;
			}
		}

		public int IncrementCount { get; private set; } = 0;

		public int DecrementCount { get; private set; } = 0;

		public int PlusCount { get; private set; } = 0;

		public int MinusCount { get; private set; } = 0;

		public int MultiplyCount { get; private set; } = 0;

		public int DivideCount { get; private set; } = 0;

		public int GreaterThanCount { get; private set; } = 0;

		public int LessThanCount { get; private set; } = 0;

		public int GreaterThanOrEqualCount { get; private set; } = 0;

		public int LessThanOrEqualCount { get; private set; } = 0;

		public int ShiftRightCount { get; private set; } = 0;

		public int ShiftLeftCount { get; private set; } = 0;
	}
}
