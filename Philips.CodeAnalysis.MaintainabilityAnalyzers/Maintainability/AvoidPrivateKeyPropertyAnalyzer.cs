﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidPrivateKeyPropertyAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Do not use PrivateKey property on X509Certificate2 class";
		private const string MessageFormat = @"Do not use PrivateKey property on X509Certificate2 class to access the Private Key. Use a Getter instead. Eg: GetRSAPrivateKey(), GetDSAPrivateKey(), GetECDsaPrivateKey().";
		private const string Description = @"Do not use PrivateKey property on X509Certificate2 class as it might cause the Application to crash. Use a Getter instead. Eg: GetRSAPrivateKey(), GetDSAPrivateKey(), GetECDsaPrivateKey()";
		private const string Category = Categories.Maintainability;
		private const string helpUri = @"https://www.pkisolutions.com/accessing-and-using-certificate-private-keys-in-net-framework-net-core/";

		private const string PrivateKeyProperty = @"PrivateKey";
		private const string ObjectTypeFullName = "System.Security.Cryptography.X509Certificates.X509Certificate2";
		private const string ObjectType = @"X509Certificate2";

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			MemberAccessExpressionSyntax memberAccessExpressionSyntax = (MemberAccessExpressionSyntax)context.Node;
			if (memberAccessExpressionSyntax == null)
			{
				return;
			}

			if (!memberAccessExpressionSyntax.Name.ToString().Equals(PrivateKeyProperty, System.StringComparison.Ordinal))
			{
				return;
			}

			ITypeSymbol typeSymbol = context.SemanticModel.GetTypeInfo(memberAccessExpressionSyntax.Expression).Type;

			if (typeSymbol != null && typeSymbol.Name.Equals(ObjectType, System.StringComparison.Ordinal))
			{
				var location = memberAccessExpressionSyntax.GetLocation();
				Diagnostic diagnostic = Diagnostic.Create(Rule, location);
				context.ReportDiagnostic(diagnostic);
			}

		}

		public static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticId.AvoidPrivateKeyProperty), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description, helpLinkUri: helpUri);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction(ctx =>
			{
				if (ctx.Compilation.GetTypeByMetadataName(ObjectTypeFullName) != null)
				{
					ctx.RegisterSyntaxNodeAction(Analyze, SyntaxKind.SimpleMemberAccessExpression);
				}
			});
		}
	}
}
