// © 2024 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidAssemblyGetEntryAssemblyAnalyzer : SingleDiagnosticAnalyzer<InvocationExpressionSyntax, AvoidAssemblyGetEntryAssemblySyntaxNodeAction>
	{
		private const string Title = @"Don't use Assembly.GetEntryAssembly()";
		private const string MessageFormat = @"Don't use Assembly.GetEntryAssembly(), use typeof().Assembly instead";
		private const string Description = @"During testing Assembly.GetEntryAssembly will point to the test runner, which is not expected.";
		public AvoidAssemblyGetEntryAssemblyAnalyzer()
			: base(DiagnosticId.AvoidAssemblyGetEntryAssembly, Title, MessageFormat, Description, Categories.RuntimeFailure, isEnabled: true)
		{ }
	}
	public class AvoidAssemblyGetEntryAssemblySyntaxNodeAction : SyntaxNodeAction<InvocationExpressionSyntax>
	{
		private const string AssemblyTypeName = "Assembly";
		private const string AssemblyNamespace = "System.Reflection";

		public override void Analyze()
		{
			if (!Node.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
			{
				return;
			}

			var memberAccessExpression = (MemberAccessExpressionSyntax)Node.Expression;
			NamespaceResolver resolver = Helper.ForNamespaces.GetUsingAliases(Node);
			var name = memberAccessExpression.Name.Identifier.Text;
			if (resolver.IsOfType(memberAccessExpression, AssemblyNamespace, AssemblyTypeName) && name == "GetEntryAssembly")
			{
				Location location = memberAccessExpression.Expression.GetLocation();
				ReportDiagnostic(location);
			}
		}
	}
}
