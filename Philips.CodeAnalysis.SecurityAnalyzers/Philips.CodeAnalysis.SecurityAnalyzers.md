| Rule ID | Title                  | Description                                                         |
| ------- | ---------------------- | ------------------------------------------------------------------- |
| PH2100  | Avoid Password         | Naming something Password suggests a potential hard-coded password. |
| PH3136  | Regex needs timeout    | When constructing a new Regex instance, provide a timeout (or `RegexOptions.NonBacktracking` in .NET 7 and higher) as this can facilitate denial-of-serice attacks.|
