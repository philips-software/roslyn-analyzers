// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidPrivateKeyPropertyAnalyzer : SingleDiagnosticAnalyzer<MemberAccessExpressionSyntax, AvoidPrivateKeyPropertySyntaxNodeAction>
	{
		private const string Title = @"Do not use PrivateKey property on X509Certificate2 class";
		private const string MessageFormat = @"Do not use PrivateKey property on X509Certificate2 class to access the Private Key. Use a Getter instead. Eg: GetRSAPrivateKey(), GetDSAPrivateKey(), GetECDsaPrivateKey().";
		private const string Description = @"Do not use PrivateKey property on X509Certificate2 class as it might cause the Application to crash. Use a Getter instead. Eg: GetRSAPrivateKey(), GetDSAPrivateKey(), GetECDsaPrivateKey()";
		private const string HelpUri = @"https://www.pkisolutions.com/accessing-and-using-certificate-private-keys-in-net-framework-net-core/";

		public AvoidPrivateKeyPropertyAnalyzer()
			: base(DiagnosticId.AvoidPrivateKeyProperty, Title, MessageFormat, Description, Categories.Maintainability, helpUri: HelpUri )
		{
			FullyQualifiedMetaDataName = "System.Security.Cryptography.X509Certificates.X509Certificate2";
		}
	}

	public class AvoidPrivateKeyPropertySyntaxNodeAction : SyntaxNodeAction<MemberAccessExpressionSyntax>
	{
		private const string PrivateKeyProperty = @"PrivateKey";
		private const string ObjectType = @"X509Certificate2";

		public override void Analyze()
		{
			if (!Node.Name.ToString().Equals(PrivateKeyProperty, System.StringComparison.Ordinal))
			{
				return;
			}

			ITypeSymbol typeSymbol = Context.SemanticModel.GetTypeInfo(Node.Expression).Type;

			if (typeSymbol != null && typeSymbol.Name.Equals(ObjectType, System.StringComparison.Ordinal))
			{
				var location = Node.GetLocation();
				ReportDiagnostic(location);
			}
		}
	}
}
