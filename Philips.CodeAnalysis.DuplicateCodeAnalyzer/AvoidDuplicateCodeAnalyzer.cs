// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.DuplicateCodeAnalyzer
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidDuplicateCodeAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Avoid Duplicate Code";
		private const string MessageFormat = @"Duplicate shape found at {0}. Refactor logic or exempt duplication. Duplicate shape details: ""{1}""";
		private const string Description = @"Duplicate code is less maintainable";
		private const string Category = Categories.Maintainability;

		public int DefaultDuplicateTokenThreshold = 100;

		public static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.AvoidDuplicateCode), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		private const string InvalidTokenCountTitle = @"The token_count specified in the EditorConfig is invalid.";
		private const string InvalidTokenCountMessage = @"The token_count {0} specified in the EditorConfig is invalid.";
		private static readonly DiagnosticDescriptor InvalidTokenCountRule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.AvoidDuplicateCode), InvalidTokenCountTitle, InvalidTokenCountMessage, Category, DiagnosticSeverity.Error, true, Description);

		private const string TokenCountTooBigTitle = @"The token_count specified in the EditorConfig is too big.";
		private const string TokenCountTooBigMessage = @"The token_count {0} specified in the EditorConfig cannot be greater than {1}.";
		private static readonly DiagnosticDescriptor TokenCountTooBigRule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.AvoidDuplicateCode), TokenCountTooBigTitle, TokenCountTooBigMessage, Category, DiagnosticSeverity.Error, true, Description);

		private const string TokenCountTooSmallTitle = @"The token_count specified in the EditorConfig is too small.";
		private const string TokenCountTooSmallMessage = @"The token_count {0} specified in the EditorConfig cannot be less than {1}.";
		private static readonly DiagnosticDescriptor TokenCountTooSmallRule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.AvoidDuplicateCode), TokenCountTooSmallTitle, TokenCountTooSmallMessage, Category, DiagnosticSeverity.Error, true, Description);

		private const string UnhandledException = @"AvoidDuplicateCodeAnalyzer had an internal error. ({0}) Details: {1}";
		private static readonly DiagnosticDescriptor UnhandledExceptionRule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.AvoidDuplicateCode), UnhandledException, UnhandledException, Category, DiagnosticSeverity.Info, true, Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule, InvalidTokenCountRule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(compilationContext =>
			{
				EditorConfigOptions options = InitializeEditorConfigOptions(compilationContext.Options, compilationContext.Compilation, out Diagnostic configurationError);
				HashSet<string> exceptions = new HashSet<string>();
				if (!options.IgnoreExceptionsFile)
				{
					exceptions = InitializeExceptions(compilationContext.Options.AdditionalFiles);
				}
				var compilationAnalyzer = new CompilationAnalyzer(options.TokenCount, exceptions, options.GenerateExceptionsFile, configurationError);
				compilationContext.RegisterSyntaxNodeAction(compilationAnalyzer.AnalyzeMethod, SyntaxKind.MethodDeclaration);
				compilationContext.RegisterCompilationEndAction(compilationAnalyzer.EndCompilationAction);
			});
		}

		public const string ExceptionsFileName = @"DuplicateCode.Allowed.txt";

		public virtual HashSet<string> InitializeExceptions(ImmutableArray<AdditionalText> additionalFiles)
		{
			foreach (AdditionalText additionalFile in additionalFiles)
			{
				string fileName = Path.GetFileName(additionalFile.Path);
				StringComparer comparer = StringComparer.OrdinalIgnoreCase;
				if (comparer.Equals(fileName, ExceptionsFileName))
				{
					return LoadAllowedMethods(additionalFile.GetText());
				}
			}
			return new HashSet<string>();
		}

		public virtual EditorConfigOptions InitializeEditorConfigOptions(AnalyzerOptions analyzerOptions, Compilation compilation, out Diagnostic error)
		{
			error = null;
			EditorConfigOptions options = new EditorConfigOptions(DefaultDuplicateTokenThreshold);
			var editorConfigHelper = new AdditionalFilesHelper(analyzerOptions, compilation);

			ExceptionsOptions exceptionsOptions = editorConfigHelper.LoadExceptionsOptions(Rule.Id);
			options.IgnoreExceptionsFile = exceptionsOptions.IgnoreExceptionsFile;
			options.GenerateExceptionsFile = exceptionsOptions.GenerateExceptionsFile;

			string strTokenCount = editorConfigHelper.GetValueFromEditorConfig(Rule.Id, @"token_count");
			if (!string.IsNullOrWhiteSpace(strTokenCount))
			{
				strTokenCount = strTokenCount.Trim();
				bool isParseSuccessful = int.TryParse(strTokenCount, out int duplicateTokenThreshold);

				if (!isParseSuccessful)
				{
					duplicateTokenThreshold = DefaultDuplicateTokenThreshold;
					error = Diagnostic.Create(InvalidTokenCountRule, null, strTokenCount);
				}

				const int MaxTokenCount = 200;
				if (duplicateTokenThreshold > MaxTokenCount)
				{
					error = Diagnostic.Create(TokenCountTooBigRule, null, duplicateTokenThreshold, MaxTokenCount);
					duplicateTokenThreshold = MaxTokenCount;
				}
				const int MinTokenCount = 20;
				if (duplicateTokenThreshold < MinTokenCount)
				{
					error = Diagnostic.Create(TokenCountTooSmallRule, null, duplicateTokenThreshold, MinTokenCount);
					duplicateTokenThreshold = MinTokenCount;
				}
				options.TokenCount = duplicateTokenThreshold;
			}
			return options;
		}

		public virtual HashSet<string> LoadAllowedMethods(SourceText text)
		{
			HashSet<string> result = new HashSet<string>();
			foreach (TextLine line in text.Lines)
			{
				result.Add(line.ToString());
			}
			return result;
		}

		private class CompilationAnalyzer
		{
			private readonly DuplicateDetectorDictionary _library = new DuplicateDetectorDictionary();
			private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();
			private readonly int _duplicateTokenThreshold;
			private readonly HashSet<string> _exceptions;
			private readonly bool _generateExceptionsFile;

			public CompilationAnalyzer(int duplicateTokenThreshold, HashSet<string> exceptions, bool generateExceptionsFile, Diagnostic configurationError)
			{
				_duplicateTokenThreshold = duplicateTokenThreshold;
				_exceptions = exceptions;
				_generateExceptionsFile = generateExceptionsFile;
				if (configurationError != null)
				{
					_diagnostics.Add(configurationError);
				}
			}

			private string ToPrettyReference(FileLinePositionSpan fileSpan)
			{
				string file = Path.GetFileName(fileSpan.Path);

				// This API uses 0-based line positioning, so add 1
				return $@"{file} line {fileSpan.StartLinePosition.Line + 1} character {fileSpan.StartLinePosition.Character + 1}";
			}

			public void AnalyzeMethod(SyntaxNodeAnalysisContext obj)
			{
				try
				{
					MethodDeclarationSyntax methodDeclarationSyntax = (MethodDeclarationSyntax)obj.Node;
					SyntaxNode body = methodDeclarationSyntax.Body;
					if (body == null)
					{
						return;
					}

					if (_exceptions.Contains(methodDeclarationSyntax.Identifier.ValueText))
					{
						return;
					}

					RollingTokenSet rollingTokenSet = new RollingTokenSet(_duplicateTokenThreshold);

					foreach (SyntaxToken token in body.DescendantTokens())
					{
						GetShapeDetails(token);

						// For every set of token_count contiguous tokens, create a hash and add it to a dictionary with some evidence.
						(int hash, Evidence evidence) = rollingTokenSet.Add(new TokenInfo(token));

						if (rollingTokenSet.IsFull())
						{
							Evidence existingEvidence = _library.TryAdd(hash, evidence);
							if (existingEvidence != null)
							{
								Location location = evidence.LocationEnvelope.Contents();
								Location existingEvidenceLocation = existingEvidence.LocationEnvelope.Contents();

								// We found a duplicate, but if it's partially duplicated with itself, ignore it.
								if (!location.SourceSpan.IntersectsWith(existingEvidenceLocation.SourceSpan))
								{
									string shapeDetails = GetShapeDetails(token);
									string reference = ToPrettyReference(existingEvidenceLocation.GetLineSpan());

									_diagnostics.Add(Diagnostic.Create(Rule, location, new List<Location>() { existingEvidenceLocation }, reference, shapeDetails));

									if (_generateExceptionsFile)
									{
										File.AppendAllText(@"DuplicateCode.Allowed.GENERATED.txt", methodDeclarationSyntax.Identifier.ValueText + Environment.NewLine);
									}

									// Don't pile on.  Move on to the next method.
									return;
								}

								// Don't pile on.  Move on to the next method.
								return;
							}
						}
					}
				}
				catch (Exception ex)
				{
					string result = string.Empty;
					string[] lines = ex.StackTrace.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (string line in lines)
					{
						if (line.StartsWith("line") && line.Length >= 8)
						{
							result += line.Substring(0, 8);
							result += " ";
						}
					}
					if (string.IsNullOrWhiteSpace(result))
					{
						result = ex.StackTrace.Replace(Environment.NewLine, " ## ");
					}
					_diagnostics.Add(Diagnostic.Create(UnhandledExceptionRule, obj.Node.GetLocation(), result, ex.Message));
				}
			}

			public void EndCompilationAction(CompilationAnalysisContext context)
			{
				foreach (Diagnostic diagnostic in _diagnostics)
				{
					context.ReportDiagnostic(diagnostic);
				}
			}

			private string GetShapeDetails(SyntaxToken token)
			{
				List<SyntaxToken> tokens = new List<SyntaxToken>();
				SyntaxToken currentToken = token;
				for (int i = 0; i < _duplicateTokenThreshold; i++)
				{
					tokens.Add(currentToken);
					currentToken = currentToken.GetPreviousToken();
				}
				tokens.Reverse();
				StringBuilder details = new StringBuilder();
				foreach (SyntaxToken t in tokens)
				{
					string result;
					switch (t.Kind())
					{
						case SyntaxKind.VirtualKeyword:
						case SyntaxKind.OverrideKeyword:
						case SyntaxKind.NewKeyword:
						case SyntaxKind.SealedKeyword:
						case SyntaxKind.ReadOnlyKeyword:
						case SyntaxKind.InternalKeyword:
						case SyntaxKind.StaticKeyword:
						case SyntaxKind.PartialKeyword:
						case SyntaxKind.PublicKeyword:
						case SyntaxKind.PrivateKeyword:
						case SyntaxKind.ProtectedKeyword:
						case SyntaxKind.IfKeyword:
						case SyntaxKind.ElseKeyword:
						case SyntaxKind.WhileKeyword:
						case SyntaxKind.ForKeyword:
						case SyntaxKind.AbstractKeyword:
						case SyntaxKind.AddKeyword:
						case SyntaxKind.AliasKeyword:
						case SyntaxKind.AmpersandAmpersandToken:
						case SyntaxKind.AmpersandEqualsToken:
						case SyntaxKind.AmpersandToken:
						case SyntaxKind.AnnotationsKeyword:
						case SyntaxKind.ArgListKeyword:
						case SyntaxKind.AscendingKeyword:
						case SyntaxKind.AsKeyword:
						case SyntaxKind.AssemblyKeyword:
						case SyntaxKind.AsteriskEqualsToken:
						case SyntaxKind.AsteriskToken:
						case SyntaxKind.AsyncKeyword:
						case SyntaxKind.AwaitKeyword:
						case SyntaxKind.BackslashToken:
						case SyntaxKind.BarBarToken:
						case SyntaxKind.BarEqualsToken:
						case SyntaxKind.BarToken:
						case SyntaxKind.BaseKeyword:
						case SyntaxKind.BoolKeyword:
						case SyntaxKind.BreakKeyword:
						case SyntaxKind.ByKeyword:
						case SyntaxKind.ByteKeyword:
						case SyntaxKind.CaretEqualsToken:
						case SyntaxKind.CaseKeyword:
						case SyntaxKind.ClassKeyword:
						case SyntaxKind.CloseBraceToken:
						case SyntaxKind.CloseBracketToken:
						case SyntaxKind.CloseParenToken:
						case SyntaxKind.ColonToken:
						case SyntaxKind.ColonColonToken:
						case SyntaxKind.TrueKeyword:
						case SyntaxKind.FalseKeyword:
						case SyntaxKind.DotToken:
						case SyntaxKind.IntKeyword:
						case SyntaxKind.InterpolatedStringStartToken:
						case SyntaxKind.InterpolatedStringEndToken:
						case SyntaxKind.InterpolatedVerbatimStringStartToken:
						case SyntaxKind.PlusEqualsToken:
						case SyntaxKind.PlusToken:
						case SyntaxKind.OpenBraceToken:
						case SyntaxKind.OpenParenToken:
						case SyntaxKind.SemicolonToken:
						case SyntaxKind.OpenBracketToken:
						case SyntaxKind.CommaToken:
						case SyntaxKind.LessThanToken:
						case SyntaxKind.LessThanSlashToken:
						case SyntaxKind.GreaterThanEqualsToken:
						case SyntaxKind.GreaterThanToken:
						case SyntaxKind.EqualsEqualsToken:
						case SyntaxKind.EqualsToken:
						case SyntaxKind.EqualsGreaterThanToken:
						case SyntaxKind.StringKeyword:
						case SyntaxKind.PlusPlusToken:
						case SyntaxKind.ReturnKeyword:
						case SyntaxKind.UsingKeyword:
						case SyntaxKind.UShortKeyword:
						case SyntaxKind.UIntKeyword:
						case SyntaxKind.ShortKeyword:
						case SyntaxKind.SwitchKeyword:
						case SyntaxKind.ForEachKeyword:
						case SyntaxKind.LockKeyword:
						case SyntaxKind.RefKeyword:
						case SyntaxKind.OutKeyword:
						case SyntaxKind.CharKeyword:
						case SyntaxKind.QuestionToken:
						case SyntaxKind.QuestionQuestionToken:
						case SyntaxKind.QuestionQuestionEqualsToken:
						case SyntaxKind.ExclamationToken:
						case SyntaxKind.ExclamationEqualsToken:
						case SyntaxKind.ObjectKeyword:
						case SyntaxKind.NullKeyword:
						case SyntaxKind.NullableKeyword:
						case SyntaxKind.RegionKeyword:
						case SyntaxKind.EndRegionKeyword:
						case SyntaxKind.TryKeyword:
						case SyntaxKind.CatchKeyword:
						case SyntaxKind.FinallyKeyword:
						case SyntaxKind.InKeyword:
						case SyntaxKind.TypeOfKeyword:
						case SyntaxKind.MinusToken:
						case SyntaxKind.MinusEqualsToken:
						case SyntaxKind.MinusMinusToken:
						case SyntaxKind.MinusGreaterThanToken:
						case SyntaxKind.CheckedKeyword:
						case SyntaxKind.UncheckedKeyword:
						case SyntaxKind.ContinueKeyword:
						case SyntaxKind.CaretToken:
						case SyntaxKind.ConstKeyword:
						case SyntaxKind.DelegateKeyword:
						case SyntaxKind.DoKeyword:
						case SyntaxKind.EnumKeyword:
						case SyntaxKind.DoubleKeyword:
						case SyntaxKind.EventKeyword:
						case SyntaxKind.FloatKeyword:
						case SyntaxKind.FixedKeyword:
						case SyntaxKind.FromKeyword:
						case SyntaxKind.GetKeyword:
						case SyntaxKind.SetKeyword:
						case SyntaxKind.GotoKeyword:
						case SyntaxKind.GreaterThanGreaterThanEqualsToken:
						case SyntaxKind.GreaterThanGreaterThanToken:
						case SyntaxKind.GreaterThanOrEqualExpression:
						case SyntaxKind.LessThanEqualsToken:
						case SyntaxKind.LessThanLessThanEqualsToken:
						case SyntaxKind.LessThanLessThanToken:
						case SyntaxKind.LongKeyword:
						case SyntaxKind.NameOfKeyword:
						case SyntaxKind.PercentEqualsToken:
						case SyntaxKind.PercentToken:
						case SyntaxKind.SingleQuoteToken:
						case SyntaxKind.DoubleQuoteToken:
						case SyntaxKind.SizeOfKeyword:
						case SyntaxKind.SlashToken:
						case SyntaxKind.SlashEqualsToken:
						case SyntaxKind.StructKeyword:
						case SyntaxKind.ThisKeyword:
						case SyntaxKind.ThrowKeyword:
						case SyntaxKind.TildeToken:
						case SyntaxKind.YieldKeyword:
						case SyntaxKind.WhenKeyword:
						case SyntaxKind.VoidKeyword:
						case SyntaxKind.UnderscoreToken:
						case SyntaxKind.UnsafeKeyword:
						case SyntaxKind.ULongKeyword:
							result = t.ValueText;
							break;
						default:
							result = t.Kind().ToString();
							result = result.Replace(@"Token", "");
							break;
					}
					details.Append(result);
					details.Append(' ');
				}
				return details.ToString().Trim();
			}
		}
	}

	public class EditorConfigOptions
	{
		public EditorConfigOptions(int defaultTokenCount)
		{
			TokenCount = defaultTokenCount;
		}

		public int TokenCount { get; set; }
		public bool IgnoreExceptionsFile { get; set; }
		public bool GenerateExceptionsFile { get; set; }
	}

	public class DuplicateDetectorDictionary
	{
		private readonly Dictionary<int, List<Evidence>> _library = new Dictionary<int, List<Evidence>>();
		private readonly object _lock = new object();

		public Evidence TryAdd(int key, Evidence value)
		{
			lock (_lock)
			{
				if (_library.TryGetValue(key, out List<Evidence> existingValues))
				{
					// We found a potential duplicate.  Is it actually?
					foreach (Evidence e in existingValues)
					{
						if (e.Components.SequenceEqual(value.Components))
						{
							// Yes, just return the duplicate information
							return e;
						}
					}
					// Our key exists already, but not us.  I.e., a hash collision.
					existingValues.Add(value);
					return null;
				}

				// New key
				existingValues = new List<Evidence>
				{
					value
				};
				_library.Add(key, existingValues);
				return null;
			}
		}
	}

	public class Evidence
	{
		private readonly Func<LocationEnvelope> _materializeEnvelope;

		public Evidence(Func<LocationEnvelope> materializeEnvelope, List<int> components, int componentSum)
		{
			_materializeEnvelope = materializeEnvelope;
			Components = components;
			Hash = componentSum;
		}

		public LocationEnvelope LocationEnvelope { get { return _materializeEnvelope(); } }
		public List<int> Components { get; }
		public int Hash { get; }
	}

	public class LocationEnvelope
	{
		private readonly Location _location;
		public LocationEnvelope(Location location = null)
		{
			_location = location;
		}
		public virtual Location Contents()
		{
			return _location;
		}
	}

	public class TokenInfo
	{
		private readonly SyntaxToken _syntaxToken;
		private readonly int _hash;

		public TokenInfo(int hash)
		{
			_hash = hash;
		}

		public TokenInfo(SyntaxToken syntaxToken)
		{
			_syntaxToken = syntaxToken;

			// SyntaxKinds start at 8193.
			_hash = ((int)_syntaxToken.Kind()) - 8192;
		}

		public virtual LocationEnvelope GetLocationEnvelope()
		{
			return new LocationEnvelope(_syntaxToken.GetLocation());
		}

		public virtual SyntaxTree GetSyntaxTree()
		{
			return _syntaxToken.SyntaxTree;
		}

		public override int GetHashCode()
		{
			return _hash;
		}
	}

	public class RollingHashCalculator<T>
	{
		protected Queue<T> _components = new Queue<T>();
		private readonly int _basePowMaxComponentsModulusCache;
		private readonly int _base;
		private readonly int _modulus;

		public RollingHashCalculator(int maxItems, int baseModulus = 227, int modulus = 1000005)
		{
			MaxItems = maxItems;
			_base = baseModulus;
			_modulus = modulus;

			_basePowMaxComponentsModulusCache = _base;
			_basePowMaxComponentsModulusCache %= _modulus;
			for (int i = 1; i < MaxItems; i++)
			{
				_basePowMaxComponentsModulusCache *= _base;
				_basePowMaxComponentsModulusCache %= _modulus;
			}
		}

		public Queue<T> Components { get { return _components; } }

		public (List<int> components, int hash) ToComponentHashes()
		{
			int sum = 0;
			var componentHashes = new List<int>(Components.Count);
			foreach (T token in Components)
			{
				int hashcode = token.GetHashCode();

				componentHashes.Add(hashcode);
				sum += hashcode;
			}

			return (componentHashes, sum);
		}

		public int MaxItems { get; private set; }
		public int HashCode { get; protected set; }

		public bool IsFull()
		{
			return _components.Count >= MaxItems;
		}

		public T Add(T token)
		{
			_components.Enqueue(token);
			T purgedHashComponent = default;
			bool isPurged = false;
			if (_components.Count > MaxItems)
			{
				// Remove old value
				purgedHashComponent = _components.Dequeue();
				isPurged = true;
			}
			CalcNewHashCode(token, isPurged, purgedHashComponent);
			return _components.Peek();
		}

		protected virtual void CalcNewHashCode(T hashComponent, bool isPurged, T purgedHashComponent)
		{
			// Shift left
			HashCode *= _base;
			HashCode %= _modulus;

			// Add new value
			HashCode += hashComponent.GetHashCode();
			HashCode %= _modulus;

			if (isPurged)
			{
				// Remove old value
				HashCode = (HashCode - ((purgedHashComponent.GetHashCode() * _basePowMaxComponentsModulusCache) % _modulus) + _modulus) % _modulus;
			}
		}
	}

	public class RollingTokenSet
	{
		private readonly RollingHashCalculator<TokenInfo> _hashCalculator;

		public RollingTokenSet(int maxItems)
			: this(new RollingHashCalculator<TokenInfo>(maxItems))
		{ }

		public RollingTokenSet(RollingHashCalculator<TokenInfo> hashCalculator)
		{
			_hashCalculator = hashCalculator;
		}

		public virtual Func<LocationEnvelope> MakeFullLocationEnvelope(TokenInfo firstToken, TokenInfo lastToken)
		{
			LocationEnvelope cache = null;

			return () =>
			{
				if (cache != null)
				{
					return cache;
				}

				if (firstToken.GetLocationEnvelope().Contents() == null)
				{
					return firstToken.GetLocationEnvelope();
				}

				int start = firstToken.GetLocationEnvelope().Contents().SourceSpan.Start;
				int end = lastToken.GetLocationEnvelope().Contents().SourceSpan.End;
				TextSpan textSpan = TextSpan.FromBounds(start, end);
				Location location = Location.Create(firstToken.GetSyntaxTree(), textSpan);

				cache = new LocationEnvelope(location);

				return cache;
			};
		}

		public (int, Evidence) Add(TokenInfo token)
		{
			TokenInfo firstToken = _hashCalculator.Add(token);

			(var components, var hash) = _hashCalculator.ToComponentHashes();

			Evidence e = new Evidence(MakeFullLocationEnvelope(firstToken, token), components, hash);

			return (_hashCalculator.HashCode, e);
		}

		public bool IsFull()
		{
			return _hashCalculator.IsFull();
		}
	}
}
