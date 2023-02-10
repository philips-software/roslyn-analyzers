// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
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

		protected override ImmutableArray<MetadataReference> GetMetadataReferences()
		{
			string mockReference = typeof(Mock<>).Assembly.Location;
			MetadataReference reference = MetadataReference.CreateFromFile(mockReference);

			return base.GetMetadataReferences().Add(reference);
		}

		#endregion

		#region Public Interface

		[DataRow(false, "m.Object, EventArgs.Empty")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EventMustHaveCorrectArgumentsAsync(bool isError, string args)
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


			string arguments = string.Empty;
			if (args.Length > 0)
			{
				arguments = $", {args}";
			}
			var code = string.Format(template, arguments);

			if (isError)
			{
				var expectedErrors = new[]
				{
					new DiagnosticResult()
					{
						Id= Helper.ToDiagnosticId(DiagnosticId.MockRaiseArgumentsMustMatchEvent),
						Location = new DiagnosticResultLocation(15),
						Severity = DiagnosticSeverity.Error,
					}
				};
				await VerifyDiagnostic(code, expectedErrors).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(code).ConfigureAwait(false);
			}
		}

		[DataRow(true, "")]
		[DataRow(true, "m.Object")]
		[DataRow(false, "m.Object, EventArgs.Empty")]
		[DataRow(false, "EventArgs.Empty")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EventMustHaveCorrectArgumentCountAsync(bool isError, string args)
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

			string arguments = string.Empty;
			if (args.Length > 0)
			{
				arguments = $", {args}";
			}
			var code = string.Format(template, arguments);

			if (isError)
			{
				var expectedErrors =
					new DiagnosticResult()
					{
						Id = Helper.ToDiagnosticId(DiagnosticId.MockRaiseArgumentCountMismatch),
						Location = new DiagnosticResultLocation(15),
						Severity = DiagnosticSeverity.Error,
					};
				await VerifyDiagnostic(code, expectedErrors).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(code).ConfigureAwait(false);
			}
		}

		//[DataRow(true, "", DiagnosticIds.MockRaiseArgumentCountMismatch)]
		[DataRow("EventArgs.Empty", @"PH2053")]
		//[DataRow(false, "new DerivedEventArgs()", DiagnosticIds.MockRaiseArgumentsMustMatchEvent)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EventMustHaveCorrectArgumentCount2Async(string args, string diagnosticId)
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

			var expectedErrors =
				new DiagnosticResult()
				{
					Id = diagnosticId,
					Location = new DiagnosticResultLocation(18),
					Severity = DiagnosticSeverity.Error,
				};

			string arguments = string.Empty;
			if (args.Length > 0)
			{
				arguments = $", {args}";
			}

			await VerifyDiagnostic(string.Format(template, arguments), expectedErrors).ConfigureAwait(false);
		}

		[DataRow("", 1, @"PH2054")]
		[DataRow("EventArgs.Empty", 1, @"PH2054")]
		[DataRow("EventArgs.Empty, EventArgs.Empty", 1, @"PH2054")]
		[DataRow("EventArgs.Empty, EventArgs.Empty, EventArgs.Empty", 3, @"PH2053")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EventMustHaveCorrectArgumentsNonEventHandlerAsync(string args, int errorCount, string diagnosticId)
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

			var expectedErrors = diagnosticResults.ToArray();

			string arguments = string.Empty;
			if (args.Length > 0)
			{
				arguments = $", {args}";
			}
			string code = string.Format(template, arguments);

			if (errorCount == 1)
			{
				await VerifyDiagnostic(code, expectedErrors[0]).ConfigureAwait(false);
			}
			else
			{
				await VerifyDiagnostic(code, expectedErrors).ConfigureAwait(false);
			}
		}

		[DataRow("", @"PH2054")]
		[DataRow("EventArgs.Empty", @"PH2054")]
		[DataRow("EventArgs.Empty, EventArgs.Empty, EventArgs.Empty", @"PH2054")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EventMustHaveCorrectArgumentsNonEventHandler2Async(string args, string diagnosticId)
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

			var result = new DiagnosticResult()
			{
				Id = diagnosticId,
				Location = new DiagnosticResultLocation(18),
				Severity = DiagnosticSeverity.Error,
			};

			string arguments = string.Empty;
			if (args.Length > 0)
			{
				arguments = $", {args}";
			}

			await VerifyDiagnostic(string.Format(template, arguments), result).ConfigureAwait(false);
		}

		#endregion
	}
}