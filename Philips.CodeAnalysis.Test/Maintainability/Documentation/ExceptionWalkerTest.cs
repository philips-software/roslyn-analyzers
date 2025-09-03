// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Philips.CodeAnalysis.Common.Inspection;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation;
using Philips.CodeAnalysis.Test.Helpers;

namespace Philips.CodeAnalysis.Test.Maintainability.Documentation
{
	/// <summary>
	/// Test class for methods in <see cref="ExceptionWalker"/>.
	/// </summary>
	[TestClass]
	public class ExceptionWalkerTest
	{
		public const string Exception = "System.Exception";
		public const string ArgumentException = "System.ArgumentException";
		public const string ArgumentNullException = "System.ArgumentNullException";
		public const string ArgumentOutOfRangeException = "System.ArgumentOutOfRangeException";
		public const string DirectoryNotFoundException = "System.IO.DirectoryNotFoundException";
		public const string IoException = "System.IO.IOException";
		public const string NotSupportedException = "System.NotSupportedException";
		public const string PathTooLongException = "System.IO.PathTooLongException";
		public const string SecurityException = "System.SecurityException";
		public const string UnauthorizedException = "System.UnauthorizedException";
		public const string IndexOutOfRangeException = "System.IndexOutOfRangeException";
		public const string OutOfMemoryException = "System.OutOfMemoryException";

		[TestMethod]
		[DataRow("Insert", ArgumentOutOfRangeException)]
		[DataRow("InsertRange", ArgumentNullException, ArgumentOutOfRangeException)]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CreateCallTreeFromListWithExpectedNumberOfNodes(string methodName, params string[] expectedExceptions)
		{
			Type type = typeof(System.Collections.Generic.List<>);
			AssertCorrectUnhandledExceptions(type, methodName, expectedExceptions);
		}

		[TestMethod]
		[DataRow("CreateDirectory", IoException, UnauthorizedException, ArgumentException, ArgumentNullException, PathTooLongException, DirectoryNotFoundException)]
		[DataRow("Delete", IoException, UnauthorizedException, ArgumentException, ArgumentNullException, PathTooLongException, DirectoryNotFoundException)]
		[DataRow("GetFiles", ArgumentOutOfRangeException, ArgumentException, ArgumentNullException, IndexOutOfRangeException, OutOfMemoryException)]
		[DataRow("EnumerateFiles", ArgumentOutOfRangeException, ArgumentException, ArgumentNullException, IndexOutOfRangeException, OutOfMemoryException)]
		[DataRow("EnumerateDirectories", ArgumentOutOfRangeException, ArgumentException, ArgumentNullException, IndexOutOfRangeException, OutOfMemoryException)]
		[DataRow("GetDirectories", ArgumentOutOfRangeException, ArgumentException, ArgumentNullException, IndexOutOfRangeException, OutOfMemoryException)]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CreateCallTreeFromSystemIoDirectoryWithExpectedNumberOfNodes(string methodName, params string[] expectedExceptions)
		{
			Type type = typeof(System.IO.Directory);
			AssertCorrectUnhandledExceptions(type, methodName, expectedExceptions);
		}

		private void AssertCorrectUnhandledExceptions(Type type, string methodName, string[] expectedExceptions)
		{
			System.Reflection.Assembly assembly = type.Assembly;
			var module = ModuleDefinition.ReadModule(assembly.Location);
			TypeDefinition typeDef = module.GetType(type.FullName);
			MethodDefinition methodDef = typeDef.GetMethods().FirstOrDefault(method => method.Name == methodName);
			var tree = CallTreeNode.CreateCallTree(methodDef);
			ExceptionWalker walker = new();
			// Act
			var actualExceptions = walker.UnhandledExceptionsFromCallTree(tree).ToList();
			// Assert
			CollectionAssert.IsSubsetOf(expectedExceptions, actualExceptions);
			// Clean up
			CallTreeNode.ClearCache();
		}
	}
}
