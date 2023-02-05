﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class WinFormsInitializeComponentMustBeCalledOnceAnalyzer : SingleDiagnosticAnalyzer<ClassDeclarationSyntax>
	{
		private const string Title = @"Check for UserControl constructor chains calls to InitializeComponent()";
		public static readonly string MessageFormat = @"Class ""{0}"" constructor triggers {1} calls to ""InitializeComponent()"".  Must only trigger 1.";
		private const string Description = @"All UserControl constructor chains must call InitializeComponent().";

		private readonly TestHelper _testHelper;

		public WinFormsInitializeComponentMustBeCalledOnceAnalyzer()
			:this(new TestHelper(), new Helper())
		{ }

		public WinFormsInitializeComponentMustBeCalledOnceAnalyzer(TestHelper testHelper, Helper helper)
			: base(DiagnosticId.InitializeComponentMustBeCalledOnce, Title, MessageFormat, Description, Categories.Maintainability, helper)
		{
			_testHelper = testHelper;
			FullyQualifiedMetaDataName = "System.Windows.Forms.ContainerControl";
		}

		private void IsInitializeComponentInConstructors(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax node, ConstructorDeclarationSyntax[] constructors)
		{
			if (constructors.Length == 0)
			{
				ReportDiagnostic(context, node.Identifier.GetLocation(), node.Identifier.ToString(), 0);
				return;
			}

			ConstructorSyntaxHelper constructorSyntaxHelper = new();
			var mapping = constructorSyntaxHelper.CreateMapping(context, constructors);

			foreach (ConstructorDeclarationSyntax ctor in constructors)
			{
				var chain = constructorSyntaxHelper.GetCtorChain(mapping, ctor);

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
					ReportDiagnostic(context, location, ctor.Identifier, count);
				}
			}
		}

		private bool IsInitializeComponentInConstructorChainOnce(IReadOnlyList<ConstructorDeclarationSyntax> chain, out int count)
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
				if (string.Equals(@"InitializeComponent", name.Identifier.ValueText, System.StringComparison.Ordinal))
				{
					count++;
				}
			}
			return count;
		}


		protected override void Analyze(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax node)
		{
			if (!node.Modifiers.Any(SyntaxKind.PartialKeyword) && !node.Members.OfType<MethodDeclarationSyntax>().Any(x => x.Identifier.Text == "InitializeComponent"))
			{
				return;
			}

			// If we're in a TestClass, let it go.
			if (_testHelper.IsInTestClass(context))
			{
				return;
			}

			// If we're not within a Control/Form, let it go.
			INamedTypeSymbol type = context.SemanticModel.GetDeclaredSymbol(node);
			if (!Helper.IsUserControl(type))
			{
				return;
			}

			ConstructorDeclarationSyntax[] constructors = node.Members.OfType<ConstructorDeclarationSyntax>().Where(x => !x.Modifiers.Any(SyntaxKind.StaticKeyword)).ToArray();
			IsInitializeComponentInConstructors(context, node, constructors);
		}
	}
}
