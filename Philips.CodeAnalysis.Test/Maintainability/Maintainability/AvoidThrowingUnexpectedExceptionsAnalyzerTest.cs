// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidThrowingUnexpectedExceptionsAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidThrowingUnexpectedExceptionsAnalyzer();
		}

		private const string CorrectExplicitCast = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""ArgumentException"">
    public static explicit operator int(Foo f1)
    {
        throw new ArgumentException(""Error"");
    }
}
";

		private const string CorrectOperatorPlus = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""ArgumentException"">
    public static Foo operator +(Foo f1, int i)
    {
        throw new ArgumentException(""Error"");
    }
}
";

		private const string WrongStaticConstructor = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""ArgumentException"">
    static Foo()
    {
        throw new ArgumentException(""Error"");
    }
}
";

		private const string WrongFinalizer = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""ArgumentException"">
    ~Foo()
    {
        throw new ArgumentException(""Error"");
    }
}
";

		private const string WrongDispose = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""ArgumentException"">
    public void Dispose()
    {
        throw new ArgumentException(""Error"");
    }
}
";

		private const string WrongToString = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""ArgumentException"">
    public override string ToString()
    {
        throw new ArgumentException(""Error"");
    }
}
";

		private const string WrongEquals = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""ArgumentException"">
    public override bool Equals(object other)
    {
        throw new ArgumentException(""Error"");
    }
}
";

		private const string WrongGetHashCode = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""ArgumentException"">
    public override int GetHashCode()
    {
        throw new ArgumentException(""Error"");
    }
}
";

		private const string WrongOperatorEquals = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""ArgumentException"">
    public static bool operator ==(Foo f1, Foo f2)
    {
        throw new ArgumentException(""Error"");
    }
}
";

		private const string WrongOperatorNotEquals = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""ArgumentException"">
    public static bool operator !=(Foo f1, Foo f2)
    {
        throw new ArgumentException(""Error"");
    }
}
";

		private const string WrongImplicitCast = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""ArgumentException"">
    public static implicit operator int(Foo f1)
    {
        throw new ArgumentException(""Error"");
    }
}
";

		private const string WrongExceptionConstructor = @"
public class FooException
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""ArgumentException"">
    public Foo()
    {
        throw new ArgumentException(""Error"");
    }
}
";

		[DataTestMethod]
		[DataRow(CorrectExplicitCast, DisplayName = nameof(CorrectExplicitCast)),
		 DataRow(CorrectOperatorPlus, DisplayName = nameof(CorrectOperatorPlus))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CorrectCodeShouldNotTriggerAnyDiagnosticsAsync(string testCode)
		{
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow(WrongStaticConstructor, ".*static constructor.*", DisplayName = nameof(WrongStaticConstructor)),
		 DataRow(WrongFinalizer, ".*finalizer.*", DisplayName = nameof(WrongFinalizer)),
		 DataRow(WrongDispose, ".*Dispose.*", DisplayName = nameof(WrongDispose)),
		 DataRow(WrongToString, ".*ToString.*", DisplayName = nameof(WrongToString)),
		 DataRow(WrongEquals, ".*Equals.*", DisplayName = nameof(WrongEquals)),
		 DataRow(WrongGetHashCode, ".*GetHashCode.*", DisplayName = nameof(WrongGetHashCode)),
		 DataRow(WrongOperatorEquals, ".*equality comparison operator.*", DisplayName = nameof(WrongOperatorEquals)),
		 DataRow(WrongOperatorNotEquals, ".*equality comparison operator.*", DisplayName = nameof(WrongOperatorNotEquals)),
		 DataRow(WrongImplicitCast, ".*implicit cast.*", DisplayName = nameof(WrongImplicitCast)),
		 DataRow(WrongExceptionConstructor, ".*constructor of an Exception derived type.*", DisplayName = nameof(WrongExceptionConstructor))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MissingOrWrongDocumentationShouldTriggerDiagnosticAsync(string testCode, string regex)
		{
			await VerifyDiagnostic(testCode, regex: regex).ConfigureAwait(false);
		}
	}
}
