| Rule ID | Title                                        | Description                                                  |
| ------- | -------------------------------------------- | ------------------------------------------------------------ |
| PH2001  | Avoid empty XML Summary comments             | Summary XML comments for classes, methods, etc. must be non-empty or non-existent. |
| PH2006  | Match namespace, path, assembly, and project | In order to prevent pollution of namespaces, and maintainability of namespaces, the File Path, Assembly, Project, and Namespace must all match |
| PH2020  | Avoid Thread.Sleep                           | This method is a code smell.                                 |
| PH2021  | Avoid inline new                             | Do not inline the constructor call.  Instead, create a local variable or a field for the temporary instance. |
| PH2026  | Avoid SuppressMessage attribute              | SuppressMessage results in violations of codified coding guidelines.<br />*This analyzer is currently incorrectly assigned to the MsTestAnalyzers package.* |
| PH2027  | Avoid static methods                         | Static methods are difficult to Unit Test.<br />Create a whitelist, one class per line, in a file named StaticClasses.Allowed.txt in the project marked as an AdditionalFile. |
| PH2028  | Copyright present                            |                                                              |
| PH2029  | Avoid #pragma                                | #pragmas result in violations of codified coding guidelines. |
| PH2030  | Variable naming conventions                  |                                                              |
| PH2031  | Avoid TryParse without Culture               |                                                              |
| PH2032  | Avoid Empty Constructor                      | Empty constructors are unnecessary.                          |
| PH2044  | Avoid dynamic keyword                        | The `dynamic` keyword is not checked for type safety at compile time. |
| PH2045  | Avoid static classes                         | Create a whitelist, one class per line, in a file named StaticClasses.Allowed.txt in the project marked as an AdditionalFile. |
| PH2047  | Avoid public member variables                | Avoid public fields in a class. Declare public property if needed for static fields. |
| PH2051  | Avoid unnecessary range checks               | Do not superfluously check the length of a List or Array before iterating over it. |
| PH2060  | Bool naming conventions                      | Bool names start with is, are, should, has, does, or was.    |
| PH2066  | Readonly Lock                                | Locks are readonly.                                          |
| PH2067  | Avoid nested string.Format                   | Don't nest string.Format (or similar) methods.               |
| PH2068  | Avoid goto                                   | Avoid goto                                                   |
| PH2069  | Avoid unnecessary string.Format              | Don't call string.Format unnecessarily.                      |
| PH2070  | Avoid protected fields                       | Avoid protected fields                                       |
| PH2072  | Require editorconfig                         | Editorconfig files help enforce and configure Analyzers      |
| PH2073  | Call Extension method as instance            | If Foo is an extension method of MyClass, call it as `MyClass.Foo`. |
| PH2074  | Avoid Register in Dispose                    | Dispose methods should be unregistering rather than registering. |


