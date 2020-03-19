using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Philips.CodeAnalysis.Common
{
	public class AdditionalFilesHelper
	{
		private readonly ImmutableArray<AdditionalText> _additionalFiles;
		private readonly string _diagnosticId;

		public const string EditorConfig = @".editorconfig";

		public virtual ExceptionsOptions ExceptionsOptions { get; private set; } = new ExceptionsOptions();

		public AdditionalFilesHelper(ImmutableArray<AdditionalText> additionalFiles, string diagnosticId)
		{
			_additionalFiles = additionalFiles;
			_diagnosticId = diagnosticId;
		}

		public virtual HashSet<string> InitializeExceptions(string exceptionsFile)
		{
			ExceptionsOptions = InitializeExceptionsOptions();
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


		public virtual ExceptionsOptions InitializeExceptionsOptions()
		{
			SourceText lines = RetrieveSourceText(EditorConfig);
			if (lines != null)
			{
				return LoadExceptionsOptions(lines);
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

		public virtual ExceptionsOptions LoadExceptionsOptions(SourceText text)
		{
			ExceptionsOptions options = new ExceptionsOptions();

			foreach (TextLine textLine in text.Lines)
			{
				string line = textLine.ToString();
				if (line.Contains($@"dotnet_code_quality.{_diagnosticId}.ignore_exceptions_file"))
				{
					options.IgnoreExceptionsFile = true;
				}
				if (line.Contains($@"dotnet_code_quality.{_diagnosticId}.generate_exceptions_file"))
				{
					options.GenerateExceptionsFile = true;
				}
			}
			return options;
		}

	}

	public class ExceptionsOptions
	{
		public bool IgnoreExceptionsFile { get; set; } = false;
		public bool GenerateExceptionsFile { get; set; } = false;
	}
}
