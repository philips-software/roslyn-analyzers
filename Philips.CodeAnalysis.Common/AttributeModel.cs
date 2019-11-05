// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Philips.CodeAnalysis.Common
{
	internal class AttributeModel
	{
		public AttributeModel(string name, string fullName, string title, string messageFormat, string description, DiagnosticIds diagnosticId, bool canBeSuppressed, bool isEnabledByDefault)
		{
			Name = name;
			FullName = fullName;
			Title = title;
			MessageFormat = messageFormat;
			Description = description;
			DiagnosticId = diagnosticId;
			Rule = CreateRule(isEnabledByDefault);
			CanBeSuppressed = canBeSuppressed;
		}

		public AttributeModel(AttributeDefinition attribute, string title, string messageFormat, string description, DiagnosticIds diagnosticId, bool canBeSuppressed, bool isEnabledByDefault)
			: this(attribute.Name, attribute.FullName, title, messageFormat, description, diagnosticId, canBeSuppressed, isEnabledByDefault)
		{ }

		public string Name { get; } = string.Empty;

		public string FullName { get; } = string.Empty;

		public string Title { get; } = string.Empty;

		public string MessageFormat { get; } = string.Empty;

		public string Description { get; } = string.Empty;

		public DiagnosticIds DiagnosticId { get; }

		public DiagnosticDescriptor Rule { get; } = null;

		public bool CanBeSuppressed { get; }

		private const string Category = Categories.Maintainability;

		private DiagnosticDescriptor CreateRule(bool isEnabledByDefaultFlag)
		{
			return new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticId),
					Title,
					MessageFormat,
					Category,
					DiagnosticSeverity.Error,
					isEnabledByDefault: isEnabledByDefaultFlag,
					description: Description);
		}
	}
}
