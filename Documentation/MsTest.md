# MS Test

The "Philips MsTest" category of diagnostics suggests ways to improve your test code using the MsTest framework.

These provide guidelines to writing test code that runs reliably and correctly. This in turn improve the quality of your code.

## ⚠️ Important: Microsoft's Official MSTest Analyzers Available

Microsoft now provides **official MSTest analyzers** that cover most functionality of the Philips MSTest analyzers. We **strongly recommend migrating** to Microsoft's official analyzers for overlapping functionality. See the [Migration Guide](#migration-guide) below for details.

---

# MSTest Analyzers Migration Guide

Microsoft now provides official MSTest analyzers as part of their testing framework. Many of the Philips MSTest analyzer rules have equivalent or superior counterparts in Microsoft's official analyzer package.

## Overview

Microsoft's official MSTest analyzers are available via the `MSTest.Analyzers` NuGet package and provide comprehensive coverage of MSTest best practices. The official analyzers use rule IDs in the format `MSTEST####` and include extensive documentation.

## Recommendation

**We recommend migrating to Microsoft's official MSTest analyzers** for all overlapping functionality. Microsoft's analyzers are:
- Officially supported and maintained by the MSTest team
- More comprehensive with 46+ rules vs Philips' 30 rules
- Better integrated with the MSTest framework
- Actively updated with new MSTest features
- Include extensive documentation and code fixes

## Rule Mapping

### Direct Overlaps (Recommended Migration)

These Philips rules have direct equivalents in Microsoft's official analyzers:

| Philips Rule | Microsoft Rule | Description | Recommendation |
|--------------|----------------|-------------|----------------|
| [PH2004](../Documentation/Diagnostics/PH2004.md) | [MSTEST0006](https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/mstest0006) | Avoid ExpectedException attribute | ✅ **Migrate to MSTEST0006** |
| [PH2005](../Documentation/Diagnostics/PH2005.md) | [MSTEST0005](https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/mstest0005) | TestContext usage validation | ✅ **Migrate to MSTEST0005** |
| [PH2013](../Documentation/Diagnostics/PH2013.md) | [MSTEST0015](https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/mstest0015) | Avoid Ignore attribute | ✅ **Migrate to MSTEST0015** |
| [PH2016](../Documentation/Diagnostics/PH2016.md) | [MSTEST0008](https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/mstest0008) | TestInitialize validation | ✅ **Migrate to MSTEST0008** |
| [PH2017](../Documentation/Diagnostics/PH2017.md) | [MSTEST0010](https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/mstest0010) | ClassInitialize validation | ✅ **Migrate to MSTEST0010** |
| [PH2018](../Documentation/Diagnostics/PH2018.md) | [MSTEST0011](https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/mstest0011) | ClassCleanup validation | ✅ **Migrate to MSTEST0011** |
| [PH2019](../Documentation/Diagnostics/PH2019.md) | [MSTEST0009](https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/mstest0009) | TestCleanup validation | ✅ **Migrate to MSTEST0009** |
| [PH2033](../Documentation/Diagnostics/PH2033.md) | [MSTEST0014](https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/mstest0014) | DataRow validation | ✅ **Migrate to MSTEST0014** |
| [PH2034](../Documentation/Diagnostics/PH2034.md) | [MSTEST0030](https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/mstest0030) | TestMethod requires TestClass | ✅ **Migrate to MSTEST0030** |
| [PH2036](../Documentation/Diagnostics/PH2036.md) | [MSTEST0003](https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/mstest0003) | TestMethod must be public | ✅ **Migrate to MSTEST0003** |
| [PH2038](../Documentation/Diagnostics/PH2038.md) | [MSTEST0002](https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/mstest0002) | TestClass must be public | ✅ **Migrate to MSTEST0002** |
| [PH2058](../Documentation/Diagnostics/PH2058.md) | [MSTEST0026](https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/mstest0026) | Avoid Assert conditional check | ✅ **Migrate to MSTEST0026** |
| [PH2059](../Documentation/Diagnostics/PH2059.md) | [MSTEST0029](https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/mstest0029) | Public methods should be TestMethod | ✅ **Migrate to MSTEST0029** |

### Partial Overlaps (Consider Migration)

These Philips rules have similar functionality in Microsoft's analyzers, often with broader coverage:

