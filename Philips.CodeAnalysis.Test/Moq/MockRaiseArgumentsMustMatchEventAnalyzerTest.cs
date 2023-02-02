// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MoqAnalyzers;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Moq
{
	[TestClass]
	public class MockRaiseArgumentsMustMatchEventAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new MockRaiseArgumentsMustMatchEventAnalyzer();
		}

		protected override MetadataReference[] GetMetadataReferences()
		{
			string mockReference = typeof(Mock<>).Assembly.Location;
			MetadataReference reference = MetadataReference.CreateFromFile(mockReference);

			return base.GetMetadataReferences().Concat(new[] { reference }).ToArray();
		}

		#endregion

		#region Public Interface

		[DataRow(false, "m.Object, EventArgs.Empty")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void EventMustHaveCorrectArguments(bool isError, string args)
		{
			const string template = @"
using Moq;
using System;
public class Mockable
{{
	public virtual event EventHandler<EventArgs> Event;
}}

public static class Bar
{{
	public static void Method()
	{{
		Mock<Mockable> m = new Mock<Mockable>();

		m.Raise(x => x.Event += null{0});
	}}
}}
";

			DiagnosticResult[] expectedErrors = Array.Empty<DiagnosticResult>();
			if (isError)
			{
				expectedErrors = new[]
				{
					new DiagnosticResult()
					{
						Id= Helper.ToDiagnosticId(DiagnosticId.MockRaiseArgumentsMustMatchEvent),
						Location = new DiagnosticResultLocation(15),
						Severity = DiagnosticSeverity.Error,
					}
				};
			}

			string arguments = string.Empty;
			if (args.Length > 0)
			{
				arguments = $", {args}";
			}

			VerifyDiagnostic(string.Format(template, arguments), expectedErrors);
		}

		[DataRow(true, "")]
		[DataRow(true, "m.Object")]
		[DataRow(false, "m.Object, EventArgs.Empty")]
		[DataRow(false, "EventArgs.Empty")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void EventMustHaveCorrectArgumentCount(bool isError, string args)
		{
			const string template = @"
using Moq;
using System;
public class Mockable
{{
	public virtual event EventHandler<EventArgs> Event;
}}

public static class Bar
{{
	public static void Method()
	{{
		Mock<Mockable> m = new Mock<Mockable>();

		m.Raise(x => x.Event += null{0});
	}}
}}
";

			DiagnosticResult[] expectedErrors = Array.Empty<DiagnosticResult>();
			if (isError)
			{
				expectedErrors = new[]
				{
					new DiagnosticResult()
					{
						Id= Helper.ToDiagnosticId(DiagnosticId.MockRaiseArgumentCountMismatch),
						Location = new DiagnosticResultLocation(15),
						Severity = DiagnosticSeverity.Error,
					}
				};
			}

			string arguments = string.Empty;
			if (args.Length > 0)
			{
				arguments = $", {args}";
			}

			VerifyDiagnostic(string.Format(template, arguments), expectedErrors);
		}

		//[DataRow(true, "", DiagnosticIds.MockRaiseArgumentCountMismatch)]
		[DataRow(true, "EventArgs.Empty", @"PH2053")]
		//[DataRow(false, "new DerivedEventArgs()", DiagnosticIds.MockRaiseArgumentsMustMatchEvent)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void EventMustHaveCorrectArgumentCount2(bool isError, string args, string diagnosticId)
		{
			const string template = @"
using Moq;
using System;

public class DerivedEventArgs : EventArgs {{}}

public class Mockable
{{
	public virtual event EventHandler<DerivedEventArgs> Event;
}}

public static class Bar
{{
	public static void Method()
	{{
		Mock<Mockable> m = new Mock<Mockable>();

		m.Raise(x => x.Event += null{0});
	}}
}}
";

			DiagnosticResult[] expectedErrors = Array.Empty<DiagnosticResult>();
			if (isError)
			{
				expectedErrors = new[]
				{
					new DiagnosticResult()
					{
						Id= diagnosticId,
						Location = new DiagnosticResultLocation(18),
						Severity = DiagnosticSeverity.Error,
					}
				};
			}

			string arguments = string.Empty;
			if (args.Length > 0)
			{
				arguments = $", {args}";
			}

			VerifyDiagnostic(string.Format(template, arguments), expectedErrors);
		}

		[DataRow(true, "", 1, @"PH2054")]
		[DataRow(true, "EventArgs.Empty", 1, @"PH2054")]
		[DataRow(true, "EventArgs.Empty, EventArgs.Empty", 1, @"PH2054")]
		[DataRow(true, "EventArgs.Empty, EventArgs.Empty, EventArgs.Empty", 3, @"PH2053")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void EventMustHaveCorrectArgumentsNonEventHandler(bool isError, string args, int errorCount, string diagnosticId)
		{
			const string template = @"
using Moq;
using System;

delegate void FooHandler(int i, string s, bool b);

public class Mockable
{{
	public virtual event FooHandler Event;
}}

public static class Bar
{{
	public static void Method()
	{{
		Mock<Mockable> m = new Mock<Mockable>();

		m.Raise(x => x.Event += null{0});
	}}
}}
";

			DiagnosticResult[] expectedErrors = Array.Empty<DiagnosticResult>();
			if (isError)
			{
				List<DiagnosticResult> diagnosticResults = new();

				for (int i = 0; i < errorCount; i++)
				{
					diagnosticResults.Add(new DiagnosticResult()
					{
						Id = diagnosticId,
						Location = new DiagnosticResultLocation(18),
						Severity = DiagnosticSeverity.Error,
					});
				}

				expectedErrors = diagnosticResults.ToArray();
			}

			string arguments = string.Empty;
			if (args.Length > 0)
			{
				arguments = $", {args}";
			}

			VerifyDiagnostic(string.Format(template, arguments), expectedErrors);
		}

		[DataRow(true, "", 1, @"PH2054")]
		[DataRow(true, "EventArgs.Empty", 1, @"PH2054")]
		[DataRow(true, "EventArgs.Empty, EventArgs.Empty, EventArgs.Empty", 1, @"PH2054")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void EventMustHaveCorrectArgumentsNonEventHandler2(bool isError, string args, int errorCount, string diagnosticId)
		{
			const string template = @"
using Moq;
using System;

delegate void FooHandler(int i, EventArgs args);

public class Mockable
{{
	public virtual event FooHandler Event;
}}

public static class Bar
{{
	public static void Method()
	{{
		Mock<Mockable> m = new Mock<Mockable>();

		m.Raise(x => x.Event += null{0});
	}}
}}
";

			DiagnosticResult[] expectedErrors = Array.Empty<DiagnosticResult>();
			if (isError)
			{
				List<DiagnosticResult> diagnosticResults = new();

				for (int i = 0; i < errorCount; i++)
				{
					diagnosticResults.Add(new DiagnosticResult()
					{
						Id = diagnosticId,
						Location = new DiagnosticResultLocation(18),
						Severity = DiagnosticSeverity.Error,
					});
				}

				expectedErrors = diagnosticResults.ToArray();
			}

			string arguments = string.Empty;
			if (args.Length > 0)
			{
				arguments = $", {args}";
			}

			VerifyDiagnostic(string.Format(template, arguments), expectedErrors);
		}

		#endregion
	}
}