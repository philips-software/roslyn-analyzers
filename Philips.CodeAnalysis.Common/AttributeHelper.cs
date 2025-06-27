// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	public class AttributeHelper
	{
		internal AttributeHelper()
		{
			// Hide the constructor
		}

		public bool HasAnyAttribute(SyntaxList<AttributeListSyntax> attributeLists, SyntaxNodeAnalysisContext context, params AttributeDefinition[] attributes)
		{
			return attributes.Any(x => HasAttribute(attributeLists, context, x));
		}

		public bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, SyntaxNodeAnalysisContext context, AttributeDefinition attribute)
		{
			return HasAttribute(attributeLists, context, attribute.Name, attribute.FullName, out _);
		}

		public bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, SyntaxNodeAnalysisContext context, string name, string fullName, out Location location)
		{
			return HasAttribute(attributeLists, context, name, fullName, out location, out _);
		}

		public bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, SyntaxNodeAnalysisContext context, AttributeDefinition attributeDefinition, out Location location, out AttributeArgumentSyntax argument)
		{
			return HasAttribute(attributeLists, context, attributeDefinition.Name, attributeDefinition.FullName, out location, out argument);
		}

		public bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, SyntaxNodeAnalysisContext context, string name, string fullName, out Location location, out AttributeArgumentSyntax argumentValue)
		{
			return HasAttribute(attributeLists, () => { return context.SemanticModel; }, name, fullName, out location, out argumentValue);
		}
		public bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, Func<SemanticModel> getSemanticModel, string name, string fullName, out Location location, out AttributeArgumentSyntax argumentValue)
		{
			location = null;
			argumentValue = default;

			foreach (AttributeListSyntax attributes in attributeLists)
			{
				if (HasAttribute(attributes, getSemanticModel, name, fullName, out location, out argumentValue))
				{
					return true;
				}
			}
			return false;
		}

		public bool HasAttribute(AttributeListSyntax attributes, SyntaxNodeAnalysisContext context, AttributeDefinition attributeDefinition, out Location location)
		{
			return HasAttribute(attributes, context, attributeDefinition.Name, attributeDefinition.FullName, out location);
		}

		public bool HasAttribute(AttributeListSyntax attributes, SyntaxNodeAnalysisContext context, string name, string fullName, out Location location)
		{
			location = null;
			foreach (AttributeSyntax attribute in attributes.Attributes)
			{
				if (attribute.Name.ToString().Contains(name) && context.SemanticModel.GetSymbolInfo(attribute).Symbol is IMethodSymbol memberSymbol && memberSymbol.ToString().StartsWith(fullName))
				{
					location = attribute.GetLocation();
					return true;
				}
			}
			return false;
		}

		public bool HasAttribute(AttributeListSyntax attributes, Func<SemanticModel> getSemanticModel, string name, string fullName, out Location location, out AttributeArgumentSyntax argument)
		{
			location = null;
			argument = default;
			foreach (AttributeSyntax attribute in attributes.Attributes)
			{
				if (IsAttribute(attribute, getSemanticModel, name, fullName, out location, out argument))
				{
					return true;
				}
			}

			return false;
		}

		public bool IsAttribute(AttributeSyntax attribute, Func<SemanticModel> getSemanticModel, string name, string fullName, out Location location, out AttributeArgumentSyntax argument)
		{
			location = null;
			argument = default;

			if (attribute.Name.ToString().Contains(name))
			{
				if (fullName == null)
				{
					// Skip the full namespace check.
					return true;
				}
				SymbolInfo symbolInfo = getSemanticModel().GetSymbolInfo(attribute);
				if (symbolInfo.Symbol is IMethodSymbol memberSymbol && memberSymbol.ToString().StartsWith(fullName))
				{
					location = attribute.GetLocation();
					argument = attribute.ArgumentList?.Arguments.FirstOrDefault();
					return true;
				}
			}

			return false;
		}
		public bool IsAttribute(AttributeSyntax attribute, SyntaxNodeAnalysisContext context, AttributeDefinition attributeDefinition, out Location location, out AttributeArgumentSyntax argument)
		{
			return IsAttribute(attribute, () => { return context.SemanticModel; }, attributeDefinition.Name, attributeDefinition.FullName, out location, out argument);
		}

		public bool IsDataRowAttribute(AttributeSyntax attribute, SyntaxNodeAnalysisContext context)
		{
			return IsAttribute(attribute, context, MsTestFrameworkDefinitions.DataRowAttribute, out _, out _);
		}
	}
}

