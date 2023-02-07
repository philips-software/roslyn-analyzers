// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ServiceContractHasOperationContractAnalyzer : SingleDiagnosticAnalyzer<InterfaceDeclarationSyntax, ServiceContractHasOperationContractSyntaxNodeAction>
	{
		private const string Title = @"Interfaces marked with [ServiceContract] must have methods marked with [OperationContract]";
		private const string MessageFormat = @"Method '{0}' is not marked [OperationContract]";
		private const string Description = @"Attribute method with [OperationContract]";

		public ServiceContractHasOperationContractAnalyzer()
			: base(DiagnosticId.ServiceContractsMustHaveOperationContractAttributes, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: false)
		{ }
	}

	public class ServiceContractHasOperationContractSyntaxNodeAction : SyntaxNodeAction<InterfaceDeclarationSyntax>
	{
		private readonly AttributeHelper _attributeHelper = new();

		public override void Analyze()
		{
			if (!_attributeHelper.HasAttribute(Node.AttributeLists, Context, "ServiceContract", null, out _))
			{
				return;
			}

			foreach (MethodDeclarationSyntax method in Node.Members.OfType<MethodDeclarationSyntax>())
			{
				if (!_attributeHelper.HasAttribute(method.AttributeLists, Context, "OperationContract", null, out _))
				{
					ReportDiagnostic(method.Identifier.GetLocation(), method.Identifier);
				}
			}
		}
	}
}
