// © 2024 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Philips.CodeAnalysis.Common
{
	public class NamespaceResolver
	{
		private readonly Dictionary<string, string> _aliases = [];
		private readonly List<string> _namespaces = [];
		private readonly object _syncRoot = new();

		public NamespaceResolver(SyntaxNode node)
		{
			SyntaxNode root = node.SyntaxTree.GetRoot();
			foreach (UsingDirectiveSyntax child in root.DescendantNodes(n => n is not TypeDeclarationSyntax)
				.OfType<UsingDirectiveSyntax>())
			{
				lock (_syncRoot)
				{
					if (child.Alias != null)
					{
						_aliases.Add(child.Alias.Name.Identifier.Text, child.Name.ToString());
					}
					else
					{
						_namespaces.Add(child.Name.ToString());
					}
				}
			}
		}

		/// <summary>
		/// Resolve the given <see cref="TypeSyntax"/> fully qualified name in the expected namespace.
		/// </summary>
		/// <remarks>
		/// The fact that a valid name is returned, does not indicate that such a type exists. This method will not do a SemanticModel inspection.
		/// Instead, consider this as an better performing alternative to SemanticModel.
		/// </remarks>
		/// <param name="type">The type to resolve the name for</param>
		/// <param name="expectedNamespace">The namespace in which the type is expected</param>
		/// <returns>The fully qualified name or the empty string otherwise.</returns>
		public string ResolveFullyQualifiedName(TypeSyntax type, string expectedNamespace)
		{
			var result = string.Empty;
			var deAliasedName = GetDealiasedName(type);
			if (!deAliasedName.Contains("."))
			{
				lock (_syncRoot)
				{
					// Deep namespaces include shallower ones implicitly in C#.
					if (_namespaces.Exists(ns => ns.Contains(expectedNamespace)))
					{
						result = $"{expectedNamespace}.{deAliasedName}";
					}
				}
			}
			else
			{
				result = deAliasedName;
			}

			return result;
		}

		/// <summary>
		/// Resolve the Type part of the given <see cref="MemberAccessExpressionSyntax"/> to its fully qualified name in the expected namespace.
		/// </summary>
		/// <param name="memberAccess"></param>
		/// <param name="expectedNamespace"></param>
		/// <returns></returns>
		public string ResolveFullyQualifiedName(MemberAccessExpressionSyntax memberAccess, string expectedNamespace)
		{
			if (memberAccess.Expression is TypeSyntax plainType)
			{
				return ResolveFullyQualifiedName(plainType, expectedNamespace);
			}

			if (memberAccess.Expression is MemberAccessExpressionSyntax innerMemberAccessExpression)
			{
				var right = innerMemberAccessExpression.Name.Identifier.Text;
				var left = GetDealiasedName(innerMemberAccessExpression.Expression as TypeSyntax);
				return $"{left}.{right}";
			}

			return string.Empty;
		}

		/// <summary>
		/// Returns true if the <paramref name="type"/> is of the expected type and namespace.
		/// </summary>
		/// <param name="type">The <see cref="TypeSyntax"/> to check.</param>
		/// <param name="expectedNamespace">The namespace of the expected type.</param>
		/// <param name="expectedType">The class or struct name of the expected type, excluding its namespace.</param>
		/// <returns>True if <paramref name="type"/> might be of type <paramref name="expectedType"/> in namespace <paramref name="expectedNamespace"/>.</returns>
		public bool IsOfType(TypeSyntax type, string expectedNamespace, string expectedType)
		{
			var expectedFullName = $"{expectedNamespace}.{expectedType}";
			return ResolveFullyQualifiedName(type, expectedNamespace) == expectedFullName;
		}

		/// <summary>
		/// Returns true if the <paramref name="memberAccess"/> has a Type that is of the expected type and namespace.
		/// </summary>
		/// <param name="memberAccess">Expression who's type to check.</param>
		/// <param name="expectedNamespace">The namespace of the expected type.</param>
		/// <param name="expectedType">The class or struct name of the expected type, excluding its namespace.</param>
		/// <returns>True if <paramref name="memberAccess"/> might refer to the type <paramref name="expectedType"/> in namespace <paramref name="expectedNamespace"/>.</returns>
		public bool IsOfType(MemberAccessExpressionSyntax memberAccess, string expectedNamespace, string expectedType)
		{
			var expectedFullName = $"{expectedNamespace}.{expectedType}";
			return ResolveFullyQualifiedName(memberAccess, expectedNamespace) == expectedFullName;
		}

		/// <summary>
		/// Gets the Type's name, including resolving any aliases but excluding its namespace.
		/// </summary>
		public string GetDealiasedName(TypeSyntax typeSyntax)
		{
			var name = string.Empty;
			if (typeSyntax is SimpleNameSyntax simpleNameSyntax)
			{
				name = simpleNameSyntax.Identifier.Text;
			}
			else if (typeSyntax is QualifiedNameSyntax qualifiedNameSyntax)
			{
				var left = GetDealiasedName(qualifiedNameSyntax.Left);
				var right = qualifiedNameSyntax.Right.Identifier.Text;
				name = $"{left}.{right}";
			}

			lock (_syncRoot)
			{
				if (_aliases.TryGetValue(name, out var aliased))
				{
					name = aliased;
				}
			}

			return name;
		}

		/// <summary>
		/// Based on the 'using' statements in this source file, determine if the <paramref name="type"/> MIGHT be any of the fully qualified names in the <paramref name="fullyQualifiedList"/>.
		/// </summary>
		public bool MightByOfType(TypeSyntax type, IReadOnlyList<string> fullyQualifiedList)
		{
			var deAliased = $".{GetDealiasedName(type)}";
			var deAliasedLength = deAliased.Length;
			IEnumerable<string> candidates = fullyQualifiedList.Where(qualified => qualified.EndsWith(deAliased));
			return candidates.Any(candidate => _namespaces.Contains(candidate.Substring(0, candidate.Length - deAliasedLength)));
		}
	}
}
