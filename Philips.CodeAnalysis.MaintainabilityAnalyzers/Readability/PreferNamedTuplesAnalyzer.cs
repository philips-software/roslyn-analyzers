// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class PreferNamedTuplesAnalyzer : SingleDiagnosticAnalyzer<TupleTypeSyntax, PreferNamedTuplesSyntaxNodeAction>
	{
		private const string Title = @"Prefer tuples that have names";
		private const string MessageFormat = @"Name this tuple field";
		private const string Description = @"Name this tuple field for readability";

		public PreferNamedTuplesAnalyzer()
			: base(DiagnosticId.PreferTuplesWithNamedFields, Title, MessageFormat, Description, Categories.Readability)
		{
			FullyQualifiedMetaDataName = StringConstants.TupleFullyQualifiedName;
		}
	}

	public class PreferNamedTuplesSyntaxNodeAction : SyntaxNodeAction<TupleTypeSyntax>
	{
		public override IEnumerable<Diagnostic> Analyze()
		{
			return Node.Elements
				.Filter(element => element.Identifier.Kind() == SyntaxKind.None)
				.Select(element => PrepareDiagnostic(element.GetLocation()));
		}
	}
}
