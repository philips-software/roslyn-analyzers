﻿// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidTaskResultAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Avoid Task.Result";
		public const string MessageFormat = @"Methods may not call Result on a Task.";
		private const string Description = @"To avoid deadlocks, methods may not call Result on a Task.";
		private const string Category = Categories.Maintainability;
		private const string HelpUri = @"https://docs.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming#async-all-the-way";

		private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.AvoidTaskResult), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description, helpLinkUri: HelpUri);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		private const string ContainingNamespace = @"Tasks";
		private const string ContainingType = @"Task";
		public string Identifier = @"Result";

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction(startContext =>
			{
				if (startContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task") == null)
				{
					return;
				}
				startContext.RegisterOperationAction(Analyze, OperationKind.PropertyReference);
			});
		}

		private void Analyze(OperationAnalysisContext context)
		{
			IPropertyReferenceOperation propertyReference = (IPropertyReferenceOperation)context.Operation;

			MemberAccessExpressionSyntax propertySyntax = propertyReference.Syntax as MemberAccessExpressionSyntax;

			if (propertySyntax == null)
			{
				return;
			}

			if (propertySyntax.Name.Identifier.ValueText == Identifier)
			{
				IPropertySymbol propertySymbol = propertyReference.Property;
				if (propertySymbol.ContainingNamespace.Name == ContainingNamespace &&
					propertySymbol.ContainingType.Name.Contains(ContainingType))
				{
					Diagnostic diagnostic = Diagnostic.Create(Rule, propertySyntax.Name.GetLocation());
					context.ReportDiagnostic(diagnostic);
				}
			}
		}
	}
}
