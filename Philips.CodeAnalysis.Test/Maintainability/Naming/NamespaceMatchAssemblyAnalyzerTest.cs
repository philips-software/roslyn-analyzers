// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Naming
{
	[TestClass]
	public class NamespaceMatchAssemblyAnalyzerUseFolderTest : DiagnosticVerifier
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

		public static DiagnosticResultLocation GetBaseDiagnosticLocation(string path, int rowOffset = 0, int columnOffset = 0)
		{
			return new DiagnosticResultLocation(path + ".cs", 4 + rowOffset, 14 + columnOffset);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			Mock<AdditionalFilesHelper> _mockAdditionalFilesHelper = new(new AnalyzerOptions(ImmutableArray.Create<AdditionalText>()), null);
			_mockAdditionalFilesHelper.Setup(c => c.GetValueFromEditorConfig(It.IsAny<string>(), It.IsAny<string>())).Returns("true");
			return new NamespaceMatchAssemblyAnalyzer(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics, _mockAdditionalFilesHelper.Object);
		}

		[DataTestMethod]
		[DataRow("Philips.Test", "C:\\development\\Philips.Production\\code\\MyTest.cs")]
		[DataRow("Philips.CodeAnalysis.Test", "C:\\Philips.CodeAnalysis.Production\\MyAnalyzer.cs")]
		[DataRow("Philips.CodeAnalysis.Test", "C:\\Philips.CodeAnalysis.TestFramework\\MyHelper.cs")]
		[DataRow("Philips.Test", "C:\\development\\Philips.Test\\code\\MyTest.cs", DisplayName="Namespace Match, Folder Does not")]
		[DataRow("Philips.CodeAnalysis.Test", "C:\\Philips.CodeAnalysis.Test\\src\\MyTest.cs", DisplayName = "Namespace Match, Folder Does not")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ReportIncorrectNamespaceMatch(string ns, string path)
		{
			string sanitizedPath = path.Replace('\\', Path.DirectorySeparatorChar);
			string code = string.Format(ClassString, ns);
			DiagnosticResult expected = new()
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.NamespaceMatchAssembly),
				Message = new Regex(".+ "),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					GetBaseDiagnosticLocation(sanitizedPath, 0,0)
				}
			};

			VerifyDiagnostic(code, sanitizedPath, expected);
		}

		[DataTestMethod]
		[DataRow("Philips.CodeAnalysis.Test.Maintainability", "C:\\Philips.CodeAnalysis.Test\\Maintainability\\blah.cs", DisplayName = "Folder Match Included 1")]
		[DataRow("Philips.CodeAnalysis.Test.Maintainability", "C:\\repos\\Philips.CodeAnalysis.Test\\Maintainability\\blah.cs", DisplayName = "Folder Match Included 2")]
		[DataRow("Philips.CodeAnalysis.Test.Maintainability.Foo", "C:\\repos\\Philips.CodeAnalysis.Test\\Maintainability\\Foo\\blah.cs", DisplayName = "Folder Match Included 2")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DoNotReportANamespaceSupersetMatch(string ns, string path)
		{
			string sanitizedPath = path.Replace('\\', Path.DirectorySeparatorChar);
			string code = string.Format(ClassString, ns);
			VerifyDiagnostic(code, sanitizedPath, Array.Empty<DiagnosticResult>());
		}
	}

	[TestClass]
	public class NamespaceMatchAssemblyAnalyzerNoFolderTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			Mock<AdditionalFilesHelper> _mockAdditionalFilesHelper = new(new AnalyzerOptions(ImmutableArray.Create<AdditionalText>()), null);
			_mockAdditionalFilesHelper.Setup(c => c.GetValueFromEditorConfig(It.IsAny<string>(), It.IsAny<string>())).Returns("false");
			return new NamespaceMatchAssemblyAnalyzer(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics, _mockAdditionalFilesHelper.Object);
		}

		[DataTestMethod]
		[DataRow("Philips.Test", "C:\\development\\Philips.Production\\code\\MyTest.cs")]
		[DataRow("Philips.CodeAnalysis.Test", "C:\\Philips.CodeAnalysis.Production\\MyAnalyzer.cs")]
		[DataRow("Philips.CodeAnalysis.Test", "C:\\Philips.CodeAnalysis.TestFramework\\MyHelper.cs")]
		[DataRow("Philips.CodeAnalysis.Test.Maintainability", "C:\\Philips.CodeAnalysis.Test\\Maintainability\\blah.cs", DisplayName = "Folder Match Included 1")]
		[DataRow("Philips.CodeAnalysis.Test.Maintainability", "C:\\repos\\Philips.CodeAnalysis.Test\\Maintainability\\blah.cs", DisplayName = "Folder Match Included 2")]
		[DataRow("Philips.CodeAnalysis.Test.Maintainability.Foo", "C:\\repos\\Philips.CodeAnalysis.Test\\Maintainability\\Foo\\blah.cs", DisplayName = "Folder Match Included 2")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ReportIncorrectNamespaceMatch(string ns, string path)
		{
			string sanitizedPath = path.Replace('\\', Path.DirectorySeparatorChar);
			string code = string.Format(NamespaceMatchAssemblyAnalyzerUseFolderTest.ClassString, ns);
			DiagnosticResult expected = new()
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.NamespaceMatchAssembly),
				Message = new Regex(".+ "),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					NamespaceMatchAssemblyAnalyzerUseFolderTest.GetBaseDiagnosticLocation(sanitizedPath, 0,0)
				}
			};

			VerifyDiagnostic(code, sanitizedPath, expected);
		}

		[DataTestMethod]
		[DataRow("Philips.Test", "C:\\development\\Philips.Test\\code\\MyTest.cs", DisplayName = "Namespace Match, Folder Does not")]
		[DataRow("Philips.CodeAnalysis.Test", "C:\\Philips.CodeAnalysis.Test\\src\\MyTest.cs", DisplayName = "Namespace Match, Folder Does not")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DoNotReportANamespaceSupersetMatch(string ns, string path)
		{
			string sanitizedPath = path.Replace('\\', Path.DirectorySeparatorChar);
			string code = string.Format(NamespaceMatchAssemblyAnalyzerUseFolderTest.ClassString, ns);
			VerifyDiagnostic(code, sanitizedPath, Array.Empty<DiagnosticResult>());
		}
	}

}
