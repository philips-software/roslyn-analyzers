// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidThreadSleepAnalyzer : SingleDiagnosticAnalyzer<InvocationExpressionSyntax, AvoidThreadSleepSyntaxNodeAction>
	{
		private const string Title = @"Avoid Thread.Sleep";
		public const string MessageFormat = @"Methods may not have Thread.Sleep.";
		private const string Description = @"Methods may not have Thread.Sleep to prevent inaccurate timeout.";

		public AvoidThreadSleepAnalyzer()
			: base(DiagnosticId.AvoidThreadSleep, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: false)
		{ }
	}
	public class AvoidThreadSleepSyntaxNodeAction : SyntaxNodeAction<InvocationExpressionSyntax>
	{
		public override void Analyze()
		{
			if (Node.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
			{
				return;
			}

			var memberName = memberAccessExpression.Expression.ToString();
			var name = memberAccessExpression.Name.ToString();

			if (memberName == @"Thread" && name == @"Sleep")
			{
				TypeDeclarationSyntax typeDeclaration = Context.Node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
				if (typeDeclaration == null)
				{
					return;
				}

				SyntaxList<AttributeListSyntax> classAttributeList = typeDeclaration.AttributeLists;
				if (!Helper.ForAttributes.HasAttribute(classAttributeList, Context, MsTestFrameworkDefinitions.TestClassAttribute, out _) &&
					(Context.SemanticModel.GetSymbolInfo(memberAccessExpression).Symbol is IMethodSymbol memberSymbol) && memberSymbol.ToString().StartsWith("System.Threading.Thread"))
				{
					Location location = Node.GetLocation();
					ReportDiagnostic(location);
				}
			}
		}
	}
}
