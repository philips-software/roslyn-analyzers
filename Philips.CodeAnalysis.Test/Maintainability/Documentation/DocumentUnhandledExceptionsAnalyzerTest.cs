// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Documentation
{
	[TestClass]
	public class DocumentUnhandledExceptionsAnalyzerTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new DocumentUnhandledExceptionsAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new DocumentThrownExceptionsCodeFixProvider();
		}

		private const string CorrectCatchAll = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    public void MethodA()
    {
        try {
            System.IO.Directory.CreateDirectory(""abc"");
        } catch (System.Exception ex) {
        }
    }
}
";

		private const string CorrectCatchAllAlias = @"
using System;
using MyException = System.Exception;
public class Foo
{
    /// <summary> Helpful text. </summary>
    public void MethodA()
    {
        try {
            System.IO.Directory.CreateDirectory(""abc"");
        } catch (MyException ex) {
        }
    }
}
";

		private const string CorrectEnumerateFiles = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
	/// <exception cref=""System.IO.IOException"">abc</exception>
	/// <exception cref=""System.SecurityException"">abc</exception>
	/// <exception cref=""System.ArgumentOutOfRangeException"">abc</exception>
	/// <exception cref=""System.ArgumentException"">abc</exception>
	/// <exception cref=""System.ArgumentNullException"">abc</exception>
	/// <exception cref=""System.IO.PathTooLongException"">abc</exception>
	/// <exception cref=""System.IO.DirectoryNotFoundException"">abc</exception>
    /// <exception cref=""System.IndexOutOfRangeException"">abc</exception>
	/// <exception cref=""System.OutOfMemoryException"">abc</exception>
    public void MethodA()
    {
        System.IO.Directory.EnumerateFiles(""abc"");
    }
}
";

		private const string CorrectEnumerateDirectories = @"
using System;
using System.IO;
public class Foo
{
    /// <summary> Helpful text. </summary>
	/// <exception cref=""IOException"">abc</exception>
	/// <exception cref=""SecurityException"">abc</exception>
	/// <exception cref=""ArgumentOutOfRangeException"">abc</exception>
	/// <exception cref=""ArgumentException"">abc</exception>
	/// <exception cref=""ArgumentNullException"">abc</exception>
	/// <exception cref=""PathTooLongException"">abc</exception>
	/// <exception cref=""DirectoryNotFoundException"">abc</exception>
    /// <exception cref=""System.IndexOutOfRangeException"">abc</exception>
	/// <exception cref=""System.OutOfMemoryException"">abc</exception>
    public void MethodA()
    {
        Directory.EnumerateFiles(""abc"");
    }
}
";

		private const string WrongDirectoryCreate = @"
using System.IO;
public class Foo
{
    /// <summary> Helpful text. </summary>
    public void MethodA()
    {
        try {
            Directory.CreateDirectory(""abc"");
        } catch (IOException ex) {
        }
    }
}
";

		private const string FixedDirectoryCreate = @"
using System.IO;
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""System.ArgumentNullException""></exception>
    /// <exception cref=""System.ArgumentException""></exception>
    /// <exception cref=""System.IO.IOException""></exception>
    /// <exception cref=""System.IO.FileNotFoundException""></exception>
    /// <exception cref=""System.IO.DirectoryNotFoundException""></exception>
    /// <exception cref=""System.IO.PathTooLongException""></exception>
    /// <exception cref=""System.UnauthorizedException""></exception>
    /// <exception cref=""System.Globalization.CultureNotFoundException""></exception>
    /// <exception cref=""System.IndexOutOfRangeException""></exception>
    /// <exception cref=""System.OutOfMemoryException""></exception>
    /// <exception cref=""System.ArgumentOutOfRangeException""></exception>
    /// <exception cref=""System.Runtime.InteropServices.ExternalException""></exception>
	public void MethodA()
    {
        try {
            Directory.CreateDirectory(""abc"");
        } catch (IOException ex) {
        }
    }
}
";

		private const string FixedDirectoryCreateOnLinux = @"
