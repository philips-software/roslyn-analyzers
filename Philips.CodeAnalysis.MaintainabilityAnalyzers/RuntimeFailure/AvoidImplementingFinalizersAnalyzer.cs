// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidImplementingFinalizersAnalyzer : SingleDiagnosticAnalyzer<DestructorDeclarationSyntax, AvoidImplementingFinalizersSyntaxNodeAction>
	{
		private const string Title = @"Avoid implementing a finalizer";
		private const string MessageFormat = @"Avoid implement a finalizer, use Dispose instead.";
		private const string Description = @"Avoid implement a finalizer, use Dispose instead. If the class has unmanaged fields, finalizers are allowed if they only call Dispose.";

		public AvoidImplementingFinalizersAnalyzer()
			: base(DiagnosticId.AvoidImplementingFinalizers, Title, MessageFormat, Description, Categories.RuntimeFailure)
		{ }
	}

	public class AvoidImplementingFinalizersSyntaxNodeAction : SyntaxNodeAction<DestructorDeclarationSyntax>
	{
		public override void Analyze()
		{
			var body = Node.Body;
			var children = body != null ? body.ChildNodes() : Array.Empty<SyntaxNode>();
			if (children.Any() && children.All(IsDisposeCall))
			{
				return;
			}
			var loc = Node.GetLocation();
			ReportDiagnostic(loc);
		}

		private static bool IsDisposeCall(SyntaxNode node)
		{
			if (node is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax invocation })
			{
				return invocation.Expression.ToString() == "Dispose";
			}

			return false;
		}
	}
}
