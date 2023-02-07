// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	/// <summary>
	/// Don't allow protected fields, they violate encapsulation
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NoProtectedFieldsAnalyzer : SingleDiagnosticAnalyzer<FieldDeclarationSyntax, NoProtectedFieldsSyntaxNodeAction>
	{
		private const string Title = @"Do not use protected fields";
		private const string MessageFormat = Title;
		private const string Description = Title;
		public NoProtectedFieldsAnalyzer()
			: base(DiagnosticId.NoProtectedFields, Title, MessageFormat, Description, Categories.Maintainability)
		{ }
	}

	public class NoProtectedFieldsSyntaxNodeAction : SyntaxNodeAction<FieldDeclarationSyntax>
	{
		public override void Analyze()
		{
			if (Node.Modifiers.Any(SyntaxKind.ProtectedKeyword))
			{
				var location = Node.GetLocation();
				ReportDiagnostic();
			}
		}
	}
}