using System.IO;
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""System.ArgumentNullException""></exception>
    /// <exception cref=""System.ArgumentException""></exception>
    /// <exception cref=""System.IO.IOException""></exception>
    /// <exception cref=""System.IO.FileNotFoundException""></exception>
    /// <exception cref=""System.IO.DirectoryNotFoundException""></exception>
    /// <exception cref=""System.IO.PathTooLongException""></exception>
    /// <exception cref=""System.UnauthorizedException""></exception>
    /// <exception cref=""System.IndexOutOfRangeException""></exception>
	public void MethodA()
    {
        try {
            Directory.CreateDirectory(""abc"");
        } catch (IOException ex) {
        }
    }
}
";

		private const string WrongEnumerateFiles = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    public void MethodA()
    {
        try {
            System.IO.Directory.EnumerateFiles(""abc"");
        } catch (System.IO.IOException ex) {
        }
    }
}
";

		private const string FixedEnumerateFiles = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
	/// <exception cref=""System.ArgumentNullException""></exception>
	/// <exception cref=""System.ArgumentException""></exception>
	/// <exception cref=""System.ArgumentOutOfRangeException""></exception>
    /// <exception cref=""System.IndexOutOfRangeException""></exception>
	/// <exception cref=""System.OutOfMemoryException""></exception>
	public void MethodA()
    {
        try {
            System.IO.Directory.EnumerateFiles(""abc"");
        } catch (System.IO.IOException ex) {
        }
    }
}
";

		private const string WrongDangerous = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    public void MethodA()
    {
        try {
            Dangerous();
        } catch (System.IO.IOException ex) {
        }
    }
    /// <summary> Helpful text. </summary>
	/// <exception cref=""System.IO.IOException"">abc</exception>
	/// <exception cref=""System.SecurityException"">abc</exception>
	/// <exception cref=""System.ArgumentOutOfRangeException"">abc</exception>
	/// <exception cref=""System.ArgumentException"">abc</exception>
	/// <exception cref=""System.ArgumentNullException"">abc</exception>
	/// <exception cref=""System.IO.PathTooLongException"">abc</exception>
	/// <exception cref=""System.IO.DirectoryNotFoundException"">abc</exception>
    public bool Dangerous()
    {
        return System.IO.Directory.EnumerateFiles(""abc"").Any();
    }
}
";

		private const string FixedDangerous = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    public void MethodA()
    {
        try {
            Dangerous();
        } catch (System.IO.IOException ex) {
        }
    }
    /// <summary> Helpful text. </summary>
	/// <exception cref=""System.IO.IOException"">abc</exception>
	/// <exception cref=""System.SecurityException"">abc</exception>
	/// <exception cref=""System.ArgumentOutOfRangeException"">abc</exception>
	/// <exception cref=""System.ArgumentException"">abc</exception>
	/// <exception cref=""System.ArgumentNullException"">abc</exception>
	/// <exception cref=""System.IO.PathTooLongException"">abc</exception>
	/// <exception cref=""System.IO.DirectoryNotFoundException"">abc</exception>
    /// <exception cref=""System.IndexOutOfRangeException""></exception>
	/// <exception cref=""System.OutOfMemoryException""></exception>
	public bool Dangerous()
    {
        return System.IO.Directory.EnumerateFiles(""abc"").Any();
    }
}
";

		[DataTestMethod]
		[DataRow(CorrectCatchAll, DisplayName = nameof(CorrectCatchAll)),
		 DataRow(CorrectCatchAllAlias, DisplayName = nameof(CorrectCatchAllAlias)),
		 DataRow(CorrectEnumerateFiles, DisplayName = nameof(CorrectEnumerateFiles)),
		 DataRow(CorrectEnumerateDirectories, DisplayName = nameof(CorrectEnumerateDirectories))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CorrectCodeShouldNotTriggerAnyDiagnosticsAsync(string testCode)
		{
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow(WrongDirectoryCreate, FixedDirectoryCreate, FixedDirectoryCreateOnLinux, DisplayName = nameof(WrongDirectoryCreate)),
		 DataRow(WrongEnumerateFiles, FixedEnumerateFiles, null, DisplayName = nameof(WrongEnumerateFiles)),
		 DataRow(WrongDangerous, FixedDangerous, null, DisplayName = nameof(WrongDangerous))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MissingOrWrongDocumentationShouldTriggerDiagnostic(string testCode, string fixedCode, string fixedCodeOnLinux)
		{
			await VerifyDiagnostic(testCode).ConfigureAwait(false);
			var expectedFixedCode = fixedCode;
			if (fixedCodeOnLinux != null && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				expectedFixedCode = fixedCodeOnLinux;
			}
			await VerifyFix(testCode, expectedFixedCode).ConfigureAwait(false);
		}
	}
}
