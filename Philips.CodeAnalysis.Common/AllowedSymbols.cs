// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Philips.CodeAnalysis.Common
{
	/// <summary>
	/// Keep track of allowed symbols and provide ability to check requested symbols.
	/// </summary>
	public class AllowedSymbols
	{
		private const int NamespaceIndexDelta = 3;
		private const int TypeIndexDelta = 2;
		private const int MethodIndexDelta = 1;
		private const string Wildcard = "*";
		private readonly Compilation _compilation;
		private readonly HashSet<IMethodSymbol> _allowedMethods;
		private readonly HashSet<ITypeSymbol> _allowedTypes;
		private readonly HashSet<INamespaceSymbol> _allowedNamespaces;
		private readonly HashSet<string> _allowedLines;
		private readonly object _syncRoot;

		internal AllowedSymbols(Compilation compilation)
		{
			_compilation = compilation;
			_allowedMethods = [];
			_allowedTypes = [];
			_allowedNamespaces = [];
			_allowedLines = [];
			_syncRoot = new();
		}

		/// <summary>
		/// Total number of allowed symbols defined.
		/// </summary>
		public int Count => _allowedMethods.Count + _allowedLines.Count + _allowedTypes.Count + _allowedNamespaces.Count;

		public bool Initialize(ImmutableArray<AdditionalText> additionalFiles, string filenameToInitialize)
		{
			foreach (AdditionalText additionalFile in additionalFiles)
			{
				var fileName = Path.GetFileName(additionalFile.Path);
				if (StringComparer.OrdinalIgnoreCase.Equals(fileName, filenameToInitialize))
				{
					SourceText allowedMethods = additionalFile.GetText();
					LoadAllowedMethods(allowedMethods);
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Register a new line.
		/// </summary>
		/// <exception cref="InvalidDataException">When an invalid type is supplied.</exception>
		public void RegisterLine(string line)
		{
			if (line.StartsWith("~"))
			{
				var id = line.Substring(1);
				ImmutableArray<ISymbol> symbols = DocumentationCommentId.GetSymbolsForDeclarationId(id, _compilation);
				if (!symbols.IsDefaultOrEmpty)
				{
					foreach (ISymbol symbol in symbols)
					{
						RegisterSymbol(symbol);
					}
				}
			}
			else
			{
				lock (_syncRoot)
				{
					_ = _allowedLines.Add(line);
				}
			}
		}

		/// <summary>
		/// Load the methods for which duplicate code is allowed.
		/// </summary>
		private void LoadAllowedMethods(SourceText text)
		{
			foreach (TextLine textLine in text.Lines)
			{
				var line = StripComments(textLine.ToString());
				if (!string.IsNullOrWhiteSpace(line))
				{
					RegisterLine(line);
				}
			}
		}

		public bool IsAllowed(IMethodSymbol requested)
		{
			INamedTypeSymbol requestedType = requested.ContainingType;
			INamespaceSymbol requestedNamespace = requestedType.ContainingNamespace;
			lock (_syncRoot)
			{
				return
					_allowedMethods.Contains(requested) ||
					_allowedTypes.Contains(requestedType) ||
					_allowedNamespaces.Contains(requestedNamespace) ||
					MatchesAnyLine(requestedNamespace, requestedType, requested);
			}
		}

		public bool IsAllowed(INamedTypeSymbol requested)
		{
			INamespaceSymbol requestedNamespace = requested.ContainingNamespace;
			lock (_syncRoot)
			{
				return
					_allowedLines.Contains(requested.Name) ||
					_allowedTypes.Contains(requested) ||
					_allowedNamespaces.Contains(requestedNamespace) ||
					MatchesAnyLine(requestedNamespace, requested, null);
			}
		}

		/// <summary>
		/// Register a new symbol.
		/// </summary>
		/// <exception cref="InvalidDataException">When an invalid type is supplied.</exception>
		private void RegisterSymbol(ISymbol symbol)
		{
			lock (_syncRoot)
			{
				if (symbol is IMethodSymbol methodSymbol)
				{
					_ = _allowedMethods.Add(methodSymbol);
				}
				else if (symbol is ITypeSymbol typeSymbol)
				{
					_ = _allowedTypes.Add(typeSymbol);
				}
				else if (symbol is INamespaceSymbol namespaceSymbol)
				{
					_ = _allowedNamespaces.Add(namespaceSymbol);
				}
				else
				{
					throw new InvalidDataException(
						"Invalid symbol type found: " + symbol.MetadataName);
				}
			}
		}

		private bool MatchesAnyLine(INamespaceSymbol ns, INamedTypeSymbol type, IMethodSymbol method)
		{
			var nsName = ns.ToString();
			var typeName = type.Name;
			lock (_syncRoot)
			{
				return _allowedLines.Any(line =>
				{
					var parts = line.Split('.');
					if (parts.Length == 1)
					{
						return (method == null) ? MatchesPattern(line, typeName) : MatchesPattern(line, method.Name);
					}
					else if (parts.Length == 2)
					{
						return (parts[0] == Wildcard)
							? MatchesPattern(parts[1], typeName)
							: parts[0] == nsName && MatchesPattern(parts[1], typeName);
					}
					else
					{
						return MatchesFullNamespace(parts, nsName, typeName, method);
					}
				});
			}
		}

		private bool MatchesFullNamespace(string[] parts, string nsName, string typeName, IMethodSymbol method)
		{
			var methodName = method?.Name;
			var isMatch = true;
			var length = parts.Length;
			var nsIndex = length - NamespaceIndexDelta;
			var typeIndex = length - TypeIndexDelta;
			var methodIndex = length - MethodIndexDelta;
			if (method == null)
			{
				nsIndex = length - TypeIndexDelta;
				typeIndex = length - MethodIndexDelta;
			}

			if (parts[nsIndex] != Wildcard)
			{
				var fullNs = string.Join(".", parts, 0, nsIndex + 1);
				isMatch &= fullNs == nsName;
			}

			if (parts[typeIndex] != Wildcard)
			{
				isMatch &= MatchesPattern(parts[typeIndex], typeName);
			}

			if (method != null && parts[methodIndex] != Wildcard)
			{
				isMatch &= MatchesPattern(parts[methodIndex], methodName);
			}

			return isMatch;
		}

		/// <summary>
		/// Matches a pattern against a target string, supporting wildcards at the beginning and end.
		/// </summary>
		/// <param name="pattern">The pattern to match, which may contain wildcards (*)</param>
		/// <param name="target">The target string to match against</param>
		/// <returns>True if the pattern matches the target</returns>
		private bool MatchesPattern(string pattern, string target)
		{
			if (pattern == Wildcard || pattern == target)
			{
				return true;
			}

			var starts = pattern.StartsWith(Wildcard);
			var ends = pattern.EndsWith(Wildcard);

			return (starts, ends) switch
			{
				(true, true) => target.Contains(pattern.Substring(1, pattern.Length - 2)),
				(true, false) => target.EndsWith(pattern.Substring(1)),
				(false, true) => target.StartsWith(pattern.Substring(0, pattern.Length - 1)),
				_ => false
			};
		}


		private string StripComments(string input)
		{
			var stripped = input;
			// Remove Banned API style messages
			var semiColonIndex = input.IndexOf(';');
			var hashIndex = input.IndexOf('#');
			var singleLineCommentIndex = input.IndexOf("//");
			var index = (semiColonIndex >= 0) ? semiColonIndex : input.Length;
			index = (hashIndex >= 0) ? Math.Min(index, hashIndex) : index;
			index = (singleLineCommentIndex >= 0) ? Math.Min(index, singleLineCommentIndex) : index;
			if (index < input.Length)
			{
				stripped = input.Substring(0, index);
			}
			return stripped.Trim();
		}
	}
}
