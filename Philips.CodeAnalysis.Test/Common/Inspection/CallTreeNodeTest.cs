// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Philips.CodeAnalysis.Common.Inspection;
using Philips.CodeAnalysis.Test.Helpers;

namespace Philips.CodeAnalysis.Test.Common.Inspection
{
	/// <summary>
	/// Test class for methods in <see cref="CallTreeNode"/>.
	/// </summary>
	[TestClass]
	public class CallTreeNodeTest
	{
		[DataTestMethod]
		[DataRow("CreateDirectory", 4)]
		[DataRow("Delete", 2)]
		[DataRow("Exists", 3)]
		[DataRow("EnumerateFiles", 2)]
		[DataRow("GetFiles", 2)]
		[DataRow("Move", 3)]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CreateCallTreeFromSystemIoDirectoryWithExpectedNumberOfNodes(string methodName, int expectedNodeCount)
		{
			var type = typeof(System.IO.Directory);
			var assembly = type.Assembly;
			ModuleDefinition module = ModuleDefinition.ReadModule(assembly.Location);
			var typeDef = module.GetType(type.FullName);
			var methodDef = typeDef.GetMethods().FirstOrDefault(method => method.Name == methodName);
			var tree = CallTreeNode.CreateCallTree(methodDef);
			Assert.AreEqual(expectedNodeCount, tree.Children.Count);
		}
	}
}
