// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Test.Helpers
{
	internal sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
	{
		private readonly Dictionary<string, string> _settings;

		public TestAnalyzerConfigOptions(Dictionary<string, string> settings)
		{
			_settings = settings ?? new Dictionary<string, string>();
		}

		public override bool TryGetValue(string key, [NotNullWhen(true)] out string value)
		{
			return _settings.TryGetValue(key, out value);
		}
	}

	internal sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
	{
		private readonly TestAnalyzerConfigOptions _options;

		public TestAnalyzerConfigOptionsProvider(Dictionary<string, string> settings)
		{
			_options = new TestAnalyzerConfigOptions(settings);
		}

		public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
		{
			return _options;
		}

		public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
		{
			return _options;
		}
	}

}
