// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class WinFormsInitializeComponentMustBeCalledOnceAnalyzer : DiagnosticAnalyzer
	{
		#region Non-Public Data Members

		private const string Title = @"Check for UserControl constructor chains calls to InitializeComponent()";
		public static string MessageFormat = @"Class ""{0}"" constructor triggers {1} calls to ""InitializeComponent()"".  Must only trigger 1.";
		private const string Description = @"All UserControl constructor chains must call InitializeComponent().";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.InitializeComponentMustBeCalledOnce),
												Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);


		#endregion

		#region Non-Public Properties/Methods

		/// <summary>
		/// IsInitializeComponentInConstructors
		/// </summary>
		/// <param name="context"></param>
		/// <param name="constructors"></param>
		/// <param name="classDeclaration"></param>
		/// <returns></returns>
		private void IsInitializeComponentInConstructors(SyntaxNodeAnalysisContext context, ConstructorDeclarationSyntax[] constructors, ClassDeclarationSyntax classDeclaration)
		{
			if (constructors.Length == 0)
			{
				Diagnostic diagnostic0 = Diagnostic.Create(Rule, classDeclaration.Identifier.GetLocation(), classDeclaration.Identifier.ToString(), 0);
				context.ReportDiagnostic(diagnostic0);
				return;
			}

			Dictionary<ConstructorDeclarationSyntax, ConstructorDeclarationSyntax> mapping = ConstructorSyntaxHelper.CreateMapping(context, constructors);

			foreach (ConstructorDeclarationSyntax ctor in constructors)
			{
				var chain = ConstructorSyntaxHelper.GetCtorChain(mapping, ctor);

				if (!IsInitializeComponentInConstructorChainOnce(chain, out int count))
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
					context.ReportDiagnostic(Diagnostic.Create(Rule, location, ctor.Identifier, count));
				}
			}
		}

		private bool IsInitializeComponentInConstructorChainOnce(List<ConstructorDeclarationSyntax> chain, out int count)
		{
			count = 0;
			foreach (var ctor in chain)
			{
				count += IsInitializeComponentInConstructor(ctor);
			}

			return count == 1;
		}

		private int IsInitializeComponentInConstructor(ConstructorDeclarationSyntax constructor)
		{
			int count = 0;
			foreach (InvocationExpressionSyntax invocation in constructor.DescendantNodes().OfType<InvocationExpressionSyntax>())
			{
				if (invocation.Expression is not IdentifierNameSyntax name)
				{
					continue;
				}
				if (string.Equals(@"InitializeComponent", name.Identifier.ValueText))
				{
					count++;
				}
			}
			return count;
		}


		/// <summary>
		/// Analyze
		/// </summary>
		/// <param name="context"></param>
		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			ClassDeclarationSyntax classDeclaration = (ClassDeclarationSyntax)context.Node;
			if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
			{
				if (!classDeclaration.Members.OfType<MethodDeclarationSyntax>().Any(x => x.Identifier.Text == "InitializeComponent"))
				{
					return;
				}
			}

			if (Helper.IsGeneratedCode(context))
			{
				return;
			}

			// If we're in a TestClass, let it go.
			if (Helper.IsInTestClass(context))
			{
				return;
			}

			// If we're not within a Control/Form, let it go.
			INamedTypeSymbol type = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
			if (!Helper.IsUserControl(type))
			{
				return;
			}

			ConstructorDeclarationSyntax[] constructors = classDeclaration.Members.OfType<ConstructorDeclarationSyntax>().Where(x => !x.Modifiers.Any(SyntaxKind.StaticKeyword)).ToArray();
			IsInitializeComponentInConstructors(context, constructors, classDeclaration);
		}

		#endregion

		#region Public Interface

		/// <summary>
		/// SupportedDiagnostics
		/// </summary>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		/// <summary>
		/// Initialize
		/// </summary>
		/// <param name="context"></param>
		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(startContext =>
			{
				if (startContext.Compilation.GetTypeByMetadataName("System.Windows.Forms.ContainerControl") == null)
				{
					return;
				}

				startContext.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
			});
		}

		#endregion
	}
}
