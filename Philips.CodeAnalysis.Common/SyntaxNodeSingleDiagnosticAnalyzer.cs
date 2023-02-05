// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	public abstract class SingleDiagnosticAnalyzer<T> : SingleDiagnosticAnalyzer where T : SyntaxNode
	{
		protected string FullyQualifiedMetaDataName { get; set; }

		protected SingleDiagnosticAnalyzer(DiagnosticId id, string title, string messageFormat, string description, string category,
											Helper helper = null, DiagnosticSeverity severity = DiagnosticSeverity.Error, bool isEnabled = true)
			: base(id, title, messageFormat, description, category, helper, severity, isEnabled)
		{ }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			SyntaxKind syntaxKind = GetSyntaxKind();

			if (string.IsNullOrEmpty(FullyQualifiedMetaDataName))
			{
				context.RegisterSyntaxNodeAction(StartAnalysis, syntaxKind);
			}
			else
			{
				context.RegisterCompilationStartAction(startContext =>
				{
					if (startContext.Compilation.GetTypeByMetadataName(FullyQualifiedMetaDataName) == null)
					{
						return;
					}

					startContext.RegisterSyntaxNodeAction(StartAnalysis, syntaxKind);
				});
			}

		}

		public void ReportDiagnostic(SyntaxNodeAnalysisContext context, Location location = null, params object[] messageArgs)
		{
			Diagnostic diagnostic = Diagnostic.Create(Rule, location, messageArgs);
			context.ReportDiagnostic(diagnostic);
		}

		private void StartAnalysis(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			Analyze(context, (T)context.Node);
		}

		protected abstract void Analyze(SyntaxNodeAnalysisContext context, T node);

		private SyntaxKind GetSyntaxKind()
		{
			return typeof(T).Name switch
			{
				nameof(CompilationUnitSyntax) => SyntaxKind.CompilationUnit,
				nameof(MethodDeclarationSyntax) => SyntaxKind.MethodDeclaration,
				nameof(PropertyDeclarationSyntax) => SyntaxKind.PropertyDeclaration,
				nameof(ClassDeclarationSyntax) => SyntaxKind.ClassDeclaration,
				nameof(NamespaceDeclarationSyntax) => SyntaxKind.NamespaceDeclaration,
				nameof(IdentifierNameSyntax) => SyntaxKind.IdentifierName,
				_ => SyntaxKind.None,
			};
		}
	}
}
