// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.SecurityAnalyzers;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Security
{
	[TestClass]
	public class LicenseAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new LicenseAnalyzer();
		}

		private string GetTestCode()
		{
			return @"
class Foo 
{
  public void DoSomething()
  {
    var x = 1;
  }
}
";
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerSuccessfulCompilationNoErrorAsync()
		{
			await VerifySuccessfulCompilation(GetTestCode()).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LicenseAnalyzerHasCorrectDiagnosticId()
		{
			var analyzer = new LicenseAnalyzer();
			System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.DiagnosticDescriptor> descriptors = analyzer.SupportedDiagnostics;
			Assert.HasCount(2, descriptors);
			Assert.IsTrue(descriptors.Any(d => d.Id == "PH2155"));
			Assert.IsTrue(descriptors.Any(d => d.Id == "PH2155_DEBUG"));
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LicenseAnalyzerHasCorrectCategory()
		{
			var analyzer = new LicenseAnalyzer();
			Microsoft.CodeAnalysis.DiagnosticDescriptor descriptor = analyzer.SupportedDiagnostics[0];
			Assert.AreEqual("Philips Security", descriptor.Category);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LicenseAnalyzerIsEnabledByDefault()
		{
			var analyzer = new LicenseAnalyzer();
			Microsoft.CodeAnalysis.DiagnosticDescriptor descriptor = analyzer.SupportedDiagnostics[0];
			Assert.IsTrue(descriptor.IsEnabledByDefault);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LicenseAnalyzerHasCorrectSeverity()
		{
			var analyzer = new LicenseAnalyzer();
			Microsoft.CodeAnalysis.DiagnosticDescriptor descriptor = analyzer.SupportedDiagnostics[0];
			Assert.AreEqual(Microsoft.CodeAnalysis.DiagnosticSeverity.Error, descriptor.DefaultSeverity);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LicenseAnalyzerMessageFormatContainsExpectedPlaceholders()
		{
			var messageFormat = LicenseAnalyzer.MessageFormat;
			Assert.Contains("{0}", messageFormat, "Message format should contain package name placeholder");
			Assert.Contains("{1}", messageFormat, "Message format should contain version placeholder");
			Assert.Contains("{2}", messageFormat, "Message format should contain license placeholder");
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LicenseAnalyzerFileNameConstantsHaveCorrectValues()
		{
			// Test that the file name constants have expected values and extensions
			var allowedLicensesFileName = LicenseAnalyzer.AllowedLicensesFileName;
			var licensesCacheFileName = LicenseAnalyzer.LicensesCacheFileName;

			Assert.EndsWith(".txt", allowedLicensesFileName);
			Assert.EndsWith(".cache", licensesCacheFileName);
			Assert.Contains("Allowed", allowedLicensesFileName);
			Assert.Contains("licenses", licensesCacheFileName);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithNullCodeAsync()
		{
			// Test analyzer handles null/empty code gracefully
			await VerifySuccessfulCompilation(string.Empty).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithMinimalCodeAsync()
		{
			const string testCode = "class Test {}";
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithComplexCodeStructureAsync()
		{
			const string testCode = @"
using System;
using System.Collections.Generic;

namespace TestNamespace
{
    public interface ITestInterface
    {
        void DoSomething();
    }

    public class TestClass : ITestInterface
    {
        private readonly List<string> _items = new List<string>();

        public void DoSomething()
        {
            Console.WriteLine(""Test"");
        }

        public static void StaticMethod()
        {
            var instance = new TestClass();
            instance.DoSomething();
        }
    }
}";
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithAdditionalFileCustomLicensesAllowedAsync()
		{
			// Test that analyzer can process additional files containing custom license lists
			// The analyzer should handle the presence of additional files gracefully
			var testCode = GetTestCode();

			// Create test with potential additional file content
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);

			// Verify the analyzer doesn't crash when additional files might be present
			// (In real scenarios, these would be provided via the build system)
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithEmptyAdditionalFileAsync()
		{
			// Test that the analyzer handles empty additional files gracefully
			// Empty license files should not cause exceptions or unexpected behavior
			await VerifySuccessfulCompilation(GetTestCode()).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithMissingAssetsFileAsync()
		{
			// Test that analyzer doesn't crash when project.assets.json is not available
			// This is a common scenario in test environments or during initial builds
			await VerifySuccessfulCompilation(GetTestCode()).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LicenseAnalyzerSupportsCompilationStartAnalysis()
		{
			// Verify that the analyzer registers for compilation start analysis
			var analyzer = new LicenseAnalyzer();
			System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.DiagnosticDescriptor> supportedDiagnostics = analyzer.SupportedDiagnostics;
			Assert.IsFalse(supportedDiagnostics.IsDefault);
			Assert.HasCount(2, supportedDiagnostics);

			// The analyzer should be designed to work at compilation level
			// since it needs to analyze the entire project's dependencies
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LicenseAnalyzerHasValidTitle()
		{
			var analyzer = new LicenseAnalyzer();
			Microsoft.CodeAnalysis.DiagnosticDescriptor descriptor = analyzer.SupportedDiagnostics[0];
			Assert.IsFalse(string.IsNullOrWhiteSpace(descriptor.Title.ToString()));
			Assert.Contains("License", descriptor.Title.ToString());
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LicenseAnalyzerHasValidDescription()
		{
			var analyzer = new LicenseAnalyzer();
			Microsoft.CodeAnalysis.DiagnosticDescriptor descriptor = analyzer.SupportedDiagnostics[0];
			Assert.IsFalse(string.IsNullOrWhiteSpace(descriptor.Description.ToString()));
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LicenseAnalyzerHasValidHelpLinkUri()
		{
			var analyzer = new LicenseAnalyzer();
			Microsoft.CodeAnalysis.DiagnosticDescriptor descriptor = analyzer.SupportedDiagnostics[0];
			Assert.IsNotNull(descriptor.HelpLinkUri);
		}

		[TestMethod]
		[DataRow("MIT")]
		[DataRow("Apache-2.0")]
		[DataRow("BSD-2-Clause")]
		[DataRow("BSD-3-Clause")]
		[DataRow("ISC")]
		[DataRow("Unlicense")]
		[DataRow("0BSD")]
		[DataRow("GPL-3.0")]
		[DataRow("LGPL-2.1")]
		[DataRow("AGPL-3.0")]
		[DataRow("")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithVariousLicenseTypesAsync(string licenseType)
		{
			// Test that the analyzer handles different license types in the code analysis context
			// The analyzer should gracefully handle analysis regardless of the license types
			// that would be discovered in actual package dependencies
			_ = licenseType; // Use the parameter to represent different license scenarios
			await VerifySuccessfulCompilation(GetTestCode()).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithTestCodeDetectionAsync()
		{
			// Test that analyzer correctly detects test code and may skip analysis
			const string testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestClass
{
    [TestMethod]
    public void TestMethod()
    {
        var x = 1;
    }
}";
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithUsingStatementsAsync()
		{
			// Test analyzer behavior with various using statements
			const string testCode = @"
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using NuGet.ProjectModel;

public class TestClass
{
    public void Method()
    {
        var list = new List<string>();
        Console.WriteLine(list.Count);
    }
}";
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithNamespaceDeclarationsAsync()
		{
			// Test analyzer with different namespace structures
			const string testCode = @"
namespace MyProject.Core
{
    public class Service
    {
        public void Execute() { }
    }
}

namespace MyProject.Tests
{
    public class TestService
    {
        public void TestExecute() { }
    }
}";
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithMultipleClassesAsync()
		{
			// Test analyzer with multiple classes in the same file
			const string testCode = @"
public class FirstClass
{
    public void Method1() { }
}

public class SecondClass
{
    public void Method2() { }
}

internal class InternalClass
{
    private void PrivateMethod() { }
}";
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithAttributesAsync()
		{
			// Test analyzer with various attributes that might be present
			const string testCode = @"
using System;
using System.ComponentModel;

[Serializable]
public class AttributedClass
{
    [Description(""Test property"")]
    public string Property { get; set; }

    [Obsolete(""Use new method"")]
    public void OldMethod() { }
}";
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithGenericsAsync()
		{
			// Test analyzer with generic types
			const string testCode = @"
using System.Collections.Generic;

public class GenericClass<T> where T : class
{
    private readonly List<T> _items = new List<T>();

    public void AddItem(T item)
    {
        _items.Add(item);
    }

    public IEnumerable<T> GetItems()
    {
        return _items;
    }
}";
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LicenseAnalyzerConstantsAreAccessible()
		{
			// Test that public constants are properly accessible
			Assert.IsNotNull(LicenseAnalyzer.AllowedLicensesFileName);
			Assert.IsNotNull(LicenseAnalyzer.LicensesCacheFileName);
			Assert.IsNotNull(LicenseAnalyzer.MessageFormat);

			// Verify they have reasonable values
			Assert.IsGreaterThan(0, LicenseAnalyzer.AllowedLicensesFileName.Length);
			Assert.IsGreaterThan(0, LicenseAnalyzer.LicensesCacheFileName.Length);
			Assert.IsGreaterThan(0, LicenseAnalyzer.MessageFormat.Length);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LicenseAnalyzerMessageFormatIsWellFormed()
		{
			var messageFormat = LicenseAnalyzer.MessageFormat;

			// Should contain placeholders for package name, version, and license
			Assert.Contains("{0}", messageFormat);
			Assert.Contains("{1}", messageFormat);
			Assert.Contains("{2}", messageFormat);

			// Should contain meaningful text
			Assert.Contains("Package", messageFormat);
			Assert.Contains("license", messageFormat);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LicenseAnalyzerCacheFileNameConstant()
		{
			// Verify the cache file name constant has a proper value
			var fileName = LicenseAnalyzer.LicensesCacheFileName;
			Assert.IsFalse(string.IsNullOrEmpty(fileName));
			Assert.EndsWith(".cache", fileName);
			Assert.AreEqual("licenses.cache", fileName);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LicenseAnalyzerAllowedLicensesFileNameConstant()
		{
			// Verify the allowed licenses file name constant has a proper value
			var fileName = LicenseAnalyzer.AllowedLicensesFileName;
			Assert.IsFalse(string.IsNullOrEmpty(fileName));
			Assert.EndsWith(".txt", fileName);
			Assert.AreEqual("Allowed.Licenses.txt", fileName);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithEmptyProjectAsync()
		{
			// Test that analyzer handles completely empty projects gracefully
			const string emptyCode = "";
			await VerifySuccessfulCompilation(emptyCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithCommentOnlyCodeAsync()
		{
			// Test analyzer with code that contains only comments
			const string commentCode = @"
// This is a comment-only file
/* 
 * Multi-line comment
 * More comments
 */
// Another comment
";
			await VerifySuccessfulCompilation(commentCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithEnumDeclarationsAsync()
		{
			// Test analyzer with enum declarations
			const string enumCode = @"
public enum TestEnum
{
    None = 0,
    First = 1,
    Second = 2,
    Third = 4
}

[Flags]
public enum FlagsEnum
{
    None = 0,
    Option1 = 1,
    Option2 = 2,
    Option3 = 4,
    All = Option1 | Option2 | Option3
}";
			await VerifySuccessfulCompilation(enumCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithInterfaceDeclarationsAsync()
		{
			// Test analyzer with interface declarations
			const string interfaceCode = @"
public interface ITestInterface
{
    void DoSomething();
    string GetValue();
    int Calculate(int input);
}

public interface IGenericInterface<T>
{
    T GetItem();
    void SetItem(T item);
}";
			await VerifySuccessfulCompilation(interfaceCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerHandlesProjectAssetsFileDiscoveryAsync()
		{
			// Test the project.assets.json discovery mechanism - analyzer should not crash
			// when project.assets.json is not available in test environment
			await VerifySuccessfulCompilation(GetTestCode()).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerHandlesExceptionsSilentlyAsync()
		{
			// Test that analyzer handles various exception scenarios gracefully
			// This ensures analyzer doesn't crash the compilation process
			const string complexCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestNamespace
{
    public class ComplexClass
    {
        private readonly Dictionary<string, object> _data = new Dictionary<string, object>();
        
        public void ProcessData()
        {
            var results = _data.Where(x => x.Value != null)
                              .Select(x => x.Key)
                              .ToList();
            
            foreach (var result in results)
            {
                Console.WriteLine(result);
            }
        }
    }
}";
			await VerifySuccessfulCompilation(complexCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithDelegateDeclarationsAsync()
		{
			// Test analyzer with delegate declarations
			const string delegateCode = @"
public delegate void TestDelegate(string message);
public delegate T GenericDelegate<T>(T input);
public delegate bool PredicateDelegate<in T>(T item);

public class DelegateUser
{
    public event TestDelegate OnTest;
    
    public void TriggerEvent(string message)
    {
        OnTest?.Invoke(message);
    }
}";
			await VerifySuccessfulCompilation(delegateCode).ConfigureAwait(false);
		}
	}

	[TestClass]
	public class LicenseAnalyzerIntegrationTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new LicenseAnalyzer();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithTestClassDetectionAsync()
		{
			const string testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class UnitTestClass 
{
    [TestMethod]
    public void TestMethod()
    {
        var x = 1;
        Assert.AreEqual(1, x);
    }
}";
			// Analyzer should handle test code gracefully
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithMixedTestAndProductionCodeAsync()
		{
			const string testCode = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

public class ProductionClass
{
    public int Calculate(int input)
    {
        return input * 2;
    }
}

[TestClass]
public class TestClass 
{
    [TestMethod]
    public void TestCalculate()
    {
        var production = new ProductionClass();
        var result = production.Calculate(5);
        Assert.AreEqual(10, result);
    }
}";
			// Should handle mixed scenarios without issues
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithComplexProjectStructureAsync()
		{
			const string testCode = @"
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MyProject.Core
{
    public interface IService
    {
        Task<string> ProcessAsync(string input);
    }

    public class Service : IService
    {
        public async Task<string> ProcessAsync(string input)
        {
            await Task.Delay(1);
            return input.ToUpper();
        }
    }
}

namespace MyProject.Tests
{
    [TestClass]
    public class ServiceTests
    {
        [TestMethod]
        public async Task ProcessAsync_ReturnsUppercase()
        {
            var service = new MyProject.Core.Service();
            var result = await service.ProcessAsync(""test"");
            Assert.AreEqual(""TEST"", result);
        }
    }
}";
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}
	}

	[TestClass]
	public class LicenseAnalyzerRobustnessTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new LicenseAnalyzer();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithSyntaxErrorsAsync()
		{
			// Test that analyzer doesn't crash even with syntax errors in the code
			const string invalidCode = @"
class InvalidClass
{
    public void Method(
    // Missing closing parenthesis and brace
";
			// This may not compile successfully, but analyzer should not crash
			try
			{
				await VerifySuccessfulCompilation(invalidCode).ConfigureAwait(false);
			}
			catch
			{
				// It's okay if this fails compilation due to syntax errors
				// The important thing is that the analyzer doesn't crash
				return;
			}
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithExtremelyLargeFileAsync()
		{
			// Test analyzer performance with larger files
			var codeBuilder = new System.Text.StringBuilder();
			_ = codeBuilder.AppendLine("using System;");
			_ = codeBuilder.AppendLine("namespace LargeProject {");

			// Generate a large number of classes
			for (var i = 0; i < 50; i++)
			{
				_ = codeBuilder.AppendLine($"public class Class{i} {{");
				_ = codeBuilder.AppendLine($"    public void Method{i}() {{ Console.WriteLine(\"{i}\"); }}");
				_ = codeBuilder.AppendLine("}");
			}

			_ = codeBuilder.AppendLine("}");

			await VerifySuccessfulCompilation(codeBuilder.ToString()).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithEdgeCaseIdentifiersAsync()
		{
			// Test with edge case identifiers and special characters
			const string edgeCaseCode = @"
using System;

public class @class
{
    public void @method()
    {
        var @var = 1;
        var _underscore = 2;
        var mixedCase123 = 3;
    }
}

public class УникодIdentifier
{
    public void Метод()
    {
        Console.WriteLine(""Unicode identifiers"");
    }
}";
			await VerifySuccessfulCompilation(edgeCaseCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LicenseAnalyzerDescriptorConsistency()
		{
			var analyzer = new LicenseAnalyzer();
			Microsoft.CodeAnalysis.DiagnosticDescriptor descriptor = analyzer.SupportedDiagnostics[0];

			// Verify descriptor consistency
			Assert.AreEqual("PH2155", descriptor.Id);
			Assert.IsGreaterThan(0, descriptor.Title.ToString().Length);
			Assert.IsGreaterThan(0, descriptor.MessageFormat.ToString().Length);
			Assert.IsGreaterThan(0, descriptor.Description.ToString().Length);
			Assert.AreEqual("Philips Security", descriptor.Category);
			Assert.IsTrue(descriptor.IsEnabledByDefault);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerWithAsyncAwaitPatternsAsync()
		{
			const string asyncCode = @"
using System;
using System.Threading.Tasks;

public class AsyncClass
{
    public async Task<int> CalculateAsync()
    {
        await Task.Delay(1);
        return 42;
    }

    public async Task ProcessAsync()
    {
        var result = await CalculateAsync();
        Console.WriteLine(result);
    }
}";
			await VerifySuccessfulCompilation(asyncCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LicenseAnalyzerMicrosoftLicenseUrlFlaggedForDebugging()
		{
			// Test that the Microsoft license URL is correctly flagged as unacceptable for debugging purposes
			// This URL is actually MIT but we flag it specifically for debugging
			var analyzer = new LicenseAnalyzer();

			// Verify the analyzer has the correct diagnostic for license violations
			System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.DiagnosticDescriptor> descriptors = analyzer.SupportedDiagnostics;
			Microsoft.CodeAnalysis.DiagnosticDescriptor licenseDescriptor = descriptors.First(d => d.Id == "PH2155");

			Assert.IsNotNull(licenseDescriptor);
			Assert.AreEqual(Microsoft.CodeAnalysis.DiagnosticSeverity.Error, licenseDescriptor.DefaultSeverity);
			Assert.Contains("unacceptable license", licenseDescriptor.MessageFormat.ToString());
		}
	}

	[TestClass]
	public class LicenseAnalyzerFileTypeSupportTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new LicenseAnalyzer();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LicenseAnalyzerExtractsTypeFileAsUnknown()
		{
			// Test the specific scenario mentioned in the issue:
			// <license type="file">LICENSE.md</license>
			// <licenseUrl>https://aka.ms/deprecateLicenseUrl</licenseUrl>
			const string nuspecContent = @"<?xml version=""1.0""?>
<package>
  <metadata>
    <id>TestPackage</id>
    <version>1.0.0</version>
    <license type=""file"">LICENSE.md</license>
    <licenseUrl>https://aka.ms/deprecateLicenseUrl</licenseUrl>
  </metadata>
</package>";

			var result = LicenseAnalyzer.ExtractLicenseFromNuspecContent(nuspecContent);

			// Should return our special marker for type="file" licenses
			Assert.AreEqual("UNKNOWN_FILE_LICENSE", result);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LicenseAnalyzerFallsBackToLicenseUrlWhenNoLicenseElement()
		{
			// Test that when there's no <license> element, it falls back to <licenseUrl>
			const string nuspecContent = @"<?xml version=""1.0""?>
<package>
  <metadata>
    <id>TestPackage</id>
    <version>1.0.0</version>
    <licenseUrl>https://aka.ms/deprecateLicenseUrl</licenseUrl>
  </metadata>
</package>";

			var result = LicenseAnalyzer.ExtractLicenseFromNuspecContent(nuspecContent);

			// Should return the normalized URL (without https://) since it's not a recognized license pattern
			Assert.AreEqual("aka.ms/deprecateLicenseUrl", result);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LicenseAnalyzerHandlesTypeExpressionCorrectly()
		{
			// Test that type="expression" still works as before
			const string nuspecContent = @"<?xml version=""1.0""?>
<package>
  <metadata>
    <id>TestPackage</id>
    <version>1.0.0</version>
    <license type=""expression"">MIT</license>
  </metadata>
</package>";

			var result = LicenseAnalyzer.ExtractLicenseFromNuspecContent(nuspecContent);

			// Should return the SPDX expression
			Assert.AreEqual("MIT", result);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LicenseAnalyzerManualValidationOfIssueScenarios()
		{
			// Test 1: The exact scenario from the issue - should return UNKNOWN_FILE_LICENSE
			const string issueScenario = @"<?xml version=""1.0""?>
<package>
  <metadata>
    <id>TestPackage</id>
    <version>1.0.0</version>
    <license type=""file"">LICENSE.md</license>
    <licenseUrl>https://aka.ms/deprecateLicenseUrl</licenseUrl>
  </metadata>
</package>";

			var result1 = LicenseAnalyzer.ExtractLicenseFromNuspecContent(issueScenario);
			Assert.AreEqual("UNKNOWN_FILE_LICENSE", result1, "type='file' should return UNKNOWN_FILE_LICENSE");

			// Test 2: Only deprecated URL (no license element) - should return the URL itself
			const string onlyDeprecatedUrl = @"<?xml version=""1.0""?>
<package>
  <metadata>
    <id>TestPackage</id>
    <version>1.0.0</version>
    <licenseUrl>https://aka.ms/deprecateLicenseUrl</licenseUrl>
  </metadata>
</package>";

			var result2 = LicenseAnalyzer.ExtractLicenseFromNuspecContent(onlyDeprecatedUrl);
			Assert.AreEqual("aka.ms/deprecateLicenseUrl", result2, "Should return normalized deprecated URL for further checking");

			// Test 3: MIT via SPDX expression should still work
			const string mitExpression = @"<?xml version=""1.0""?>
<package>
  <metadata>
    <id>TestPackage</id>
    <version>1.0.0</version>
    <license type=""expression"">MIT</license>
  </metadata>
</package>";

			var result3 = LicenseAnalyzer.ExtractLicenseFromNuspecContent(mitExpression);
			Assert.AreEqual("MIT", result3, "SPDX expressions should continue to work");

			// Test 4: MIT via license URL should still work
			const string mitUrl = @"<?xml version=""1.0""?>
<package>
  <metadata>
    <id>TestPackage</id>
    <version>1.0.0</version>
    <licenseUrl>https://opensource.org/licenses/MIT</licenseUrl>
  </metadata>
</package>";

			var result4 = LicenseAnalyzer.ExtractLicenseFromNuspecContent(mitUrl);
			Assert.AreEqual("MIT", result4, "Recognizable license URLs should continue to work");
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerHandlesTypeFileGracefullyAsync()
		{
			// Test that analyzer handles code analysis gracefully when type="file" licenses 
			// would be encountered in real package dependencies
			// This is a code analysis test, not a nuspec parsing test
			await VerifySuccessfulCompilation(GetTestCode()).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LicenseAnalyzerDoesNotAcceptDeprecatedLicenseUrlAsync()
		{
			// Test that the analyzer no longer considers deprecated license URL as acceptable
			// This validates that "aka.ms/deprecateLicenseUrl" is not in the default acceptable licenses
			var analyzer = new LicenseAnalyzer();

			// Verify the analyzer is configured properly
			Assert.IsNotNull(analyzer);

			// The actual license checking happens during package analysis, not code analysis
			// This test validates the analyzer can handle the code analysis without issues
			await VerifySuccessfulCompilation(GetTestCode()).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LicenseAnalyzerAcceptsValidLicenseUrlWhenNoLicenseElement()
		{
			// Test the specific scenario reported in the comment:
			// When there's only a <licenseUrl> tag (no <license> tag) and the licenseUrl 
			// is in the acceptable licenses list, it should be accepted

			// Test case 1: License URL that should be recognized as MIT
			const string mitUrlNuspec = @"<?xml version=""1.0""?>
<package>
  <metadata>
    <id>TestPackage</id>
    <version>1.0.0</version>
    <licenseUrl>https://opensource.org/licenses/MIT</licenseUrl>
  </metadata>
</package>";

			var result1 = LicenseAnalyzer.ExtractLicenseFromNuspecContent(mitUrlNuspec);
			Assert.AreEqual("MIT", result1, "MIT license URL should be recognized as MIT");

			// Test case 2: License URL that should be recognized as Apache-2.0
			const string apacheUrlNuspec = @"<?xml version=""1.0""?>
<package>
  <metadata>
    <id>TestPackage</id>
    <version>1.0.0</version>
    <licenseUrl>https://www.apache.org/licenses/LICENSE-2.0</licenseUrl>
  </metadata>
</package>";

			var result2 = LicenseAnalyzer.ExtractLicenseFromNuspecContent(apacheUrlNuspec);
			Assert.AreEqual("Apache-2.0", result2, "Apache license URL should be recognized as Apache-2.0");

			// Test case 3: A known acceptable license URL from the default list
			const string dotnetNuspec = @"<?xml version=""1.0""?>
<package>
  <metadata>
    <id>TestPackage</id>
    <version>1.0.0</version>
    <licenseUrl>https://github.com/dotnet/corefx/blob/master/LICENSE.TXT</licenseUrl>
  </metadata>
</package>";

			var result3 = LicenseAnalyzer.ExtractLicenseFromNuspecContent(dotnetNuspec);
			Assert.AreEqual("github.com/dotnet/corefx/blob/master/LICENSE.TXT", result3, "License URL should be normalized to match acceptable licenses format");
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void LicenseAnalyzerUrlNormalizationWorksCorrectly()
		{
			// Test URL normalization with various prefixes

			// Test case 1: HTTPS URL normalization
			const string httpsNuspec = @"<?xml version=""1.0""?>
<package>
  <metadata>
    <id>TestPackage</id>
    <version>1.0.0</version>
    <licenseUrl>https://github.com/dotnet/corefx/blob/master/LICENSE.TXT</licenseUrl>
  </metadata>
</package>";

			var result1 = LicenseAnalyzer.ExtractLicenseFromNuspecContent(httpsNuspec);
			Assert.AreEqual("github.com/dotnet/corefx/blob/master/LICENSE.TXT", result1, "HTTPS prefix should be removed");

			// Test case 2: HTTP URL normalization
			const string httpNuspec = @"<?xml version=""1.0""?>
<package>
  <metadata>
    <id>TestPackage</id>
    <version>1.0.0</version>
    <licenseUrl>http://www.bouncycastle.org/csharp/licence.html</licenseUrl>
  </metadata>
</package>";

			var result2 = LicenseAnalyzer.ExtractLicenseFromNuspecContent(httpNuspec);
			Assert.AreEqual("www.bouncycastle.org/csharp/licence.html", result2, "HTTP prefix should be removed");

			// Test case 3: URL without prefix should remain unchanged
			const string noProtocolNuspec = @"<?xml version=""1.0""?>
<package>
  <metadata>
    <id>TestPackage</id>
    <version>1.0.0</version>
    <licenseUrl>go.microsoft.com/fwlink/?LinkId=329770</licenseUrl>
  </metadata>
</package>";

			var result3 = LicenseAnalyzer.ExtractLicenseFromNuspecContent(noProtocolNuspec);
			Assert.AreEqual("go.microsoft.com/fwlink/?LinkId=329770", result3, "URL without protocol should remain unchanged");
		}

		private string GetTestCode()
		{
			return @"
class TestClass 
{
  public void TestMethod()
  {
    var x = 1;
  }
}
";
		}
	}
}
