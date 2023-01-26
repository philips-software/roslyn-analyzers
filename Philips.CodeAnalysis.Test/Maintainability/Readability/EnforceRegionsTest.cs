// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	[TestClass]
	public class EnforceRegionsTest : DiagnosticVerifier
	{
		[DataTestMethod]
		[DataRow(@"public static void b() {{}}", false, 6, 2)]
		[DataRow(@"private static void b() {{}}", true, 6, 22)]
		[DataRow(@"protected void b() {{}}", true, 6, 17)]
		[DataRow(@"internal void b() {{}}", true, 6, 16)]
		[DataRow(@"protected internal void b() {{}}", true, 6, 26)]
		[DataRow(@"private protected void b() {{}}", true, 6, 25)]
		[DataRow(@"public string a;", false, 6, 2)]
		[DataRow(@"private string a;", true, 6, 2)]
		[DataRow(@"public int a;", false, 6, 2)]
		[DataRow(@"int a;", true, 6, 2)]
		[DataRow(@"private string a;", true, 6, 2)]
		[DataRow(@"public Foo(){{}}", false, 6, 2)]
		[DataRow(@"private Foo() {{}}", true, 6, 2)]
		[DataRow(@"Foo() {{}}", true, 6, 2)]
		[DataRow(@"public int A {{}}", false, 6, 2)]
		[DataRow(@"private int A {{}}", true, 6, 14)]
		[DataRow(@"public event A", false, 7, 2)]
		[DataRow(@"private event A", true, 7, 4)]
		[DataRow(@"event A", true, 7, 6)]
		[DataRow(@"public event EventHandler<StringEventArgs> RfidDataReceived = null;", false, 6, 2)]
		[DataRow(@"private event EventHandler<StringEventArgs> RfidDataReceived = null;", true, 6, 2)]
		[DataRow(@"event EventHandler<StringEventArgs> RfidDataReceived = null;", true, 6, 2)]
		[DataRow(@"struct A {}", false, 6, 6)]
		[DataRow(@"public bool operator == (x, y) {{}}", false, 6, 6)]
		[DataRow(@"private bool operator == (x, y) {{}}", true, 6, 2)]
		[DataRow(@"bool operator == (x, y) {{}}", true, 6, 2)]
		[DataRow(@"public TValue this[Tkey key] {{ get {{}}; set{{}};}}", false, 6, 2)]
		[DataRow(@"private TValue this[Tkey key] {{ get {{}}; set{{}};}}", true, 6, 2)]
		[DataRow(@"TValue this[Tkey key] {{ get {{}}; set{{}};}}", true, 6, 2)]
		[DataRow(@"~Regis() {{}}", true, 6, 2)]
		[DataRow(@"public ~Regis() {{}}", false, 6, 3)]
		[DataRow(@"private ~Regis() {{}}", true, 6, 11)]
		[DataRow(@"enum A {{}}", true, 6, 2)]
		[DataRow(@"private enum A {{}}", true, 6, 15)]
		[DataRow(@"public enum A {{}}", false, 6, 2)]
		[DataRow(@"delegate int();", true, 6, 2)]
		[DataRow(@"private delegate int();", true, 6, 22)]
		[DataRow(@"public delegate int();", false, 6, 2)]
		[DataRow(@"public class A {{}}", false, 6, 2)]
		[DataRow(@"class A {{}}", true, 6, 2)]
		[DataRow(@"private class A {{}}", true, 6, 2)]
		public void EnforcePublicInterfaceRegion(string given, bool isError, int line, int column)
		{

			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo
{{
	#region Public Interface
	{0}
	#endregion
}}";
			VerifyError(baseline, given, isError, 6, 2);

		}

		[TestMethod]
		public void UnnamedRegionTest()
		{
			string baseline = @"
class Foo
{{
	#region
	private class A {{}}
	#endregion
}}";
			VerifySuccessfulCompilation(baseline);

		}

		[DataTestMethod]
		[DataRow(@"public static void b() {{}}", true, 6, 21)]
		[DataRow(@"private static void b() {{}}", false, 6, 2)]
		[DataRow(@"protected void b() {{}}", false, 6, 2)]
		[DataRow(@"internal void b() {{}}", false, 6, 2)]
		[DataRow(@"protected internal void b() {{}}", false, 6, 2)]
		[DataRow(@"private protected void b() {{}}", false, 6, 2)]
		[DataRow(@"public string a;", true, 6, 2)]
		[DataRow(@"private string a;", true, 6, 2)]
		[DataRow(@"public int a;", true, 6, 2)]
		[DataRow(@"int a;", true, 6, 2)]
		[DataRow(@"private string a;", true, 6, 2)]
		[DataRow(@"public Foo() {{}}", true, 6, 9)]
		[DataRow(@"Foo() {{}}", false, 6, 2)]
		[DataRow(@"private Foo() {{}}", false, 6, 2)]
		[DataRow(@"public int A {{}}", true, 6, 13)]
		[DataRow(@"private int A {{}}", false, 6, 13)]
		[DataRow(@"public event A", true, 7, 2)]
		[DataRow(@"private event A", false, 7, 4)]
		[DataRow(@"event A", false, 7, 4)]
		[DataRow(@"public event EventHandler<StringEventArgs> RfidDataReceived = null;", true, 6, 2)]
		[DataRow(@"private event EventHandler<StringEventArgs> RfidDataReceived = null;", false, 6, 2)]
		[DataRow(@"event EventHandler<StringEventArgs> RfidDataReceived = null;", false, 6, 2)]
		[DataRow(@"struct A {}", false, 6, 6)]
		[DataRow(@"public bool operator == (x, y) {{}}", true, 6, 2)]
		[DataRow(@"private bool operator == (x, y) {{}}", false, 6, 6)]
		[DataRow(@"bool operator == (x, y) {{}}", false, 6, 6)]
		[DataRow(@"public class A {{}}", true, 6, 15)]
		[DataRow(@"private class A {{}}", false, 6, 6)]
		[DataRow(@"class A {{}}", false, 6, 6)]
		[DataRow(@"public TValue this[Tkey key] {{ get {{}}; set{{}};}}", true, 6, 2)]
		[DataRow(@"private TValue this[Tkey key] {{ get {{}}; set{{}};}}", false, 6, 2)]
		[DataRow(@"TValue this[Tkey key] {{ get {{}}; set{{}};}}", false, 6, 2)]
		[DataRow(@"~Regis() {{}}", false, 6, 3)]
		[DataRow(@"public ~Regis() {{}}", true, 6, 10)]
		[DataRow(@"private ~Regis() {{}}", false, 6, 3)]
		[DataRow(@"enum A {{}}", true, 6, 7)]
		[DataRow(@"private enum A {{}}", true, 6, 15)]
		[DataRow(@"public enum A {{}}", true, 6, 14)]
		[DataRow(@"delegate int();", true, 6, 14)]
		[DataRow(@"private delegate int();", true, 6, 22)]
		[DataRow(@"public delegate int();", true, 6, 21)]
		public void EnforeNonPublicPropertiesMethodsRegion(string given, bool isError, int line, int column)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo
{{
	#region Non-Public Properties/Methods
	{0}
	#endregion
}}";
			VerifyError(baseline, given, isError, 6, 2);
		}

		[DataTestMethod]
		[DataRow(@"public  static int a;", true, 6, 2)]
		[DataRow(@"private  static int a;", false, 6, 2)]
		[DataRow(@"protected int a", false, 6, 2)]
		[DataRow(@"internal int a", false, 6, 2)]
		[DataRow(@"int a;", false, 6, 2)]
		[DataRow(@"protected internal int a", false, 6, 2)]
		[DataRow(@"private protected int a", false, 6, 2)]
		[DataRow(@"private void a() {{}}", true, 6, 15)]
		[DataRow(@"public void a() {{}}", true, 6, 14)]
		[DataRow(@"public Foo(){{}}", true, 6, 9)]
		[DataRow(@"private Foo(){{}}", true, 6, 10)]
		[DataRow(@"Foo(){{}}", true, 6, 2)]
		[DataRow(@"public int A {{}}", true, 6, 13)]
		[DataRow(@"private int A {{}}", true, 6, 14)]
		[DataRow(@"public event A", true, 7, 2)]
		[DataRow(@"private event A", true, 7, 4)]
		[DataRow(@"event A", true, 7, 4)]
		[DataRow(@"public event EventHandler<StringEventArgs> RfidDataReceived = null;", true, 6, 2)]
		[DataRow(@"private event EventHandler<StringEventArgs> RfidDataReceived = null;", true, 6, 2)]
		[DataRow(@"struct A {}", false, 6, 6)]
		[DataRow(@"public bool operator == (x, y) {{}}", true, 6, 2)]
		[DataRow(@"private bool operator == (x, y) {{}}", true, 6, 2)]
		[DataRow(@"bool operator == (x, y) {{}}", true, 6, 2)]
		[DataRow(@"public class A {{}}", true, 6, 15)]
		[DataRow(@"private class A {{}}", true, 6, 16)]
		[DataRow(@"class A {{}}", true, 6, 8)]
		[DataRow(@"public TValue this[Tkey key] {{ get {{}}; set{{}};}}", true, 6, 2)]
		[DataRow(@"private TValue this[Tkey key] {{ get {{}}; set{{}};}}", true, 6, 2)]
		[DataRow(@"TValue this[Tkey key] {{ get {{}}; set{{}};}}", true, 6, 2)]
		[DataRow(@"~Regis() {{}}", true, 6, 3)]
		[DataRow(@"public ~Regis() {{}}", true, 6, 10)]
		[DataRow(@"private ~Regis() {{}}", true, 6, 11)]
		[DataRow(@"enum A {{}}", false, 6, 2)]
		[DataRow(@"private enum A {{}}", false, 6, 2)]
		[DataRow(@"public enum A {{}}", true, 6, 14)]
		[DataRow(@"delegate int();", false, 6, 2)]
		[DataRow(@"private delegate int();", false, 6, 2)]
		[DataRow(@"public delegate int();", true, 6, 21)]
		public void EnforeNonPublicDataMembersRegion(string given, bool isError, int line, int column)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo
{{
	#region Non-Public Data Members
	{0}
	#endregion
}}";
			VerifyError(baseline, given, isError, 6, 2);
		}

		[DataTestMethod]
		[DataRow(@"#region Constants", false)]
		[DataRow(@"#region Public Properties/Methods", false)]
		[DataRow(@"#region Non-Public Properties/Methods", true)]
		[DataRow(@"#region Public Interface", true)]
		[DataRow(@"Public Interface", false)]
		[DataRow(@"#region", false)]
		[DataRow(@"#regionPublic Interface", false)]
		public void RegionNameTest(string given, bool isError)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo
{{
	{0}
	private int asd;
	#endregion


}}";
			VerifyError(baseline, given, isError, 6, 2);
		}

		[DataTestMethod]
		[DataRow(@"Non-Public Properties/Methods", true)]
		public void DupliateRegionTest(string given, bool isError)
		{
			string baseline = @"
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
			string givenText = string.Format(baseline, given);
			DiagnosticResult[] results;
			if (isError)
			{
				results = new[] { new DiagnosticResult()
					{
						Id = Helper.ToDiagnosticId(DiagnosticIds.EnforceNonDuplicateRegion),
						Message = new System.Text.RegularExpressions.Regex(EnforceRegionsAnalyzer.EnforceNonDuplicateRegionMessageFormat),
						Severity = DiagnosticSeverity.Error,
						Locations = new[]
						{
							new DiagnosticResultLocation("Test0.cs", 8, 3)
						}
					}
				};
			}
			else
			{
				results = Array.Empty<DiagnosticResult>();
			}
			VerifyDiagnostic(givenText, results);
		}

		private void VerifyError(string baseline, string given, bool isError, int line = 6, int column = 2)
		{
			string givenText = string.Format(baseline, given);
			DiagnosticResult[] results;
			if (isError)
			{
				results = new[] { new DiagnosticResult()
					{
						Id = Helper.ToDiagnosticId(DiagnosticIds.EnforceRegions),
						Message = new System.Text.RegularExpressions.Regex(EnforceRegionsAnalyzer.EnforceRegionMessageFormat),
						Severity = DiagnosticSeverity.Error,
						Locations = new[]
						{
							new DiagnosticResultLocation("Test0.cs", line, column)
						}
					}
				};
			}
			else
			{
				results = Array.Empty<DiagnosticResult>();
			}
			VerifyDiagnostic(givenText, results);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new EnforceRegionsAnalyzer();
		}
	}
}
