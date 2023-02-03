﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Documentation
{
	[TestClass]
	public class CopyrightPresentAnalyzerTest : DiagnosticVerifier
	{
		private const string configuredCompanyName = @"Koninklijke Philips N.V.";

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new CopyrightPresentAnalyzer();
		}

		protected override Dictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			Dictionary<string, string> options = new()
			{
				{ $@"dotnet_code_quality.{ Helper.ToDiagnosticId(DiagnosticId.CopyrightPresent) }.company_name", configuredCompanyName  }
			};
			return options;
		}

		[DataRow(@"#region H
			#endregion", false, 2)]
		[DataRow(@"#region Header
#endregion", false, 2)]
		[DataRow(@"#region Header
// ©
#endregion", false, 2)]
		[DataRow(@"#region Header
// © Koninklijke Philips N.V.
#endregion", false, 2)]
		[DataRow(@"#region Header
// © 2021
#endregion", false, 2)]
		[DataRow(@"#region Header
// © Koninklijke Philips N.V. 2021
#endregion", true, 2)]
		[DataRow(@"#region Header
//
// © Koninklijke Philips N.V. 2021
#endregion", false, 0)]
		[DataRow(@"#region Header

// © Koninklijke Philips N.V. 2021
#endregion", true, 0)]
		[DataRow(@"#region © Koninklijke Philips N.V. 2021
//
// All rights are reserved. Reproduction or transmission in whole or in part,
// in any form or by any means, electronic, mechanical or otherwise, is
// prohibited without the prior written consent of the copyright owner.
//
// Filename: Dummy.cs
//
#endregion", true, 1)]
		[DataRow(@"#region © Koninklijke Philips N.V. 2021
#endregion", true, 1)]
		[DataRow(@"#region Copyright Koninklijke Philips N.V. 2021
#endregion", true, 1)]
		[DataRow(@"#region Koninklijke Philips N.V. 2021
#endregion", false, 2)]
		[DataRow(@"#region Copyright 2021
#endregion", false, 2)]
		[DataRow(@"#region Copyright Koninklijke Philips N.V.
#endregion", false, 2)]
		[DataRow(@"// ©", false, -1)]
		[DataRow(@"// © Koninklijke Philips N.V.", false, -1)]
		[DataRow(@"// © 2021", false, -1)]
		[DataRow(@"// Copyright 2021", false, -1)]
		[DataRow(@"// Copyright Koninklijke Philips N.V. 2021", true, -1)]
		[DataRow(@"/* Copyright Koninklijke Philips N.V. 2021", true, -1)]
		[DataRow(@"// © Koninklijke Philips N.V. 2021", true, -1)]
		[DataRow(@"/* © Koninklijke Philips N.V. 2021", true, -1)]
		[DataRow(@"// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

namespace Philips.CodeAnalysis.Common
{
}", true, -1)]
		[DataRow(@"", false, 2)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void HeaderIsDetected(string content, bool isGood, int errorStartLine)
		{
			string baseline = @"{0}
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo 
{{
  public void Foo()
  {{
  }}
}}
";
			string givenText = string.Format(baseline, content);

			DiagnosticResult[] expected;
			int errorEndLine = errorStartLine;

			if (isGood)
			{
				expected = Array.Empty<DiagnosticResult>();
			}
			else
			{
				expected = new[] { new DiagnosticResult
				{
					Id = Helper.ToDiagnosticId(DiagnosticId.CopyrightPresent),
					Message = new Regex(".+"),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
					new DiagnosticResultLocation("Test0.cs", errorStartLine, 1, errorEndLine, null)
				} }
				};
			}

			VerifyDiagnostic(givenText, expected);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void HeaderIsDetected2()
		{
			string baseline = @"using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo 
{{
  public void Foo()
  {{
  }}
}}
";
			VerifyDiagnostic(baseline);
		}

		[DataRow(@"")]
		[DataRow(@"
")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void EmptyUnitIsIgnored(string text)
		{
			VerifySuccessfulCompilation(text);
		}


		[DataRow(@"// ------
// <auto-generated>
// content
// </auto-generated>
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute()]
", "blah")]
		[DataRow(@"// <auto-generated>
// content
// </auto-generated>
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute()]
", "blah")]
		[DataRow(@"// <auto-generated />
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute()]
", "blah")]
		[DataRow(@"// <autogenerated />
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute()]
", "blah")]
		[DataRow(@"using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute()]
", "AssemblyInfo")]
		[DataRow(@"using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute()]
", "Foo.AssemblyInfo")]
		[DataRow(@"using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute()]
", "Blah.Designer")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AutogeneratedIsIgnored(string text, string filenamePrefix)
		{
			VerifySuccessfulCompilation(text, filenamePrefix);
		}

		[DataTestMethod]
		[DataRow("RuntimeFailure", "DereferenceNullAnalyzer")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DogFoodMaintainability(string folder, string analyzerName)
		{
			var path = Path.Combine("..", "..", "..", "..", "Philips.CodeAnalysis.MaintainabilityAnalyzers", folder, $"{analyzerName}.cs");
			VerifySuccessfulCompilationFromFile(path);
		}

		[DataTestMethod]
		[DataRow("MsTestAttributeDefinitions")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DogFoodMsTest(string analyzerName)
		{
			var path = Path.Combine("..", "..", "..", "..", "Philips.CodeAnalysis.MsTestAnalyzers", $"{analyzerName}.cs");
			VerifySuccessfulCompilationFromFile(path);
		}
	}
}
