// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

namespace Philips.CodeAnalysis.Common
{
	public static class MsTestFrameworkDefinitions
	{
		public static readonly AttributeDefinition TestClassAttribute = new MsTestAttributeDefinition("TestClass");
		public static readonly AttributeDefinition TestInitializeAttribute = new MsTestAttributeDefinition("TestInitialize");
		public static readonly AttributeDefinition TestCleanupAttribute = new MsTestAttributeDefinition("TestCleanup");
		public static readonly AttributeDefinition TestMethodAttribute = new MsTestAttributeDefinition("TestMethod");
		public static readonly AttributeDefinition DataTestMethodAttribute = new MsTestAttributeDefinition("DataTestMethod");
		public static readonly AttributeDefinition ClassInitializeAttribute = new MsTestAttributeDefinition("ClassInitialize");
		public static readonly AttributeDefinition ClassCleanupAttribute = new MsTestAttributeDefinition("ClassCleanup");
		public static readonly AttributeDefinition AssemblyInitializeAttribute = new MsTestAttributeDefinition("AssemblyInitialize");
		public static readonly AttributeDefinition AssemblyCleanupAttribute = new MsTestAttributeDefinition("AssemblyCleanup");
		public static readonly AttributeDefinition DataRowAttribute = new MsTestAttributeDefinition("DataRow");
		public static readonly AttributeDefinition TestCategoryAttribute = new MsTestAttributeDefinition("TestCategory");
		public static readonly AttributeDefinition TimeoutAttribute = new MsTestAttributeDefinition("Timeout");
		public static readonly AttributeDefinition DescriptionAttribute = new MsTestAttributeDefinition("Description");
		public static readonly AttributeDefinition DynamicDataAttribute = new MsTestAttributeDefinition("DynamicData");
	}
}
