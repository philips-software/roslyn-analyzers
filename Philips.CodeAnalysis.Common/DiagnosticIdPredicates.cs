// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

namespace Philips.CodeAnalysis.Common
{
	public static class DiagnosticIdsPredicates
	{
		public static string ToId(this DiagnosticId id)
		{
			return @"PH" + ((int)id).ToString();
		}

		public static string ToHelpLinkUrl(this DiagnosticId id)
		{
			return $"https://github.com/philips-software/roslyn-analyzers/blob/main/Documentation/Diagnostics/{id.ToId()}.md";
		}
	}
}
