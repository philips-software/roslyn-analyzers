// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	/// <summary>
	/// 
	/// </summary>
	public class RegionVisitor : CSharpSyntaxWalker
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		#endregion

		#region Public Interface

		public RegionVisitor() : base(SyntaxWalkerDepth.StructuredTrivia)
		{ }

		public override void VisitRegionDirectiveTrivia(RegionDirectiveTriviaSyntax node)
		{
			base.VisitRegionDirectiveTrivia(node);

			Regions.Add(node);
		}

		public override void VisitEndRegionDirectiveTrivia(EndRegionDirectiveTriviaSyntax node)
		{
			base.VisitEndRegionDirectiveTrivia(node);

			Regions.Add(node);
		}

		public List<DirectiveTriviaSyntax> Regions { get; } = new List<DirectiveTriviaSyntax>();

		#endregion
	}
}
