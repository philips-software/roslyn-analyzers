// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class EnableDocumentationCreationAnalyzer : SingleDiagnosticAnalyzer<CompilationUnitSyntax, EnableDocumentationCreationAction>
	{
		private const string Title = @"Enable documentation creation";
		private const string MessageFormat = @"Add XML documentation generation to the project file, to be able to see Diagnostics for XML documentation.";
		private const string Description = Title;

		public EnableDocumentationCreationAnalyzer()
			: base(DiagnosticId.EnableDocumentationCreation, Title, MessageFormat, Description, Categories.Documentation, isEnabled: false)
		{ }
	}

	public class EnableDocumentationCreationAction : SyntaxNodeAction<CompilationUnitSyntax>
	{
		public override void Analyze()
		{
			if (Node.SyntaxTree.Options.DocumentationMode == DocumentationMode.None)
			{
				ReportDiagnostic();
			}
		}
	}
}
