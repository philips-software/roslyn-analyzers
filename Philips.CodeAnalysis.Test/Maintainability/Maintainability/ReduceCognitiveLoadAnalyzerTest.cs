// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using System.Data;
using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using Philips.CodeAnalysis.Test;
using System.Text.RegularExpressions;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class ReduceCognitiveLoadAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new ReduceCognitiveLoadAnalyzer(1);
		}

		[TestMethod]
		public void CognitiveLoad1()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
	}
}
";
			VerifyCSharpDiagnostic(template);
		}

		[TestMethod]
		public void CognitiveLoadIf()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (1==1) {}
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.ReduceCognitiveLoad, new Regex("Load of 2 ")));
		}

		[TestMethod]
		public void CognitiveLoadIfNotEqual()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (1!=1) {}
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.ReduceCognitiveLoad, new Regex("Load of 3 ")));
		}

		[TestMethod]
		public void CognitiveLoadIfReturn()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (1==1) {return;}
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.ReduceCognitiveLoad, new Regex("Load of 3 ")));
		}

		[TestMethod]
		public void CognitiveLoadIfLogicalOr()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (1==1 || 2==2) {}
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.ReduceCognitiveLoad, new Regex("Load of 3 ")));
		}

		[TestMethod]
		public void CognitiveLoadIfLogicalAnd()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (1==1 && 2==2) {}
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.ReduceCognitiveLoad, new Regex("Load of 3 ")));
		}

		[TestMethod]
		public void CognitiveLoadBreak()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		while (1<2) { break; }
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.ReduceCognitiveLoad, new Regex("Load of 3 ")));
		}

		[TestMethod]
		public void CognitiveLoadBang()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (!(1==1)) {}
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.ReduceCognitiveLoad, new Regex("Load of 3 ")));
		}

		[TestMethod]
		public void CognitiveLoadNested1()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (1==1)
		{
			// Nested block statement counts double
			if (2 == 2) {}
		}
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.ReduceCognitiveLoad, new Regex("Load of 4 ")));
		}

		[TestMethod]
		public void CognitiveLoadNested2()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (1==1)
		{
			// Nested block statement counts double
			if (2 == 2) {}
			// Nested block statement counts double
			if (3 == 3) {}
		}
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.ReduceCognitiveLoad, new Regex("Load of 6 ")));
		}

		[TestMethod]
		public void CognitiveLoadNested3()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (1==1)
		{
			// Nested block statement counts double
			if (2 == 2)
			{
				// Nested block statement counts more than triple - it's exponential
				if (3 == 3) {}
			}
		}
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.ReduceCognitiveLoad, new Regex("Load of 8 ")));
		}



		[TestMethod]
		public void CognitiveLoadCombo()
		{
			const string template = @"
class Foo
{
	private void Test()
	{ //1
		if(1==1)
		{ //2
			if(2==2) {return;} //3,4,5
		}
		if(3==3)
		{ return;} // 6,7
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.ReduceCognitiveLoad, new Regex("Load of 7 ")));
		}

		[TestMethod]
		public void CognitiveLoad3()
		{
			const string template = @"
class Foo
{
	private void Test()
	{ //1
		int dynamicDataCount = 0;
		int dataRowCount = 0;
		bool hasTestSource = false;
		foreach (AttributeSyntax attribute in methodDeclaration.AttributeLists.SelectMany(x => x.Attributes))
		{ //2
			if (Helper.IsDataRowAttribute(attribute, context))
			{ //3,4
				dataRowCount++;
				continue; //5
			}
			if (Helper.IsAttribute(attribute, context, MsTestFrameworkDefinitions.DynamicDataAttribute, out _, out _))
			{ //6,7
				dynamicDataCount++;
				continue; //8
			}
			SymbolInfo symbol = context.SemanticModel.GetSymbolInfo(attribute);
			if (symbol.Symbol is IMethodSymbol method)
			{ //9,10
				if (method.ContainingType.AllInterfaces.Contains(Definitions.ITestSourceSymbol))
				{ //11,12,13,14
					hasTestSource = true;
				}
			}
		}
		if (isDataTestMethod)
		{ //15
			if (dataRowCount != 0 && dynamicDataCount == 0) //16,17
			{ // 18,19
				return; // 20
			}
			if (dataRowCount == 0 && dynamicDataCount == 1) // 21
			{ //22,23
				return; //24
			}
			if (dataRowCount != 0 && dynamicDataCount != 0) //25,26,27
			{ //28,29
				context.ReportDiagnostic(Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier.ToString(), dataRowCount, dynamicDataCount));
			}
			else if (!hasTestSource) // 30
			{ //31,32
				context.ReportDiagnostic(Diagnostic.Create(RuleShouldBeTestMethod, methodDeclaration.Identifier.GetLocation()));
			}
		}
		else
		{ //33
			if (dataRowCount == 0 && dynamicDataCount == 0) //34
			{ //35,36
				return; //37
			}
			context.ReportDiagnostic(Diagnostic.Create(RuleShouldBeDataTestMethod, methodDeclaration.Identifier.GetLocation()));
		}
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.ReduceCognitiveLoad, new Regex("Load of 37 ")));
		}


		[TestMethod]
		public void CognitiveLoad4()
		{
			const string template = @"
class Foo
{			
	private void Test()
	{																							// 1
		PropertyDeclarationSyntax property = (PropertyDeclarationSyntax)context.Node;
		if (property.Type.ToString() != @""TestContext"")										// 2
		{																						// 3
			return;																				// 4
		}
																								// 5,6
		if ((context.SemanticModel.GetSymbolInfo(property.Type).Symbol is not ITypeSymbol symbol) || (symbol.ToString() != @""Microsoft.VisualStudio.TestTools.UnitTesting.TestContext""))
		{																						// 7
			return;																				// 8
		}
		string varName = string.Empty;
		string propName = property.Identifier.ToString();
		IEnumerable<SyntaxNode> propNodes = context.Node.DescendantNodes();
		IEnumerable<ReturnStatementSyntax> returnNodes = propNodes.OfType<ReturnStatementSyntax>();
		if (returnNodes.Any())
		{																						// 9
			ReturnStatementSyntax returnStatement = returnNodes.First();
			if (returnStatement != null)														// 10
			{																					// 11,12
				if (returnStatement.Expression is IdentifierNameSyntax returnVar)
				{																				// 13,14,15,16
					varName = returnVar.Identifier.ToString();
				}
			}
		}
		// find out if the property or its underlying variable is actually used
		foreach (IdentifierNameSyntax identifier in context.Node.Parent.DescendantNodes().OfType<IdentifierNameSyntax>())
		{																									// 17
			if ((identifier.Identifier.ToString() == propName) && (identifier.Parent != property) &&		// 18,19
				context.SemanticModel.GetSymbolInfo(identifier).Symbol is not ITypeSymbol)
			{																								// 20,21
				// if we find the same identifier as the propery and it's not a type or the original instance, it's used
				return;																						// 22
			}
			if ((identifier.Identifier.ToString() == varName)
				&& identifier.Parent is not VariableDeclarationSyntax										// 23
				&& !propNodes.Contains(identifier)															// 24,25
				&& context.SemanticModel.GetSymbolInfo(identifier).Symbol is not ITypeSymbol)				// 26
			{																								// 27,28
				// if we find the same identifier as the variable and it's not a type, the original declaration, or part of the property, it's used
				return;																						// 29,30
			}
		}
		// if not, report a diagnostic error
		Diagnostic diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation());
		context.ReportDiagnostic(diagnostic);
		return;																								// 31
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.ReduceCognitiveLoad, new Regex("Load of 31 ")));
		}

		[TestMethod]
		public void CognitiveLoad5()
		{
			const string template = @"
class Foo
{
	private void Test()
	{	//1
		SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);
		// find the underlying variable
		IEnumerable<SyntaxNode> propNodes = declaration.DescendantNodes();
		ReturnStatementSyntax returnStatement = propNodes.OfType<ReturnStatementSyntax>().First();
		string varName = string.Empty;
		if (returnStatement != null) //2
		{ //3
			if (returnStatement.Expression is IdentifierNameSyntax returnVar)
			{ //4,5
				varName = returnVar.Identifier.ToString();
			}
		}
		// remove the property
		if (rootNode != null) //6
		{ //7,8
			rootNode = rootNode.RemoveNode(declaration, SyntaxRemoveOptions.KeepNoTrivia);
			if (!string.IsNullOrEmpty(varName)) //9
			{ //10,11
				foreach (VariableDeclarationSyntax varDeclaration in rootNode.DescendantNodes()
					.OfType<VariableDeclarationSyntax>())
				{ // 12,13,14,15
					if (varDeclaration.Variables[0].Identifier.ToString() == varName)
					{ //16,17,18,19,20,21,22,23
						// remove the underlying variable
						if (varDeclaration.Parent != null) // 24
						{ 23+16 = 39
							rootNode = rootNode.RemoveNode(varDeclaration.Parent, SyntaxRemoveOptions.KeepNoTrivia);
						}
						break; //40
					}
				}
			}
			document = document.WithSyntaxRoot(rootNode);
		}
		return document; //41
	}
}";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.ReduceCognitiveLoad, new Regex("Load of 41 ")));
		}
	}

	[TestClass]
	public class ReduceCognitiveLoadAnalyzerInitializationTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			// Force the initialization logic to run, which will default to 25
			return new ReduceCognitiveLoadAnalyzer();
		}

		[TestMethod]
		public void CognitiveLoadInitPassTest()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
	}
}
";
			VerifyCSharpDiagnostic(template);
		}

		[TestMethod]
		public void CognitiveLoadInitFailTest()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {} // over the threshold
	}
}
";
			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.ReduceCognitiveLoad, new Regex("Load of 27 ")));
		}

	}

}
