// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class CastCompleteObjectAnalyzer : SingleDiagnosticAnalyzer<ConversionOperatorDeclarationSyntax, CastCompleteObjectSyntaxNodeAction>
	{
		private const string Title = @"Cast the complete object";
		private const string MessageFormat = @"This casts down to one of the field types, but not all of them. Consider to move this into an AsType() or ToType() method instead.";
		private const string Description = @"A cast should include all information from the previous type. By casting to a type of one of the fields, the cast is losing information. Use an AsType() or ToType() method instead.";

		public CastCompleteObjectAnalyzer()
			: base(DiagnosticId.CastCompleteObject, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: false)
		{ }
	}

	public class CastCompleteObjectSyntaxNodeAction : SyntaxNodeAction<ConversionOperatorDeclarationSyntax>
	{
		public override void Analyze()
		{
			TypeSyntax container = Node.ParameterList.Parameters.FirstOrDefault()?.Type;
			if (
				container == null ||
				Context.SemanticModel.GetSymbolInfo(Node.Type).Symbol is not INamedTypeSymbol convertTo ||
				Context.SemanticModel.GetSymbolInfo(container).Symbol is not INamedTypeSymbol containingType)
			{
				return;
			}
			System.Collections.Generic.IEnumerable<IFieldSymbol> itsFields = containingType.GetMembers().OfType<IFieldSymbol>();

			if (itsFields is not null && itsFields.Count() > 1 && itsFields.Any(f => f.Type.Name == convertTo.Name))
			{
				Location loc = Node.Type.GetLocation();
				ReportDiagnostic(loc);
			}
		}
	}
}
