// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

namespace Philips.CodeAnalysis.Common
{
	internal class AttributeDefinition
	{
		public AttributeDefinition(string name, string fullName)
		{
			Name = name;
			FullName = fullName;
		}

		public string Name { get; }
		public string FullName { get; }
	}

	internal class MsTestAttributeDefinition : AttributeDefinition
	{
		public MsTestAttributeDefinition(string name) : base(name, $"Microsoft.VisualStudio.TestTools.UnitTesting.{name}Attribute")
		{ }
	}
}
