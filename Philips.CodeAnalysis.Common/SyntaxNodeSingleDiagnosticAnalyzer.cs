// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	public abstract class SingleDiagnosticAnalyzer<T> : SingleDiagnosticAnalyzer where T : SyntaxNode
	{
		protected SyntaxNodeAnalysisContext Context { get; private set; }
		protected T Node { get; private set; }

		protected SingleDiagnosticAnalyzer(DiagnosticId id, string title, string messageFormat, string description, string category,
											Helper helper = null, DiagnosticSeverity severity = DiagnosticSeverity.Error, bool isEnabled = true)
			: base(id, title, messageFormat, description, category, helper, severity, isEnabled)
		{ }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			SyntaxKind syntaxKind = GetSyntaxKind();
			context.RegisterSyntaxNodeAction(StartAnalysis, syntaxKind);
		}

		private void StartAnalysis(SyntaxNodeAnalysisContext context)
		{
			Context = context;
			Node = (T)context.Node;

			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			Analyze();
		}

		protected abstract void Analyze();

		private SyntaxKind GetSyntaxKind()
		{
			return typeof(T).Name switch
			{
				nameof(CompilationUnitSyntax) => SyntaxKind.CompilationUnit,
				nameof(MethodDeclarationSyntax) => SyntaxKind.MethodDeclaration,
				nameof(PropertyDeclarationSyntax) => SyntaxKind.PropertyDeclaration,
				nameof(ClassDeclarationSyntax) => SyntaxKind.ClassDeclaration,
				nameof(NamespaceDeclarationSyntax) => SyntaxKind.NamespaceDeclaration,
				_ => SyntaxKind.None,
			};
		}
	}
}
