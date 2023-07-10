// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	public class Helper
	{
		public Helper(AnalyzerOptions options, Compilation compilation)
		{
			ForAllowedSymbols = new AllowedSymbols(compilation);
			ForAdditionalFiles = new AdditionalFilesHelper(options, compilation);
			ForAssemblies = new AssembliesHelper();
			ForAttributes = new AttributeHelper();
			ForConstructors = new ConstructorSyntaxHelper();
			ForGeneratedCode = new GeneratedCodeDetector(this);
			ForLiterals = new LiteralHelper();
			ForNamespaces = new NamespacesHelper();
			ForTests = new TestHelper(this);
			ForTypes = new TypesHelper(this);
		}

		public AllowedSymbols ForAllowedSymbols { get; }

		public AdditionalFilesHelper ForAdditionalFiles { get; }

		public AssembliesHelper ForAssemblies { get; }

		public AttributeHelper ForAttributes { get; }

		public ConstructorSyntaxHelper ForConstructors { get; }

		public GeneratedCodeDetector ForGeneratedCode { get; }

		public LiteralHelper ForLiterals { get; }

		public NamespacesHelper ForNamespaces { get; }

		public TestHelper ForTests { get; }

		public TypesHelper ForTypes { get; }
	}
}
