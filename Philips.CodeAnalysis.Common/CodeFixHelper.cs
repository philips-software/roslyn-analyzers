// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Philips.CodeAnalysis.Common
{
	public class CodeFixHelper
	{
		public CodeFixHelper()
		{
			ForAssemblies = new AssembliesHelper();
			ForAttributes = new AttributeHelper();
			ForConstructors = new ConstructorSyntaxHelper();
			ForGeneratedCode = new GeneratedCodeDetector(this);
			ForLiterals = new LiteralHelper();
			ForNamespaces = new NamespacesHelper();
			ForTests = new TestHelper(this);
			ForTypes = new TypesHelper();
		}

		public AssembliesHelper ForAssemblies { get; }

		public AttributeHelper ForAttributes { get; }

		public ConstructorSyntaxHelper ForConstructors { get; }

		public GeneratedCodeDetector ForGeneratedCode { get; }

		public LiteralHelper ForLiterals { get; }

		public NamespacesHelper ForNamespaces { get; }

		public TestHelper ForTests { get; }

		public TypesHelper ForTypes { get; }

		public DocumentationHelper ForDocumentationOf(SyntaxNode node)
		{
			return new DocumentationHelper(node);
		}
	}
}
