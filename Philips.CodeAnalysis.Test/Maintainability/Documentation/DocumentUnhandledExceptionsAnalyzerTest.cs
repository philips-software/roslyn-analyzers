// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation;

namespace Philips.CodeAnalysis.Test.Maintainability.Documentation
{
	[TestClass]
	public class DocumentUnhandledExceptionsAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new DocumentUnhandledExceptionsAnalyzer();
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
    public void Dangerous()
    {
        System.IO.Directory.EnumerateFiles(""abc"");
    }
}
";

		[DataTestMethod]
		[DataRow(CorrectCatchAll, DisplayName = nameof(CorrectCatchAll)),
		 DataRow(CorrectCatchAllAlias, DisplayName = nameof(CorrectCatchAllAlias)),
		 DataRow(CorrectEnumerateFiles, DisplayName = nameof(CorrectEnumerateFiles)),
		 DataRow(CorrectEnumerateDirectories, DisplayName = nameof(CorrectEnumerateDirectories))]
		public void CorrectCodeShouldNotTriggerAnyDiagnostics(string testCode)
		{
			VerifySuccessfulCompilation(testCode);
		}

		[DataTestMethod]
		[DataRow(WrongDirectoryCreate, DisplayName = nameof(WrongDirectoryCreate)),
		 DataRow(WrongEnumerateFiles, DisplayName = nameof(WrongEnumerateFiles)),
		 DataRow(WrongDangerous, DisplayName = nameof(WrongDangerous))]
		public void MissingOrWrongDocumentationShouldTriggerDiagnostic(string testCode)
		{
			VerifyDiagnostic(testCode, DiagnosticResultHelper.Create(DiagnosticIds.DocumentUnhandledExceptions));
		}
	}
}
