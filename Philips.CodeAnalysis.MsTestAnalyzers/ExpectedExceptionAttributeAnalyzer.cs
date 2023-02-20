// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ExpectedExceptionAttributeAnalyzer : SingleDiagnosticAnalyzer<AttributeListSyntax, ExpectedExceptionAttributeSyntaxNodeAction>
	{
		public const string MessageFormat = @"Tests may not use the ExpectedException attribute. Use ThrowsException instead.";
		private const string Title = @"Avoid ExpectedException attribute";
		private const string Description = @"The [ExpectedException()] attribute does not have line number granularity and trips the debugger anyway.  Use AssertEx.Throws() instead.";

		public ExpectedExceptionAttributeAnalyzer()
			: base(DiagnosticId.ExpectedExceptionAttribute, Title, MessageFormat, Description, Categories.Maintainability)
		{
			FullyQualifiedMetaDataName = "Microsoft.VisualStudio.TestTools.UnitTesting.ExpectedExceptionAttribute";
		}
	}
	public class ExpectedExceptionAttributeSyntaxNodeAction : SyntaxNodeAction<AttributeListSyntax>
	{
		public override void Analyze()
		{
			foreach (AttributeSyntax attribute in Node.Attributes)
			{
				if (attribute.Name.ToString().Contains(@"ExpectedException"))
				{
					var location = attribute.GetLocation();
					ReportDiagnostic(location);
					return;
				}
			}
		}
	}
}
