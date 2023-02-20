// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	/// <summary>
	/// Include the original exception when rethrowing.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ThrowInnerExceptionAnalyzer : SingleDiagnosticAnalyzer<CatchClauseSyntax, ThrowInnerExceptionSyntaxNodeAction>
	{
		private const string Title = "Use inner exceptions for unhandled exceptions";
		private const string MessageFormat = "Rethrown exception should include caught exception.";
		private const string Description = Title;

		public ThrowInnerExceptionAnalyzer()
			: base(DiagnosticId.ThrowInnerException, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: false)
		{ }
	}

	public class ThrowInnerExceptionSyntaxNodeAction : SyntaxNodeAction<CatchClauseSyntax>
	{
		public override void Analyze()
		{
			// Look for throw statements and check them.
			var hasBadThrowNodes = Node.DescendantNodes()
				.OfType<ThrowStatementSyntax>().Any(node => !IsCorrectThrow(node));
			if (hasBadThrowNodes)
			{
				var location = Node.CatchKeyword.GetLocation();
				ReportDiagnostic(location);
			}
		}

		// Throw should rethrow same exception, or include original exception
		// when creating new Exception.
		// Alternatively, also allow the HttpResponseException method using in ASP .NET Core.
		private bool IsCorrectThrow(ThrowStatementSyntax node)
		{
			bool isOk = true;
			var newNodes = node.ChildNodes().OfType<ObjectCreationExpressionSyntax>();
			if (newNodes.Any())
			{
				foreach (var creation in newNodes)
				{
					// Constructor needs to have at least two arguments.
					isOk = creation.ArgumentList != null && creation.ArgumentList.Arguments.Count > 1;
					if (!isOk)
					{
						// The HttpResponseException has only a single argument.
						var typeSymbol = Context.SemanticModel.GetTypeInfo(creation).Type;
						if (typeSymbol.Name == "HttpResponseException")
						{
							isOk = true;
							break;
						}
					}
				}
			}
			return isOk;
		}
	}
}
