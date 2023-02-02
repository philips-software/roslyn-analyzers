// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
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
		private readonly HashSet<IMethodSymbol> _allowedMethods;
		private readonly HashSet<ITypeSymbol> _allowedTypes;
		private readonly HashSet<INamespaceSymbol> _allowedNamespaces;
		private readonly HashSet<string> _allowedLines;

		public AllowedSymbols()
		{
			_allowedMethods = new HashSet<IMethodSymbol>();
			_allowedTypes = new HashSet<ITypeSymbol>();
			_allowedNamespaces = new HashSet<INamespaceSymbol>();
			_allowedLines = new HashSet<string>();
		}

		/// <summary>
		/// Load the methods for which duplicate code is allowed.
		/// </summary>
		public void LoadAllowedMethods(SourceText text, Compilation compilation)
		{
			foreach (var textLine in text.Lines)
			{
				string line = StripComments(textLine.ToString());
				if (!string.IsNullOrWhiteSpace(line))
				{
					RegisterLine(line, compilation);
				}
			}
		}

		/// <summary>
		/// Register a new line.
		/// </summary>
		/// <exception cref="InvalidDataException">When an invalid type is supplied.</exception>
		private void RegisterLine(string line, Compilation compilation)
		{
			if (line.StartsWith("~"))
			{
				var id = line.Substring(1);
				var symbols = DocumentationCommentId.GetSymbolsForDeclarationId(id, compilation);
				if (!symbols.IsDefaultOrEmpty)
				{
					foreach (var symbol in symbols)
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

		public bool IsAllowed(IMethodSymbol requested)
		{
			var requestedType = requested.ContainingType;
			var requestedNamespace = requestedType.ContainingNamespace;
			return
				_allowedMethods.Contains(requested) ||
				_allowedTypes.Contains(requestedType) ||
				_allowedNamespaces.Contains(requestedNamespace) ||
				MatchesAnyLine(requestedNamespace.Name, requestedType.Name, requested.Name);
		}

		public bool IsAllowed(INamedTypeSymbol requested)
		{
			var requestedNamespace = requested.ContainingNamespace;
			return 
				_allowedLines.Contains(requested.Name) ||
			    _allowedTypes.Contains(requested) ||
			    _allowedNamespaces.Contains(requestedNamespace) ||
				MatchesAnyLine(requestedNamespace.Name, requested.Name, null);
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

		private bool MatchesAnyLine(string nsName, string typeName, string methodName)
		{
			return _allowedLines.Any(line =>
			{
				var parts = line.Split('.');
				switch (parts.Length)
				{
					case 1:
						return (methodName == null) ? line == typeName : line == methodName;
					case 2:
						return (parts[0] == "*") ? parts[1] == typeName : parts[0] == nsName && parts[1] == typeName;
					case 3:
						bool result = true;
						if (parts[0] != "*")
						{
							result &= parts[0] == nsName;
						}
						if(parts[1] != "*")
						{
							result &= parts[1] == typeName;
						}
						result &= parts[2] == methodName;
						return result;
					default:
						return false;
				}
			});
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
