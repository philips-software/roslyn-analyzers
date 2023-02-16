// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

namespace Philips.CodeAnalysis.Common
{
	public class MsTestAttributeDefinition : AttributeDefinition
	{
		public MsTestAttributeDefinition(string name) : base(name, $"Microsoft.VisualStudio.TestTools.UnitTesting.{name}Attribute")
		{ }
	}
}
