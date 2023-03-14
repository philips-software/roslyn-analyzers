// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

using static LanguageExt.Prelude;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class OrderPropertyAccessorsAnalyzer : SingleDiagnosticAnalyzer<PropertyDeclarationSyntax, OrderPropertyAccessorsSyntaxNodeAction>
	{
		private const string Title = @"Accessors should be ordered";
		private const string MessageFormat = @"Accessors should be ordered.";
		private const string Description = @"Properties should be ordered get then set (or init)";
		public OrderPropertyAccessorsAnalyzer()
			: base(DiagnosticId.OrderPropertyAccessors, Title, MessageFormat, Description, Categories.Documentation)
		{ }
	}

	public class OrderPropertyAccessorsSyntaxNodeAction : SyntaxNodeAction<PropertyDeclarationSyntax>
	{
		public override IEnumerable<Diagnostic> Analyze()
		{
			return Optional(Node.AccessorList)
				.SelectMany(accsessors =>
					accsessors.Accessors
					.Fold(Option<bool>.None, ReduceSetIsBeforeGet)
					.Filter(setIsBeforeGet => setIsBeforeGet)
					.Select((setIsBeforeGet) => PrepareDiagnostic(accsessors.GetLocation()))
				);
		}

		private static Option<bool> ReduceSetIsBeforeGet(Option<bool> setIsBeforeGet, AccessorDeclarationSyntax accessor)
		{
			if (setIsBeforeGet.IsNone)
			{
				if (accessor.Keyword.IsKind(SyntaxKind.GetKeyword))
				{
					return Optional(false);
				}
				else if (accessor.Keyword.IsKind(SyntaxKind.SetKeyword) || accessor.Keyword.Text == "init")
				{
					return Optional(true);
				}
			}
			return setIsBeforeGet;
		}
	}
}
