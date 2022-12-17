// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	public class LocationRangeModel
	{
		public LocationRangeModel(int startLine, int endLine)
		{
			StartLine = startLine;
			EndLine = endLine;
		}

		public int StartLine { get; }

		public int EndLine { get; set; }
	}
}