| Philips Rule | Microsoft Rule | Description | Recommendation |
|--------------|----------------|-------------|----------------|
| [PH2003](../Documentation/Diagnostics/PH2003.md) | [MSTEST0037](https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/mstest0037) | Assert.AreEqual usage patterns | ⚠️ **Consider MSTEST0037** (broader assert validation) |
| [PH2008](../Documentation/Diagnostics/PH2008.md) | [MSTEST0037](https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/mstest0037) | Assert parameter types match | ⚠️ **Consider MSTEST0037** (broader assert validation) |
| [PH2009](../Documentation/Diagnostics/PH2009.md) | [MSTEST0037](https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/mstest0037) | Assert.IsTrue/IsFalse usage | ⚠️ **Consider MSTEST0037** (broader assert validation) |
| [PH2035](../Documentation/Diagnostics/PH2035.md) | [MSTEST0014](https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/mstest0014) | DataTestMethod parameter count | ⚠️ **Consider MSTEST0014** (broader DataRow validation) |
| [PH2055](../Documentation/Diagnostics/PH2055.md) | [MSTEST0037](https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/mstest0037) | Avoid Assert.IsTrue(true) | ⚠️ **Consider MSTEST0037** (broader assert validation) |
| [PH2056](../Documentation/Diagnostics/PH2056.md) | [MSTEST0037](https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/mstest0037) | Avoid Assert.AreEqual(true, true) | ⚠️ **Consider MSTEST0037** (broader assert validation) |
| [PH2076](../Documentation/Diagnostics/PH2076.md) | [MSTEST0025](https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/mstest0025) | Assert.Fail alternatives | ⚠️ **Consider MSTEST0025** (similar intent) |

### Philips-Specific Rules (Keep if Needed)

These rules provide functionality not available in Microsoft's official analyzers:

| Philips Rule | Description | Recommendation |
|--------------|-------------|----------------|
| [PH2000](../Documentation/Diagnostics/PH2000.md) | Avoid test method prefix | 📌 **Keep if naming convention is important** |
| [PH2010](../Documentation/Diagnostics/PH2010.md) | Avoid unnecessary parentheses | 📌 **Keep if style preference matters** (Note: Generic IDE rule [IDE0047](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0047-ide0048) also available) |
| [PH2011](../Documentation/Diagnostics/PH2011.md) | Description attribute usage | 📌 **Keep if using Description attributes** |
| [PH2012](../Documentation/Diagnostics/PH2012.md) | TestTimeout required | 📌 **Keep if timeout enforcement is needed** |
| [PH2014](../Documentation/Diagnostics/PH2014.md) | Avoid Owner attribute | 📌 **Keep if avoiding Owner attributes** |
| [PH2015](../Documentation/Diagnostics/PH2015.md) | Required Categories attribute | 📌 **Keep if category enforcement is needed** |
| [PH2041](../Documentation/Diagnostics/PH2041.md) | Avoid MS Fakes | 📌 **Keep if enforcing Moq over MS Fakes** |

### Additional Microsoft Rules

Microsoft provides many additional rules not covered by Philips analyzers. Consider enabling these for comprehensive test analysis:

- **MSTEST0001**: Use Parallelize attribute for performance
- **MSTEST0017**: Assertion arguments in correct order  
- **MSTEST0018**: DynamicData validation
- **MSTEST0023**: Don't negate boolean assertions
- **MSTEST0024**: Don't store static TestContext
- **MSTEST0032**: Review always-true assert conditions
- **MSTEST0038**: Avoid AreSame with value types
- **MSTEST0040**: Avoid asserts in async void
- And many more...

## Migration Steps

### 1. Install Microsoft's MSTest Analyzers

```xml
<PackageReference Include="MSTest.Analyzers" PrivateAssets="all" />
```

### 2. Update .editorconfig

**Disable overlapping Philips rules and enable Microsoft equivalents (all set to error for strictness):**

