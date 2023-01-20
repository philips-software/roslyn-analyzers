// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
		private const string DocumentTitle = @"Document thrown exceptions";
		private const string DocumentMessageFormat = @"Document that this method can potentially throw an {0}.";
		private const string DocumentDescription = @"Be clear to your callers what exception can be thrown from your method by mentioning each of them in an <exception> element in the documentation of the method.";
		private const string InformationalTitle = @"Throw only informational exceptions";
		private const string InformationalMessageFormat = @"Specify context to the {0} exception, by using a constructor overload that sets the Message property.";
		private const string InformationalDescription = @"Specify context to a thrown exception, by using a constructor overload that sets the Message property.";
		private const string LocationsTitle = @"Avoid throwing exceptions from unexpected locations";
		private const string LocationsMessageFormat = @"Avoid throwing exceptions from {0}, as this location is unexpected.";
		private const string LocationsDescription = @"Avoid throwing exceptions from unexpected locations, like Finalizers, Dispose, Static Constructors, etc...";
		private const string Category = Categories.Documentation;

		private static readonly DiagnosticDescriptor DocumentRule = new(Helper.ToDiagnosticId(DiagnosticIds.DocumentThrownExceptions), DocumentTitle, DocumentMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: false, description: DocumentDescription);
		private static readonly DiagnosticDescriptor InformationalRule = new(Helper.ToDiagnosticId(DiagnosticIds.ThrowInformationalExceptions), InformationalTitle, InformationalMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: false, description: InformationalDescription);
		private static readonly DiagnosticDescriptor LocationsRule = new(Helper.ToDiagnosticId(DiagnosticIds.AvoidExceptionsFromUnexpectedLocations), LocationsTitle, LocationsMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: false, description: LocationsDescription);

		[SuppressMessage("", "IDE0090", Justification = "When type is removed the literal type cannot be deduced")]
		private static readonly Dictionary<string, string> specialMethods = new Dictionary<string, string>
		{
			{ ".ctor", "constructor" },
			{ ".cctor", "static constructor" },
			{ "Equals", "Equals method" },
			{ "GetHashCode", "GetHashCode method" },
			{ "Dispose", "Dispose method" },
			{ "ToString", "ToString method" }
		};

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DocumentRule, InformationalRule, LocationsRule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ThrowStatement);
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			var throwStatement = (ThrowStatementSyntax)context.Node;

			string thrownExceptionName = null;
			if(throwStatement.Expression is ObjectCreationExpressionSyntax exceptionCreation)
			{
				// Search of string arguments in the constructor invocation.
				thrownExceptionName = (exceptionCreation.Type as IdentifierNameSyntax)?.Identifier.Text;
				if(!HasStringArgument(context, exceptionCreation.ArgumentList))
				{
					var loc = exceptionCreation.GetLocation();
					Diagnostic diagnostic = Diagnostic.Create(InformationalRule, loc, thrownExceptionName);
					context.ReportDiagnostic(diagnostic);
				}
			}
			else
			{
				// Rethrowing an existing Exception instance.
				if(throwStatement.Expression is IdentifierNameSyntax localVar)
				{
					thrownExceptionName = context.SemanticModel.GetTypeInfo(localVar).Type?.Name;
				}
			}

			if (string.IsNullOrEmpty(thrownExceptionName))
			{
				return;
			}

			// Determine our parent.
			SyntaxNode methodDeclaration = throwStatement.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
			if (methodDeclaration == null)
			{
				methodDeclaration = throwStatement.Ancestors().OfType<BasePropertyDeclarationSyntax>().FirstOrDefault();
				if (methodDeclaration == null)
				{
					return;
				}
			}

			// Check if the Parent is an unexpected location.
			if (methodDeclaration is MethodDeclarationSyntax method)
			{
				if (specialMethods.TryGetValue(method.Identifier.Text, out string specialMethodKind))
				{
					var loc = method.Identifier.GetLocation();
					Diagnostic diagnostic = Diagnostic.Create(LocationsRule, loc, specialMethodKind);
					context.ReportDiagnostic(diagnostic);
				}
			}
			else if (methodDeclaration is OperatorDeclarationSyntax { OperatorToken.Text: "==" or "!=" } operatorDeclaration)
			{
				var loc = operatorDeclaration.OperatorKeyword.GetLocation();
				Diagnostic diagnostic = Diagnostic.Create(LocationsRule, loc, "equality comparison operator");
				context.ReportDiagnostic(diagnostic);
			}
			else if (methodDeclaration is ConversionOperatorDeclarationSyntax { ImplicitOrExplicitKeyword.Text: "implicit" } conversionDeclaration)
			{
				var loc = conversionDeclaration.OperatorKeyword.GetLocation();
				Diagnostic diagnostic = Diagnostic.Create(LocationsRule, loc, "implicit cast operator");
				context.ReportDiagnostic(diagnostic);
			}
			else if(methodDeclaration is ConstructorDeclarationSyntax constructorDeclaration)
			{
				bool? withinExceptionClass =
					(methodDeclaration.Parent as TypeDeclarationSyntax)?.Identifier.Text.EndsWith("Exception");
				if ((withinExceptionClass.HasValue && (bool)withinExceptionClass) || constructorDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
				{
					var loc = constructorDeclaration.Identifier.GetLocation();
					Diagnostic diagnostic = Diagnostic.Create(LocationsRule, loc, "implicit cast operator");
					context.ReportDiagnostic(diagnostic);
				}
			}
			else if(methodDeclaration is DestructorDeclarationSyntax destructorDeclaration)
			{
				var loc = destructorDeclaration.Identifier.GetLocation();
				Diagnostic diagnostic = Diagnostic.Create(LocationsRule, loc, "finalizer");
				context.ReportDiagnostic(diagnostic);
			}

			var mentionedExceptions = methodDeclaration.GetLeadingTrivia()
				.Select(i => i.GetStructure())
				.OfType<DocumentationCommentTriviaSyntax>()
				.SelectMany(n => n.ChildNodes().OfType<XmlElementSyntax>())
				.Where(IsExceptionElement)
				.Select(GetCrefAttributeValue);
			if (!mentionedExceptions.Contains(thrownExceptionName))
			{
				var loc = throwStatement.ThrowKeyword.GetLocation();
				Diagnostic diagnostic = Diagnostic.Create(DocumentRule, loc, thrownExceptionName);
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

		private static bool HasStringArgument(SyntaxNodeAnalysisContext context, ArgumentListSyntax attributeList)
		{
			const string stringTypeName = "String";
			return attributeList.Arguments.Any(a =>
			{
				SyntaxNode node = null;
				if (a.Expression is LiteralExpressionSyntax literal)
				{
					node = literal;
				}
				else if (a.Expression is IdentifierNameSyntax identifierName)
				{
					node = identifierName;
				} 
				else if(a.Expression is InvocationExpressionSyntax invocation)
				{
					node = invocation;
				}
				else
				{
					return false;
				}

				return context.SemanticModel.GetTypeInfo(node).Type?.Name == stringTypeName;
			});
		}
	}
}
