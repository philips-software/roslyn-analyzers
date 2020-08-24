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
	internal class AdditionalFilesHelper
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
			HashSet<string> exceptions = new HashSet<string>();
			if (!ExceptionsOptions.IgnoreExceptionsFile)
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
					return Convert(additionalFile.GetText());
				}
			}
			return new HashSet<string>();
		}

		public virtual HashSet<string> Convert(SourceText text)
		{
			HashSet<string> result = new HashSet<string>();
			foreach (TextLine line in text.Lines)
			{
				result.Add(line.ToString());
			}
			return result;
		}

		public virtual ExceptionsOptions LoadExceptionsOptions(string diagnosticId)
		{
			ExceptionsOptions options = new ExceptionsOptions();

			string ignoreExceptionsFile = GetValueFromEditorConfig(diagnosticId, @"ignore_exceptions_file");
			options.IgnoreExceptionsFile = !string.IsNullOrWhiteSpace(ignoreExceptionsFile);

			string generateExceptionsFile = GetValueFromEditorConfig(diagnosticId, @"generate_exceptions_file");
			options.GenerateExceptionsFile = !string.IsNullOrWhiteSpace(generateExceptionsFile);
			return options;
		}

		private string GetRawValue(string settingKey)
		{
			var analyzerConfigOptions = _options.AnalyzerConfigOptionsProvider.GetOptions(_compilation.SyntaxTrees.First());

#nullable enable
			if (analyzerConfigOptions.TryGetValue(settingKey, out string? value))
			{
				if (value == null)
				{
					return string.Empty;
				}
				return value.ToString();
			}
#nullable disable
			return string.Empty;
		}

		public virtual string GetValueFromEditorConfig(string diagnosticId, string settingKey)
		{
			return GetRawValue($@"dotnet_code_quality.{diagnosticId}.{settingKey}");
		}

		/// <summary>
		/// Get a list of values (comma separated) for the given setting in editorconfig
		/// </summary>
		/// <returns></returns>
		public virtual HashSet<string> GetValuesFromEditorConfig(string diagnosticId, string settingKey)
		{
			HashSet<string> values = new HashSet<string>();
			string value = GetValueFromEditorConfig(diagnosticId, settingKey);

			foreach (string v in value.Split(','))
			{
				values.Add(v);
			}
			return values;
		}
	}


	internal class ExceptionsOptions
	{
		public bool IgnoreExceptionsFile { get; set; } = false;
		public bool GenerateExceptionsFile { get; set; } = false;
	}
}
