// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Philips.CodeAnalysis.Common;

using static LanguageExt.Prelude;

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
		public override IEnumerable<Diagnostic> Analyze()
		{
			// Look for throw statements and check them.
			var hasOnlyCorrectThrow = Node.DescendantNodes()
				.OfType<ThrowStatementSyntax>().ForAll(IsCorrectThrow);
			if (hasOnlyCorrectThrow)
			{
				return Option<Diagnostic>.None;
			}
			Location location = Node.CatchKeyword.GetLocation();
			return Optional(PrepareDiagnostic(location));
		}

		// Throw should rethrow same exception, or include original exception
		// when creating new Exception.
		// Alternatively, also allow the HttpResponseException method using in ASP .NET Core.
		private bool IsCorrectThrow(ThrowStatementSyntax node)
		{
			var isOk = true;
			IEnumerable<ObjectCreationExpressionSyntax> newNodes = node.ChildNodes().OfType<ObjectCreationExpressionSyntax>();
			if (newNodes.Any())
			{
				foreach (ObjectCreationExpressionSyntax creation in newNodes)
				{
					// Constructor needs to have at least two arguments.
					isOk = creation.ArgumentList != null && creation.ArgumentList.Arguments.Count > 1;
					if (!isOk)
					{
						// The HttpResponseException has only a single argument.
						ITypeSymbol typeSymbol = Context.SemanticModel.GetTypeInfo(creation).Type;
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
