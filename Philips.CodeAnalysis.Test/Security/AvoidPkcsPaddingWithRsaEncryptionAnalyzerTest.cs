// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.SecurityAnalyzers;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Security
{
	[TestClass]
	public class AvoidPkcsPaddingWithRsaEncryptionAnalyzerTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidPkcsPaddingWithRsaEncryptionAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AvoidPkcsPaddingWithRsaEncryptionCodeFixProvider();
		}

		protected override ImmutableArray<MetadataReference> GetMetadataReferences()
		{
			var cryptographyReference = typeof(RSA).Assembly.Location;
			MetadataReference reference = MetadataReference.CreateFromFile(cryptographyReference);

			return base.GetMetadataReferences().Add(reference);
		}

		private string GetTemplate()
		{
			return @"
using System;
using System.Security.Cryptography;
using System.Text;

namespace AvoidPkcsPaddingWithRsaEncryptionTest
{{
	public class Foo 
	{{
		public void TestMethod()
		{{
			using RSA rsa = RSA.Create(2048);
			string originalData = ""sensitive data"";
			byte[] dataToEncrypt = Encoding.UTF8.GetBytes(originalData);
			
			{0}
		}}
	}}
}}
";
		}

		[DataTestMethod]
		[DataRow(@"byte[] encrypted = rsa.Encrypt(dataToEncrypt, RSAEncryptionPadding.Pkcs1);")]
		[DataRow(@"byte[] decrypted = rsa.Decrypt(dataToEncrypt, RSAEncryptionPadding.Pkcs1);")]
		[DataRow(@"RSAEncryptionPadding padding = RSAEncryptionPadding.Pkcs1;")]
		[DataRow(@"var padding = RSAEncryptionPadding.Pkcs1;")]
		[DataRow(@"byte[] encrypted = rsa.Encrypt(dataToEncrypt, System.Security.Cryptography.RSAEncryptionPadding.Pkcs1);")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task Pkcs1PaddingShouldTriggerDiagnosticAsync(string content)
		{
			var format = GetTemplate();
			var testCode = string.Format(format, content);
			await VerifyDiagnostic(testCode, DiagnosticId.AvoidPkcsPaddingWithRsaEncryption).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow(@"byte[] encrypted = rsa.Encrypt(dataToEncrypt, RSAEncryptionPadding.OaepSHA1);")]
		[DataRow(@"byte[] encrypted = rsa.Encrypt(dataToEncrypt, RSAEncryptionPadding.OaepSHA256);")]
		[DataRow(@"byte[] decrypted = rsa.Decrypt(dataToEncrypt, RSAEncryptionPadding.OaepSHA256);")]
		[DataRow(@"RSAEncryptionPadding padding = RSAEncryptionPadding.OaepSHA1;")]
		[DataRow(@"var padding = RSAEncryptionPadding.OaepSHA256;")]
		[DataRow(@"byte[] encrypted = rsa.Encrypt(dataToEncrypt, System.Security.Cryptography.RSAEncryptionPadding.OaepSHA256);")]
		[DataRow(@"// This is just a comment about Pkcs1")]
		[DataRow(@"string text = ""This mentions Pkcs1 but is not crypto"";")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task SecurePaddingShouldNotTriggerDiagnosticAsync(string content)
		{
			var format = GetTemplate();
			var testCode = string.Format(format, content);
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DoesNotTriggerDiagnosticInTestCodeAsync()
		{
			const string template = @"
using System;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Foo
{
	[TestMethod]
	public void Test()
	{
		using RSA rsa = RSA.Create(2048);
		byte[] data = new byte[10];
		byte[] encrypted = rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);
	}
}
";
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow(
			@"byte[] encrypted = rsa.Encrypt(dataToEncrypt, RSAEncryptionPadding.Pkcs1);",
			@"byte[] encrypted = rsa.Encrypt(dataToEncrypt, RSAEncryptionPadding.OaepSHA256);")]
		[DataRow(
			@"byte[] decrypted = rsa.Decrypt(dataToEncrypt, RSAEncryptionPadding.Pkcs1);",
			@"byte[] decrypted = rsa.Decrypt(dataToEncrypt, RSAEncryptionPadding.OaepSHA256);")]
		[DataRow(
			@"RSAEncryptionPadding padding = RSAEncryptionPadding.Pkcs1;",
			@"RSAEncryptionPadding padding = RSAEncryptionPadding.OaepSHA256;")]
		[DataRow(
			@"var padding = RSAEncryptionPadding.Pkcs1;",
			@"var padding = RSAEncryptionPadding.OaepSHA256;")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CodeFixShouldReplaceWithOaepSHA256Async(string originalCode, string expectedCode)
		{
			var originalSource = string.Format(GetTemplate(), originalCode);
			var expectedSource = string.Format(GetTemplate(), expectedCode);

			await VerifyFix(originalSource, expectedSource).ConfigureAwait(false);
		}
	}
}