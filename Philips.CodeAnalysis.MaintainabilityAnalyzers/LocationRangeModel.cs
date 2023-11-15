// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	public class LocationRangeModel
	{
		public LocationRangeModel(Location location, int startLine, int endLine)
		{
			Location = location;
			StartLine = startLine;
			EndLine = endLine;
		}

		public Location Location { get; }

		public int StartLine { get; }

		public int EndLine { get; set; }
	}
}
