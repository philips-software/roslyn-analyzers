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
				case SyntaxKind.EqualsEqualsToken:
					EqualCount += 1;
					break;
			}
		}

		public int IncrementCount { get; private set; }

		public int DecrementCount { get; private set; }

		public int PlusCount { get; private set; }

		public int MinusCount { get; private set; }

		public int MultiplyCount { get; private set; }

		public int DivideCount { get; private set; }

		public int GreaterThanCount { get; private set; }

		public int LessThanCount { get; private set; }

		public int GreaterThanOrEqualCount { get; private set; }

		public int LessThanOrEqualCount { get; private set; }

		public int ShiftRightCount { get; private set; }

		public int ShiftLeftCount { get; private set; }

		public int EqualCount { get; private set; }
	}
}
