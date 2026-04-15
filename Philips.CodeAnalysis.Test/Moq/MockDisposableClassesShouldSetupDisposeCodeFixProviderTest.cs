// © 2026 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MoqAnalyzers;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Moq
{
	[TestClass]
	public class MockDisposableClassesShouldSetupDisposeCodeFixProviderTest : CodeFixVerifier
	{
		private const string ConfiguredDisposableMockType = "MyNamespace.DisposableObjectMock";

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new MockDisposableClassesShouldSetupDisposeAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new MockDisposableClassesShouldSetupDisposeCodeFixProvider();
		}

		protected override ImmutableDictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			return base.GetAdditionalAnalyzerConfigOptions()
				.Add($@"dotnet_code_quality.{DiagnosticId.MockDisposableObjectsShouldSetupDispose.ToId()}.preferred_disposable_mock_type", ConfiguredDisposableMockType);
		}

		protected override ImmutableArray<MetadataReference> GetMetadataReferences()
		{
			var mockReference = typeof(Mock<>).Assembly.Location;
			MetadataReference reference = MetadataReference.CreateFromFile(mockReference);
			return base.GetMetadataReferences().Add(reference);
		}

		protected override ImmutableArray<(string name, string content)> GetAdditionalSourceCode()
		{
			return base.GetAdditionalSourceCode()
.Add(("DisposableClass.cs", @"
using System;

class Dependency
{
}

class DisposableClass : IDisposable
{
	public DisposableClass()
	{
	}

	public DisposableClass(Dependency dependency)
	{
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
	}
}"))
.Add(("DisposableObjectMock.cs", @"
using Moq;
using System;
namespace MyNamespace
{
	public class DisposableObjectMock<T> : Mock<T>
		where T : class, IDisposable
	{
		public DisposableObjectMock(params object[] args)
			: base(args)
		{
			this.Protected().Setup(""Dispose"", ItExpr.IsAny<bool>()).CallBase();
		}
	}
}"));
		}

		protected override void AssertFixAllProvider(FixAllProvider fixAllProvider)
		{
			Assert.IsTrue(fixAllProvider.GetSupportedFixAllScopes().Contains(FixAllScope.Document));
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ExplicitLocalMockTypeWithTargetTypedNewAndArgumentsIsReplacedAsync()
		{
			const string template = @"
		using Moq;

		class Foo
		{
			public void Test()
			{
				Dependency dependency = new();
				Mock<DisposableClass> mock = new(dependency);
			}
		}";
			const string expected = @"
		using Moq;

		class Foo
		{
			public void Test()
			{
				Dependency dependency = new();
				MyNamespace.DisposableObjectMock<DisposableClass> mock = new(dependency);
			}
		}";
			await VerifyFix(template, expected, null, shouldAllowNewCompilerDiagnostics: true).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ExplicitLocalMockTypeWithTargetTypedNewAndArgumentIsReplacedInWrapperModeAsync()
		{
			const string template = @"
		using Moq;

		class Foo
		{
			public void Test()
			{
				Dependency dependency = new();
				Mock<DisposableClass> mock = new(dependency);
			}
		}";
			const string expected = @"
		using Moq;

		class Foo
		{
			public void Test()
			{
				Dependency dependency = new();
				MyNamespace.DisposableObjectMock<DisposableClass> mock = new(dependency);
			}
		}";
			await VerifyFix(template, expected, null, shouldAllowNewCompilerDiagnostics: true).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ExplicitLocalMockTypeWithTargetTypedNewIsReplacedAsync()
		{
			const string template = @"
		using Moq;

		class Foo
		{
			public void Test()
			{
				Mock<DisposableClass> mock = new();
			}
		}";
			const string expected = @"
		using Moq;

		class Foo
		{
			public void Test()
			{
				MyNamespace.DisposableObjectMock<DisposableClass> mock = new();
			}
		}";
			await VerifyFix(template, expected, null, shouldAllowNewCompilerDiagnostics: true).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FieldInitializerMockTypeWithTargetTypedNewIsReplacedAsync()
		{
			const string template = @"
		using Moq;

		class Foo
		{
			private readonly Mock<DisposableClass> _mock = new();
		}";
			const string expected = @"
		using Moq;

		class Foo
		{
			private readonly MyNamespace.DisposableObjectMock<DisposableClass> _mock = new();
		}";
			await VerifyFix(template, expected, null, shouldAllowNewCompilerDiagnostics: true).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AutoPropertyInitializerMockTypeWithTargetTypedNewIsReplacedAsync()
		{
			const string template = @"
				using Moq;

				class Foo
				{
					public Mock<DisposableClass> Dependency { get; } = new();
				}";
			const string expected = @"
				using Moq;

				class Foo
				{
					public MyNamespace.DisposableObjectMock<DisposableClass> Dependency { get; } = new();
				}";
			await VerifyFix(template, expected, null, shouldAllowNewCompilerDiagnostics: true).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ExplicitLocalMockTypeIsReplacedAsync()
		{
			const string template = @"
				using Moq;

				class Foo
				{
					public void Test()
					{
						Mock<DisposableClass> mock = new Mock<DisposableClass>();
					}
				}";
			const string expected = @"
				using Moq;

				class Foo
				{
					public void Test()
					{
						MyNamespace.DisposableObjectMock<DisposableClass> mock = new MyNamespace.DisposableObjectMock<DisposableClass>();
					}
				}";
			await VerifyFix(template, expected, null, shouldAllowNewCompilerDiagnostics: true).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task VarMockTypeOnlyReplacesObjectCreationAsync()
		{
			const string template = @"
				using Moq;

				class Foo
				{
					public void Test()
					{
						var mock = new Mock<DisposableClass>();
					}
				}";
			const string expected = @"
				using Moq;

				class Foo
				{
					public void Test()
					{
						var mock = new MyNamespace.DisposableObjectMock<DisposableClass>();
					}
				}";
			// Introduces "unnecessary 'using Moq'
			await VerifyFix(template, expected, null, shouldAllowNewCompilerDiagnostics: true).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FieldInitializerMockTypeIsReplacedAsync()
		{
			const string template = @"
				using Moq;

				class Foo
				{
					private readonly Mock<DisposableClass> _mock = new Mock<DisposableClass>();
				}";
			const string expected = @"
				using Moq;

				class Foo
				{
					private readonly MyNamespace.DisposableObjectMock<DisposableClass> _mock = new MyNamespace.DisposableObjectMock<DisposableClass>();
				}";
			await VerifyFix(template, expected, null, shouldAllowNewCompilerDiagnostics: true).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AutoPropertyInitializerMockTypeIsReplacedAsync()
		{
			const string template = @"
				using Moq;

				class Foo
				{
					public Mock<DisposableClass> Dependency { get; } = new Mock<DisposableClass>();
				}";
			const string expected = @"
				using Moq;

				class Foo
				{
					public MyNamespace.DisposableObjectMock<DisposableClass> Dependency { get; } = new MyNamespace.DisposableObjectMock<DisposableClass>();
				}";
			await VerifyFix(template, expected, null, shouldAllowNewCompilerDiagnostics: true).ConfigureAwait(false);
		}
	}
}

