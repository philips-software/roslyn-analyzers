// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	public class TestHelper
	{
		private readonly CodeFixHelper _helper;

		internal TestHelper(CodeFixHelper helper)
		{
			_helper = helper;
		}

		public bool IsInTestClass(SyntaxNodeAnalysisContext context)
		{
			return IsInTestClass(context, out _);
		}

		public bool IsInTestClass(SyntaxNodeAnalysisContext context, out ClassDeclarationSyntax classDeclaration)
		{
			classDeclaration = context.Node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
			if (classDeclaration == null)
			{
				return false;
			}
			SyntaxList<AttributeListSyntax> classAttributeList = classDeclaration.AttributeLists;
			return _helper.ForAttributes.HasAnyAttribute(classAttributeList, context, MsTestFrameworkDefinitions.TestClassAttribute, MsTestFrameworkDefinitions.StaTestClassAttribute);
		}

		public bool IsTestClass(ClassDeclarationSyntax classDeclaration, SyntaxNodeAnalysisContext context)
		{
			SyntaxList<AttributeListSyntax> classAttributeList = classDeclaration.AttributeLists;
			return _helper.ForAttributes.HasAnyAttribute(classAttributeList, context, MsTestFrameworkDefinitions.TestClassAttribute, MsTestFrameworkDefinitions.StaTestClassAttribute);
		}

		public bool IsTestMethod(MethodDeclarationSyntax method, SyntaxNodeAnalysisContext context)
		{
			return IsTestMethod(method.AttributeLists, context, out _);
		}

		public bool IsTestMethod(SyntaxList<AttributeListSyntax> attributes, SyntaxNodeAnalysisContext context, out bool isDataTestMethod)
		{
			foreach (AttributeListSyntax syntax in attributes)
			{
				if (IsTestMethod(syntax, context, out _, out isDataTestMethod))
				{
					return true;
				}
			}

			isDataTestMethod = false;
			return false;
		}

		public bool IsTestMethod(AttributeListSyntax attributes, SyntaxNodeAnalysisContext context, out Location location, out bool isDataTestMethod)
		{
			isDataTestMethod = false;
			var hasAttribute = _helper.ForAttributes.HasAttribute(attributes, context, MsTestFrameworkDefinitions.TestMethodAttribute, out location);
			if (!hasAttribute)
			{
				hasAttribute = _helper.ForAttributes.HasAttribute(attributes, context, MsTestFrameworkDefinitions.DataTestMethodAttribute, out location);

				if (hasAttribute)
				{
					isDataTestMethod = true;
				}
			}
			return hasAttribute;
		}

		public static bool IsTestProject(Compilation compilation)
		{
			// Check if this is a test project by looking for common test framework references
			var referencedAssemblyNames = new HashSet<string>(
				compilation.ReferencedAssemblyNames.Select(name => name.Name),
				StringComparer.OrdinalIgnoreCase);

			return referencedAssemblyNames.Contains("Microsoft.VisualStudio.TestPlatform.TestFramework") ||
				   referencedAssemblyNames.Contains("MSTest.TestFramework") ||
				   referencedAssemblyNames.Contains("NUnit.Framework") ||
				   referencedAssemblyNames.Contains("xunit") ||
				   referencedAssemblyNames.Contains("xunit.core");
		}
	}
}
