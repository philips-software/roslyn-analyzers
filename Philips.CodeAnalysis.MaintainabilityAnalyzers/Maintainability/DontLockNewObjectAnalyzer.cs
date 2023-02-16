// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
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

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(startContext =>
			{
				startContext.RegisterOperationAction(Analyze, OperationKind.Lock);
			});
		}

		private void Analyze(OperationAnalysisContext context)
		{
			ILockOperation lockOperation = (ILockOperation)context.Operation;

			if (lockOperation.LockedValue is IObjectCreationOperation || lockOperation is IDynamicObjectCreationOperation)
			{
				context.ReportDiagnostic(Diagnostic.Create(Rule, lockOperation.LockedValue.Syntax.GetLocation(), lockOperation.LockedValue.Syntax));
			}

			if (lockOperation.LockedValue is IInvocationOperation invocationOperation && invocationOperation.Instance is IObjectCreationOperation)
			{
				context.ReportDiagnostic(Diagnostic.Create(Rule, lockOperation.LockedValue.Syntax.GetLocation(), lockOperation.LockedValue.Syntax));
			}
		}
	}
}
