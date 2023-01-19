// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation
{
	/// <summary>
	/// Analyzer that checks if the text of the XML code documentation contains a reference to each exception being thrown inside the method or property.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class DocumentThrownExceptionsAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Document thrown exceptions";
		private const string MessageFormat = @"Document that this method can potentially throw an {0}.";
		private const string Description = @"Be clear to your callers what exception can be thrown from your method by mentioning each of them in an <exception> element in the documentation of the method.";
		private const string Category = Categories.Documentation;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.DocumentThrownExceptions), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: false, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ThrowStatement);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			var throwStatement = (ThrowStatementSyntax)context.Node;
			if (throwStatement.Expression is not ObjectCreationExpressionSyntax exceptionType)
			{
				return;
			}

			var thrownExceptionName = (exceptionType.Type as IdentifierNameSyntax)?.Identifier.Text;
			if (string.IsNullOrEmpty(thrownExceptionName))
			{
				return;
			}

			var methodDeclaration = throwStatement.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
			if (methodDeclaration == null)
			{
				return;
			}

			var mentionedExceptions = methodDeclaration.GetLeadingTrivia()
				.Select(i => i.GetStructure())
				.OfType<DocumentationCommentTriviaSyntax>()
				.SelectMany(n => n.ChildNodes().OfType<XmlElementSyntax>())
				.Where(IsExceptionElement)
				.Select(GetCrefAttributeValue);
			if (!mentionedExceptions.Contains(thrownExceptionName))
			{
				Diagnostic diagnostic = Diagnostic.Create(Rule, exceptionType.GetLocation(), thrownExceptionName);
				context.ReportDiagnostic(diagnostic);
			}
		}

		private static bool IsExceptionElement(XmlElementSyntax element)
		{
			return element.StartTag.Name.LocalName.Text == "exception";
		}

		private static string GetCrefAttributeValue(XmlElementSyntax element)
		{
			return element.StartTag.Attributes.OfType<XmlCrefAttributeSyntax>().Select(cref => cref.Cref.ToString()).FirstOrDefault();
		}

	}
}
