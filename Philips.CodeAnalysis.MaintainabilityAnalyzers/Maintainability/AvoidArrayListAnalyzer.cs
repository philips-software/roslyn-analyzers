// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidArrayListAnalyzer : SingleDiagnosticAnalyzer<VariableDeclarationSyntax, AvoidArrayListSyntaxNodeAction>
	{
		private const string Title = @"Don't use ArrayList, use List<T> instead";
		private const string MessageFormat = @"Don't use ArrayList for variable {0}, use List<T> instead";
		private const string Description = @"Usage of Arraylist is discouraged by Microsoft for performance reasons, use List<T> instead.";
		public AvoidArrayListAnalyzer()
			: base(DiagnosticId.AvoidArrayList, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: false)
		{ }
	}
	public class AvoidArrayListSyntaxNodeAction : SyntaxNodeAction<VariableDeclarationSyntax>
	{
		private const string ArrayListTypeName = "ArrayList";
		private const string ArrayListNamespace = "System.Collections";

		public override void Analyze()
		{
			NamespaceResolver aliases = Helper.ForNamespaces.GetUsingAliases(Node);
			if (aliases.IsOfType(Node.Type, ArrayListNamespace, ArrayListTypeName))
			{
				var variableName = Node.Variables.FirstOrDefault()?.Identifier.Text ?? string.Empty;
				Location location = Node.Type.GetLocation();
				ReportDiagnostic(location, variableName);
			}
		}
	}
}
