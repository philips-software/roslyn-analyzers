﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public abstract class SingleDiagnosticAnalyzer<T, U> : SingleDiagnosticAnalyzer where T : SyntaxNode where U : SyntaxNodeAction<T>, new()
	{
		protected string FullyQualifiedMetaDataName { get; set; }

		protected SingleDiagnosticAnalyzer(DiagnosticId id, string title, string messageFormat, string description, string category,
											DiagnosticSeverity severity = DiagnosticSeverity.Error, bool isEnabled = true)
			: base(id, title, messageFormat, description, category, severity, isEnabled)
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

		private void StartAnalysis(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			U syntaxNodeAction = new()
			{
				Context = context,
				Node = (T)context.Node,
				Rule = Rule,
			};
			syntaxNodeAction.Analyze();
		}

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
				nameof(TupleTypeSyntax) => SyntaxKind.TupleType,
				_ => SyntaxKind.None,
			};
		}
	}
}