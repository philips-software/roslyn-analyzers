// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

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

		protected override MetadataReference[] GetMetadataReferences()
		{
			string mockReference = typeof(X509Certificate2).Assembly.Location;
			MetadataReference reference = MetadataReference.CreateFromFile(mockReference);

			return base.GetMetadataReferences().Concat(new[] { reference }).ToArray();
		}


		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidPrivateKeyPropertyAnalyzer();
		}

		private DiagnosticResultLocation GetBaseDiagnosticLocation(int rowOffset = 0, int columnOffset = 0)
		{
			return new DiagnosticResultLocation("Test.cs", 8 + rowOffset, 8 + columnOffset);
		}

		[DataTestMethod]
		[DataRow(@"_ = new X509Certificate2().PrivateKey", 0, 2)]
		[DataRow(@"X509Certificate2 cert = new X509Certificate2();
			_ = cert.PrivateKey;", 1, 0)]
		public void AvoidPrivateKeyPropertyOnX509Certificate(string s, int row, int col)
		{

			string code = string.Format(ClassString, s);
			DiagnosticResult expected = new()
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.AvoidPrivateKeyProperty),
				Message = new Regex(".+ "),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					GetBaseDiagnosticLocation(row,col)
				}
			};

			VerifyDiagnostic(code, expected);
		}
	}
}
