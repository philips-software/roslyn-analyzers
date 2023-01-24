// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation;

namespace Philips.CodeAnalysis.Test.Maintainability.Documentation
{
	[TestClass]
	public class DocumentThrownExceptionsAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
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

		private const string CorrectWithAlias = @"
using MyException = System.ArgumentException;
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""ArgumentException"">
    public void MethodA()
    {
        throw new MyException(""Error"");
    }
}
";

		private const string CorrectInProperty = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""ArgumentOutOfRangeException"">
    public int Index
    {
        get
        {
            throw new ArgumentOutOfRangeException(""Error"");
        }
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

    private ArgumentException CreateException() { return new ArgumentException(""FromFactory"");}
}
";

        private const string CorrectRethrow = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""ArgumentException"">
    public void MethodA()
    {
        try {
            DangerousMethod();
        } catch (ArgumentException ex) {
            throw ex;
        }
    }

    private ArgumentException DangerousMethod() { return new ArgumentException(""FromFactory"");}
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

        private const string WrongInProperty = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    public int Index
    {
        get
        {
            throw new ArgumentOutOfRangeException(""Error"");
        }
    }
}
";

        private const string WrongRethrow = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    public void MethodA()
    {
        try {
            DangerousMethod();
        } catch (Exception ex) {
            throw ex;
        }
    }

    private ArgumentException DangerousMethod() { return new ArgumentException(""FromFactory"");}
}
";

        [DataTestMethod]
		[DataRow(CorrectNoThrow, DisplayName = nameof(CorrectNoThrow)),
		 DataRow(CorrectWithThrow, DisplayName = nameof(CorrectWithThrow)),
		 DataRow(CorrectWithAlias, DisplayName = nameof(CorrectWithAlias)),
		 DataRow(CorrectInProperty, DisplayName = nameof(CorrectInProperty)),
		 DataRow(CorrectWithMethod, DisplayName = nameof(CorrectWithMethod)),
		 DataRow(CorrectRethrow, DisplayName = nameof(CorrectRethrow))]
		public void CorrectCodeShouldNotTriggerAnyDiagnostics(string testCode)
		{
			VerifyDiagnostic(testCode);
		}

		[DataTestMethod]
		[DataRow(WrongNoDoc, DisplayName = nameof(WrongNoDoc)),
		 DataRow(WrongNoCref, DisplayName = nameof(WrongNoCref)),
		 DataRow(WrongEmptyCref, DisplayName = nameof(WrongEmptyCref)),
         DataRow(WrongType, DisplayName = nameof(WrongType)),
		 DataRow(WrongInProperty, DisplayName = nameof(WrongInProperty)),
		 DataRow(WrongRethrow, DisplayName = nameof(WrongRethrow))]
		public void MissingOrWrongDocumentationShouldTriggerDiagnostic(string testCode)
		{
			VerifyDiagnostic(testCode, DiagnosticResultHelper.Create(DiagnosticIds.DocumentThrownExceptions));
		}
	}
}
