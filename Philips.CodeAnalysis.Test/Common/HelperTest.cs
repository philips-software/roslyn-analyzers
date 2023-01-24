// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.Test.Common
{
	[TestClass]
	public class HelperTest
	{
		private Diagnostic Make(string id)
		{
			var descriptor = new DiagnosticDescriptor(id, string.Empty, string.Empty, string.Empty, DiagnosticSeverity.Error, false);
			return Diagnostic.Create(descriptor, null);
		}

		[TestMethod]
		public void ToPrettyListTest()
		{
			Diagnostic diagnostic1 = Make("PH1000");
			Diagnostic diagnostic2 = Make("PH2000");
			Diagnostic diagnostic3 = Make("PH3000");

			Assert.AreEqual("", Helper.ToPrettyList(Array.Empty<Diagnostic>()));
			Assert.AreEqual("PH1000", Helper.ToPrettyList(new Diagnostic[] { diagnostic1 }));
			Assert.AreEqual("PH1000, PH2000", Helper.ToPrettyList(new Diagnostic[] { diagnostic1, diagnostic2 }));
			Assert.AreEqual("PH1000, PH2000, PH3000", Helper.ToPrettyList(new Diagnostic[] { diagnostic1, diagnostic2, diagnostic3 }));
		}
	}
}
