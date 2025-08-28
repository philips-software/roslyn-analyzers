// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidTodoCommentsAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidTodoCommentsAnalyzer();
		}

		[DataTestMethod]
		[DataRow(@"// This is a regular comment")]
		[DataRow(@"/* This is a multi-line comment */")]
		[DataRow(@"// This comment mentions a to-do item but not the keyword")]
		[DataRow(@"// FIXME: This needs to be fixed")]
		[DataRow(@"// NOTE: This is important")]
		[DataRow(@"// BedIdToDomainGroupContextMap")]
		[DataRow(@"/* BedIdToDomainGroupContextMap */")]
		[DataRow(@"// The methodName should be camelCased")]
		[DataRow(@"// This contains TODOs but not the exact word")]
		[DataRow(@"")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AcceptableCommentsAreFineAsync(string content)
		{
			await VerifySuccessfulCompilation(content).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow(@"// TODO: This needs to be done", DisplayName = "Single line TODO")]
		[DataRow(@"// TODO:", DisplayName = "TODO with colon only")]
		[DataRow(@"// todo: lowercase version", DisplayName = "Lowercase todo")]
		[DataRow(@"// Todo: Mixed case", DisplayName = "Mixed case Todo")]
		[DataRow(@"// TODO This needs to be done", DisplayName = "TODO without colon")]
		[DataRow(@"/* TODO: This is in a multi-line comment */", DisplayName = "Multi-line TODO")]
		[DataRow(@"/* 
		TODO: This spans multiple lines 
		*/", DisplayName = "Multi-line comment with TODO")]
		[DataRow(@"// TODO", DisplayName = "TODO alone")]
		[DataRow(@"/* TODO */", DisplayName = "TODO alone in multiline")]
		[DataRow(@"// (TODO) This is in parentheses", DisplayName = "TODO in parentheses")]
		[DataRow(@"// TODO-item", DisplayName = "TODO with hyphen")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TodoCommentsShouldTriggerDiagnosticAsync(string content)
		{
			await VerifyDiagnostic(content).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MultipleTodoCommentsShouldTriggerMultipleDiagnosticsAsync()
		{
			const string content = @"
public class TestClass
{
	// TODO: First item
	public void Method1()
	{
		// TODO: Second item  
		int x = 0;
	}
	
	/* TODO: Third item */
	public void Method2() { }
}";
			await VerifyDiagnostic(content, 3).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TodoInStringLiteralShouldNotTriggerDiagnosticAsync()
		{
			const string content = @"
public class TestClass
{
	public void Method()
	{
		string message = ""TODO: This is just a string literal"";
	}
}";
			await VerifySuccessfulCompilation(content).ConfigureAwait(false);
		}
	}
}
