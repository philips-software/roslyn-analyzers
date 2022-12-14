// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidStaticClassAnalyzerTest2 : AvoidStaticClassAnalyzerTest
	{
		private readonly Mock<AvoidStaticClassesAnalyzer> _mock = new() { CallBase = true };

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return _mock.Object;
		}

		[TestMethod]
		public void AvoidStaticClassesShouldWhitelistTest()
		{
			HashSet<string> exceptions = new()
			{
				KnownWhitelistClassNamespace + "." + KnownWhitelistClassClassName
			};
			_mock.Setup(c => c.CreateCompilationAnalyzer(It.IsAny<HashSet<string>>(), It.IsAny<bool>())).Returns(new AvoidStaticClassesCompilationAnalyzer(exceptions, false));
			VerifyNoDiagnostic(CreateFunction("static", KnownWhitelistClassNamespace, KnownWhitelistClassClassName));
		}
	}

	/// <summary>
	/// 
	/// </summary>
	[TestClass]
	public class AvoidStaticClassAnalyzerTest : DiagnosticVerifier
	{
		public const string KnownWhitelistClassNamespace = "Philips.Monitoring.Common";
		public const string KnownWhitelistClassClassName = "SerializationHelper";
		public const string KnownWildcardClassName = "AssemblyInitialize";
		public const string AnotherKnownWildcardClassName = "Program";

		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new AvoidStaticClassesAnalyzer();
		}

		protected string CreateFunction(string staticModifier, string nameSpace = "Sweet", string className = "Caroline", bool isExtension = false, bool hasNonExtensionMethods = true)
		{
			string extensionMethod = isExtension ? $@"public {staticModifier} IServiceCollection BaBaBummmm(this IServiceCollection services)
					{{
						return services;
					}}" : string.Empty;

			string method = hasNonExtensionMethods ? $@"public {staticModifier} IServiceCollection BaBaBa(IServiceCollection services)
					{{
						return services;
					}}" : string.Empty;

			return $@"
			namespace {nameSpace} {{
				{staticModifier} class {className}
				{{
					{extensionMethod}
					{method}

					private {staticModifier} void SomethingAboutTheGoodTimes()
					{{
						return;
					}}
				}}
			}}";
		}

		#endregion

		#region Public Interface

		[TestMethod]
		public void AvoidStaticClassesTest()
		{
			VerifyDiagnostic(CreateFunction("static"));
		}


		[TestMethod]
		public void AvoidStaticClassesShouldNotWhitelistWhenNamespaceUnmatchedTest()
		{
			VerifyDiagnostic(CreateFunction("static", "IAmSooooooNotWhitelisted", KnownWhitelistClassClassName));
		}

		[TestMethod]
		public void AvoidStaticClassesShouldWhitelistWildCardClassTest()
		{
			VerifyNoDiagnostic(CreateFunction("static", "IAmSooooooNotWhitelisted", KnownWildcardClassName));
			VerifyNoDiagnostic(CreateFunction("static", "IAmSooooooNotWhitelisted", AnotherKnownWildcardClassName));
		}

		[TestMethod]
		public void AvoidStaticClassesShouldWhitelistExtensionClasses()
		{
			VerifyNoDiagnostic(CreateFunction("static", isExtension: true, hasNonExtensionMethods: false));
			VerifyDiagnostic(CreateFunction("static", isExtension: true));
		}

		[TestMethod]
		public void AvoidNoStaticClassesTest()
		{
			VerifyNoDiagnostic(CreateFunction(""));
		}


		protected void VerifyNoDiagnostic(string file)
		{
			VerifyCSharpDiagnostic(file);
		}

		private void VerifyDiagnostic(string file)
		{
			VerifyCSharpDiagnostic(file, new DiagnosticResult()
			{
				Id = AvoidStaticClassesAnalyzer.Rule.Id,
				Message = new Regex(".+"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 3, -1),
				}
			});
		}

		#endregion
	}
}
