// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

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
		[DataRow("CreateDirectory", IoException, UnauthorizedException, ArgumentException, ArgumentNullException, PathTooLongException, DirectoryNotFoundException, NotSupportedException)]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CreateCallTreeFromSystemIoDirectoryWithExpectedNumberOfNodes(string methodName, params string[] expectedExceptions)
		{
			var type = typeof(System.IO.Directory);
			var assembly = type.Assembly;
			ModuleDefinition module = ModuleDefinition.ReadModule(assembly.Location);
			var typeDef = module.GetType(type.FullName);
			var methodDef = typeDef.GetMethods().Where(method => method.Name == methodName).FirstOrDefault();
			var tree = CallTreeNode.CreateCallTree(methodDef);
			ExceptionWalker walker = new();
			// Act
			var actualExceptions = walker.UnhandledExceptionsFromCallTree(tree).ToList();
			// Assert
			CollectionAssert.AreEquivalent(expectedExceptions, actualExceptions);
		}
	}
}
