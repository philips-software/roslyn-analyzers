// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using LanguageExt.SomeHelp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class WinFormsInitializeComponentMustBeCalledOnceAnalyzer : SingleDiagnosticAnalyzer<ClassDeclarationSyntax, WinFormsInitializeComponentMustBeCalledOnceSyntaxNodeAction>
	{
		private const string Title = @"Check for UserControl constructor chains calls to InitializeComponent()";
		public static readonly string MessageFormat = @"Class ""{0}"" constructor triggers {1} calls to ""InitializeComponent()"".  Must only trigger 1.";
		private const string Description = @"All UserControl constructor chains must call InitializeComponent().";

		public WinFormsInitializeComponentMustBeCalledOnceAnalyzer()
			: base(DiagnosticId.InitializeComponentMustBeCalledOnce, Title, MessageFormat, Description, Categories.Maintainability)
		{
			FullyQualifiedMetaDataName = "System.Windows.Forms.ContainerControl";
		}
	}

	public class WinFormsInitializeComponentMustBeCalledOnceSyntaxNodeAction : SyntaxNodeAction<ClassDeclarationSyntax>
	{
		private readonly TestHelper _testHelper = new();

		private IEnumerable<Diagnostic> IsInitializeComponentInConstructors(ConstructorDeclarationSyntax[] constructors)
		{
			if (constructors.Length == 0)
			{
				Location identifierLocation = Node.Identifier.GetLocation();
				return PrepareDiagnostic(identifierLocation, Node.Identifier.ToString(), 0).ToSome();
			}

			ConstructorSyntaxHelper constructorSyntaxHelper = new();
			IReadOnlyDictionary<ConstructorDeclarationSyntax, ConstructorDeclarationSyntax> mapping = constructorSyntaxHelper.CreateMapping(Context, constructors);

			var errors = new List<Diagnostic>();

			foreach (ConstructorDeclarationSyntax ctor in constructors)
			{
				IReadOnlyList<ConstructorDeclarationSyntax> chain = constructorSyntaxHelper.GetCtorChain(mapping, ctor);

				if (!IsInitializeComponentInConstructorChainOnce(chain, out var count))
				{
					Location location;
					if (ctor.Initializer == null)
					{
						location = ctor.GetLocation();
					}
					else
					{
						location = ctor.Initializer.GetLocation();
					}
					errors.Add(PrepareDiagnostic(location, ctor.Identifier, count));
				}
			}
			return errors;
		}

		private bool IsInitializeComponentInConstructorChainOnce(IReadOnlyList<ConstructorDeclarationSyntax> chain, out int count)
		{
			count = 0;
			foreach (ConstructorDeclarationSyntax ctor in chain)
			{
				count += IsInitializeComponentInConstructor(ctor);
			}

			return count == 1;
		}

		private int IsInitializeComponentInConstructor(ConstructorDeclarationSyntax constructor)
		{
			var count = 0;
			foreach (InvocationExpressionSyntax invocation in constructor.DescendantNodes().OfType<InvocationExpressionSyntax>())
			{
				if (invocation.Expression is not IdentifierNameSyntax name)
				{
					continue;
				}
				if (string.Equals(@"InitializeComponent", name.Identifier.ValueText, System.StringComparison.Ordinal))
				{
					count++;
				}
			}
			return count;
		}


		public override IEnumerable<Diagnostic> Analyze()
		{
			if (!Node.Modifiers.Any(SyntaxKind.PartialKeyword) && !Node.Members.OfType<MethodDeclarationSyntax>().Any(x => x.Identifier.Text == "InitializeComponent"))
			{
				return Option<Diagnostic>.None;
			}

			// If we're in a TestClass, let it go.
			if (_testHelper.IsInTestClass(Context))
			{
				return Option<Diagnostic>.None;
			}

			// If we're not within a Control/Form, let it go.
			INamedTypeSymbol type = Context.SemanticModel.GetDeclaredSymbol(Node);
			if (!Helper.IsUserControl(type))
			{
				return Option<Diagnostic>.None;
			}

			ConstructorDeclarationSyntax[] constructors = Node.Members.OfType<ConstructorDeclarationSyntax>().Where(x => !x.Modifiers.Any(SyntaxKind.StaticKeyword)).ToArray();
			return IsInitializeComponentInConstructors(constructors);
		}
	}
}
