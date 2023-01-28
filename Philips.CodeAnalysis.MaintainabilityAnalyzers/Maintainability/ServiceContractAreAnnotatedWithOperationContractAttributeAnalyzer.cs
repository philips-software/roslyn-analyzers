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
	public class ServiceContractAreAnnotatedWithOperationContractAttributeAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Interfaces marked with [ServiceContract] must have methods marked with [OperationContract]";
		private const string MessageFormat = @"Method '{0}' is not marked [OperationContract]";
		private const string Description = @"Attribute method with [OperationContract]";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.ServiceContractsMustHaveOperationContractAttributes), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: false, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		private readonly AttributeHelper _attributeHelper;

		public ServiceContractAreAnnotatedWithOperationContractAttributeAnalyzer()
			: this(new AttributeHelper())
		{ }

		public ServiceContractAreAnnotatedWithOperationContractAttributeAnalyzer(AttributeHelper attributeHelper)
		{
			_attributeHelper = attributeHelper;
		}
		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(startContext =>
			{
				if (startContext.Compilation.GetTypeByMetadataName("System.ServiceModel.ServiceContractAttribute") == null)
				{
					return;
				}

				startContext.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InterfaceDeclaration);
			});
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			InterfaceDeclarationSyntax interfaceDeclaration = (InterfaceDeclarationSyntax)context.Node;

			if (!_attributeHelper.HasAttribute(interfaceDeclaration.AttributeLists, context, "ServiceContract", "System.ServiceModel.ServiceContractAttribute", out _))
			{
				return;
			}

			foreach (MethodDeclarationSyntax method in interfaceDeclaration.Members.OfType<MethodDeclarationSyntax>())
			{
				if (!_attributeHelper.HasAttribute(method.AttributeLists, context, "OperationContract", "System.ServiceModel.OperationContractAttribute", out _))
				{
					context.ReportDiagnostic(Diagnostic.Create(Rule, method.Identifier.GetLocation(), method.Identifier));
				}
			}
		}
	}
}
