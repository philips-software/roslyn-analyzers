// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
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
			ForNamespaces = new NamespacesHelper();
			ForTests = new TestHelper();
			ForTypes = new TypesHelper(this);
		}

		public AdditionalFilesHelper ForAdditionalFiles { get; }

		public AssembliesHelper ForAssemblies { get; }

		public AttributeHelper ForAttributes { get; }

		public ConstructorSyntaxHelper ForConstructors { get; }

		public GeneratedCodeDetector ForGeneratedCode { get; }

		public LiteralHelper ForLiterals { get; }

		public NamespacesHelper ForNamespaces { get; }

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
	}
}
