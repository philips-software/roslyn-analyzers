// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidExcludeFromCodeCoverageAnalyzer : SingleDiagnosticAnalyzer<AttributeListSyntax, AvoidExcludeFromCodeCoverageAnalyzerSyntaxNodeAction>
	{
		private const string Title = @"Avoid the ExcludeFromCodeCoverage attribute";
		public const string MessageFormat = Title;
		private const string Description = MessageFormat;

		public AvoidExcludeFromCodeCoverageAnalyzer()
			: base(DiagnosticId.AvoidExcludeFromCodeCoverage, Title, MessageFormat, Description, Categories.Maintainability)
		{ }
	}

	public class AvoidExcludeFromCodeCoverageAnalyzerSyntaxNodeAction : SyntaxNodeAction<AttributeListSyntax>
	{
		private const string ExcludeFromCodeCoverageAttributeTypeName = "ExcludeFromCodeCoverage";
		public override void Analyze()
		{
			NamespaceResolver aliases = Helper.ForNamespaces.GetUsingAliases(Node);
			foreach (AttributeSyntax attribute in Node.Attributes.Where(attr => IsExcludeFromCodeCoverage(aliases, attr)))
			{
				Location location = Node.GetLocation();
				ReportDiagnostic(location);
			}
		}

		private bool IsExcludeFromCodeCoverage(NamespaceResolver aliases, AttributeSyntax attribute)
		{
			return aliases.GetDealiasedName(attribute.Name).Contains(ExcludeFromCodeCoverageAttributeTypeName);
		}
	}
}
