﻿// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Philips.CodeAnalysis.DuplicateCodeAnalyzer;

namespace Philips.CodeAnalysis.Benchmark
{
	[ExcludeFromCodeCoverage]
	public static class Program
	{
		public static void Main()
		{
			_ = BenchmarkRunner.Run<DuplicationDetectorBenchmark>();
		}
	}

	[ExcludeFromCodeCoverage]
	public class Input
	{
		public string Folder { get; set; }
		public IReadOnlyDictionary<MethodDeclarationSyntax, IEnumerable<SyntaxToken>> Data { get; set; }

		public override string ToString()
		{
			return Folder;
		}
	}

	[ExcludeFromCodeCoverage]
	[SimpleJob(launchCount: LaunchCount, warmupCount: WarmupCount, targetCount: TargetCount)]
	public class DuplicationDetectorBenchmark
	{
		public const int LaunchCount = 3;
		public const int WarmupCount = 2;
		public const int TargetCount = 5;
		private const int BaseModulus1 = 2048;
		private const int Modulus1 = 1723;
		private const int BaseModulus2 = 227;
		private const int Modulus2 = 1000005;

		[ParamsSource(nameof(ValuesForA))]
		public Input A { get; set; }

		// public property
		public IEnumerable<Input> ValuesForA
		{
			get
			{
				foreach (var dir in Array.Empty<string>())
				{
					Dictionary<MethodDeclarationSyntax, IEnumerable<SyntaxToken>> tokens = new();

					foreach (var file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
					{
						if (!file.EndsWith(".cs"))
						{
							continue;
						}

						var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), new CSharpParseOptions(LanguageVersion.Latest));

						foreach (var method in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
						{
							if (method.Body is null)
							{
								continue;
							}

							tokens[method] = method.Body.DescendantTokens();
						}
					}

					yield return new Input { Data = tokens, Folder = dir };
				}
			}
		}
		private void TestDictionary(DuplicateDetector _library, int baseModulus, int modulus)
		{
			A.Data.AsParallel().ForAll(kvp =>
			{
				var rollingTokenSet = new RollingTokenSet(new RollingHashCalculator<TokenInfo>(100, baseModulus, modulus));

				foreach (var list in kvp.Value)
				{
					(int hash, Evidence evidence) = rollingTokenSet.Add(new TokenInfo(list));

					if (rollingTokenSet.IsFull())
					{
						_ = _library.Register(hash, evidence);
					}
				}
			});
		}

		[Benchmark]
		public void OriginalHashParameters()
		{
			DuplicateDetector _library = new();
			TestDictionary(_library, BaseModulus1, Modulus1);
		}

		[Benchmark]
		public void BiggerPrimes()
		{
			DuplicateDetector _library = new();
			TestDictionary(_library, BaseModulus2, Modulus2);
		}
	}
}
