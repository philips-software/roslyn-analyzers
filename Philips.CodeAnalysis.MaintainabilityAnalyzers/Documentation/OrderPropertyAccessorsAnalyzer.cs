// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

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
		public override void Analyze()
		{
			AccessorListSyntax accessors = Node.AccessorList;

			if (accessors is null)
			{
				return;
			}

			var getIndex = -1;
			var setIndex = int.MaxValue;

			for (var i = 0; i < accessors.Accessors.Count; i++)
			{
				AccessorDeclarationSyntax accessor = accessors.Accessors[i];

				if (accessor.Keyword.IsKind(SyntaxKind.GetKeyword))
				{
					getIndex = i;
					continue;
				}

				// SyntaxKind.InitKeyword doesn't exist in the currently used version of Roslyn (it exists in at least 3.9.0)
				if (accessor.Keyword.IsKind(SyntaxKind.SetKeyword) || accessor.Keyword.Text == "init")
				{
					setIndex = i;
				}
			}

			if (setIndex < getIndex)
			{
				Location location = accessors.GetLocation();
				ReportDiagnostic(location);
			}
		}
	}
}
