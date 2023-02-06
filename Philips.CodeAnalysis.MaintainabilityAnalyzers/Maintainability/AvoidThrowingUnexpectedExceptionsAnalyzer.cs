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

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	/// <summary>
	/// Analyzer that checks exception are not thrown in locations in the code where they are not expected or cause issues.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidThrowingUnexpectedExceptionsAnalyzer : DiagnosticAnalyzer
	{
		private const string LocationsTitle = @"Avoid throwing exceptions from unexpected locations";
		private const string LocationsMessageFormat = @"Avoid throwing exceptions from {0}, as this location is unexpected.";
		private const string LocationsDescription = @"Avoid throwing exceptions from unexpected locations, like Finalizers, Dispose, Static Constructors, etc...";
		private const string Category = Categories.Documentation;

		private static readonly DiagnosticDescriptor LocationsRule = new(Helper.ToDiagnosticId(DiagnosticId.AvoidExceptionsFromUnexpectedLocations), LocationsTitle, LocationsMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: LocationsDescription);

		private static readonly Dictionary<string, string> specialMethods = new()
		{
			{ "Equals", "Equals method" },
			{ "GetHashCode", "GetHashCode method" },
			{ "Dispose", "Dispose method" },
			{ "ToString", "ToString method" }
		};

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(LocationsRule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ThrowStatement);
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			var throwStatement = (ThrowStatementSyntax)context.Node;

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
				// Check overriden methods of Object.
				if (specialMethods.TryGetValue(method.Identifier.Text, out string specialMethodKind))
				{
					var loc = throwStatement.ThrowKeyword.GetLocation();
					Diagnostic diagnostic = Diagnostic.Create(LocationsRule, loc, specialMethodKind);
					context.ReportDiagnostic(diagnostic);
				}
			}
			else if (methodDeclaration is OperatorDeclarationSyntax { OperatorToken.Text: "==" or "!=" })
			{
				// Check == and != operators.
				var loc = throwStatement.ThrowKeyword.GetLocation();
				Diagnostic diagnostic = Diagnostic.Create(LocationsRule, loc, "equality comparison operator");
				context.ReportDiagnostic(diagnostic);
			}
			else if (methodDeclaration is ConversionOperatorDeclarationSyntax { ImplicitOrExplicitKeyword.Text: "implicit" })
			{
				// Check implicit cast operators.
				var loc = throwStatement.ThrowKeyword.GetLocation();
				Diagnostic diagnostic = Diagnostic.Create(LocationsRule, loc, "implicit cast operator");
				context.ReportDiagnostic(diagnostic);
			}
			else if (methodDeclaration is ConstructorDeclarationSyntax constructorDeclaration)
			{
				// Check constructors of an Exception.
				bool? withinExceptionClass =
					(methodDeclaration.Parent as TypeDeclarationSyntax)?.Identifier.Text.EndsWith("Exception");
				if ((withinExceptionClass.HasValue && (bool)withinExceptionClass) || constructorDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
				{
					var loc = throwStatement.ThrowKeyword.GetLocation();
					Diagnostic diagnostic = Diagnostic.Create(LocationsRule, loc, "constructor of an Exception derived type");
					context.ReportDiagnostic(diagnostic);
				}
			}
			else if (methodDeclaration is DestructorDeclarationSyntax)
			{
				// Check finalizers.
				var loc = throwStatement.ThrowKeyword.GetLocation();
				Diagnostic diagnostic = Diagnostic.Create(LocationsRule, loc, "finalizer");
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
