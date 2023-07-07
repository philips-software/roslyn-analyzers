// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AlignFilenameAndClassNameAnalyzer : SingleDiagnosticAnalyzer
	{
		private const string Title = @"Align filename and class name";
		private const string MessageFormat = @"Name the file {0}.cs to align with the name of the {1} it contains.";
		private const string Description = @"Name the file after the class, struct or enum it contains";

		public AlignFilenameAndClassNameAnalyzer()
			: base(DiagnosticId.AlignFilenameAndClassName, Title, MessageFormat, Description, Categories.Naming, isEnabled: false)
		{ }

		protected override void InitializeAnalysis(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxTreeAction(Analyze);
		}

		private void Analyze(SyntaxTreeAnalysisContext context)
		{
			SyntaxTree tree = context.Tree;
			SyntaxNode firstType = tree.GetRoot().DescendantNodes()
				.FirstOrDefault(node => node is TypeDeclarationSyntax or EnumDeclarationSyntax);
			string typeKind = null;
			SyntaxToken identifier = default;
			if (firstType is ClassDeclarationSyntax cls)
			{
				typeKind = "class";
				identifier = cls.Identifier;
			}
			else if (firstType is StructDeclarationSyntax value)
			{
				typeKind = "struct";
				identifier = value.Identifier;
			}
			else if (firstType is EnumDeclarationSyntax enumeration)
			{
				typeKind = "enum";
				identifier = enumeration.Identifier;
			}

			if (string.IsNullOrEmpty(typeKind) || identifier == default)
			{
				return;
			}
			GeneratedCodeDetector generatedCodeDetector = new(Helper);
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			var filename = Path.GetFileNameWithoutExtension(tree.FilePath);
			var indexOfDot = filename.IndexOf('.');
			if (indexOfDot != -1)
			{
				filename = filename.Substring(0, indexOfDot);
			}
			var indexOfCurly = filename.IndexOf('{');
			if (indexOfCurly != -1)
			{
				filename = filename.Substring(0, indexOfCurly);
			}
			if (StringComparer.OrdinalIgnoreCase.Compare(identifier.Text, filename) != 0)
			{
				Location location = identifier.GetLocation();
				var diagnostic = Diagnostic.Create(Rule, location, identifier.Text, typeKind);
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
