// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidPublicMemberVariableAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidPublicMemberVariableAnalyzer();
		}

		#endregion

		#region Public Interface
		/// <summary>
		/// Verify that member variables are initialized 
		/// Ignore strunct / static / const
		/// </summary>
		/// <param name="content"></param>
		/// <param name="isError"></param>
		[DataTestMethod]
		[DataRow("", false)]
		[DataRow("public const int InitialCount = 1;", false)]
		[DataRow("public int i = 1;", true)]
		[DataRow("static int i = 1;", false)]
		[DataRow("public static int i = 1;", false)]
		[DataRow("static readonly int i = 1;", false)]
		[DataRow("private int i = 1;", false)]
		[DataRow("private struct testStruct { public int i; }", false)]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidPublicMemberVariablesTest(string content, bool isError)
		{
			const string template = @"public class C {{    {0}       }}";
			string classContent = string.Format(template, content);
			DiagnosticResult[] results;
			if (isError)
			{
				results = new[] { new DiagnosticResult()
					{
						Id = Helper.ToDiagnosticId(DiagnosticIds.AvoidPublicMemberVariables),
						Message = new Regex(".*"),
						Severity = DiagnosticSeverity.Error,
						Locations = new[]
						{
							new DiagnosticResultLocation("Test0.cs", 1, 21)
						}
					}
				};
			}
			else
			{
				results = Array.Empty<DiagnosticResult>();
			}
			VerifyDiagnostic(classContent, results);

		}

		#endregion
	}
}