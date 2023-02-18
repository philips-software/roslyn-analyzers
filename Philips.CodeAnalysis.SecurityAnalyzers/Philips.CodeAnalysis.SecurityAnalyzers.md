| Rule ID | Title                  | Description                                                         |
| ------- | ---------------------- | ------------------------------------------------------------------- |
| [PH2100](../Documentation/Diagnostics/PH2100.md)  | Avoid Password         | Naming something Password suggests a potential hard-coded password. |
| [PH2137](../Documentation/Diagnostics/PH2137.md)  | Regex needs timeout    | When constructing a new Regex instance, provide a timeout (or `RegexOptions.NonBacktracking` in .NET 7 and higher) as this can facilitate denial-of-serice attacks.|