```ini
# Disable overlapping Philips MSTest rules
dotnet_diagnostic.PH2004.severity = none  # Use MSTEST0006 instead
dotnet_diagnostic.PH2005.severity = none  # Use MSTEST0005 instead  
dotnet_diagnostic.PH2013.severity = none  # Use MSTEST0015 instead
dotnet_diagnostic.PH2016.severity = none  # Use MSTEST0008 instead
dotnet_diagnostic.PH2017.severity = none  # Use MSTEST0010 instead
dotnet_diagnostic.PH2018.severity = none  # Use MSTEST0011 instead
dotnet_diagnostic.PH2019.severity = none  # Use MSTEST0009 instead
dotnet_diagnostic.PH2033.severity = none  # Use MSTEST0014 instead
dotnet_diagnostic.PH2034.severity = none  # Use MSTEST0030 instead
dotnet_diagnostic.PH2036.severity = none  # Use MSTEST0003 instead
dotnet_diagnostic.PH2038.severity = none  # Use MSTEST0002 instead
dotnet_diagnostic.PH2058.severity = none  # Use MSTEST0026 instead
dotnet_diagnostic.PH2059.severity = none  # Use MSTEST0029 instead

# Consider disabling partial overlaps
dotnet_diagnostic.PH2003.severity = none  # Consider MSTEST0037 instead
dotnet_diagnostic.PH2008.severity = none  # Consider MSTEST0037 instead
dotnet_diagnostic.PH2009.severity = none  # Consider MSTEST0037 instead
dotnet_diagnostic.PH2035.severity = none  # Consider MSTEST0014 instead
dotnet_diagnostic.PH2055.severity = none  # Consider MSTEST0037 instead
dotnet_diagnostic.PH2056.severity = none  # Consider MSTEST0037 instead
dotnet_diagnostic.PH2076.severity = none  # Consider MSTEST0025 instead

# Enable all Microsoft MSTest rules as error (except MSTEST0019)
dotnet_diagnostic.MSTEST0001.severity = error
dotnet_diagnostic.MSTEST0002.severity = error
dotnet_diagnostic.MSTEST0003.severity = error
dotnet_diagnostic.MSTEST0004.severity = error
dotnet_diagnostic.MSTEST0005.severity = error
dotnet_diagnostic.MSTEST0006.severity = error
dotnet_diagnostic.MSTEST0007.severity = error
dotnet_diagnostic.MSTEST0008.severity = error
dotnet_diagnostic.MSTEST0009.severity = error
dotnet_diagnostic.MSTEST0010.severity = error
dotnet_diagnostic.MSTEST0011.severity = error
dotnet_diagnostic.MSTEST0012.severity = error
dotnet_diagnostic.MSTEST0013.severity = error
dotnet_diagnostic.MSTEST0014.severity = error
dotnet_diagnostic.MSTEST0015.severity = error
dotnet_diagnostic.MSTEST0016.severity = error
dotnet_diagnostic.MSTEST0017.severity = error
dotnet_diagnostic.MSTEST0018.severity = error
dotnet_diagnostic.MSTEST0019.severity = warning  # Exception: not set to error
dotnet_diagnostic.MSTEST0020.severity = error
dotnet_diagnostic.MSTEST0021.severity = error
dotnet_diagnostic.MSTEST0022.severity = error
dotnet_diagnostic.MSTEST0023.severity = error
dotnet_diagnostic.MSTEST0024.severity = error
dotnet_diagnostic.MSTEST0025.severity = error
dotnet_diagnostic.MSTEST0026.severity = error
dotnet_diagnostic.MSTEST0027.severity = error
dotnet_diagnostic.MSTEST0028.severity = error
dotnet_diagnostic.MSTEST0029.severity = error
dotnet_diagnostic.MSTEST0030.severity = error
dotnet_diagnostic.MSTEST0031.severity = error
dotnet_diagnostic.MSTEST0032.severity = error
dotnet_diagnostic.MSTEST0033.severity = error
dotnet_diagnostic.MSTEST0034.severity = error
dotnet_diagnostic.MSTEST0035.severity = error
dotnet_diagnostic.MSTEST0036.severity = error
dotnet_diagnostic.MSTEST0037.severity = error
dotnet_diagnostic.MSTEST0038.severity = error
dotnet_diagnostic.MSTEST0039.severity = error
dotnet_diagnostic.MSTEST0040.severity = error
dotnet_diagnostic.MSTEST0041.severity = error
dotnet_diagnostic.MSTEST0042.severity = error
dotnet_diagnostic.MSTEST0043.severity = error
dotnet_diagnostic.MSTEST0044.severity = error
dotnet_diagnostic.MSTEST0045.severity = error
dotnet_diagnostic.MSTEST0046.severity = error
dotnet_diagnostic.MSTEST0047.severity = error
dotnet_diagnostic.MSTEST0048.severity = error
dotnet_diagnostic.MSTEST0049.severity = error
dotnet_diagnostic.MSTEST0050.severity = error

# Keep Philips-specific rules as needed
dotnet_diagnostic.PH2000.severity = suggestion  # Test method naming
dotnet_diagnostic.PH2012.severity = warning     # Test timeout required
dotnet_diagnostic.PH2015.severity = warning     # Required categories
```

### 3. Consider Removing Philips MSTest Package

Once migration is complete and you've verified the Microsoft analyzers work for your needs, consider removing the Philips package if you don't find sufficient value from the remaining rules:

```xml
<!-- Remove this when ready -->
<PackageReference Include="Philips.CodeAnalysis.MsTestAnalyzers" Version="1.0.0" PrivateAssets="all" />
```

## Benefits of Migration

1. **Official Support**: Backed by the MSTest team at Microsoft
2. **Better Integration**: Designed specifically for MSTest framework
3. **More Comprehensive**: 46+ rules vs 30 Philips rules
4. **Active Development**: Regular updates with new MSTest features
5. **Extensive Documentation**: Each rule has detailed documentation
6. **Code Fixes**: Many rules include automatic code fixes
7. **Future-Proof**: Will evolve with MSTest framework

## Questions or Issues?

If you have questions about the migration or need help with specific scenarios, please:
1. Check the [Microsoft MSTest Analyzers documentation](https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/overview)
2. Review individual rule documentation linked in the mapping table above
3. Open an issue in this repository for Philips-specific questions

---

*This migration guide was created in response to Microsoft releasing official MSTest analyzers that provide equivalent or superior functionality to many Philips analyzer rules.*