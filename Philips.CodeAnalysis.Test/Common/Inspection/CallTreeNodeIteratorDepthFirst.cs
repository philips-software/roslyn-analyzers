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
	/// Test class for methods in <see cref="CallTreeIteratorDeepestFirst"/>.
	/// </summary>
	[TestClass]
	public class CallTreeIteratorDepthFirstTest
	{
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ShouldCorrectlyTraverse()
		{
			// Arrange
			var root = new CallTreeNode(null, null);
			var firstChild = root.AddChild(null);
			var secondChild = root.AddChild(null);
			var iterator = new CallTreeIteratorDeepestFirst(root);

			// Act
			var firstMoveResult = iterator.MoveNext();
			var firstNode = iterator.Current;
			var secondMoveResult = iterator.MoveNext();
			var secondNode = iterator.Current;
			var thirdMoveResult = iterator.MoveNext();
			var thirdNode = iterator.Current;
			var fourthMoveResult = iterator.MoveNext();

			// Assert
			Assert.IsTrue(firstMoveResult);
			Assert.AreSame(firstChild, firstNode);
			Assert.IsTrue(secondMoveResult);
			Assert.AreSame(secondChild, secondNode);
			Assert.IsTrue(thirdMoveResult);
			Assert.AreSame(root, thirdNode);
			Assert.IsFalse(fourthMoveResult);
		}

		[DataTestMethod]
		[DataRow("CreateDirectory", 331266)]
		[DataRow("Delete", 720725)]
		[DataRow("Exists", 111881)]
		[DataRow("EnumerateFiles", 20536)]
		[DataRow("GetFiles", 20536)]
		[DataRow("Move", 185652)]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CreateCallTreeFromSystemIoDirectoryWithExpectedNumberOfNodes(string methodName, int expectedNodeCount)
		{
			var type = typeof(System.IO.Directory);
			var assembly = type.Assembly;
			ModuleDefinition module = ModuleDefinition.ReadModule(assembly.Location);
			var typeDef = module.GetType(type.FullName);
			var methodDef = typeDef.GetMethods().Where(method => method.Name == methodName).FirstOrDefault();
			var tree = CallTreeNode.CreateCallTree(methodDef);
			var iterator = new CallTreeIteratorDeepestFirst(tree);
			// Act
			var actualCount = iterator.Count();
			// Assert
			Assert.AreEqual(expectedNodeCount, actualCount);
		}

	}
}
