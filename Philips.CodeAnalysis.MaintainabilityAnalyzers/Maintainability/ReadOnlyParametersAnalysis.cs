// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DisposeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	internal class ReadOnlyParameterAnalysis : ForwardDataFlowAnalysis<DictionaryAnalysisData<AbstractLocation, ReadOnlyParameterAbstractValue>, ReadOnlyParameterAnalysisContext, ReadOnlyParameterAnalysisResult, ReadOnlyParameterBlockAnalysisResult, ReadOnlyParameterAbstractAnalysisValue>
	{
		public ReadOnlyParameterAnalysis(AbstractAnalysisDomain<DictionaryAnalysisData<AbstractLocation, ReadOnlyParameterAbstractValue>> analysisDomain, DataFlowOperationVisitor<DictionaryAnalysisData<AbstractLocation, ReadOnlyParameterAbstractValue>, ReadOnlyParameterAnalysisContext, ReadOnlyParameterAnalysisResult, ReadOnlyParameterAbstractAnalysisValue> operationVisitor) : base(analysisDomain, operationVisitor)
		{
		}

		protected override ReadOnlyParameterBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, DictionaryAnalysisData<AbstractLocation, ReadOnlyParameterAbstractValue> blockAnalysisData)
		{
			throw new NotImplementedException();
		}

		protected override ReadOnlyParameterAnalysisResult ToResult(ReadOnlyParameterAnalysisContext analysisContext, DataFlowAnalysisResult<ReadOnlyParameterBlockAnalysisResult, ReadOnlyParameterAbstractAnalysisValue> dataFlowAnalysisResult)
		{
			throw new NotImplementedException();
		}
	}

	internal class ReadOnlyParameterAbstractValue : CacheBasedEquatable<DisposeAbstractValue>
	{
		protected override void ComputeHashCodeParts(Action<int> addPart)
		{
			throw new NotImplementedException();
		}
	}

	internal sealed class ReadOnlyParameterAnalysisContext : AbstractDataFlowAnalysisContext<DictionaryAnalysisData<AbstractLocation, ReadOnlyParameterAbstractValue>, ReadOnlyParameterAnalysisContext, ReadOnlyParameterAnalysisResult, ReadOnlyParameterAbstractAnalysisValue>
	{
		public ReadOnlyParameterAnalysisContext(
			AbstractValueDomain<ReadOnlyParameterAbstractAnalysisValue> valueDomain,
			WellKnownTypeProvider wellKnownTypeProvider,
			ControlFlowGraph controlFlowGraph,
			ISymbol owningSymbol,
			AnalyzerOptions analyzerOptions,
			InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
			bool pessimisticAnalysis,
			bool predicateAnalysis,
			bool exceptionPathsAnalysis,
			DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue> copyAnalysisResultOpt,
			PointsToAnalysisResult pointsToAnalysisResultOpt,
			DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue> valueContentAnalysisResultOpt,
			Func<ReadOnlyParameterAnalysisContext, ReadOnlyParameterAnalysisResult> tryGetOrComputeAnalysisResult,
			ControlFlowGraph parentControlFlowGraphOpt,
			InterproceduralAnalysisData<DictionaryAnalysisData<AbstractLocation, ReadOnlyParameterAbstractValue>, ReadOnlyParameterAnalysisContext, ReadOnlyParameterAbstractAnalysisValue> interproceduralAnalysisDataOpt,
			InterproceduralAnalysisPredicate interproceduralAnalysisPredicateOpt)
			: base(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, analyzerOptions, interproceduralAnalysisConfig, pessimisticAnalysis, predicateAnalysis, exceptionPathsAnalysis, copyAnalysisResultOpt, pointsToAnalysisResultOpt, valueContentAnalysisResultOpt, tryGetOrComputeAnalysisResult, parentControlFlowGraphOpt, interproceduralAnalysisDataOpt, interproceduralAnalysisPredicateOpt)
		{
		}

		public override ReadOnlyParameterAnalysisContext ForkForInterproceduralAnalysis(
			IMethodSymbol invokedMethod,
			ControlFlowGraph invokedControlFlowGraph,
			IOperation operation,
			PointsToAnalysisResult pointsToAnalysisResultOpt,
			DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue> copyAnalysisResultOpt,
			DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue> valueContentAnalysisResultOpt,
			InterproceduralAnalysisData<DictionaryAnalysisData<AbstractLocation, ReadOnlyParameterAbstractValue>, ReadOnlyParameterAnalysisContext, ReadOnlyParameterAbstractAnalysisValue> interproceduralAnalysisData)
		{
			return new ReadOnlyParameterAnalysisContext(
				ValueDomain,
				WellKnownTypeProvider,
				invokedControlFlowGraph,
				invokedMethod,
				AnalyzerOptions,
				InterproceduralAnalysisConfiguration,
				PessimisticAnalysis,
				false,
				ExceptionPathsAnalysis,
				copyAnalysisResultOpt,
				pointsToAnalysisResultOpt,
				valueContentAnalysisResultOpt,
				TryGetOrComputeAnalysisResult,
				null,
				InterproceduralAnalysisDataOpt,
				InterproceduralAnalysisPredicateOpt);
		}

		protected override void ComputeHashCodePartsSpecific(Action<int> builder)
		{
			throw new NotImplementedException();
		}
	}

	internal class ReadOnlyParameterAnalysisResult : DataFlowAnalysisResult<ReadOnlyParameterBlockAnalysisResult, ReadOnlyParameterAbstractAnalysisValue>
	{
		protected ReadOnlyParameterAnalysisResult(DataFlowAnalysisResult<ReadOnlyParameterBlockAnalysisResult, ReadOnlyParameterAbstractAnalysisValue> other) : base(other)
		{
		}
	}

	internal class ReadOnlyParameterBlockAnalysisResult : AbstractBlockAnalysisResult
	{
		public ReadOnlyParameterBlockAnalysisResult(BasicBlock basicBlock) : base(basicBlock)
		{
		}
	}

	internal class ReadOnlyParameterAbstractAnalysisValue : CacheBasedEquatable<ReadOnlyParameterAbstractAnalysisValue>
	{
		protected override void ComputeHashCodeParts(Action<int> addPart)
		{
			throw new NotImplementedException();
		}
	}
}
