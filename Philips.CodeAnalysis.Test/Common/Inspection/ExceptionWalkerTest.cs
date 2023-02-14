// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Philips.CodeAnalysis.Common.Inspection;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation;
using Philips.CodeAnalysis.Test.Helpers;

namespace Philips.CodeAnalysis.Test.Common.Inspection
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

		[DataTestMethod]
		[DataRow("Insert", ArgumentOutOfRangeException)]
		[DataRow("InsertRange", ArgumentNullException, ArgumentOutOfRangeException)]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CreateCallTreeFromListWithExpectedNumberOfNodes(string methodName, params string[] expectedExceptions)
		{
			var type = typeof(System.Collections.Generic.List<>);
			AssertCorrectUnhandledExceptions(type, methodName, expectedExceptions);
		}

		[DataTestMethod]
		//[DataRow("CreateDirectory", IoException, UnauthorizedException, ArgumentException, ArgumentNullException, PathTooLongException, DirectoryNotFoundException, NotSupportedException)]
		[DataRow("Delete", IoException, UnauthorizedException, ArgumentException, ArgumentNullException, PathTooLongException, DirectoryNotFoundException)]
		//[DataRow("GetFiles", IoException, ArgumentOutOfRangeException, ArgumentException, ArgumentNullException, PathTooLongException, DirectoryNotFoundException, SecurityException)]
		//[DataRow("EnumerateFiles", IoException, ArgumentOutOfRangeException, ArgumentException, ArgumentNullException, PathTooLongException, DirectoryNotFoundException, SecurityException)]
		//[DataRow("EnumerateDirectories", IoException, ArgumentOutOfRangeException, ArgumentException, ArgumentNullException, PathTooLongException, DirectoryNotFoundException, SecurityException)]
		//[DataRow("GetDirectories", IoException, ArgumentOutOfRangeException, ArgumentException, ArgumentNullException, PathTooLongException, DirectoryNotFoundException)]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CreateCallTreeFromSystemIoDirectoryWithExpectedNumberOfNodes(string methodName, params string[] expectedExceptions)
		{
			var type = typeof(System.IO.Directory);
			AssertCorrectUnhandledExceptions(type, methodName, expectedExceptions);
		}

		private void AssertCorrectUnhandledExceptions(Type type, string methodName, string[] expectedExceptions)
		{
			var assembly = type.Assembly;
			ModuleDefinition module = ModuleDefinition.ReadModule(assembly.Location);
			var typeDef = module.GetType(type.FullName);
			var methodDef = typeDef.GetMethods().FirstOrDefault(method => method.Name == methodName);
			var tree = CallTreeNode.CreateCallTree(methodDef);
			ExceptionWalker walker = new();
			// Act
			var actualExceptions = walker.UnhandledExceptionsFromCallTree(tree).ToList();
			// Assert
			CollectionAssert.IsSubsetOf(expectedExceptions, actualExceptions);
		}
	}
}
