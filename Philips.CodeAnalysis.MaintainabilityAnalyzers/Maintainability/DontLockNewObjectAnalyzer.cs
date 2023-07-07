// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class DontLockNewObjectAnalyzer : SingleDiagnosticAnalyzer
	{
		private const string Title = @"Don't lock new object";
		private const string MessageFormat = @"Poor choice of lock object '{0}'";
		private const string Description = @"Lock objects must be sharable between threads";

		public DontLockNewObjectAnalyzer()
			: base(DiagnosticId.DontLockNewObject, Title, MessageFormat, Description, Categories.Maintainability)
		{ }

		protected override void InitializeAnalysis(CompilationStartAnalysisContext context)
		{
			context.RegisterOperationAction(Analyze, OperationKind.Lock);
		}

		private void Analyze(OperationAnalysisContext context)
		{
			var lockOperation = (ILockOperation)context.Operation;

			if (lockOperation.LockedValue is IObjectCreationOperation || lockOperation is IDynamicObjectCreationOperation)
			{
				Location location = lockOperation.LockedValue.Syntax.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(Rule, location, lockOperation.LockedValue.Syntax));
			}

			if (lockOperation.LockedValue is IInvocationOperation invocationOperation && invocationOperation.Instance is IObjectCreationOperation)
			{
				Location location = lockOperation.LockedValue.Syntax.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(Rule, location, lockOperation.LockedValue.Syntax));
			}
		}
	}
}
