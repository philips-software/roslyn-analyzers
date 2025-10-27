// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	public class Helper(AnalyzerOptions options, Compilation compilation) : CodeFixHelper
	{
		public AllowedSymbols ForAllowedSymbols { get; } = new AllowedSymbols(compilation);

		public AdditionalFilesHelper ForAdditionalFiles { get; } = new AdditionalFilesHelper(options, compilation);

		public AttributeHelper AttributeHelper { get; } = new AttributeHelper();
	}
}
