using System.Collections.Generic;
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
	public class Program
	{
		public static void Main(string[] args)
		{
			var summary = BenchmarkRunner.Run<DuplicationDetectorBenchmark>();
		}
	}

	public class Input
	{
		public string Folder { get; set; }
		public Dictionary<MethodDeclarationSyntax, IEnumerable<SyntaxToken>> Data { get; set; }

		public override string ToString()
		{
			return Folder;
		}
	}

	[SimpleJob(launchCount: 3, warmupCount: 2, targetCount: 5)]
	public class DuplicationDetectorBenchmark
	{
		[ParamsSource(nameof(ValuesForA))]
		public Input A { get; set; }

		// public property
		public IEnumerable<Input> ValuesForA
		{
			get
			{
				foreach (var dir in new string[] { })
				{
					Dictionary<MethodDeclarationSyntax, IEnumerable<SyntaxToken>> tokens = new Dictionary<MethodDeclarationSyntax, IEnumerable<SyntaxToken>>();

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
		private void TestDictionary(DuplicateDetectorDictionary _library, int baseModulus, int modulus)
		{
			A.Data.AsParallel().ForAll(kvp =>
			{
				var rollingTokenSet = new RollingTokenSet(new RollingHashCalculator<TokenInfo>(100, baseModulus, modulus));

				foreach (var list in kvp.Value)
				{
					(int hash, Evidence evidence) = rollingTokenSet.Add(new TokenInfo(list));

					if (rollingTokenSet.IsFull())
					{
						Evidence existingEvidence = _library.TryAdd(hash, evidence);
					}
				}
			});
		}

		[Benchmark]
		public void OriginalHashParameters()
		{
			DuplicateDetectorDictionary _library = new DuplicateDetectorDictionary();

			TestDictionary(_library, 2048, 1723);
		}

		[Benchmark]
		public void ExistingBiggerPrimes()
		{
			DuplicateDetectorDictionary _library = new DuplicateDetectorDictionary();

			TestDictionary(_library, 227, 1000005);
		}
	}
}
