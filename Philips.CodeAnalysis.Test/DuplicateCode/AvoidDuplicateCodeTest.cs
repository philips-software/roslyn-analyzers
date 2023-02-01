// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Philips.CodeAnalysis.DuplicateCodeAnalyzer;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.DuplicateCode
{
	[TestClass]
	public class AvoidDuplicateCodeAnalyzerTest : CodeFixVerifier
	{
		private const string allowedMethodName = @"Foo.AllowedInitializer()
Foo.AllowedInitializer(Bar)
Foo.WhitelistedFunction";

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidDuplicateCodeAnalyzer() { DefaultDuplicateTokenThreshold = 100 };
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AvoidDuplicateCodeFixProvider();
		}

		protected override Dictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			var options = new Dictionary<string, string>
			{
				{ $@"dotnet_code_quality.{ AvoidDuplicateCodeAnalyzer.Rule.Id }.token_count", @"20" }
			};
			return options;
		}

		protected override (string name, string content)[] GetAdditionalTexts()
		{
			return new[] { ("NotFile.txt", "data"), (AvoidDuplicateCodeAnalyzer.AllowedFileName, allowedMethodName) };
		}

		protected override void AssertFixAllProvider(FixAllProvider fixAllProvider)
		{
			Assert.IsTrue(fixAllProvider.GetSupportedFixAllScopes().Contains(FixAllScope.Project));
			Assert.IsTrue(fixAllProvider.GetSupportedFixAllScopes().Contains(FixAllScope.Document));
			Assert.IsTrue(fixAllProvider.GetSupportedFixAllScopes().Contains(FixAllScope.Solution));
		}

		public class SumHashCalculator : RollingHashCalculator<TokenInfo>
		{
			public SumHashCalculator(int maxItems)
				: base(maxItems)
			{ }

			protected override void CalcNewHashCode(TokenInfo hashComponent, bool isPurged, TokenInfo purgedHashComponent)
			{
				HashCode = 0;
				foreach (TokenInfo value in Components)
				{
					HashCode += value.GetHashCode();
				}
			}
		}

		[DataTestMethod]
		[DataRow(10)]
		[DataRow(20)]
		[DataRow(50)]
		[TestCategory(TestDefinitions.UnitTests)]
		public void RollingTokenSetCountTest(int duplicateTokenThreshold)
		{
			var rollingTokenSet = new RollingTokenSet(new SumHashCalculator(duplicateTokenThreshold));
			int hash = 0;

			for (int i = 1; i < duplicateTokenThreshold * 2; i++)
			{
				Mock<TokenInfo> mockToken = new(i);
				mockToken.Setup(x => x.GetLocationEnvelope()).Returns(new LocationEnvelope());
				mockToken.Setup(x => x.GetHashCode()).Returns(i);

				(hash, _) = rollingTokenSet.Add(mockToken.Object);

				static int Fib(int n)
				{
					return n == 0 ? 0 : n + Fib(n - 1);
				}

				if (i >= duplicateTokenThreshold)
				{
					Assert.IsTrue(rollingTokenSet.IsFull());
					// E.g., Threshold = 3, i = 7, then expectedValue = 7+6+5+4+3+2+1 - (4+3+2+1) =  7+6+5
					int expectedValue = Fib(i) - Fib(i - duplicateTokenThreshold);
					Assert.AreEqual(expectedValue, hash);
				}
				else
				{
					Assert.IsFalse(rollingTokenSet.IsFull());
					int expectedValue = Fib(i);
					Assert.AreEqual(expectedValue, hash);
				}
			}
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void RollingHashCalculator1Test()
		{
			var r = new RollingHashCalculator<int>(1, 256, 101);

			r.Add(7);
			Assert.AreEqual(7, r.HashCode);

			r.Add(17);
			Assert.AreEqual(17, r.HashCode);
		}


		// See https://en.wikipedia.org/wiki/Rabin–Karp_algorithm for "hi" example
		[DataTestMethod]
		[DataRow(256, 101, 104, 105, 65)]
		[TestCategory(TestDefinitions.UnitTests)]
		public void RollingHashCalculator2Test(int b, int m, int val1, int val2, int result)
		{
			var r = new RollingHashCalculator<int>(2, b, m);

			r.Add(val1);
			r.Add(val2);
			Assert.AreEqual(result, r.HashCode);

			// If we flush it other stuff, the hash should be something else
			r.Add(5);
			r.Add(120);
			Assert.AreNotEqual(result, r.HashCode);


			// If we flush "hi" back in, the hash should come back.
			r.Add(val1);
			r.Add(val2);
			Assert.AreEqual(result, r.HashCode);
		}

		// See https://en.wikipedia.org/wiki/Rabin–Karp_algorithm for "abracadabra" example
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void RollingHashCalculator3Test()
		{
			var r = new RollingHashCalculator<int>(3, 256, 101);

			//"abr"
			r.Add(97);
			r.Add(98);
			r.Add(114);
			Assert.AreEqual(4, r.HashCode);

			//"bra"
			r.Add(97);
			Assert.AreEqual(30, r.HashCode);

			// back to "abr"
			r.Add(98);
			r.Add(114);
			Assert.AreEqual(4, r.HashCode);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void SequenceEqualTest()
		{
			var r = new RollingHashCalculator<int>(4, 2048, 10007);

			r.Add(8);
			r.Add(316);
			r.Add(76);
			r.Add(130);

			var s = new RollingHashCalculator<int>(4, 2048, 10007);

			s.Add(8);
			s.Add(316);
			s.Add(76);
			s.Add(130);

			Assert.IsTrue(r.IsDuplicate(s));
			Assert.AreEqual(r.HashCode, s.HashCode);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void KnownCollisionTest()
		{
			var r = new RollingHashCalculator<int>(30, 2048, 10007);

			r.Add(8);
			r.Add(316);
			r.Add(76);
			r.Add(130);
			r.Add(9);
			r.Add(150);
			r.Add(162);
			r.Add(316);
			r.Add(8);
			r.Add(316);
			r.Add(8);
			r.Add(316);
			r.Add(9);
			r.Add(9);
			r.Add(20);
			r.Add(133);
			r.Add(8);
			r.Add(124);
			r.Add(26);
			r.Add(316);
			r.Add(8);
			r.Add(316);
			r.Add(9);
			r.Add(9);
			r.Add(150);
			r.Add(162);
			r.Add(316);
			r.Add(8);
			r.Add(316);
			r.Add(8);

			var s = new RollingHashCalculator<int>(30, 2048, 10007);

			s.Add(316);
			s.Add(170);
			s.Add(316);
			s.Add(9);
			s.Add(13);
			s.Add(133);
			s.Add(8);
			s.Add(316);
			s.Add(26);
			s.Add(316);
			s.Add(8);
			s.Add(316);
			s.Add(26);
			s.Add(316);
			s.Add(24);
			s.Add(169);
			s.Add(316);
			s.Add(316);
			s.Add(9);
			s.Add(9);
			s.Add(13);
			s.Add(133);
			s.Add(8);
			s.Add(2);
			s.Add(316);
			s.Add(26);
			s.Add(316);
			s.Add(8);
			s.Add(316);
			s.Add(26);

			Assert.IsFalse(r.IsDuplicate(s));
			Assert.AreEqual(r.HashCode, s.HashCode);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DuplicateDictionaryTest()
		{
			var dictionary = new DuplicateDetector();
			var e1 = new Evidence(null, new List<int>() { 10 }, 10);

			Evidence existing = dictionary.Register(1, e1);
			Assert.IsNull(existing);

			var e2 = new Evidence(null, new List<int>() { 20 }, 20);

			existing = dictionary.Register(2, e2);
			Assert.IsNull(existing);

			var e3 = new Evidence(null, new List<int>() { 30 }, 30);

			existing = dictionary.Register(2, e3);
			Assert.IsNull(existing);

			var e4 = new Evidence(null, new List<int>() { 30 }, 30);

			existing = dictionary.Register(2, e4);
			Assert.IsNotNull(existing);
		}

		[DataTestMethod]
		[DataRow("object obj = new object();", "Foo()")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidDuplicateCodeNoError(string method1, string method2)
		{
			var file = CreateFunctions(method1, method2);
			VerifySuccessfulCompilation(file);
		}

		[DataTestMethod]
		[DataRow("object obj = new object(); object obj2 = new object(); object obj3 = new object();", "object obj = new object(); object obj2 = new object(); object obj3 = new object();")]
		[DataRow("Bar(); object obj = new object(); object obj2 = new object(); object obj3 = new object();", "object obj = new object(); object obj2 = new object(); object obj3 = new object();")]
		[DataRow("object obj = new object(); object obj2 = new object(); object obj3 = new object();", "Bar(); object obj = new object(); object obj2 = new object(); object obj3 = new object();")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidDuplicateCodeError(string method1, string method2)
		{
			var file = CreateFunctions(method1, method2);
			VerifyFix(file, file);
			VerifyFixAll(file, file);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidDuplicateCodeErrorInSameMethod()
		{
			string baseline = @"
class Foo 
{{
  public void Foo()
  {{
	  configFuncs.Add(() => ConfigureItem(1, _pcProxAdapter.PcProxSetCfgFlagsFrcBitCntEx, DefaultForceBitCntEx, out isChanged));
	  configFuncs.Add(() => ConfigureItem(2, _pcProxAdapter.PcProxSetCfgFlagsSndOnRx, DefaultSendOnRx, out isChanged));
	  configFuncs.Add(() => ConfigureItem(3, _pcProxAdapter.PcProxSetCfgFlagsHaltKBSnd, DefaultHaltKBSend, out isChanged));
  }}
}}
";

			VerifyDiagnostic(baseline);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidDuplicateCodeNoErrorWhenOverlapping()
		{
			// The first two initializations are identical to the second two initializations, but "int b = 0" is overlapping.
			string baseline = @"
class Foo 
{{
  public void Foo()
  {{
	  int a = 0;
	  int b = 0;
	  int c = 0;
  }}
}}
";

			VerifySuccessfulCompilation(baseline);
		}



		private string CreateFunctions(string content1, string content2)
		{
			string baseline = @"
namespace MyNamespace
{{
  class FooClass
  {{
    public void Foo()
    {{
	  {0};
    }}
    public void Bar()
    {{
	  {1};
    }}
  }}
}}
";

			return string.Format(baseline, content1, content2);
		}


		private void VerifyDiagnostic(string file)
		{
			VerifyDiagnostic(file,
				new DiagnosticResult()
				{
					Id = AvoidDuplicateCodeAnalyzer.Rule.Id,
					Message = new Regex("Duplicate shape found.+"),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
						new DiagnosticResultLocation("Test0.cs", null, null),
						new DiagnosticResultLocation("Test0.cs", null, null),
					}
				}
			);
		}
	}
}
