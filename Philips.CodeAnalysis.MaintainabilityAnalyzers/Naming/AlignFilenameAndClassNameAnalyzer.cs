// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AlignFilenameAndClassNameAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Align filename and class name";
		private const string MessageFormat = @"Name the file {0}.cs to align with the name of the {1} it contains.";
		private const string Description = @"Name the file after the class, struct or enum it contains";
		private const string Category = Categories.Naming;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticId.AlignFilenameAndClassName),
			Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
			description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxTreeAction(Analyze);
		}

		private void Analyze(SyntaxTreeAnalysisContext context)
		{
			var tree = context.Tree;
			var firstType = tree.GetRoot().DescendantNodes()
				.FirstOrDefault(node => node is TypeDeclarationSyntax or EnumDeclarationSyntax);
			string typeKind = null;
			SyntaxToken identifier = default;
			if (firstType is ClassDeclarationSyntax cls)
			{
				typeKind = "class";
				identifier = cls.Identifier;
			}
			else if(firstType is StructDeclarationSyntax value)
			{
				typeKind = "struct";
				identifier = value.Identifier;
			}
			else if(firstType is EnumDeclarationSyntax enumeration)
			{
				typeKind = "enum";
				identifier = enumeration.Identifier;
			}

			if(string.IsNullOrEmpty(typeKind) || identifier == default)
			{
				return;
			}
			GeneratedCodeDetector generatedCodeDetector = new();
			if(generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			var filename = Path.GetFileNameWithoutExtension(tree.FilePath);
			int indexOfDot = filename.IndexOf('.');
			if (indexOfDot != -1)
			{
				filename = filename.Substring(0,indexOfDot);
			}
			if (StringComparer.OrdinalIgnoreCase.Compare(identifier.Text, filename) != 0)
			{
				var location = identifier.GetLocation();
				Diagnostic diagnostic = Diagnostic.Create(Rule, location, identifier.Text, typeKind);
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
