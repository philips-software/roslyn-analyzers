// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Philips.CodeAnalysis.Test.Helpers
{
	public class TestAdditionalFile : AdditionalText
	{
		private readonly SourceText _text;

		public TestAdditionalFile(string path, SourceText text)
		{
			Path = path;
			_text = text;
		}

		public override SourceText GetText(CancellationToken cancellationToken = new CancellationToken())
		{
			return _text;
		}

		public override string Path { get; }
	}
}
