// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using LanguageExt.SomeHelp;
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
	public class AvoidThrowingUnexpectedExceptionsAnalyzer : SingleDiagnosticAnalyzer<ThrowStatementSyntax, AvoidThrowingUnexpectedExceptionsSyntaxNodeAction>
	{
		private const string Title = @"Avoid throwing exceptions from unexpected locations";
		private const string MessageFormat = @"Avoid throwing exceptions from {0}, as this location is unexpected.";
		private const string Description = @"Avoid throwing exceptions from unexpected locations, like Finalizers, Dispose, Static Constructors, etc...";
		public AvoidThrowingUnexpectedExceptionsAnalyzer()
			: base(DiagnosticId.AvoidExceptionsFromUnexpectedLocations, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: false)
		{ }
	}
	public class AvoidThrowingUnexpectedExceptionsSyntaxNodeAction : SyntaxNodeAction<ThrowStatementSyntax>
	{
		private static readonly Dictionary<string, string> SpecialMethods = new()
		{
			{ "Equals", "Equals method" },
			{ "GetHashCode", "GetHashCode method" },
			{ StringConstants.Dispose, "Dispose method" },
			{ StringConstants.ToStringMethodName, "ToString method" }
		};

		public override IEnumerable<Diagnostic> Analyze()
		{
			// Determine our parent.
			SyntaxNode methodDeclaration = Node.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
			if (methodDeclaration == null)
			{
				methodDeclaration = Node.Ancestors().OfType<BasePropertyDeclarationSyntax>().FirstOrDefault();
				if (methodDeclaration == null)
				{
					return Option<Diagnostic>.None;
				}
			}

			return AnalyzeMethod(methodDeclaration)
				.Concat(AnalyzeEqualityOperator(methodDeclaration))
				.Concat(AnalyzeConversionOperator(methodDeclaration))
				.Concat(AnalyzeConstructor(methodDeclaration))
				.Concat(AnalyzeDestructor(methodDeclaration));
		}

		private IEnumerable<Diagnostic> AnalyzeMethod(SyntaxNode node)
		{
			// Check overriden methods of Object.
			if (node is MethodDeclarationSyntax method && SpecialMethods.TryGetValue(method.Identifier.Text, out var specialMethodKind))
			{
				Location loc = Node.ThrowKeyword.GetLocation();
				return PrepareDiagnostic(loc, specialMethodKind).ToSome();
			}
			return Option<Diagnostic>.None;
		}

		private IEnumerable<Diagnostic> AnalyzeConstructor(SyntaxNode node)
		{
			var diags = new List<Diagnostic>();
			if (node is ConstructorDeclarationSyntax constructorDeclaration)
			{
				// Check constructors of an Exception.
				var withinExceptionClass =
					(node.Parent as TypeDeclarationSyntax)?.Identifier.Text.EndsWith(StringConstants.Exception);
				if (withinExceptionClass.HasValue && (bool)withinExceptionClass)
				{
					Location loc = Node.ThrowKeyword.GetLocation();
					diags.Add(PrepareDiagnostic(loc, "constructor of an Exception derived type"));
				}
				if (constructorDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
				{
					Location loc = Node.ThrowKeyword.GetLocation();
					diags.Add(PrepareDiagnostic(loc, "static constructor"));
				}
			}
			return diags;
		}

		private IEnumerable<Diagnostic> AnalyzeDestructor(SyntaxNode node)
		{
			if (node is DestructorDeclarationSyntax)
			{
				// Check finalizers.
				Location loc = Node.ThrowKeyword.GetLocation();
				return PrepareDiagnostic(loc, "finalizer").ToSome();
			}
			return Option<Diagnostic>.None;
		}

		private IEnumerable<Diagnostic> AnalyzeEqualityOperator(SyntaxNode node)
		{
			if (node is OperatorDeclarationSyntax { OperatorToken.Text: "==" or "!=" })
			{
				// Check == and != operators.
				Location loc = Node.ThrowKeyword.GetLocation();
				return PrepareDiagnostic(loc, "equality comparison operator").ToSome();
			}
			return Option<Diagnostic>.None;
		}

		private IEnumerable<Diagnostic> AnalyzeConversionOperator(SyntaxNode methodDeclaration)
		{
			if (methodDeclaration is ConversionOperatorDeclarationSyntax conversion && conversion.ImplicitOrExplicitKeyword.Text == "implicit")
			{
				// Check implicit cast operators.
				Location loc = Node.ThrowKeyword.GetLocation();
				return PrepareDiagnostic(loc, "implicit cast operator").ToSome();
			}
			return Option<Diagnostic>.None;
		}
	}
}
