// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Philips.CodeAnalysis.Common
{
	public class AttributeModel
	{
		public AttributeModel(string name, string fullName, string title, string messageFormat, string description, DiagnosticId diagnosticId, bool isSuppressible, bool isEnabledByDefault)
		{
			Name = name;
			FullName = fullName;
			Title = title;
			MessageFormat = messageFormat;
			Description = description;
			DiagnosticId = diagnosticId;
			Rule = CreateRule(isEnabledByDefault);
			IsSuppressible = isSuppressible;
		}

		public AttributeModel(AttributeDefinition attribute, string title, string messageFormat, string description, DiagnosticId diagnosticId, bool isSuppressible, bool isEnabledByDefault)
			: this(attribute.Name, attribute.FullName, title, messageFormat, description, diagnosticId, isSuppressible, isEnabledByDefault)
		{ }

		public string Name { get; }

		public string FullName { get; }

		public string Title { get; }

		public string MessageFormat { get; }

		public string Description { get; }

		public DiagnosticId DiagnosticId { get; }

		public DiagnosticDescriptor Rule { get; }

		public bool IsSuppressible { get; }

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
