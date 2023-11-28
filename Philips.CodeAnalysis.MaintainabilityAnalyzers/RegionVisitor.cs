// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	public class RegionVisitor : CSharpSyntaxWalker
	{
		private readonly List<DirectiveTriviaSyntax> _regions = [];

		public RegionVisitor() : base(SyntaxWalkerDepth.StructuredTrivia)
		{ }

		public override void VisitRegionDirectiveTrivia(RegionDirectiveTriviaSyntax node)
		{
			base.VisitRegionDirectiveTrivia(node);

			_regions.Add(node);
		}

		public override void VisitEndRegionDirectiveTrivia(EndRegionDirectiveTriviaSyntax node)
		{
			base.VisitEndRegionDirectiveTrivia(node);

			_regions.Add(node);
		}

		public IReadOnlyList<DirectiveTriviaSyntax> Regions => _regions;
	}
}
