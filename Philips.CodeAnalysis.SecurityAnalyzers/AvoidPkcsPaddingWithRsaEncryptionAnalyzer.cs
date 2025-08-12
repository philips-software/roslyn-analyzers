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
			if (IsRsaEncryptionPaddingPkcs1(memberAccess))
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

		private static bool IsRsaEncryptionPaddingPkcs1(MemberAccessExpressionSyntax memberAccess)
		{
			// Check if it's accessing Pkcs1 member
			if (memberAccess.Name.Identifier.ValueText != "Pkcs1")
			{
				return false;
			}

			// Fast path: check for fully qualified usage
			var expressionText = memberAccess.Expression.ToString();
			if (expressionText == "System.Security.Cryptography.RSAEncryptionPadding")
			{
				return true;
			}

			// Use NamespaceResolver to check if the expression is RSAEncryptionPadding (for using statements)
			var namespaceResolver = new NamespaceResolver(memberAccess);
			return namespaceResolver.IsOfType(memberAccess, "System.Security.Cryptography", "RSAEncryptionPadding");
		}
	}
}