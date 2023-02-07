﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class AvoidThrowingUnexpectedExceptionsAnalyzer : SingleDiagnosticAnalyzer<ThrowStatementSyntax, AvoidThrowingUnexpectedExceptionsSyntaxNodeAction>
	{
		private const string Title = @"Avoid throwing exceptions from unexpected locations";
		private const string MessageFormat = @"Avoid throwing exceptions from {0}, as this location is unexpected.";
		private const string Description = @"Avoid throwing exceptions from unexpected locations, like Finalizers, Dispose, Static Constructors, etc...";
		public AvoidThrowingUnexpectedExceptionsAnalyzer()
			: base(DiagnosticId.AvoidExceptionsFromUnexpectedLocations, Title, MessageFormat, Description, Categories.Maintainability)
		{ }
	}
	public class AvoidThrowingUnexpectedExceptionsSyntaxNodeAction : SyntaxNodeAction<ThrowStatementSyntax>
	{
		private static readonly Dictionary<string, string> specialMethods = new()
		{
			{ "Equals", "Equals method" },
			{ "GetHashCode", "GetHashCode method" },
			{ "Dispose", "Dispose method" },
			{ StringConstants.ToStringMethodName, "ToString method" }
		};

		public override void Analyze()
		{
			// Determine our parent.
			SyntaxNode methodDeclaration = Node.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
			if (methodDeclaration == null)
			{
				methodDeclaration = Node.Ancestors().OfType<BasePropertyDeclarationSyntax>().FirstOrDefault();
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
					var loc = Node.ThrowKeyword.GetLocation();
					ReportDiagnostic(loc, specialMethodKind);
				}
			}
			else if (methodDeclaration is OperatorDeclarationSyntax { OperatorToken.Text: "==" or "!=" })
			{
				// Check == and != operators.
				var loc = Node.ThrowKeyword.GetLocation();
				ReportDiagnostic(loc, "equality comparison operator");
			}
			else if (methodDeclaration is ConversionOperatorDeclarationSyntax conversion && conversion.ImplicitOrExplicitKeyword.Text == "implicit")
			{
				// Check implicit cast operators.
				var loc = Node.ThrowKeyword.GetLocation();
				ReportDiagnostic(loc, "implicit cast operator");
			}
			else if (methodDeclaration is ConstructorDeclarationSyntax constructorDeclaration)
			{
				// Check constructors of an Exception.
				bool? withinExceptionClass =
					(methodDeclaration.Parent as TypeDeclarationSyntax)?.Identifier.Text.EndsWith("Exception");
				if ((withinExceptionClass.HasValue && (bool)withinExceptionClass) || constructorDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
				{
					var loc = Node.ThrowKeyword.GetLocation();
					ReportDiagnostic(loc, "constructor of an Exception derived type");
				}
			}
			else if (methodDeclaration is DestructorDeclarationSyntax)
			{
				// Check finalizers.
				var loc = Node.ThrowKeyword.GetLocation();
				ReportDiagnostic(loc, "finalizer");
			}
		}
	}
}
