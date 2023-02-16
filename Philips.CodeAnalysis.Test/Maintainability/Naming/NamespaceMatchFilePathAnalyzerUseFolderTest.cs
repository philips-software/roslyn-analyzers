// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
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
			Mock<AdditionalFilesHelper> _mockAdditionalFilesHelper = new(new AnalyzerOptions(ImmutableArray.Create<AdditionalText>()), null);
			_mockAdditionalFilesHelper.Setup(c => c.GetValueFromEditorConfig(It.IsAny<string>(), It.IsAny<string>())).Returns("true");
			return new NamespaceMatchFilePathAnalyzer(_mockAdditionalFilesHelper.Object);
		}

		[DataTestMethod]
		[DataRow("Philips.Test", "C:\\development\\Philips.Production\\code\\MyTest.cs")]
		[DataRow("Philips.CodeAnalysis.Test", "C:\\Philips.CodeAnalysis.Production\\MyAnalyzer.cs")]
		[DataRow("Philips.CodeAnalysis.Test", "C:\\Philips.CodeAnalysis.TestFramework\\MyHelper.cs")]
		[DataRow("Philips.Test", "C:\\development\\Philips.Test\\code\\MyTest.cs", DisplayName = "Namespace Match, Folder Does not")]
		[DataRow("Philips.CodeAnalysis.Test", "C:\\Philips.CodeAnalysis.Test\\src\\MyTest.cs", DisplayName = "Namespace Match, Folder Does not")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ReportIncorrectNamespaceFolderMatchAsync(string ns, string path)
		{
			string sanitizedPath = path.Replace('\\', Path.DirectorySeparatorChar);
			string code = string.Format(ClassString, ns);
			DiagnosticResult expected = new()
			{
				Id = Helper.ToDiagnosticId(DiagnosticId.NamespaceMatchFilePath),
				Message = new Regex(".+ "),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					GetBaseDiagnosticLocation(sanitizedPath, 0,0)
				}
			};

			await VerifyDiagnostic(code, expected, sanitizedPath).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("Philips.CodeAnalysis.Test.Maintainability", "C:\\Philips.CodeAnalysis.Test\\Maintainability\\blah.cs", DisplayName = "Folder Match Included 1")]
		[DataRow("Philips.CodeAnalysis.Test.Maintainability", "C:\\repos\\Philips.CodeAnalysis.Test\\Maintainability\\blah.cs", DisplayName = "Folder Match Included 2")]
		[DataRow("Philips.CodeAnalysis.Test.Maintainability.Foo", "C:\\repos\\Philips.CodeAnalysis.Test\\Maintainability\\Foo\\blah.cs", DisplayName = "Folder Match Included 2")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DoNotReportANamespaceSupersetFolderMatchAsync(string ns, string path)
		{
			string sanitizedPath = path.Replace('\\', Path.DirectorySeparatorChar);
			string code = string.Format(ClassString, ns);
			await VerifySuccessfulCompilation(code, sanitizedPath).ConfigureAwait(false);
		}
	}
}
