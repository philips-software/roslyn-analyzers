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

		public AllowedSymbols(Compilation compilation)
		{
			_compilation = compilation;
			_allowedMethods = new HashSet<IMethodSymbol>();
			_allowedTypes = new HashSet<ITypeSymbol>();
			_allowedNamespaces = new HashSet<INamespaceSymbol>();
			_allowedLines = new HashSet<string>();
		}

		/// <summary>
		/// Total number of allowed symbols defined.
		/// </summary>
		public int Count => _allowedMethods.Count + _allowedLines.Count + _allowedTypes.Count + _allowedNamespaces.Count;

		public void Initialize(ImmutableArray<AdditionalText> additionalFiles, string filenameToInitialize)
		{
			foreach (AdditionalText additionalFile in additionalFiles)
			{
				string fileName = Path.GetFileName(additionalFile.Path);
				if (StringComparer.OrdinalIgnoreCase.Equals(fileName, filenameToInitialize))
				{
					var allowedMethods = additionalFile.GetText();
					LoadAllowedMethods(allowedMethods);
				}
			}
		}

		/// <summary>
		/// Register a new line.
		/// </summary>
		/// <exception cref="InvalidDataException">When an invalid type is supplied.</exception>
		public void RegisterLine(string line)
		{
			if(line.StartsWith("~"))
			{
				var id = line.Substring(1);
				var symbols = DocumentationCommentId.GetSymbolsForDeclarationId(id, _compilation);
				if(!symbols.IsDefaultOrEmpty)
				{
					foreach(var symbol in symbols)
					{
						RegisterSymbol(symbol);
					}
				}
			}
			else
			{
				_allowedLines.Add(line);
			}
		}
		
		/// <summary>
		/// Load the methods for which duplicate code is allowed.
		/// </summary>
		private void LoadAllowedMethods(SourceText text)
		{
			foreach (var textLine in text.Lines)
			{
				string line = StripComments(textLine.ToString());
				if (!string.IsNullOrWhiteSpace(line))
				{
					RegisterLine(line);
				}
			}
		}

		public bool IsAllowed(IMethodSymbol requested)
		{
			var requestedType = requested.ContainingType;
			var requestedNamespace = requestedType.ContainingNamespace;
			return
				_allowedMethods.Contains(requested) ||
				_allowedTypes.Contains(requestedType) ||
				_allowedNamespaces.Contains(requestedNamespace) ||
				MatchesAnyLine(requestedNamespace, requestedType, requested);
		}

		public bool IsAllowed(INamedTypeSymbol requested)
		{
			var requestedNamespace = requested.ContainingNamespace;
			return 
				_allowedLines.Contains(requested.Name) ||
			    _allowedTypes.Contains(requested) ||
			    _allowedNamespaces.Contains(requestedNamespace) ||
				MatchesAnyLine(requestedNamespace, requested, null);
		}

		/// <summary>
		/// Register a new symbol.
		/// </summary>
		/// <exception cref="InvalidDataException">When an invalid type is supplied.</exception>
		private void RegisterSymbol(ISymbol symbol)
		{
			if(symbol is IMethodSymbol methodSymbol)
			{
				_allowedMethods.Add(methodSymbol);
			}
			else if(symbol is ITypeSymbol typeSymbol)
			{
				_allowedTypes.Add(typeSymbol);
			}
			else if(symbol is INamespaceSymbol namespaceSymbol)
			{
				_allowedNamespaces.Add(namespaceSymbol);
			}
			else
			{
				throw new InvalidDataException(
					"Invalid symbol type found: " + symbol.MetadataName);
			}
		}

		private bool MatchesAnyLine(INamespaceSymbol ns, INamedTypeSymbol type, IMethodSymbol method)
		{
			var nsName = ns.ToString();
			var typeName = type.Name;
			return _allowedLines.Any(line =>
			{
				var parts = line.Split('.');
				if (parts.Length == 1)
				{
					return (method == null) ? line == typeName : line == method.Name;
				}
				else if (parts.Length == 2)
				{
					return (parts[0] == Wildcard) ? parts[1] == typeName : parts[0] == nsName && parts[1] == typeName;
				} else
				{
					return MatchesFullNamespace(parts, nsName, typeName, method);
				}
			});
		}

		private static bool MatchesFullNamespace(string[] parts, string nsName, string typeName, IMethodSymbol method)
		{
			string methodName = method?.Name;
			bool result = true;
			int length = parts.Length;
			int nsIndex = length - NamespaceIndexDelta;
			int typeIndex = length - TypeIndexDelta;
			int methodIndex = length - MethodIndexDelta;
			if (method == null)
			{
				nsIndex = length - TypeIndexDelta;
				typeIndex = length - MethodIndexDelta;
			}

			if (parts[nsIndex] != Wildcard)
			{
				var fullNs = string.Join(".", parts, 0, nsIndex + 1);
				result &= fullNs == nsName;
			}

			if (parts[typeIndex] != Wildcard)
			{
				result &= parts[typeIndex] == typeName;
			}

			if (method != null && parts[methodIndex] != Wildcard)
			{
				result &= parts[methodIndex] == methodName;
			}

			return result;
		}


		private string StripComments(string input)
		{
			string stripped = input;
			// Remove Banned API style messages
			var semiColonIndex = input.IndexOf(';');
			var hashIndex = input.IndexOf('#');
			var singleLineCommentIndex = input.IndexOf("//");
			var index = (semiColonIndex >= 0) ? semiColonIndex : input.Length;
			index = (hashIndex >= 0) ? Math.Min(index, hashIndex): index;
			index = (singleLineCommentIndex >= 0) ? Math.Min(index, singleLineCommentIndex) : index;
			if (index < input.Length)
			{
				stripped = input.Substring(0, index);
			}
			return stripped.Trim();
		}
	}
}
