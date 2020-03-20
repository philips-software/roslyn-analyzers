// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	public class LocationRangeModel
	{
		private readonly int _startLine;
		private int _endLine;

		public LocationRangeModel(int startLine, int endLine)
		{
			_startLine = startLine;
			_endLine = endLine;
		}

		public int StartLine
		{
			get { return _startLine; }
		}

		public int EndLine
		{
			get { return _endLine; }
			set { _endLine = value; }
		}
	}
}
