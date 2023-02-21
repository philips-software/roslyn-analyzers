// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	[TestClass]
	public class EnforceRegionsTest : DiagnosticVerifier
	{
		[DataTestMethod]
		[DataRow(@"public static void b() {{}}", false)]
		[DataRow(@"private static void b() {{}}", true)]
		[DataRow(@"protected void b() {{}}", true)]
		[DataRow(@"internal void b() {{}}", true)]
		[DataRow(@"protected internal void b() {{}}", true)]
		[DataRow(@"private protected void b() {{}}", true)]
		[DataRow(@"public string a;", false)]
		[DataRow(@"private string a;", true)]
		[DataRow(@"public int a;", false)]
		[DataRow(@"int a;", true)]
		[DataRow(@"private string a;", true)]
		[DataRow(@"public Foo(){{}}", false)]
		[DataRow(@"private Foo() {{}}", true)]
		[DataRow(@"Foo() {{}}", true)]
		[DataRow(@"public int A {{}}", false)]
		[DataRow(@"private int A {{}}", true)]
		[DataRow(@"public event A", false)]
		[DataRow(@"private event A", true)]
		[DataRow(@"event A", true)]
		[DataRow(@"public event EventHandler<StringEventArgs> RfidDataReceived = null;", false)]
		[DataRow(@"private event EventHandler<StringEventArgs> RfidDataReceived = null;", true)]
		[DataRow(@"event EventHandler<StringEventArgs> RfidDataReceived = null;", true)]
		[DataRow(@"struct A {}", false)]
		[DataRow(@"public bool operator == (x, y) {{}}", false)]
		[DataRow(@"private bool operator == (x, y) {{}}", true)]
		[DataRow(@"bool operator == (x, y) {{}}", true)]
		[DataRow(@"public TValue this[Tkey key] {{ get {{}}; set{{}};}}", false)]
		[DataRow(@"private TValue this[Tkey key] {{ get {{}}; set{{}};}}", true)]
		[DataRow(@"TValue this[Tkey key] {{ get {{}}; set{{}};}}", true)]
		[DataRow(@"~Regis() {{}}", true)]
		[DataRow(@"public ~Regis() {{}}", false)]
		[DataRow(@"private ~Regis() {{}}", true)]
		[DataRow(@"enum A {{}}", true)]
		[DataRow(@"private enum A {{}}", true)]
		[DataRow(@"public enum A {{}}", false)]
		[DataRow(@"delegate int();", true)]
		[DataRow(@"private delegate int();", true)]
		[DataRow(@"public delegate int();", false)]
		[DataRow(@"public class A {{}}", false)]
		[DataRow(@"class A {{}}", true)]
		[DataRow(@"private class A {{}}", true)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EnforcePublicInterfaceRegionAsync(string given, bool isError)
		{

			var baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo
{{
	#region Public Interface
	{0}
	#endregion
}}";
			await VerifyErrorAsync(baseline, given, isError, 6, 2).ConfigureAwait(false);

		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task UnnamedRegionTestAsync()
		{
			var baseline = @"
class Foo
{{
	#region
	private class A {{}}
	#endregion
}}";
			await VerifySuccessfulCompilation(baseline).ConfigureAwait(false);

		}

		[DataTestMethod]
		[DataRow(@"public static void b() {{}}", true)]
		[DataRow(@"private static void b() {{}}", false)]
		[DataRow(@"protected void b() {{}}", false)]
		[DataRow(@"internal void b() {{}}", false)]
		[DataRow(@"protected internal void b() {{}}", false)]
		[DataRow(@"private protected void b() {{}}", false)]
		[DataRow(@"public string a;", true)]
		[DataRow(@"private string a;", true)]
		[DataRow(@"public int a;", true)]
		[DataRow(@"int a;", true)]
		[DataRow(@"private string a;", true)]
		[DataRow(@"public Foo() {{}}", true)]
		[DataRow(@"Foo() {{}}", false)]
		[DataRow(@"private Foo() {{}}", false)]
		[DataRow(@"public int A {{}}", true)]
		[DataRow(@"private int A {{}}", false)]
		[DataRow(@"public event A", true)]
		[DataRow(@"private event A", false)]
		[DataRow(@"event A", false)]
		[DataRow(@"public event EventHandler<StringEventArgs> RfidDataReceived = null;", true)]
		[DataRow(@"private event EventHandler<StringEventArgs> RfidDataReceived = null;", false)]
		[DataRow(@"event EventHandler<StringEventArgs> RfidDataReceived = null;", false)]
		[DataRow(@"struct A {}", false)]
		[DataRow(@"public bool operator == (x, y) {{}}", true)]
		[DataRow(@"private bool operator == (x, y) {{}}", false)]
		[DataRow(@"bool operator == (x, y) {{}}", false)]
		[DataRow(@"public class A {{}}", true)]
		[DataRow(@"private class A {{}}", false)]
		[DataRow(@"class A {{}}", false)]
		[DataRow(@"public TValue this[Tkey key] {{ get {{}}; set{{}};}}", true)]
		[DataRow(@"private TValue this[Tkey key] {{ get {{}}; set{{}};}}", false)]
		[DataRow(@"TValue this[Tkey key] {{ get {{}}; set{{}};}}", false)]
		[DataRow(@"~Regis() {{}}", false)]
		[DataRow(@"public ~Regis() {{}}", true)]
		[DataRow(@"private ~Regis() {{}}", false)]
		[DataRow(@"enum A {{}}", true)]
		[DataRow(@"private enum A {{}}", true)]
		[DataRow(@"public enum A {{}}", true)]
		[DataRow(@"delegate int();", true)]
		[DataRow(@"private delegate int();", true)]
		[DataRow(@"public delegate int();", true)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EnforeNonPublicPropertiesMethodsRegionAsync(string given, bool isError)
		{
			var baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo
{{
	#region Non-Public Properties/Methods
	{0}
	#endregion
}}";
			await VerifyErrorAsync(baseline, given, isError, 6, 2).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow(@"public  static int a;", true)]
		[DataRow(@"private  static int a;", false)]
		[DataRow(@"protected int a", false)]
		[DataRow(@"internal int a", false)]
		[DataRow(@"int a;", false)]
		[DataRow(@"protected internal int a", false)]
		[DataRow(@"private protected int a", false)]
		[DataRow(@"private void a() {{}}", true)]
		[DataRow(@"public void a() {{}}", true)]
		[DataRow(@"public Foo(){{}}", true)]
		[DataRow(@"private Foo(){{}}", true)]
		[DataRow(@"Foo(){{}}", true)]
		[DataRow(@"public int A {{}}", true)]
		[DataRow(@"private int A {{}}", true)]
		[DataRow(@"public event A", true)]
		[DataRow(@"private event A", true)]
		[DataRow(@"event A", true)]
		[DataRow(@"public event EventHandler<StringEventArgs> RfidDataReceived = null;", true)]
		[DataRow(@"private event EventHandler<StringEventArgs> RfidDataReceived = null;", true)]
		[DataRow(@"struct A {}", false)]
		[DataRow(@"public bool operator == (x, y) {{}}", true)]
		[DataRow(@"private bool operator == (x, y) {{}}", true)]
		[DataRow(@"bool operator == (x, y) {{}}", true)]
		[DataRow(@"public class A {{}}", true)]
		[DataRow(@"private class A {{}}", true)]
		[DataRow(@"class A {{}}", true)]
		[DataRow(@"public TValue this[Tkey key] {{ get {{}}; set{{}};}}", true)]
		[DataRow(@"private TValue this[Tkey key] {{ get {{}}; set{{}};}}", true)]
		[DataRow(@"TValue this[Tkey key] {{ get {{}}; set{{}};}}", true)]
		[DataRow(@"~Regis() {{}}", true)]
		[DataRow(@"public ~Regis() {{}}", true)]
		[DataRow(@"private ~Regis() {{}}", true)]
		[DataRow(@"enum A {{}}", false)]
		[DataRow(@"private enum A {{}}", false)]
		[DataRow(@"public enum A {{}}", true)]
		[DataRow(@"delegate int();", false)]
		[DataRow(@"private delegate int();", false)]
		[DataRow(@"public delegate int();", true)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EnforeNonPublicDataMembersRegionAsync(string given, bool isError)
		{
			var baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo
{{
	#region Non-Public Data Members
	{0}
	#endregion
}}";
			await VerifyErrorAsync(baseline, given, isError, 6, 2).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow(@"#region Constants", false)]
		[DataRow(@"#region Public Properties/Methods", false)]
		[DataRow(@"#region Non-Public Properties/Methods", true)]
		[DataRow(@"#region Public Interface", true)]
		[DataRow(@"Public Interface", false)]
		[DataRow(@"#region", false)]
		[DataRow(@"#regionPublic Interface", false)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task RegionNameTestAsync(string given, bool isError)
		{
			var baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo
{{
	{0}
	private int asd;
	#endregion


}}";
			await VerifyErrorAsync(baseline, given, isError, 6, 2).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow(@"Non-Public Properties/Methods")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DupliateRegionTestAsync(string given)
		{
			var baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo
{{
	#region Non-Public Properties/Methods
	public int a;
	#endregion
	#region {0}
	public int a;
	#endregion
}}
class Foo
{{
	#region {0}

}}";
			var givenText = string.Format(baseline, given);
			await VerifyDiagnostic(givenText, DiagnosticId.EnforceNonDuplicateRegion, regex: EnforceRegionsAnalyzer.EnforceNonDuplicateRegionMessageFormat, line: 8, column: 3).ConfigureAwait(false);
		}

		private async Task VerifyErrorAsync(string baseline, string given, bool isError, int line = 6, int column = 2)
		{
			var givenText = string.Format(baseline, given);
			if (isError)
			{
				await VerifyDiagnostic(givenText, DiagnosticId.EnforceRegions, regex: EnforceRegionsAnalyzer.EnforceRegionMessageFormat, line: line, column: column).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
			}
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new EnforceRegionsAnalyzer();
		}
	}
}
