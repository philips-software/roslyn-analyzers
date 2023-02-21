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
	public class CallTreeIteratorDeepestFirstTest
	{
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ShouldCorrectlyTraverse()
		{
			// Arrange
			var root = new CallTreeNode(null, null);
			CallTreeNode firstChild = root.AddChild(null);
			CallTreeNode secondChild = root.AddChild(null);
			var iterator = new CallTreeIteratorDeepestFirst(root);

			// Act
			bool firstMoveResult = iterator.MoveNext();
			CallTreeNode firstNode = iterator.Current;
			bool secondMoveResult = iterator.MoveNext();
			CallTreeNode secondNode = iterator.Current;
			bool thirdMoveResult = iterator.MoveNext();
			CallTreeNode thirdNode = iterator.Current;
			bool fourthMoveResult = iterator.MoveNext();

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
		[DataRow("CreateDirectory")]
		[DataRow("Delete")]
		[DataRow("Exists")]
		[DataRow("EnumerateFiles")]
		[DataRow("GetFiles")]
		[DataRow("Move")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CreateCallTreeFromSystemIoDirectoryWithExpectedNumberOfNodes(string methodName)
		{
			System.Type type = typeof(System.IO.Directory);
			System.Reflection.Assembly assembly = type.Assembly;
			var module = ModuleDefinition.ReadModule(assembly.Location);
			TypeDefinition typeDef = module.GetType(type.FullName);
			MethodDefinition methodDef = typeDef.GetMethods().FirstOrDefault(method => method.Name == methodName);
			var tree = CallTreeNode.CreateCallTree(methodDef);
			int expectedNodeCount = GetNodeCount(tree);
			var iterator = new CallTreeIteratorDeepestFirst(tree);
			// Act
			int actualCount = iterator.Count();
			// Assert
			Assert.AreEqual(expectedNodeCount, actualCount);
		}

		private int GetNodeCount(CallTreeNode node)
		{
			int sum = 1;
			foreach (CallTreeNode child in node.Children)
			{
				sum += GetNodeCount(child);
			}
			return sum;
		}
	}
}
