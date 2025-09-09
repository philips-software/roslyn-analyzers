// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming;
using Philips.CodeAnalysis.Test.Helpers;

namespace Philips.CodeAnalysis.Test.Maintainability.Naming
{
	[TestClass]
	public class NamespaceMatchFilePathAnalyzerUseFolderTest : NamespaceMatchFilePathAnalyzerVerifier
	{
		public const string ClassString = @"
			using System;
			using System.Globalization;
			namespace {0}
			{{
			class Foo 
			{{
				public void Foo()
				{{
				}}
			}}
			}}
			";

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new NamespaceMatchFilePathAnalyzer();
		}

		protected override ImmutableDictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			return base.GetAdditionalAnalyzerConfigOptions().Add($@"dotnet_code_quality.{DiagnosticId.NamespaceMatchFilePath.ToId()}.folder_in_namespace", "true");
		}

		[TestMethod]
		[DataRow("Philips.Test", "C:\\Philips.Test\\MyTest.cs")]
		[DataRow("Philips.Test", "C:\\Philips.Test\\")]
		[DataRow("Philips.Test.Src", "C:\\Philips.Test\\Src\\MyTest.cs")]
		[DataRow("Philips.Test.Test", "C:\\Philips.Test\\Test\\MyTest.cs")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CorrectNamespaceNoDiagnostic(string ns, string path)
		{
			var sanitizedPath = path.Replace('\\', Path.DirectorySeparatorChar);
			var code = string.Format(ClassString, ns);
			await VerifySuccessfulCompilation(code, sanitizedPath).ConfigureAwait(false);
		}

		[TestMethod]
		[DataRow("Philips.Test", "C:\\development\\Philips.Production\\code\\MyTest.cs")]
		[DataRow("Philips.CodeAnalysis.Test", "C:\\Philips.CodeAnalysis.Production\\MyAnalyzer.cs")]
		[DataRow("Philips.CodeAnalysis.Test", "C:\\Philips.CodeAnalysis.TestFramework\\MyHelper.cs")]
		[DataRow("Philips.Test", "C:\\development\\Philips.Test\\code\\MyTest.cs", DisplayName = "Namespace Match, Folder Does not")]
		[DataRow("Philips.CodeAnalysis.Test", "C:\\Philips.CodeAnalysis.Test\\src\\MyTest.cs", DisplayName = "Namespace Match, Folder Does not")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ReportIncorrectNamespaceFolderMatch(string ns, string path)
		{
			var sanitizedPath = path.Replace('\\', Path.DirectorySeparatorChar);
			var code = string.Format(ClassString, ns);
			DiagnosticResult expected = new()
			{
				Id = DiagnosticId.NamespaceMatchFilePath.ToId(),
				Message = new Regex(".+ "),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					GetBaseDiagnosticLocation(sanitizedPath, 0,0)
				}
			};

			await VerifyDiagnostic(code, expected, sanitizedPath).ConfigureAwait(false);
		}

		[TestMethod]
		[DataRow("Philips.CodeAnalysis.Test.Maintainability", "C:\\Philips.CodeAnalysis.Test\\Maintainability\\blah.cs", DisplayName = "Folder Match Included 1")]
		[DataRow("Philips.CodeAnalysis.Test.Maintainability", "C:\\repos\\Philips.CodeAnalysis.Test\\Maintainability\\blah.cs", DisplayName = "Folder Match Included 2")]
		[DataRow("Philips.CodeAnalysis.Test.Maintainability.Foo", "C:\\repos\\Philips.CodeAnalysis.Test\\Maintainability\\Foo\\blah.cs", DisplayName = "Folder Match Included 2")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DoNotReportANamespaceSupersetFolderMatch(string ns, string path)
		{
			var sanitizedPath = path.Replace('\\', Path.DirectorySeparatorChar);
			var code = string.Format(ClassString, ns);
			await VerifySuccessfulCompilation(code, sanitizedPath).ConfigureAwait(false);
		}
	}
}
