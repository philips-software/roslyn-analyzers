// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using Philips.CodeAnalysis.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Philips.CodeAnalysis.Test.Common
{
	[TestClass]
	public class GeneratedCodeDetectorTest : DiagnosticVerifier
	{
		#region Helper Analyzer
		[DiagnosticAnalyzer(LanguageNames.CSharp)]
		private class AvoidWritingCodeAnalyzer : DiagnosticAnalyzer
		{
			public static bool ShouldAnalyzeTree { get; set; } = false;
			public static bool ShouldAnalyzeConstructor { get; set; } = false;
			public static bool ShouldAnalyzeStruct { get; set; } = false;
			public static bool ShouldAnalyzeSwitch { get; set; } = false;

			private const string Title = @"Avoid writing code";
			public DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.TestMethodName), Title, Title, Categories.Maintainability, DiagnosticSeverity.Error, true, Title);
			public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

			public override void Initialize(AnalysisContext context)
			{
				// By changing the following line to GeneratedCodeAnalysisFlags.None, and then enabling code coverage,
				// we can see it's impossible to cover the HasAttribute() invocation within HasGeneratedCodeAttribute.
				// By extension, HasGeneratedCodeAttribute can only return false, and should be able to be safely removed,
				// thereby simplifying GeneratedCodeDetector to only need to check magic file extensions.
				context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
				context.EnableConcurrentExecution();
				context.RegisterSyntaxNodeAction(AnalyzeConstructor, SyntaxKind.ConstructorDeclaration);
				context.RegisterSyntaxNodeAction(AnalyzeStruct, SyntaxKind.StructDeclaration);
				context.RegisterOperationAction(AnalyzeSwitch, OperationKind.Switch);
				context.RegisterSyntaxTreeAction(AnalyzeTree);
			}

			private void AnalyzeTree(SyntaxTreeAnalysisContext context)
			{
				if (ShouldAnalyzeTree)
				{
					GeneratedCodeDetector generatedCodeDetector = new();
					if (generatedCodeDetector.IsGeneratedCode(context))
					{
						return;
					}
					context.ReportDiagnostic(Diagnostic.Create(Rule, null));
				}
			}

			private void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
			{
				if (ShouldAnalyzeConstructor)
				{
					GeneratedCodeDetector generatedCodeDetector = new();
					if (generatedCodeDetector.IsGeneratedCode(context))
					{
						return;
					}
					context.ReportDiagnostic(Diagnostic.Create(Rule, null));
				}
			}

			private void AnalyzeStruct(SyntaxNodeAnalysisContext context)
			{
				if (ShouldAnalyzeStruct)
				{
					GeneratedCodeDetector generatedCodeDetector = new();
					if (generatedCodeDetector.IsGeneratedCode(context))
					{
						return;
					}
					context.ReportDiagnostic(Diagnostic.Create(Rule, null));
				}
			}

			private void AnalyzeSwitch(OperationAnalysisContext context)
			{
				if (ShouldAnalyzeSwitch)
				{
					GeneratedCodeDetector generatedCodeDetector = new();
					if (generatedCodeDetector.IsGeneratedCode(context))
					{
						return;
					}
					context.ReportDiagnostic(Diagnostic.Create(Rule, null));
				}
			}
		}
		#endregion Helper Analyzer

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new AvoidWritingCodeAnalyzer();
		}

		[DataRow(true, false, false, false)]
		[DataRow(false, true, false, false)]
		[DataRow(false, false, true, false)]
		[DataRow(false, false, false, true)]
		[DataTestMethod]
		public void NonGeneratedCodeIsFlagged(bool tree, bool structStatement, bool constructor, bool switchStatement)
		{
			AvoidWritingCodeAnalyzer.ShouldAnalyzeTree = tree;
			AvoidWritingCodeAnalyzer.ShouldAnalyzeStruct = structStatement;
			AvoidWritingCodeAnalyzer.ShouldAnalyzeConstructor = constructor;
			AvoidWritingCodeAnalyzer.ShouldAnalyzeSwitch = switchStatement;

			string input = @"
public class Foo
{
  public Foo()
  { }
}
public struct MyStruct {}
public void Method(int i) { switch(i) { default: break;} }
";
			DiagnosticResult[] expected = new[] { DiagnosticResultHelper.Create(DiagnosticIds.TestMethodName) };
			VerifyCSharpDiagnostic(input, expected);
		}

		[TestMethod]
		public void GeneratedConstructorIsNotFlagged()
		{
			AvoidWritingCodeAnalyzer.ShouldAnalyzeConstructor = true;

			string input = @"
[System.CodeDom.Compiler.GeneratedCodeAttribute(""protoc"", null)]
public class Foo
{
  public Foo() { }
}
";
			VerifyCSharpDiagnostic(input);
		}

		[TestMethod]
		public void GeneratedStructIsNotFlagged()
		{
			AvoidWritingCodeAnalyzer.ShouldAnalyzeStruct = true;

			string input = @"
[System.CodeDom.Compiler.GeneratedCodeAttribute(""protoc"", null)]
public struct Foo { }
";
			VerifyCSharpDiagnostic(input);
		}


		[TestMethod]
		public void GeneratedSwitchIsNotFlagged()
		{
			AvoidWritingCodeAnalyzer.ShouldAnalyzeSwitch = true;

			string input = @"
public class Foo
{
  [global::System.CodeDom.Compiler.GeneratedCodeAttribute(""protoc"", null)]
  public void Method(int data)
  {
    switch(data)
    {
      default:
        System.Console.WriteLine(data);
        break;
    }
  }
}
";

			VerifyCSharpDiagnostic(input);
		}


		[DataRow(@"Foo.Designer", true, false, false)]
		[DataRow(@"Foo.designer", true, false, false)]
		[DataRow(@"Foo.g", true, false, false)]
		[DataRow(@"Foo.g", false, true, false)]
		[DataRow(@"Foo.g", false, false, true)]
		[DataTestMethod]
		public void GeneratedFilesNamesAreNotFlagged(string fileNamePrefix, bool tree, bool constructor, bool switchStatement)
		{
			AvoidWritingCodeAnalyzer.ShouldAnalyzeTree = tree;
			AvoidWritingCodeAnalyzer.ShouldAnalyzeConstructor = constructor;
			AvoidWritingCodeAnalyzer.ShouldAnalyzeSwitch = switchStatement;

			string input = @"public class Foo { public Foo(); public void Method(int i) { switch(i) { default: break;} } }";
			DiagnosticResult[] expected = Array.Empty<DiagnosticResult>();
			VerifyCSharpDiagnostic(input, fileNamePrefix, expected);
		}

		[DataRow(@"Foo")]
		[DataRow(@"Foo.xyz")]
		[DataTestMethod]

		public void NonGeneratedFilesAreFlagged(string fileNamePrefix)
		{
			AvoidWritingCodeAnalyzer.ShouldAnalyzeTree = true;
			string input = @"public class Foo { }";
			DiagnosticResult[] expected = new[] { DiagnosticResultHelper.Create(DiagnosticIds.TestMethodName) };
			VerifyCSharpDiagnostic(input, fileNamePrefix, expected);
		}
	}
}
