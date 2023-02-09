// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using Philips.CodeAnalysis.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Test.Verifiers;
using Philips.CodeAnalysis.Test.Helpers;
using System.Threading.Tasks;

namespace Philips.CodeAnalysis.Test.Common
{
	[TestClass]
	public class GeneratedCodeDetectorTest : DiagnosticVerifier
	{
		#region Helper Analyzer
		[DiagnosticAnalyzer(LanguageNames.CSharp)]
		private sealed class AvoidWritingCodeAnalyzer : SingleDiagnosticAnalyzer
		{
			public AvoidWritingCodeAnalyzer()
				: base(DiagnosticId.TestMethodName, @"Avoid writing code", @"Message Format", @"Description", Categories.Maintainability)
			{ }

			public static bool ShouldAnalyzeTree { get; set; }
			public static bool ShouldAnalyzeConstructor { get; set; }
			public static bool ShouldAnalyzeStruct { get; set; }
			public static bool ShouldAnalyzeSwitch { get; set; }

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

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidWritingCodeAnalyzer();
		}

		[DataRow(true, false, false, false)]
		[DataRow(false, true, false, false)]
		[DataRow(false, false, true, false)]
		[DataRow(false, false, false, true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NonGeneratedCodeIsFlaggedAsync(bool shouldAnalyzeTree, bool shouldAnalyzeStruct, bool shouldAnalyzeConstructor, bool shouldAnalyzeSwitch)
		{
			AvoidWritingCodeAnalyzer.ShouldAnalyzeTree = shouldAnalyzeTree;
			AvoidWritingCodeAnalyzer.ShouldAnalyzeStruct = shouldAnalyzeStruct;
			AvoidWritingCodeAnalyzer.ShouldAnalyzeConstructor = shouldAnalyzeConstructor;
			AvoidWritingCodeAnalyzer.ShouldAnalyzeSwitch = shouldAnalyzeSwitch;

			string input = @"
public class Foo
{
  public Foo()
  { }
}
public struct MyStruct {}
public void Method(int i) { switch(i) { default: break;} }
";
			await VerifyDiagnostic(input).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task GeneratedConstructorIsNotFlaggedAsync()
		{
			AvoidWritingCodeAnalyzer.ShouldAnalyzeConstructor = true;

			string input = @"
[System.CodeDom.Compiler.GeneratedCodeAttribute(""protoc"", null)]
public class Foo
{
  public Foo() { }
}
";
			await VerifySuccessfulCompilation(input).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task GeneratedStructIsNotFlaggedAsync()
		{
			AvoidWritingCodeAnalyzer.ShouldAnalyzeStruct = true;

			string input = @"
[System.CodeDom.Compiler.GeneratedCodeAttribute(""protoc"", null)]
public struct Foo { }
";
			await VerifySuccessfulCompilation(input).ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task GeneratedSwitchIsNotFlaggedAsync()
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

			await VerifySuccessfulCompilation(input).ConfigureAwait(false);
		}


		[DataRow(@"Foo.Designer", true, false, false)]
		[DataRow(@"Foo.designer", true, false, false)]
		[DataRow(@"Foo.g", true, false, false)]
		[DataRow(@"Foo.g", false, true, false)]
		[DataRow(@"Foo.g", false, false, true)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task GeneratedFilesNamesAreNotFlaggedAsync(string fileNamePrefix, bool shouldAnalyzeTree, bool shouldAnalyzeConstructor, bool shouldAnalyzeSwitch)
		{
			AvoidWritingCodeAnalyzer.ShouldAnalyzeTree = shouldAnalyzeTree;
			AvoidWritingCodeAnalyzer.ShouldAnalyzeConstructor = shouldAnalyzeConstructor;
			AvoidWritingCodeAnalyzer.ShouldAnalyzeSwitch = shouldAnalyzeSwitch;

			string input = @"public class Foo { public Foo(); public void Method(int i) { switch(i) { default: break;} } }";
			await VerifySuccessfulCompilation(input, fileNamePrefix).ConfigureAwait(false);
		}

		[DataRow(@"Foo")]
		[DataRow(@"Foo.xyz")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NonGeneratedFilesAreFlaggedAsync(string fileNamePrefix)
		{
			AvoidWritingCodeAnalyzer.ShouldAnalyzeTree = true;
			string input = @"public class Foo { }";
			await VerifyDiagnostic(input, DiagnosticId.TestMethodName, fileNamePrefix).ConfigureAwait(false);
		}
	}
}
