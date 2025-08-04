// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Philips.CodeAnalysis.Test.Helpers
{
	internal sealed class TestTextLoader : TextLoader
	{
		private readonly Dictionary<DocumentId, string> _textDocuments = [];

		public void Register(DocumentId documentId, string content)
		{
			_textDocuments.Add(documentId, content);
		}

		public override Task<TextAndVersion> LoadTextAndVersionAsync(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
		{
			var text = SourceText.From(_textDocuments[documentId]);
			var textAndVersion = TextAndVersion.Create(text, VersionStamp.Default);
			return Task.FromResult(textAndVersion);
		}
	}
}
