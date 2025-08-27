// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.
using System.Collections.Immutable;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidPrivateKeyPropertyAnalyzerTest : DiagnosticVerifier
	{
		private const string ClassString = @"
			using System;
			using System.Security.Cryptography.X509Certificates;
            class Foo 
			{{
				public void Foo()
				{{
					{0}
				}}
			}}
			";

		protected override ImmutableArray<MetadataReference> GetMetadataReferences()
		{
			var mockReference = typeof(X509Certificate2).Assembly.Location;
			MetadataReference reference = MetadataReference.CreateFromFile(mockReference);

			return base.GetMetadataReferences().Add(reference);
		}


		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidPrivateKeyPropertyAnalyzer();
		}

		[TestMethod]
		[DataRow(@"_ = new X509Certificate2().PrivateKey")]
		[DataRow(@"X509Certificate2 cert = new X509Certificate2();
			_ = cert.PrivateKey;")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidPrivateKeyPropertyOnX509Certificate(string s)
		{
			var code = string.Format(ClassString, s);
			await VerifyDiagnostic(code).ConfigureAwait(false);
		}

		[TestMethod]
		[DataRow(@"_ = new string(""Foo"").Length")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task OtherPropertyShouldBeIgnored(string s)
		{
			var code = string.Format(ClassString, s);
			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}
	}
}
