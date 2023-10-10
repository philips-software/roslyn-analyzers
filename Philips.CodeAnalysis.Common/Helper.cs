// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	public class Helper : CodeFixHelper
	{
		public Helper(AnalyzerOptions options, Compilation compilation)
		{
			ForAllowedSymbols = new AllowedSymbols(compilation);
			ForAdditionalFiles = new AdditionalFilesHelper(options, compilation);
		}

		public AllowedSymbols ForAllowedSymbols { get; }

		public AdditionalFilesHelper ForAdditionalFiles { get; }
	}
}
