// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.Test.Helpers;

namespace Philips.CodeAnalysis.Test.Common
{
	/// <summary>
	/// Test class for methods in <see cref="AdditionalFilesHelper"/>.
	/// </summary>
	[TestClass]
	public class AdditionalFilesHelperTest
	{
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LoadExceptionsShouldReturnSame()
		{
			// Arrange
			var fileName = "DummyExceptions.txt";
			var content = new[] { "ExcludedMethod", "ExcludedClass" };
			var exceptionsFile = new TestAdditionalFile(fileName, SourceText.From(string.Join("\r\n", content)));
			AdditionalFilesHelper helper = new(CreateOptions(null, ImmutableArray.Create<AdditionalText>(exceptionsFile)),
				CreateCompilation());
			// Act
			HashSet<string> actual = helper.LoadExceptions(fileName);
			// Assert
			CollectionAssert.AreEqual(content, actual.ToArray());
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void GetValuesFromEditorConfigShouldReturnSame()
		{
			// Arrange
			var diagnosticId = "PH0000";
			var key = "DummyKey";
			var content = new[] { "Dummy1", "Dummy2" };
			var settings = new Dictionary<string, string> { { $"dotnet_code_quality.{diagnosticId}.{key}", string.Join(",", content) } };

			AdditionalFilesHelper helper = new(CreateOptions(settings), CreateCompilation());
			// Act
			IReadOnlyList<string> actual = helper.GetValuesFromEditorConfig(diagnosticId, key);
			// Assert
			CollectionAssert.AreEqual(content, actual.ToArray());
		}

		[DataTestMethod]
		[DataRow(false, true)]
		[DataRow(false, false)]
		[DataRow(true, true)]
		[DataRow(true, false)]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LoadExceptionsOptionsShouldReturnSame(bool shouldUse, bool shouldGenerate)
		{
			// Arrange
			var diagnosticId = "PH0000";
			var fileName = "DummyExceptions.txt";
			var settings = new Dictionary<string, string>();
			if (shouldUse)
			{
				settings.Add($"dotnet_code_quality.{diagnosticId}.ignore_exceptions_file", fileName);
			}
			if (shouldGenerate)
			{
				settings.Add($"dotnet_code_quality.{diagnosticId}.generate_exceptions_file", fileName);
			}

			AdditionalFilesHelper helper = new(CreateOptions(settings),
				CreateCompilation());
			// Act
			ExceptionsOptions actual = helper.LoadExceptionsOptions(diagnosticId);
			// Assert
			Assert.AreEqual(!shouldUse, actual.ShouldUseExceptionsFile);
			Assert.AreEqual(shouldGenerate, actual.ShouldGenerateExceptionsFile);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void InitializeExceptionsShouldReturnSame()
		{
			// Arrange
			var diagnosticId = "PH0000";
			var fileName = "DummyExceptions.txt";
			var content = new[] { "ExcludedMethod", "ExcludedClass" };
			var exceptionsFile = new TestAdditionalFile(fileName, SourceText.From(string.Join("\r\n", content)));
			AdditionalFilesHelper helper = new(CreateOptions(null, ImmutableArray.Create<AdditionalText>(exceptionsFile)),
				CreateCompilation());
			// Act
			HashSet<string> actual = helper.InitializeExceptions(fileName, diagnosticId);
			// Assert
			CollectionAssert.AreEqual(content, actual.ToArray());
		}

		private static AnalyzerOptions CreateOptions(Dictionary<string, string> settings)
		{
			return new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty, new TestAnalyzerConfigOptionsProvider(settings));
		}

		private static AnalyzerOptions CreateOptions(Dictionary<string, string> settings,
			ImmutableArray<AdditionalText> additionalTexts)
		{
			return new AnalyzerOptions(additionalTexts, new TestAnalyzerConfigOptionsProvider(settings));
		}

		private static Compilation CreateCompilation()
		{
			var dummyText = SourceText.From("");
			SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(dummyText);
			return CSharpCompilation.Create("Test.dll", new[] { tree });
		}
	}
}
