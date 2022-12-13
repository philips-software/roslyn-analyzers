﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidThreadSleepAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Avoid Thread.Sleep";
		public const string MessageFormat = @"Methods may not have Thread.Sleep.";
		private const string Description = @"Methods may not have Thread.Sleep to prevent inaccurate timeout.";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.AvoidThreadSleep), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: false, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();

			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			InvocationExpressionSyntax invocationExpression = (InvocationExpressionSyntax)context.Node;

			if (invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
			{
				return;
			}

			string memberName = memberAccessExpression.Expression.ToString();
			string name = memberAccessExpression.Name.ToString();


			if (memberName == @"Thread" && name == @"Sleep")
			{
				ClassDeclarationSyntax classDeclaration = (ClassDeclarationSyntax)context.Node.Parent.Parent.Parent.Parent;
				SyntaxList<AttributeListSyntax> classAttributeList = classDeclaration.AttributeLists;
				if (Helper.HasAttribute(classAttributeList, context, MsTestFrameworkDefinitions.TestClassAttribute, out _))
				{
					if ((context.SemanticModel.GetSymbolInfo(memberAccessExpression).Symbol is IMethodSymbol memberSymbol) && memberSymbol.ToString().StartsWith("System.Threading.Thread"))
					{
						Diagnostic diagnostic = Diagnostic.Create(Rule, invocationExpression.GetLocation());
						context.ReportDiagnostic(diagnostic);
					}
				}
			}
		}
	}
}
