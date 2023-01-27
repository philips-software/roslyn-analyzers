// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
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

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.AlignFilenameAndClassName),
			Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
			description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.StructDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeEnum, SyntaxKind.EnumDeclaration);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			TypeDeclarationSyntax typeDeclaration = (TypeDeclarationSyntax)context.Node;
			string typeKind = null;
			if(typeDeclaration is ClassDeclarationSyntax)
			{
				typeKind = "class";
			}
			else if(typeDeclaration is StructDeclarationSyntax)
			{
				typeKind = "struct";
			}

			if (string.IsNullOrEmpty(typeKind))
			{
				return;
			}
			Check(context, typeDeclaration.Identifier, typeKind);
		}

		private void AnalyzeEnum(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if(generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			EnumDeclarationSyntax enumDeclaration = (EnumDeclarationSyntax)context.Node;
			Check(context, enumDeclaration.Identifier, "enum");
		}

		private static void Check(SyntaxNodeAnalysisContext context, SyntaxToken identifier, string typeKind)
		{
			var filename = Path.GetFileNameWithoutExtension(context.Node.SyntaxTree.FilePath);
			int indexOfDot = filename.LastIndexOf('.');
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
