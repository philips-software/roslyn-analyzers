// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[Flags]
	public enum TestAttributes
	{
		None = 0x00000000,
		TestClass = 0x00000001,
		TestMethod = 0x00000002,
		DataTestMethod = 0x00000004,
		ClassInitialize = 0x00000008,
		ClassCleanup = 0x00000010,
		AssemblyInitialize = 0x00000010,
		AssemblyCleanup = 0x00000020,
		TestCleanup = 0x00000040,
		DataRow = 0x00000080,
		TestInitialize = 0x00000100,
		All = 0x7FFFFFFF,
	}

	public abstract class TestAttributeDiagnosticAnalyzer : DiagnosticAnalyzer
	{
		private IReadOnlyDictionary<AttributeDefinition, TestAttributes> _attributes = new Dictionary<AttributeDefinition, TestAttributes>()
		{
			{ MsTestFrameworkDefinitions.TestClassAttribute, TestAttributes.TestClass },
			{ MsTestFrameworkDefinitions.TestMethodAttribute, TestAttributes.TestMethod },
			{ MsTestFrameworkDefinitions.DataTestMethodAttribute, TestAttributes.DataTestMethod },
			{ MsTestFrameworkDefinitions.ClassInitializeAttribute, TestAttributes.ClassInitialize },
			{ MsTestFrameworkDefinitions.ClassCleanupAttribute, TestAttributes.ClassCleanup },
			{ MsTestFrameworkDefinitions.AssemblyInitializeAttribute, TestAttributes.AssemblyInitialize },
			{ MsTestFrameworkDefinitions.AssemblyCleanupAttribute, TestAttributes.AssemblyCleanup },
			{ MsTestFrameworkDefinitions.TestCleanupAttribute, TestAttributes.TestCleanup },
			{ MsTestFrameworkDefinitions.DataRowAttribute, TestAttributes.DataRow },
			{ MsTestFrameworkDefinitions.TestInitializeAttribute, TestAttributes.TestInitialize }
		};

		private readonly TestAttributes _interestedAttributes;

		protected TestAttributeDiagnosticAnalyzer(TestAttributes interestedAttributes)
		{
			_interestedAttributes = interestedAttributes;
		}

		protected virtual void OnInitializeAnalyzer(AnalyzerOptions options, Compilation compilation) { }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(startContext =>
			{
				if (startContext.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.Assert") == null)
				{
					return;
				}

				OnInitializeAnalyzer(startContext.Options, startContext.Compilation);

				startContext.RegisterSyntaxNodeAction(Analyze, SyntaxKind.MethodDeclaration);
			});
		}

		protected abstract void OnTestAttributeMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, TestAttributes attributes);

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			MethodDeclarationSyntax methodDeclaration = (MethodDeclarationSyntax)context.Node;

			TestAttributes attributes = TestAttributes.None;
			foreach (var kvp in _attributes)
			{
				if ((kvp.Value & _interestedAttributes) == 0)
				{
					continue;
				}

				if (!Helper.HasAttribute(methodDeclaration.AttributeLists, context, kvp.Key))
				{
					continue;
				}

				attributes |= kvp.Value;
			}

			if (attributes != TestAttributes.None)
			{
				OnTestAttributeMethod(context, methodDeclaration, attributes);
			}
		}
	}

	public abstract class TestMethodDiagnosticAnalyzer : TestAttributeDiagnosticAnalyzer
	{
		public TestMethodDiagnosticAnalyzer() : base(TestAttributes.DataTestMethod | TestAttributes.TestMethod)
		{ }

		protected abstract void OnTestMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, bool isDataTestMethod);

		protected override void OnTestAttributeMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, TestAttributes attributes)
		{
			bool isTestMethod = attributes.HasFlag(TestAttributes.TestMethod);
			bool isDataTestMethod = attributes.HasFlag(TestAttributes.DataTestMethod);

			if (!isTestMethod && !isDataTestMethod)
			{
				return;
			}

			OnTestMethod(context, methodDeclaration, isDataTestMethod);
		}
	}
}
