// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidOverridingWithNewKeywordAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidOverridingWithNewKeywordAnalyzer();
		}

		
		private const string Correct = @"
namespace MultiLineConditionUnitTests
{
    public class Other
    {
        public virtual void VirtualMethod()
        {
        }
    }
    public class Program : Other
    {
        public override void VirtualMethod() 
        {
        }
    }
}
";

		private const string WrongMethod = @"
namespace MultiLineConditionUnitTests
{
    public class Other
    {
        public void VirtualMethod()
        {
        }
    }
    public class Program : Other
    {
        public new void VirtualMethod() 
        {
        }
    }
}
";

		private const string WrongProperty = @"
namespace MultiLineConditionUnitTests
{
    public class Other
    {
        public int VirtualProperty
        {
            get {
                return 0;
            }
        }
    }
    public class Program : Other
    {
        public new int VirtualProperty 
        {
            get {
                return 1;
            }
        }
    }
}
";

		[DataTestMethod]
		[DataRow(Correct, DisplayName = nameof(Correct))]
		[TestCategory(TestDefinitions.UnitTests)]
		public void OverrideVirtualDoesNotTriggersDiagnostics(string input)
		{

			VerifySuccessfulCompilation(input);
		}

		[DataTestMethod]
		[DataRow(WrongMethod, DisplayName = nameof(WrongMethod)), 
		 DataRow(WrongProperty, DisplayName = nameof(WrongProperty))]
		[TestCategory(TestDefinitions.UnitTests)]
		public void OverrideWithNewKeywordTriggersDiagnostics(string input)
		{
			var expected = DiagnosticResultHelper.Create(DiagnosticId.AvoidOverridingWithNewKeyword,
				new Regex("Avoid overriding Virtual.* with the new keyword."));
			VerifyDiagnostic(input, expected);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[DataTestMethod]
		[DataRow(WrongMethod, "Dummy.Designer", DisplayName = "OutOfScopeSourceFile")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string testCode, string filePath)
		{
			VerifyDiagnostic(testCode, filePath);
		}
	}
}
