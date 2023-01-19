// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation;

namespace Philips.CodeAnalysis.Test.Maintainability.Documentation
{
	[TestClass]
	public class DocumentThrownExceptionsAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new DocumentThrownExceptionsAnalyzer();
		}

		private const string CorrectNoThrow = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    public void MethodA()
    {
    }
}
";

		private const string CorrectWithThrow = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""ArgumentException"">
    public void MethodA()
    {
        throw new ArgumentException(""Error"");
    }
}
";

		private const string CorrectWithMethod = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""ArgumentException"">
    public void MethodA()
    {
        throw CreateException();
    }

    private ArgumentException CreateEzxception() { return new ArgumentException(""FromFactory"");}
}
";

        private const string WrongNoDoc = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    public void MethodA()
    {
        throw new ArgumentException(""Error"");
    }
}
";

		private const string WrongNoCref = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception>
    public void MethodA()
    {
        throw new ArgumentException(""Error"");
    }
}
";

		private const string WrongEmptyCref = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref="""">
    public void MethodA()
    {
        throw new ArgumentException(""Error"");
    }
}
";

        private const string WrongType = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""InvalidOperationException"">
    public void MethodA()
    {
        throw new ArgumentException(""Error"");
    }
}
";

		[DataTestMethod]
		[DataRow(CorrectNoThrow, DisplayName = nameof(CorrectNoThrow)),
		 DataRow(CorrectWithThrow, DisplayName = nameof(CorrectWithThrow)),
		 DataRow(CorrectWithMethod, DisplayName = nameof(CorrectWithMethod))]
		public void CorrectCodeShouldNotTriggerAnyDiagnostics(string testCode)
		{
			VerifyCSharpDiagnostic(testCode);
		}

		[DataTestMethod]
		[DataRow(WrongNoDoc, DisplayName = nameof(WrongNoDoc)),
		 DataRow(WrongNoCref, DisplayName = nameof(WrongNoCref)),
		 DataRow(WrongEmptyCref, DisplayName = nameof(WrongEmptyCref)),
         DataRow(WrongType, DisplayName = nameof(WrongType))]
		public void MissingOrWrongDocumentationShouldTriggerDiagnostic(string testCode)
		{
			VerifyCSharpDiagnostic(testCode, DiagnosticResultHelper.Create(DiagnosticIds.DocumentThrownExceptions));
		}
	}
}
