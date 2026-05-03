// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Test.Helpers;

namespace Philips.CodeAnalysis.Test.Common
{
	[TestClass]
	public class DiagnosticHelpLinkTest
	{
		private static IEnumerable<object[]> GetAllDiagnosticDescriptors()
		{
			Assembly[] analyzerAssemblies =
			[
				typeof(DuplicateCodeAnalyzer.AvoidDuplicateCodeAnalyzer).Assembly,
				typeof(MaintainabilityAnalyzers.Maintainability.AvoidStaticClassesAnalyzer).Assembly,
				typeof(MoqAnalyzers.MockRaiseArgumentsMustMatchEventAnalyzer).Assembly,
				typeof(MsTestAnalyzers.AvoidAttributeAnalyzer).Assembly,
				typeof(SecurityAnalyzers.RegexNeedsTimeoutAnalyzer).Assembly,
			];

			foreach (Assembly assembly in analyzerAssemblies)
			{
				IEnumerable<Type> analyzerTypes = assembly.GetTypes()
					.Where(t => !t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t));

				foreach (Type analyzerType in analyzerTypes)
				{
					DiagnosticAnalyzer instance;
					try
					{
						instance = (DiagnosticAnalyzer)Activator.CreateInstance(analyzerType);
					}
					catch
					{
						continue;
					}

					foreach (DiagnosticDescriptor descriptor in instance.SupportedDiagnostics)
					{
						if (descriptor.Id.EndsWith("_DEBUG", StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}

						yield return [analyzerType.Name, descriptor.Id, descriptor.HelpLinkUri];
					}
				}
			}
		}

		public static IEnumerable<object[]> AllDiagnosticDescriptors => GetAllDiagnosticDescriptors();

		[TestMethod]
		[DynamicData(nameof(AllDiagnosticDescriptors))]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DiagnosticHasHelpLink(string analyzerName, string diagnosticId, string helpLinkUri)
		{
			Assert.IsFalse(
				string.IsNullOrEmpty(helpLinkUri),
				$"Diagnostic {diagnosticId} in {analyzerName} is missing a helpLinkUri.");
		}
	}
}
