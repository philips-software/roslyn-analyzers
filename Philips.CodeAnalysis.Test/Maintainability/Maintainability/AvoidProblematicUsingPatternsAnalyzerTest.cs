// © 2024 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidProblematicUsingPatternsAnalyzerTest : DiagnosticVerifier
	{
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenUsingFieldDirectlyDiagnosticIsTriggeredAsync()
		{
			var source = @"
using System;
using System.IO;

class TestClass 
{
    private Stream _stream;

    public void TestMethod() 
    {
        using (_stream)
        {
            // Do something
        }
    }
}";
			await VerifyDiagnostic(source).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenUsingVariableAssignmentFromFieldDiagnosticIsTriggeredAsync()
		{
			var source = @"
using System;
using System.IO;

class TestClass 
{
    private Stream _stream;

    public void TestMethod() 
    {
        using (var localStream = _stream)
        {
            // Do something
        }
    }
}";
			await VerifyDiagnostic(source).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenUsingVariableAssignmentFromVariableDiagnosticIsTriggeredAsync()
		{
			var source = @"
using System;
using System.IO;

class TestClass 
{
    public void TestMethod() 
    {
        Stream existingStream = new MemoryStream();
        using (var localStream = existingStream)
        {
            // Do something
        }
    }
}";
			await VerifyDiagnostic(source).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenUsingMemberAccessDiagnosticIsTriggeredAsync()
		{
			var source = @"
using System;
using System.IO;

class TestClass 
{
    public Stream Stream { get; set; }

    public void TestMethod() 
    {
        using (this.Stream)
        {
            // Do something
        }
    }
}";
			await VerifyDiagnostic(source).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenUsingNewObjectNoDiagnosticIsTriggeredAsync()
		{
			var source = @"
using System;
using System.IO;

class TestClass 
{
    public void TestMethod() 
    {
        using (var stream = new MemoryStream())
        {
            // Do something
        }
    }
}";
			await VerifySuccessfulCompilation(source).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenUsingMethodCallNoDiagnosticIsTriggeredAsync()
		{
			var source = @"
using System;
using System.IO;

class TestClass 
{
    public void TestMethod() 
    {
        using (var stream = CreateStream())
        {
            // Do something
        }
    }

    private Stream CreateStream()
    {
        return new MemoryStream();
    }
}";
			await VerifySuccessfulCompilation(source).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenUsingParameterNoDiagnosticIsTriggeredAsync()
		{
			var source = @"
using System;
using System.IO;

class TestClass 
{
    public void TestMethod(Stream inputStream) 
    {
        using (var stream = inputStream)
        {
            // Do something
        }
    }
}";
			await VerifySuccessfulCompilation(source).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenUsingOutParameterNoDiagnosticIsTriggeredAsync()
		{
			var source = @"
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

class TestClass 
{
    public void TestMethod() 
    {
        TryProcessResult(out X509Certificate2 issuedCertificate);
        using (issuedCertificate)
        {
            // Do something
        }
    }

    private bool TryProcessResult(out X509Certificate2 certificate)
    {
        certificate = new X509Certificate2();
        return true;
    }
}";
			await VerifySuccessfulCompilation(source).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhenUsingComplexOutParameterNoDiagnosticIsTriggeredAsync()
		{
			var source = @"
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

class TestClass 
{
    public void TestMethod() 
    {
        string response = ""test"";
        string challengePassword = ""password"";
        
        TryProcessResult(response, challengePassword, out X509Certificate2 issuedCertificate, new object(), out var other);
        
        using (issuedCertificate)
        {
            // Do something with certificate
        }
    }

    private bool TryProcessResult(string resp, string pwd, out X509Certificate2 cert, object attacher, out object other)
    {
        cert = new X509Certificate2();
        other = new object();
        return true;
    }
}";
			await VerifySuccessfulCompilation(source).ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidProblematicUsingPatternsAnalyzer();
		}
	}
}