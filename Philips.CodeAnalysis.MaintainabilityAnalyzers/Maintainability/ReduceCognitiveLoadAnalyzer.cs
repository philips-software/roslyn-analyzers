// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Philips.CodeAnalysis.Common;

using LanguageExt;
using LanguageExt.SomeHelp;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ReduceCognitiveLoadAnalyzer : SingleDiagnosticAnalyzer<MethodDeclarationSyntax, ReduceCognitiveLoadSyntaxNodeAction>
	{
		private const string Title = @"Reduce Cognitive Load";
		public const string MessageFormat = @"Reduce Cognitive Load of {0} to threshold of {1}";
		private const string Description = @"This method has too many nested block statements and logical cases to consider in this method";

		public AdditionalFilesHelper AdditionalFilesHelper { get; }

		public ReduceCognitiveLoadAnalyzer()
			: this(null)
		{ }

		public ReduceCognitiveLoadAnalyzer(AdditionalFilesHelper additionalFilesHelper)
			: base(DiagnosticId.ReduceCognitiveLoad, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: false)
		{
			AdditionalFilesHelper = additionalFilesHelper;
		}
	}

	public class ReduceCognitiveLoadSyntaxNodeAction : SyntaxNodeAction<MethodDeclarationSyntax>
	{

		private static readonly System.Collections.Generic.HashSet<SyntaxKind> matchingTokens = new()
		{
			SyntaxKind.BarBarToken,
			SyntaxKind.AmpersandAmpersandToken,
			SyntaxKind.ExclamationToken,
			SyntaxKind.ExclamationEqualsToken,
			SyntaxKind.BreakKeyword,
			SyntaxKind.ContinueKeyword,
		};

		private int MaxCognitiveLoad { get; set; }
		private const int DefaultMaxCognitiveLoad = 25;


		private int CalcCognitiveLoad(BlockSyntax blockSyntax)
		{
			var cognitiveLoad = 1;
			foreach (BlockSyntax descBlockSyntax in blockSyntax.DescendantNodes().OfType<BlockSyntax>())
			{
				cognitiveLoad += CalcCognitiveLoad(descBlockSyntax);
			}
			return cognitiveLoad;
		}

		public override IEnumerable<Diagnostic> Analyze()
		{
			BlockSyntax blockSyntax = Node.DescendantNodes().OfType<BlockSyntax>().FirstOrDefault();
			if (blockSyntax == null)
			{
				return Option<Diagnostic>.None;
			}

			var cognitiveLoad = CalcCognitiveLoad(blockSyntax);
			cognitiveLoad += blockSyntax.DescendantTokens().Count(token => matchingTokens.Contains(token.Kind()));

			InitializeMaxCognitiveLoad();
			if (cognitiveLoad > MaxCognitiveLoad)
			{
				Location location = Node.Identifier.GetLocation();
				return PrepareDiagnostic(location, cognitiveLoad, MaxCognitiveLoad).ToSome();
			}
			return Option<Diagnostic>.None;
		}

		private void InitializeMaxCognitiveLoad()
		{
			if (MaxCognitiveLoad == 0)
			{
				AdditionalFilesHelper additionalFilesHelper = (Analyzer as ReduceCognitiveLoadAnalyzer).AdditionalFilesHelper;
				additionalFilesHelper ??= new AdditionalFilesHelper(Context.Options, Context.Compilation);
				var configuredMaxCognitiveLoad = additionalFilesHelper.GetValueFromEditorConfig(Rule.Id, @"max_cognitive_load");
				if (int.TryParse(configuredMaxCognitiveLoad, NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxAllowedCognitiveLoad) && maxAllowedCognitiveLoad is >= 1 and <= 100)
				{
					MaxCognitiveLoad = maxAllowedCognitiveLoad;
					return;
				}
				MaxCognitiveLoad = DefaultMaxCognitiveLoad;
			}
		}
	}
}
