// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

		protected override ImmutableDictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			return base.GetAdditionalAnalyzerConfigOptions().Add($@"dotnet_code_quality.{AvoidDuplicateCodeAnalyzer.Rule.Id}.token_count", @"20");
		}

		protected override ImmutableArray<(string name, string content)> GetAdditionalTexts()
		{
			return base.GetAdditionalTexts().Add(("NotFile.txt", "data")).Add((AvoidDuplicateCodeAnalyzer.AllowedFileName, allowedMethodName));
		}

		protected override void AssertFixAllProvider(FixAllProvider fixAllProvider)
		{
			Assert.IsTrue(fixAllProvider.GetSupportedFixAllScopes().Contains(FixAllScope.Project));
			Assert.IsTrue(fixAllProvider.GetSupportedFixAllScopes().Contains(FixAllScope.Document));
			Assert.IsTrue(fixAllProvider.GetSupportedFixAllScopes().Contains(FixAllScope.Solution));
		}

		internal sealed class SumHashCalculator : RollingHashCalculator<TokenInfo>
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

		[TestMethod]
		[DataRow(10)]
		[DataRow(20)]
		[DataRow(50)]
		[TestCategory(TestDefinitions.UnitTests)]
		public void RollingTokenSetCountTest(int duplicateTokenThreshold)
		{
			var rollingTokenSet = new RollingTokenSet(new SumHashCalculator(duplicateTokenThreshold));
			var hash = 0;

			for (var i = 1; i < duplicateTokenThreshold * 2; i++)
			{
				Mock<TokenInfo> mockToken = new(i);
				_ = mockToken.Setup(x => x.GetLocationEnvelope()).Returns(new LocationEnvelope());
				_ = mockToken.Setup(x => x.GetHashCode()).Returns(i);

				(hash, _) = rollingTokenSet.Add(mockToken.Object);

				static int Fib(int n)
				{
					return n == 0 ? 0 : n + Fib(n - 1);
				}

				if (i >= duplicateTokenThreshold)
				{
					Assert.IsTrue(rollingTokenSet.IsFull());
					// E.g., Threshold = 3, i = 7, then expectedValue = 7+6+5+4+3+2+1 - (4+3+2+1) =  7+6+5
					var expectedValue = Fib(i) - Fib(i - duplicateTokenThreshold);
					Assert.AreEqual(expectedValue, hash);
				}
				else
				{
					Assert.IsFalse(rollingTokenSet.IsFull());
					var expectedValue = Fib(i);
					Assert.AreEqual(expectedValue, hash);
				}
			}
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void RollingHashCalculator1Test()
		{
			var r = new RollingHashCalculator<int>(1, 256, 101);

			_ = r.Add(7);
			Assert.AreEqual(7, r.HashCode);

			_ = r.Add(17);
			Assert.AreEqual(17, r.HashCode);
		}


		// See https://en.wikipedia.org/wiki/Rabin–Karp_algorithm for "hi" example
		[TestMethod]
		[DataRow(256, 101, 104, 105, 65)]
		[TestCategory(TestDefinitions.UnitTests)]
		public void RollingHashCalculator2Test(int b, int m, int val1, int val2, int result)
		{
			var r = new RollingHashCalculator<int>(2, b, m);

			_ = r.Add(val1);
			_ = r.Add(val2);
			Assert.AreEqual(result, r.HashCode);

			// If we flush it other stuff, the hash should be something else
			_ = r.Add(5);
			_ = r.Add(120);
			Assert.AreNotEqual(result, r.HashCode);


			// If we flush "hi" back in, the hash should come back.
			_ = r.Add(val1);
			_ = r.Add(val2);
			Assert.AreEqual(result, r.HashCode);
		}

		// See https://en.wikipedia.org/wiki/Rabin–Karp_algorithm for "abracadabra" example
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void RollingHashCalculator3Test()
		{
			var r = new RollingHashCalculator<int>(3, 256, 101);

			//"abr"
			_ = r.Add(97);
			_ = r.Add(98);
			_ = r.Add(114);
			Assert.AreEqual(4, r.HashCode);

			//"bra"
			_ = r.Add(97);
			Assert.AreEqual(30, r.HashCode);

			// back to "abr"
			_ = r.Add(98);
			_ = r.Add(114);
			Assert.AreEqual(4, r.HashCode);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void SequenceEqualTest()
		{
			var r = new RollingHashCalculator<int>(4, 2048, 10007);

			_ = r.Add(8);
			_ = r.Add(316);
			_ = r.Add(76);
			_ = r.Add(130);

			var s = new RollingHashCalculator<int>(4, 2048, 10007);

			_ = s.Add(8);
			_ = s.Add(316);
			_ = s.Add(76);
			_ = s.Add(130);

			Assert.IsTrue(r.IsDuplicate(s));
			Assert.AreEqual(r.HashCode, s.HashCode);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void KnownCollisionTest()
		{
			var r = new RollingHashCalculator<int>(30, 2048, 10007);

			_ = r.Add(8);
			_ = r.Add(316);
			_ = r.Add(76);
			_ = r.Add(130);
			_ = r.Add(9);
			_ = r.Add(150);
			_ = r.Add(162);
			_ = r.Add(316);
			_ = r.Add(8);
			_ = r.Add(316);
			_ = r.Add(8);
			_ = r.Add(316);
			_ = r.Add(9);
			_ = r.Add(9);
			_ = r.Add(20);
			_ = r.Add(133);
			_ = r.Add(8);
			_ = r.Add(124);
			_ = r.Add(26);
			_ = r.Add(316);
			_ = r.Add(8);
			_ = r.Add(316);
			_ = r.Add(9);
			_ = r.Add(9);
			_ = r.Add(150);
			_ = r.Add(162);
			_ = r.Add(316);
			_ = r.Add(8);
			_ = r.Add(316);
			_ = r.Add(8);

			var s = new RollingHashCalculator<int>(30, 2048, 10007);

			_ = s.Add(316);
			_ = s.Add(170);
			_ = s.Add(316);
			_ = s.Add(9);
			_ = s.Add(13);
			_ = s.Add(133);
			_ = s.Add(8);
			_ = s.Add(316);
			_ = s.Add(26);
			_ = s.Add(316);
			_ = s.Add(8);
			_ = s.Add(316);
			_ = s.Add(26);
			_ = s.Add(316);
			_ = s.Add(24);
			_ = s.Add(169);
			_ = s.Add(316);
			_ = s.Add(316);
			_ = s.Add(9);
			_ = s.Add(9);
			_ = s.Add(13);
			_ = s.Add(133);
			_ = s.Add(8);
			_ = s.Add(2);
			_ = s.Add(316);
			_ = s.Add(26);
			_ = s.Add(316);
			_ = s.Add(8);
			_ = s.Add(316);
			_ = s.Add(26);

			Assert.IsFalse(r.IsDuplicate(s));
			Assert.AreEqual(r.HashCode, s.HashCode);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void EvidenceLazyEvaluationTest()
		{
			// Test that Evidence lazy evaluation works correctly
			var componentsCalled = false;
			var components = new List<int> { 10, 20, 30 };

			List<int> LazyComponents()
			{
				componentsCalled = true;
				return components;
			}

			var evidence = new Evidence(null, LazyComponents, 60);

			// Components should not be called yet
			Assert.IsFalse(componentsCalled, "Components should not be evaluated eagerly");

			// Create another evidence for comparison
			var evidence2 = new Evidence(null, () => new List<int> { 10, 20, 30 }, 60);

			// Now call IsDuplicate which should trigger lazy evaluation
			var isDuplicate = evidence.IsDuplicate(evidence2);

			// Components should now be called
			Assert.IsTrue(componentsCalled, "Components should be evaluated when needed");
			Assert.IsTrue(isDuplicate, "Evidence objects with same components should be duplicates");
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DuplicateDictionaryTest()
		{
			var dictionary = new DuplicateDetector();
			var e1 = new Evidence(null, () => [10], 10);

			Evidence existing = dictionary.Register(1, e1);
			Assert.IsNull(existing);

			var e2 = new Evidence(null, () => [20], 20);

			existing = dictionary.Register(2, e2);
			Assert.IsNull(existing);

			var e3 = new Evidence(null, () => [30], 30);

			existing = dictionary.Register(2, e3);
			Assert.IsNull(existing);

			var e4 = new Evidence(null, () => [30], 30);

			existing = dictionary.Register(2, e4);
			Assert.IsNotNull(existing);
		}

		[TestMethod]
		[DataRow("object obj = new object();", "Foo()")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidDuplicateCodeNoErrorAsync(string method1, string method2)
		{
			var file = CreateFunctions(method1, method2);
			await VerifySuccessfulCompilation(file).ConfigureAwait(false);
		}

		[TestMethod]
		[DataRow("object obj = new object(); object obj2 = new object(); object obj3 = new object();", "object obj = new object(); object obj2 = new object(); object obj3 = new object();")]
		[DataRow("Bar(); object obj = new object(); object obj2 = new object(); object obj3 = new object();", "object obj = new object(); object obj2 = new object(); object obj3 = new object();")]
		[DataRow("object obj = new object(); object obj2 = new object(); object obj3 = new object();", "Bar(); object obj = new object(); object obj2 = new object(); object obj3 = new object();")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidDuplicateCodeError(string method1, string method2)
		{
			var file = CreateFunctions(method1, method2);
			await VerifyFix(file, file).ConfigureAwait(false);
			await VerifyFixAll(file, file).ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidDuplicateCodeErrorInSameMethodAsync()
		{
			var baseline = @"
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

			await VerifyAsync(baseline).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidDuplicateCodeNoErrorWhenOverlappingAsync()
		{
			// The first two initializations are identical to the second two initializations, but "int b = 0" is overlapping.
			var baseline = @"
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

			await VerifySuccessfulCompilation(baseline).ConfigureAwait(false);
		}



		private string CreateFunctions(string content1, string content2)
		{
			var baseline = @"
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


		private async Task VerifyAsync(string file)
		{
			await VerifyDiagnostic(file,
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
			).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TestDogfoodFalsePositivesAsync()
		{
			// Test the specific code patterns that were flagged as false positives in the dogfood build
			var baseline = @"
using System;
using System.Linq;
using System.Collections.Generic;

class TestClass 
{
	public void TestMethod1()
	{
		// Similar to DiagnosticVerifier.Helper.cs pattern around line 321-335
		var trustedAssembliesPaths = new string[] { ""test1"", ""test2"" };
		var neededAssemblies = new[] { ""System.Runtime"", ""mscorlib"" };
		
		foreach (var references in trustedAssembliesPaths.Where(p => neededAssemblies.Contains(p)))
		{
			Console.WriteLine(references);
		}
		
		var count = 0;
		var data = new[] { ""test"" }.Select(x =>
		{
			var newFileName = string.Format(""{0}{1}.{2}"", ""prefix"", count == 0 ? string.Empty : count.ToString(), ""ext"");
			count++;
			return newFileName;
		});
	}
	
	public void TestMethod2()
	{
		// Similar to CodeFixVerifier.cs pattern around line 99-129
		var actions = new List<object>();
		var analyzerDiagnostics = new[] { ""diagnostic1"" };
		var firstDiagnostic = analyzerDiagnostics.First();
		
		if (actions.Count == 0)
		{
			return;
		}
		
		if (true) // Similar to scope == FixAllScope.Custom
		{
			var document = ""test"";
		}
		else
		{
			var document = ""test2"";
		}
		
		var newDiagnostics = new object[0];
		var newCompilerDiagnostics = new object[0];
	}
}
";

			// This should NOT show any duplicates - if it does, we have a false positive
			await VerifySuccessfulCompilation(baseline).ConfigureAwait(false);
		}
	}
}
