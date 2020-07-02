// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

namespace Philips.CodeAnalysis.Common
{
	internal static class MsTestFrameworkDefinitions
	{
		public static AttributeDefinition TestClassAttribute => new MsTestAttributeDefinition("TestClass");
		public static AttributeDefinition TestInitializeAttribute => new MsTestAttributeDefinition("TestInitialize");
		public static AttributeDefinition TestCleanupAttribute => new MsTestAttributeDefinition("TestCleanup");
		public static AttributeDefinition TestMethodAttribute => new MsTestAttributeDefinition("TestMethod");
		public static AttributeDefinition DataTestMethodAttribute => new MsTestAttributeDefinition("DataTestMethod");
		public static AttributeDefinition ClassInitializeAttribute => new MsTestAttributeDefinition("ClassInitialize");
		public static AttributeDefinition ClassCleanupAttribute => new MsTestAttributeDefinition("ClassCleanup");
		public static AttributeDefinition AssemblyInitializeAttribute => new MsTestAttributeDefinition("AssemblyInitialize");
		public static AttributeDefinition AssemblyCleanupAttribute => new MsTestAttributeDefinition("AssemblyCleanup");
		public static AttributeDefinition DataRowAttribute => new MsTestAttributeDefinition("DataRow");
		public static AttributeDefinition TestCategoryAttribute => new MsTestAttributeDefinition("TestCategory");
		public static AttributeDefinition TimeoutAttribute => new MsTestAttributeDefinition("Timeout");
		public static AttributeDefinition DescriptionAttribute => new MsTestAttributeDefinition("Description");
		public static AttributeDefinition DynamicDataAttribute => new MsTestAttributeDefinition("DynamicData");
	}
}
