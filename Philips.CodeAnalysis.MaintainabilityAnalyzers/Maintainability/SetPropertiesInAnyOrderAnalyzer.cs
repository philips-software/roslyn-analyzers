// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
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
	public class SetPropertiesInAnyOrderAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Set properties in any order";
		private const string MessageFormat = @"Avoid getting other properties when setting property {0}.";
		private const string Description = @"Getting other properties in a setter makes this setter dependant on the order in which these properties are set.";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.SetPropertiesInAnyOrder),
			Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
			description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.GetAccessorDeclaration);
		}
		
		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			var getMethod = (AccessorDeclarationSyntax)context.Node;
			var prop = getMethod.Ancestors().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
			if (getMethod.Body == null || prop == null)
			{
				return;
			}

			var type = prop.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
			if (type == null)
			{
				return;
			}

			var propertiesInType = GetProperties(type);
			var otherProperties = propertiesInType.Except(new[] { prop.Identifier.Text });

			if (getMethod.Body.Statements.Any(s => ReferencesOtherProperties(s, otherProperties)))
			{
				var propertyName = prop.Identifier.Text;
				var loc = getMethod.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(Rule, loc, propertyName));
			}
		}

		private static IEnumerable<string> GetProperties(BaseTypeDeclarationSyntax type)
		{
			return type.DescendantNodes().OfType<PropertyDeclarationSyntax>().Select(prop => prop.Identifier.Text);
		}

		private static bool ReferencesOtherProperties(StatementSyntax statement, IEnumerable<string> otherProperties)
		{

			return statement.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
				.Any(name => otherProperties.Contains(name.Identifier.Text));
		}
	}
}
