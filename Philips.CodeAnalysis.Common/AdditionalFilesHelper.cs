// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Philips.CodeAnalysis.Common
{
	internal class AdditionalFilesHelper
	{
		private readonly ImmutableArray<AdditionalText> _additionalFiles;

		public const string EditorConfig = @".editorconfig";

		public virtual ExceptionsOptions ExceptionsOptions { get; private set; } = new ExceptionsOptions();

		public AdditionalFilesHelper(ImmutableArray<AdditionalText> additionalFiles)
		{
			_additionalFiles = additionalFiles;
		}

		public virtual HashSet<string> InitializeExceptions(string exceptionsFile, string diagnosticId)
		{
			ExceptionsOptions = InitializeExceptionsOptions(diagnosticId);
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


		public virtual ExceptionsOptions InitializeExceptionsOptions(string diagnosticId)
		{
			SourceText lines = RetrieveSourceText(EditorConfig);
			if (lines != null)
			{
				return LoadExceptionsOptions(lines, diagnosticId);
			}
			return new ExceptionsOptions();
		}

		public virtual SourceText RetrieveSourceText(string fileName)
		{
			foreach (AdditionalText additionalFile in _additionalFiles)
			{
				string currentFileName = Path.GetFileName(additionalFile.Path);
				StringComparer comparer = StringComparer.OrdinalIgnoreCase;
				if (comparer.Equals(currentFileName, fileName))
				{
					return additionalFile.GetText();
				}
			}
			return null;
		}

		public virtual ExceptionsOptions LoadExceptionsOptions(SourceText text, string diagnosticId)
		{
			ExceptionsOptions options = new ExceptionsOptions();

			foreach (TextLine textLine in text.Lines)
			{
				string line = textLine.ToString();
				if (line.Contains($@"dotnet_code_quality.{diagnosticId}.ignore_exceptions_file"))
				{
					options.IgnoreExceptionsFile = true;
				}
				if (line.Contains($@"dotnet_code_quality.{diagnosticId}.generate_exceptions_file"))
				{
					options.GenerateExceptionsFile = true;
				}
			}
			return options;
		}

		/// <summary>
		/// Get a list of values (comma separated) for the given setting in editorconfig
		/// </summary>
		/// <returns></returns>
		public virtual List<string> GetValuesFromEditorConfig(string diagnosticId, string settingKey)
		{
			SourceText lines = RetrieveSourceText(EditorConfig);
			List<string> values = new List<string>();

			if (lines == null)
				return values;

			foreach (TextLine textLine in lines.Lines)
			{
				string line = textLine.ToString();
				if (line.Contains($@"dotnet_code_quality.{diagnosticId}.{settingKey}"))
				{
					if (line.Contains('='))
					{
						string value = line.Substring(line.IndexOf('=') + 1).Trim();
						foreach (string v in value.Split(','))
						{
							values.Add(v);
						}
					}
					break;
				}
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
