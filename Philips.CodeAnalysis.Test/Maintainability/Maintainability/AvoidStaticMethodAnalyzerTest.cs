// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

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

		protected string CreateFunction(string methodStaticModifier, string classStaticModifier = "", string externKeyword = "", string methodName = "GoodTimes", string returnType = "void", string localMethodModifier = "", string foreignMethodModifier = "", bool factoryMethod = false)
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

			string objectDeclaration = factoryMethod ? $@"Caroline caroline = new Caroline();" : string.Empty;

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
		public void AllowExternalCode()
		{
			string template = CreateFunction("static", externKeyword: "extern");
			VerifyDiagnostic(template);
		}

		[TestMethod]
		public void IgnoreIfInStaticClass()
		{
			string template = CreateFunction("static", classStaticModifier: "static");
			VerifyDiagnostic(template);
		}

		[TestMethod]
		public void OnlyCatchStaticMethods()
		{
			string template = CreateFunction("");
			VerifyDiagnostic(template);
		}

		[TestMethod]
		public void AllowStaticMainMethod()
		{
			string template = CreateFunction("static", methodName: "Main");
			VerifyDiagnostic(template);
		}

		[TestMethod]
		public void IgnoreIfCallsLocalStaticMethod()
		{
			string template = CreateFunction("static", localMethodModifier: "static");
			// should still catch the local static method being used
			VerifyDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.AvoidStaticMethods));
		}

		[TestMethod]
		public void CatchIfUsesForeignStaticMethod()
		{
			string template = CreateFunction("static", foreignMethodModifier: "static");
			VerifyDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.AvoidStaticMethods));
		}

		[TestMethod]
		public void AllowStaticFactoryMethod()
		{
			string template = CreateFunction("static", factoryMethod: true);
			VerifyDiagnostic(template);
		}

		[TestMethod]
		public void AllowStaticDynamicDataMethod()
		{
			string template = CreateFunction("static", returnType: "IEnumerable<object[]>");
			VerifyDiagnostic(template);
		}

		[TestMethod]
		public void CatchPlainStaticMethod()
		{
			string template = CreateFunction("static");
			VerifyDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.AvoidStaticMethods));

			string fixedCode = CreateFunction(@"");
			VerifyFix(template, fixedCode);
		}
	}
}
