// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.SecurityAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidPkcsPaddingWithRsaEncryptionAnalyzer : DiagnosticAnalyzerBase
	{
		private const string Title = @"Avoid PKCS#1 v1.5 padding with RSA encryption";
		public const string MessageFormat = @"RSA should use OAEP padding instead of PKCS#1 v1.5 padding for better security";
		private const string Description = @"PKCS#1 v1.5 padding is vulnerable to padding oracle attacks. Use OAEP padding (RSAEncryptionPadding.OaepSHA1, RSAEncryptionPadding.OaepSHA256, etc.) instead for better security.";
		private const string Category = Categories.Security;

		public static readonly DiagnosticDescriptor Rule = new(
			DiagnosticId.AvoidPkcsPaddingWithRsaEncryption.ToId(),
			Title, MessageFormat, Category,
			DiagnosticSeverity.Error, isEnabledByDefault: false, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			if (Helper.ForTests.IsInTestClass(context))
			{
				return;
			}

			var memberAccess = (MemberAccessExpressionSyntax)context.Node;

			// Check for RSAEncryptionPadding.Pkcs1
			if (IsRsaEncryptionPaddingPkcs1(memberAccess, context.SemanticModel))
			{
				Location location = memberAccess.GetLocation();
				var diagnostic = Diagnostic.Create(Rule, location);
				context.ReportDiagnostic(diagnostic);
			}
		}

		private void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
		{
			Analyze(context);
		}

		private static bool IsRsaEncryptionPaddingPkcs1(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel)
		{
			// Early bailout - check string content first
			if (!memberAccess.ToString().Contains("Pkcs1"))
			{
				return false;
			}

			// Check if it's accessing Pkcs1 member
			if (memberAccess.Name.Identifier.ValueText != "Pkcs1")
			{
				return false;
			}

			// Check if the expression is RSAEncryptionPadding
			SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(memberAccess.Expression);
			if (symbolInfo.Symbol is INamedTypeSymbol typeSymbol)
			{
				return typeSymbol.ToString() == "System.Security.Cryptography.RSAEncryptionPadding";
			}

			return false;
		}
	}
}