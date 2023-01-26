// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation;

namespace Philips.CodeAnalysis.Test.Maintainability.Documentation
{
	[TestClass]
	public class ThrowInformationalExceptionsTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new DocumentThrownExceptionsAnalyzer();
		}

		private const string CorrectWithLiteral = @"
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

		private const string CorrectWithProperty = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""ArgumentException"">
    public void MethodA()
    {
        throw new ArgumentException(ExceptionMessage);
    }
    private string ExceptionMessage { get; }
}
";

		private const string CorrectWithMethod = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""ArgumentException"">
    public void MethodA()
    {
        throw new ArgumentException(Foo.GetExceptionMessage());
    }
    private static string GetExceptionMessage() { return ""Error""; }
}
";

        private const string CorrectWithLocalVar = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""ArgumentException"">
    public void MethodA()
    {
        string message = ""Error"";
        throw new ArgumentException(message);
    }
}
";

        private const string CorrectWithNameOf = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""ArgumentException"">
    public void MethodA(int num)
    {
        throw new ArgumentException(nameof(num));
    }
}
";

        private const string CorrectInterpolatedString = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""ArgumentException"">
    public void MethodA(int num)
    {
        throw new ArgumentException($""The paths '{fromDirectory} and '{toPath}' have different path roots."");
    }
}
";

		private const string WrongNoArguments = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""ArgumentException"">
    public void MethodA()
    {
        throw new ArgumentException();
    }
}
";

		private const string WrongIssue273 = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""NotImplementedException"">
    protected override DiagnosticResult GetExpectedDiagnostic(int expectedLineNumberErrorOffset = 0, int expectedColumnErrorOffset = 0)
    {
        throw new System.NotImplementedException();
    }
}
";


		[DataTestMethod]
		[DataRow(CorrectWithLiteral, DisplayName = nameof(CorrectWithLiteral)),
		 DataRow(CorrectWithLocalVar, DisplayName = nameof(CorrectWithLocalVar)),
		 DataRow(CorrectWithNameOf, DisplayName = nameof(CorrectWithNameOf)),
		 DataRow(CorrectWithProperty, DisplayName = nameof(CorrectWithProperty)),
		 DataRow(CorrectWithMethod, DisplayName = nameof(CorrectWithMethod)),
		 DataRow(CorrectInterpolatedString, DisplayName = nameof(CorrectInterpolatedString))]
		public void CorrectCodeShouldNotTriggerAnyDiagnostics(string testCode)
		{
			VerifySuccessfulCompilation(testCode);
		}

		[DataTestMethod]
		[DataRow(WrongNoArguments, DisplayName = nameof(WrongNoArguments)),
		DataRow(WrongIssue273, DisplayName = nameof(WrongIssue273))]
		public void MissingOrWrongDocumentationShouldTriggerDiagnostic(string testCode)
		{
			VerifyDiagnostic(testCode, DiagnosticResultHelper.Create(DiagnosticIds.ThrowInformationalExceptions, new Regex("Specify context to the .+, by using a constructor overload that sets the Message property.")));
		}
	}
}
