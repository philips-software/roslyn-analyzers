// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// Class for testing AvoidStaticMethodAnalyzer
	/// </summary>
	[TestClass]
	public class AvoidStaticMethodAnalyzerTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidStaticMethodAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AvoidStaticMethodCodeFixProvider();
		}

		protected string CreateFunction(string methodStaticModifier, string classStaticModifier = "", string externKeyword = "", string methodName = "GoodTimes", string returnType = "void", string localMethodModifier = "", string foreignMethodModifier = "", bool isFactoryMethod = false)
		{
			if (!string.IsNullOrWhiteSpace(methodStaticModifier))
			{
				methodStaticModifier += @" ";
			}

			if (!string.IsNullOrWhiteSpace(externKeyword))
			{
				externKeyword += @" ";
			}

			var localMethod = $@"public {localMethodModifier} string BaBaBummmm(string services)
					{{
						return services;
					}}";

			var foreignMethod = $@"public {foreignMethodModifier} string BaBaBa(string services)
					{{
						return services;
					}}";

			var useLocalMethod = (localMethodModifier == "static") ? $@"BaBaBummmm(""testing"")" : string.Empty;
			var useForeignMethod = (foreignMethodModifier == "static") ? $@"BaBaBa(""testing"")" : string.Empty;

			var objectDeclaration = isFactoryMethod ? $@"Caroline caroline = new Caroline();" : string.Empty;

			return $@"
			namespace Sweet {{
				{classStaticModifier} class Caroline
				{{
					{localMethod}

					public {methodStaticModifier}{externKeyword}{returnType} {methodName}()
					{{
						{objectDeclaration}
						{useLocalMethod}
						{useForeignMethod}
						return;
					}}
				}}
				{foreignMethodModifier} class ReachingOut
				{{
					{foreignMethod}
				}}
			}}";
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AllowExternalCodeAsync()
		{
			var template = CreateFunction("static", externKeyword: "extern");
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task IgnoreIfInStaticClassAsync()
		{
			var template = CreateFunction("static", classStaticModifier: "static");
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task OnlyCatchStaticMethodsAsync()
		{
			var template = CreateFunction("");
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AllowStaticMainMethodAsync()
		{
			var template = CreateFunction("static", methodName: "Main");
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task IgnoreIfCallsLocalStaticMethodAsync()
		{
			var template = CreateFunction("static", localMethodModifier: "static");
			// should still catch the local static method being used
			await VerifyDiagnostic(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CatchIfUsesForeignStaticMethodAsync()
		{
			var template = CreateFunction("static", foreignMethodModifier: "static");
			await VerifyDiagnostic(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AllowStaticFactoryMethodAsync()
		{
			var template = CreateFunction("static", isFactoryMethod: true);
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AllowStaticDynamicDataMethodAsync()
		{
			var template = CreateFunction("static", returnType: "IEnumerable<object[]>");
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CatchPlainStaticMethod()
		{
			var template = CreateFunction("static");
			await VerifyDiagnostic(template).ConfigureAwait(false);

			var fixedCode = CreateFunction(@"");
			await VerifyFix(template, fixedCode).ConfigureAwait(false);
		}
	}
}
