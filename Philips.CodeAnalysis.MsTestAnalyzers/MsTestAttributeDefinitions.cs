// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	public class MsTestAttributeDefinitions
	{
		public static MsTestAttributeDefinitions FromCompilation(Compilation compilation)
		{
			MsTestAttributeDefinitions definitions = new()
			{
				TestMethodSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.TestMethodAttribute.FullName),
				DataTestMethodSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.DataTestMethodAttribute.FullName),
				StaTestMethodSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.StaTestMethodAttribute.FullName),
				TestClassSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.TestClassAttribute.FullName),
				StaTestClassSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.StaTestClassAttribute.FullName),
				ClassInitializeSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.ClassInitializeAttribute.FullName),
				ClassCleanupSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.ClassCleanupAttribute.FullName),
				AssemblyInitializeSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.AssemblyInitializeAttribute.FullName),
				AssemblyCleanupSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.AssemblyCleanupAttribute.FullName),
				TestInitializeSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.TestInitializeAttribute.FullName),
				TestCleanupSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.TestCleanupAttribute.FullName),
				DataRowSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.DataRowAttribute.FullName),
				DynamicDataSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.DynamicDataAttribute.FullName),
				TestCategorySymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.TestCategoryAttribute.FullName),
				ITestSourceSymbol = compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.ITestDataSource"),
			};

			definitions.NonTestMethods = ImmutableHashSet.Create<INamedTypeSymbol>(SymbolEqualityComparer.Default,
				definitions.TestClassSymbol,
				definitions.ClassInitializeSymbol,
				definitions.ClassCleanupSymbol,
				definitions.AssemblyCleanupSymbol,
				definitions.AssemblyInitializeSymbol,
				definitions.TestInitializeSymbol,
				definitions.TestCleanupSymbol,
				definitions.DataRowSymbol,
				definitions.DynamicDataSymbol,
				definitions.TestCategorySymbol
			);

			return definitions;
		}

		private MsTestAttributeDefinitions() { }

		public INamedTypeSymbol StaTestMethodSymbol { get; private set; }
		public INamedTypeSymbol TestMethodSymbol { get; private set; }
		public INamedTypeSymbol DataTestMethodSymbol { get; private set; }
		public INamedTypeSymbol StaTestClassSymbol { get; private set; }
		public INamedTypeSymbol TestClassSymbol { get; private set; }
		public INamedTypeSymbol ClassInitializeSymbol { get; private set; }
		public INamedTypeSymbol ClassCleanupSymbol { get; private set; }
		public INamedTypeSymbol AssemblyInitializeSymbol { get; private set; }
		public INamedTypeSymbol AssemblyCleanupSymbol { get; private set; }
		public INamedTypeSymbol TestInitializeSymbol { get; private set; }
		public INamedTypeSymbol TestCleanupSymbol { get; private set; }
		public INamedTypeSymbol DataRowSymbol { get; private set; }
		public INamedTypeSymbol DynamicDataSymbol { get; private set; }
		public INamedTypeSymbol ITestSourceSymbol { get; private set; }
		public INamedTypeSymbol TestCategorySymbol { get; private set; }

		public ImmutableHashSet<INamedTypeSymbol> NonTestMethods { get; private set; }
	}

}
