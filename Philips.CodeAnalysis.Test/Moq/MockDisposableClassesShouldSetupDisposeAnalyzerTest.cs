// © 2026 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
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
	public class MockDisposableClassesShouldSetupDisposeAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new MockDisposableClassesShouldSetupDisposeAnalyzer();
		}

		protected override ImmutableArray<MetadataReference> GetMetadataReferences()
		{
			var mockReference = typeof(Mock<>).Assembly.Location;
			MetadataReference reference = MetadataReference.CreateFromFile(mockReference);
			return base.GetMetadataReferences().Add(reference);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MockDisposableConcreteClassTriggersAsync()
		{
			const string template = @"
using System;
using Moq;

class DisposableClass : IDisposable
{
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
	}
}

class Foo
{
	public void Test()
	{
		var mock = new Mock<DisposableClass>();
	}
}";
			await VerifyDiagnostic(template, DiagnosticId.MockDisposableObjectsShouldSetupDispose).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MockDisposableConcreteClassWithUsingVarTriggersAsync()
		{
			const string template = @"
using System;
using Moq;

class DisposableClass : IDisposable
{
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
	}
}

class Foo
{
	public void Test()
	{
		using var mock = new Mock<DisposableClass>();
	}
}";
			await VerifyDiagnostic(template, DiagnosticId.MockDisposableObjectsShouldSetupDispose).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MockInterfaceDoesNotTriggerAsync()
		{
			const string template = @"
using System;
using Moq;

interface IMyDisposable : IDisposable
{
}

class Foo
{
	public void Test()
	{
		var mock = new Mock<IMyDisposable>();
	}
}";
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MockNonDisposableConcreteClassDoesNotTriggerAsync()
		{
			const string template = @"
using Moq;

class NonDisposableClass
{
}

class Foo
{
	public void Test()
	{
		var mock = new Mock<NonDisposableClass>();
	}
}";
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MockDisposableConcreteClassWithoutVirtualDisposeBoolDoesNotTriggerAsync()
		{
			const string template = @"
using System;
using Moq;

class DisposableClass : IDisposable
{
	public void Dispose()
	{
	}
}

class Foo
{
	public void Test()
	{
		var mock = new Mock<DisposableClass>();
	}
}";
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MockDisposableConcreteClassWithProtectedSetupDoesNotTriggerAsync()
		{
			const string template = @"
using System;
using Moq;
using Moq.Protected;

class DisposableClass : IDisposable
{
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
	}
}

class Foo
{
	public void Test()
	{
		var mock = new Mock<DisposableClass>();
		mock.Protected().Setup(""Dispose"", ItExpr.IsAny<bool>()).CallBase();
	}
}";
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MockDisposableConcreteClassWithProtectedSetupUsingNameofDoesNotTriggerAsync()
		{
			const string template = @"
using System;
using Moq;
using Moq.Protected;

class DisposableClass : IDisposable
{
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
	}
}

class Foo
{
	public void Test()
	{
		var mock = new Mock<DisposableClass>();
		mock.Protected().Setup(nameof(IDisposable.Dispose), ItExpr.IsAny<bool>()).CallBase();
	}
}";
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MockDisposableConcreteClassWithProtectedSetupInDifferentMethodTriggersAsync()
		{
			const string template = @"
using System;
using Moq;
using Moq.Protected;

class DisposableClass : IDisposable
{
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
	}
}

class Foo
{
	private readonly Mock<DisposableClass> _mock = new Mock<DisposableClass>();

	public void Test()
	{
	}

	public void Configure()
	{
		_mock.Protected().Setup(""Dispose"", ItExpr.IsAny<bool>()).CallBase();
	}
}";
			await VerifyDiagnostic(template, DiagnosticId.MockDisposableObjectsShouldSetupDispose).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MockDisposableConcreteClassAssignedThenConfiguredDoesNotTriggerAsync()
		{
			const string template = @"
using System;
using Moq;
using Moq.Protected;

class DisposableClass : IDisposable
{
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
	}
}

class Foo
{
	public void Test()
	{
		Mock<DisposableClass> mock;
		mock = new Mock<DisposableClass>();
		mock.Protected().Setup(""Dispose"", ItExpr.IsAny<bool>()).CallBase();
	}
}";
			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task InlineMockCreationTriggersAsync()
		{
			const string template = @"
using System;
using Moq;

class DisposableClass : IDisposable
{
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
	}
}

class Foo
{
	private void Use(object value)
	{
	}

	public void Test()
	{
		Use(new Mock<DisposableClass>());
	}
}";
			await VerifyDiagnostic(template, DiagnosticId.MockDisposableObjectsShouldSetupDispose).ConfigureAwait(false);
		}
	}
}
