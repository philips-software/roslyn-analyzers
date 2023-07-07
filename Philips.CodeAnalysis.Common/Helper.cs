// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	public class Helper
	{
		public Helper(AnalyzerOptions options, Compilation compilation)
		{
			ForAdditionalFiles = new AdditionalFilesHelper(options, compilation);
			ForAssemblies = new AssembliesHelper();
			ForAttributes = new AttributeHelper();
			ForConstructors = new ConstructorSyntaxHelper();
			ForGeneratedCode = new GeneratedCodeDetector(this);
			ForLiterals = new LiteralHelper();
			ForTests = new TestHelper();
			ForTypes = new TypesHelper();
		}

		public AdditionalFilesHelper ForAdditionalFiles { get; }

		public AssembliesHelper ForAssemblies { get; }

		public AttributeHelper ForAttributes { get; }

		public ConstructorSyntaxHelper ForConstructors { get; }

		public GeneratedCodeDetector ForGeneratedCode { get; }

		public LiteralHelper ForLiterals { get; }

		public TestHelper ForTests { get; }

		public TypesHelper ForTypes { get; }

		public static string ToDiagnosticId(DiagnosticId id)
		{
			return @"PH" + ((int)id).ToString();
		}

		public static string ToHelpLinkUrl(string id)
		{
			return $"https://github.com/philips-software/roslyn-analyzers/blob/main/Documentation/Diagnostics/{id}.md";
		}

		public static string ToPrettyList(IEnumerable<Diagnostic> diagnostics)
		{
			IEnumerable<string> values = diagnostics.Select(diagnostic => diagnostic.Id);
			return string.Join(", ", values);
		}

		public static bool IsNamespaceExempt(string myNamespace)
		{
			// https://developercommunity.visualstudio.com/t/error-cs0518-predefined-type-systemruntimecompiler/1244809
			List<string> exceptions = new() { "System.Runtime.CompilerServices" };
			return exceptions.Any(e => e == myNamespace);
		}

		public static IReadOnlyDictionary<string, string> GetUsingAliases(SyntaxNode node)
		{
			var list = new Dictionary<string, string>();
			SyntaxNode root = node.SyntaxTree.GetRoot();
			foreach (UsingDirectiveSyntax child in root.DescendantNodes(n => n is not TypeDeclarationSyntax).OfType<UsingDirectiveSyntax>())
			{
				if (child.Alias != null)
				{
					var alias = child.Alias.Name.GetFullName(list);
					var name = child.Name.GetFullName(list);
					list.Add(alias, name);
				}
			}
			return list;
		}
	}
}
