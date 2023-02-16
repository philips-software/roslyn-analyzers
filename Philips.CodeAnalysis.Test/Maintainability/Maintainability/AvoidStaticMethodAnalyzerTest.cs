// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
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

			string localMethod = $@"public {localMethodModifier} string BaBaBummmm(string services)
					{{
						return services;
					}}";

			string foreignMethod = $@"public {foreignMethodModifier} string BaBaBa(string services)
					{{
						return services;
					}}";

			string useLocalMethod = (localMethodModifier == "static") ? $@"BaBaBummmm(""testing"")" : string.Empty;
			string useForeignMethod = (foreignMethodModifier == "static") ? $@"BaBaBa(""testing"")" : string.Empty;

			string objectDeclaration = isFactoryMethod ? $@"Caroline caroline = new Caroline();" : string.Empty;

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
			string template = CreateFunction("static", externKeyword: "extern");
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task IgnoreIfInStaticClassAsync()
		{
			string template = CreateFunction("static", classStaticModifier: "static");
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task OnlyCatchStaticMethodsAsync()
		{
			string template = CreateFunction("");
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AllowStaticMainMethodAsync()
		{
			string template = CreateFunction("static", methodName: "Main");
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task IgnoreIfCallsLocalStaticMethodAsync()
		{
			string template = CreateFunction("static", localMethodModifier: "static");
			// should still catch the local static method being used
			await VerifyDiagnostic(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CatchIfUsesForeignStaticMethodAsync()
		{
			string template = CreateFunction("static", foreignMethodModifier: "static");
			await VerifyDiagnostic(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AllowStaticFactoryMethodAsync()
		{
			string template = CreateFunction("static", isFactoryMethod: true);
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AllowStaticDynamicDataMethodAsync()
		{
			string template = CreateFunction("static", returnType: "IEnumerable<object[]>");
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CatchPlainStaticMethod()
		{
			string template = CreateFunction("static");
			await VerifyDiagnostic(template).ConfigureAwait(false);

			string fixedCode = CreateFunction(@"");
			await VerifyFix(template, fixedCode).ConfigureAwait(false);
		}
	}
}
