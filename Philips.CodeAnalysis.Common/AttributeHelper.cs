// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
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

		/// <summary>
		/// Checks if any attribute of the specified types appears after any attribute of the other specified types.
		/// </summary>
		/// <param name="attributeLists">The attribute lists to examine</param>
		/// <param name="context">The syntax node analysis context</param>
		/// <param name="attributesToFind">The attributes to look for that should appear first</param>
		/// <param name="attributesToCheckAfter">The attributes that should not appear before attributesToFind</param>
		/// <returns>True if any attributesToFind appears after any attributesToCheckAfter</returns>
		public bool HasAttributeAfterOther(SyntaxList<AttributeListSyntax> attributeLists, SyntaxNodeAnalysisContext context, INamedTypeSymbol[] attributesToFind, INamedTypeSymbol[] attributesToCheckAfter)
		{
			(int listIndex, int attrIndex)? firstPosition = null;

			for (var listIndex = 0; listIndex < attributeLists.Count; listIndex++)
			{
				AttributeListSyntax attributeList = attributeLists[listIndex];
				for (var attrIndex = 0; attrIndex < attributeList.Attributes.Count; attrIndex++)
				{
					AttributeSyntax attribute = attributeList.Attributes[attrIndex];
					INamedTypeSymbol attributeSymbol = context.SemanticModel.GetSymbolInfo(attribute).Symbol?.ContainingType;

					if (attributeSymbol != null)
					{
						(int listIndex, int attrIndex) currentPosition = (listIndex, attrIndex);

						if (attributesToCheckAfter.Any(attr => AttributeMatches(attributeSymbol, attr)))
						{
							firstPosition ??= currentPosition;
						}
						else if (attributesToFind.Any(attr => AttributeMatches(attributeSymbol, attr)) &&
								firstPosition != null && currentPosition.CompareTo(firstPosition.Value) > 0)
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Categorizes attributes into groups based on the provided type predicates.
		/// </summary>
		/// <param name="attributeLists">The attribute lists to categorize</param>
		/// <param name="context">The syntax node analysis context</param>
		/// <param name="categorizers">Functions that determine which category an attribute belongs to</param>
		/// <returns>A dictionary mapping category names to lists of attributes</returns>
		public Dictionary<string, List<AttributeSyntax>> CategorizeAttributes(SyntaxList<AttributeListSyntax> attributeLists, SyntaxNodeAnalysisContext context, Dictionary<string, Func<INamedTypeSymbol, bool>> categorizers)
		{
			var result = new Dictionary<string, List<AttributeSyntax>>();
			foreach (var category in categorizers.Keys)
			{
				result[category] = [];
			}

			foreach (AttributeListSyntax attributeList in attributeLists)
			{
				foreach (AttributeSyntax attribute in attributeList.Attributes)
				{
					INamedTypeSymbol attributeSymbol = context.SemanticModel.GetSymbolInfo(attribute).Symbol?.ContainingType;
					if (attributeSymbol != null)
					{
						foreach (KeyValuePair<string, Func<INamedTypeSymbol, bool>> categorizer in categorizers)
						{
							if (categorizer.Value(attributeSymbol))
							{
								result[categorizer.Key].Add(attribute);
								break; // Only add to first matching category
							}
						}
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Checks if an attribute symbol matches a target symbol (including inheritance).
		/// </summary>
		/// <param name="attributeSymbol">The attribute symbol to check</param>
		/// <param name="targetSymbol">The target symbol to match against</param>
		/// <returns>True if the symbols match or if attributeSymbol is derived from targetSymbol</returns>
		private static bool AttributeMatches(INamedTypeSymbol attributeSymbol, INamedTypeSymbol targetSymbol)
		{
			return SymbolEqualityComparer.Default.Equals(attributeSymbol, targetSymbol) ||
				   attributeSymbol.IsDerivedFrom(targetSymbol);
		}

		public bool TryExtractAttributeArgument<T>(AttributeArgumentSyntax argumentSyntax, SyntaxNodeAnalysisContext context, out string argumentString, out T value)
		{
			argumentString = argumentSyntax.Expression.ToString();

			SymbolInfo data = context.SemanticModel.GetSymbolInfo(argumentSyntax.Expression);

			if (data.Symbol == null)
			{
				var helper = new LiteralHelper();
				if (helper.TryGetLiteralValue(argumentSyntax.Expression, context.SemanticModel, out T literalValue))
				{
					value = literalValue;
					return true;
				}
				value = default;
				return false;
			}

			if (data.Symbol is IFieldSymbol field && field.HasConstantValue && field.Type.Name == typeof(T).Name)
			{
				value = (T)field.ConstantValue;
				return true;
			}

			value = default;
			return false;
		}
	}
}

