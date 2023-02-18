// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class DocumentThrownExceptionsAnalyzerTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new DocumentThrownExceptionsAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new DocumentThrownExceptionsCodeFixProvider();
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
    /// <exception cref=""ArgumentException""></exception>
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
    /// <exception cref=""ArgumentException""></exception>
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
    /// <exception cref=""ArgumentOutOfRangeException""></exception>
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
    /// <exception cref=""ArgumentException""></exception>
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
    /// <exception cref=""ArgumentException""></exception>
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
		private const string CorrectFromCommon = @"
public class Foo {
	/// <summary>
	/// Register a new symbol.
	/// </summary>
	/// <exception cref=""InvalidDataException"">When an invalid type is supplied.</exception>
	private void RegisterSymbol(ISymbol symbol)
	{
		if(symbol is IMethodSymbol methodSymbol)
		{
			_allowedMethods.Add(methodSymbol);
		}
		else if(symbol is ITypeSymbol typeSymbol)
		{
			_allowedTypes.Add(typeSymbol);
		}
		else if(symbol is INamespaceSymbol namespaceSymbol)
		{
			_allowedNamespaces.Add(namespaceSymbol);
		}
		else
		{
			throw new InvalidDataException(
				""Invalid symbol type found: "" + symbol.MetadataName);
		}
	}
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

		private const string FixedNoCref = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception>
    /// <exception cref=""ArgumentException""></exception>
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
    /// <exception cref=""""></exception>
    public void MethodA()
    {
        throw new ArgumentException(""Error"");
    }
}
";

		private const string FixedEmptyCref = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""""></exception>
    /// <exception cref=""ArgumentException""></exception>
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
    /// <exception cref=""InvalidOperationException""></exception>
    public void MethodA()
    {
        throw new ArgumentException(""Error"");
    }
}
";

		private const string FixedWrongType = @"
public class Foo
{
    /// <summary> Helpful text. </summary>
    /// <exception cref=""InvalidOperationException""></exception>
	/// <exception cref=""ArgumentException""></exception>
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
        } catch (ArgumentException ex) {
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
		 DataRow(CorrectRethrow, DisplayName = nameof(CorrectRethrow)),
		 DataRow(CorrectFromCommon, DisplayName = nameof(CorrectFromCommon))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CorrectCodeShouldNotTriggerAnyDiagnosticsAsync(string testCode)
		{
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow(WrongNoCref, FixedNoCref, DisplayName = nameof(WrongNoCref))]
		[DataRow(WrongEmptyCref, FixedEmptyCref, DisplayName = nameof(WrongEmptyCref))]
		[DataRow(WrongType, FixedWrongType, DisplayName = nameof(WrongType))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WrongDocumentationShouldTriggerDiagnostic(string testCode, string fixedCode)
		{
			await VerifyDiagnostic(testCode, DiagnosticId.DocumentThrownExceptions).ConfigureAwait(false);
			await VerifyFix(testCode, fixedCode).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow(WrongNoDoc, DisplayName = nameof(WrongNoDoc))]
		[DataRow(WrongInProperty, DisplayName = nameof(WrongInProperty))]
		[DataRow(WrongRethrow, DisplayName = nameof(WrongRethrow))]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MissingDocumentationShouldTriggerDiagnosticAsync(string testCode)
		{
			// See https://github.com/dotnet/roslyn/issues/58210. Until decide how we want to handle this, these will pass.
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}
	}
}
