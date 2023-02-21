// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Philips.CodeAnalysis.Common
{
	/// <summary>
	/// Helper class for handling &lt;AdditionalFiles&gt; elements.
	/// </summary>
	public class AdditionalFilesHelper
	{
		private readonly ImmutableArray<AdditionalText> _additionalFiles;
		private readonly AnalyzerOptions _options;
		private readonly Compilation _compilation;

		public virtual ExceptionsOptions ExceptionsOptions { get; private set; } = new ExceptionsOptions();

		public AdditionalFilesHelper(AnalyzerOptions options, Compilation compilation)
		{
			_options = options;
			_additionalFiles = options.AdditionalFiles;
			_compilation = compilation;
		}

		public virtual HashSet<string> InitializeExceptions(string exceptionsFile, string diagnosticId)
		{
			ExceptionsOptions = LoadExceptionsOptions(diagnosticId);
			HashSet<string> exceptions = new();
			if (ExceptionsOptions.ShouldUseExceptionsFile)
			{
				exceptions = LoadExceptions(exceptionsFile);
			}

			return exceptions;
		}

		public virtual HashSet<string> LoadExceptions(string exceptionsFile)
		{
			foreach (AdditionalText additionalFile in _additionalFiles)
			{
				string fileName = Path.GetFileName(additionalFile.Path);
				StringComparer comparer = StringComparer.OrdinalIgnoreCase;
				if (comparer.Equals(fileName, exceptionsFile))
				{
					SourceText text = additionalFile.GetText();
					return Convert(text);
				}
			}

			return new HashSet<string>();
		}

		public virtual HashSet<string> Convert(SourceText text)
		{
			HashSet<string> result = new();
			foreach (TextLine line in text.Lines)
			{
				_ = result.Add(line.ToString());
			}

			return result;
		}

		public virtual ExceptionsOptions LoadExceptionsOptions(string diagnosticId)
		{
			ExceptionsOptions options = new();

			string valueFromEditorConfig = GetValueFromEditorConfig(diagnosticId, @"ignore_exceptions_file");
			options.ShouldUseExceptionsFile = string.IsNullOrWhiteSpace(valueFromEditorConfig);

			string generateExceptionsFile = GetValueFromEditorConfig(diagnosticId, @"generate_exceptions_file");
			options.ShouldGenerateExceptionsFile = !string.IsNullOrWhiteSpace(generateExceptionsFile);
			return options;
		}

		private string GetRawValue(string settingKey)
		{
			SyntaxTree tree = _compilation.SyntaxTrees.First();
			AnalyzerConfigOptions analyzerConfigOptions = _options.AnalyzerConfigOptionsProvider.GetOptions(tree);

#nullable enable
			if (analyzerConfigOptions.TryGetValue(settingKey, out string? value))
			{
				return value == null ? string.Empty : value.ToString();
			}

			return string.Empty;
#nullable disable
		}

		public virtual string GetValueFromEditorConfig(string diagnosticId, string settingKey)
		{
			return GetRawValue($@"dotnet_code_quality.{diagnosticId}.{settingKey}");
		}

		/// <summary>
		/// Get a list of values (comma separated) for the given setting in editorconfig
		/// </summary>
		/// <returns></returns>
		public virtual IReadOnlyList<string> GetValuesFromEditorConfig(string diagnosticId, string settingKey)
		{
			List<string> values = new();
			string value = GetValueFromEditorConfig(diagnosticId, settingKey);

			foreach (string v in value.Split(','))
			{
				if (!string.IsNullOrWhiteSpace(v))
				{
					values.Add(v);
				}
			}

			return values;
		}
	}
}
