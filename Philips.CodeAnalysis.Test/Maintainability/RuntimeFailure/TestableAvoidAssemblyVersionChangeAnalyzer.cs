// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure;

namespace Philips.CodeAnalysis.Test.Maintainability.RuntimeFailure
{
	internal sealed class TestableAvoidAssemblyVersionChangeAnalyzer : AvoidAssemblyVersionChangeAnalyzer
	{
		private readonly Version _version;

		public TestableAvoidAssemblyVersionChangeAnalyzer(string version)
		{
			_version = Version.Parse(version);
		}

		protected override Version GetCompilationAssemblyVersion(Compilation compilation)
		{
			return _version;
		}
	}
}
