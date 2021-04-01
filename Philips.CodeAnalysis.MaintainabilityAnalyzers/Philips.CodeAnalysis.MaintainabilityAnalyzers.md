| Rule ID | Title                                        | Description                                                  |
| ------- | -------------------------------------------- | ------------------------------------------------------------ |
| PH2001  | Avoid empty XML Summary comments             | Summary XML comments for classes, methods, etc. must be non-empty or non-existent. |
| PH2006  | Match namespace, path, assembly, and project | The File Path, Assembly, Project, and Namespace must all match |
| PH2020  | Avoid Thread.Sleep                           | This method is a code smell.                                 |
| PH2021  | Avoid inline new                             | Do not inline the constructor call.  Instead, create a local variable or a field for the temporary instance. |
| PH2026  | Avoid SuppressMessage attribute              | SuppressMessage results in violations of codified coding guidelines.|
| PH2027  | Avoid static methods                         | Static methods complicate Unit Testing.                      |
| PH2028  | Copyright present                            | The top of the file should have a copyright.                 |
| PH2029  | Avoid #pragma                                | #pragmas result in violations of codified coding guidelines. |
| PH2030  | Variable naming conventions                  | Fields look like `_foo`. Locals look like `foo`.  (This analyzer does not respect IntelliSense settings in the .editorconfig.  It assumes this is your naming convention.)|
| PH2031  | Avoid TryParse without Culture               | When interpreting a string as a number, always specify culture information.                                                             |
| PH2032  | Avoid Empty Constructor                      | Empty constructors are unnecessary.                          |
| PH2040  | ServiceContracts have InterfaceContracts     | Interfaces marked with [ServiceContract] must have methods marked with [OperationContract]|
| PH2042  | WinForms InitalizeComponent                  | Legacy WinForms must call InitializeComponent exactly once. (As we moved away from static classes, and introduced construtor injection patterns, this pattern requires refactoring legacy code, and we found ourselves breaking these legacy forms.) |
| PH2044  | Avoid dynamic keyword                        | The `dynamic` keyword is not checked for type safety at compile time. |
| PH2045  | Avoid static classes                         | Static classes complicate Unite Testing. This rule supports a whitelist, one class per line, in a file named StaticClasses.Allowed.txt in the project marked as an AdditionalFile. |
| PH2047  | Avoid public member variables                | Avoid public fields in a class. Declare public property if needed for static fields. |
| PH2051  | Avoid unnecessary range checks               | Do not superfluously check the length of a List or Array before iterating over it. |
| PH2060  | Bool naming conventions                      | Bool names start with is, are, should, has, does, or was.    |
| PH2061  | Public/Private #regions                      | Regions called `Non-Public Data Members`, `Non-Public Properties/Methods` or `Public Interface` must have appropriate contents|
| PH2064  | Avoid duplicate #regions                     | A class cannot have regions of the same name.                |
| PH2066  | Readonly lock                                | Locks are readonly.                                          |
| PH2067  | Avoid nested string.Format                   | Don't nest string.Format (or similar) methods.               |
| PH2068  | Avoid goto                                   | Avoid goto.                                                  |
| PH2069  | Avoid unnecessary string.Format              | Don't call string.Format unnecessarily.                      |
| PH2070  | Avoid protected fields                       | Avoid protected fields.                                      |
| PH2072  | Require local editorconfig                   | Disabled by default.  As of VS 16.3, [Analyzers can now see settings](https://developercommunity.visualstudio.com/content/problem/791119/editorconfig-has-stopped-working.html) of Editorconfig files in parent folders.|
| PH2073  | Call extension method as instance            | If Foo is an extension method of MyClass, call it as `MyClass.Foo`. |
| PH2074  | Avoid register in Dispose                    | Dispose methods should unregister rather than register.      |
| PH2075  | Avoid AssemblyVersion change                 | Do not accidentally change AssemblyVersion, as it breaks runtime compatibility. Specify the intended `dotnet_code_quality.PH2075.assembly_version` in an .editorconfig specific to the project|
| PH2077  | Avoid Switch statement with no cases         | Avoid Switch statements when there are no cases|
| PH2078  | Avoid PrivateKey Property                    | Do not use PrivateKey property on X509Certificate2 class as it might cause the Application to crash. Use a Getter instead. Eg: GetRSAPrivateKey(), GetDSAPrivateKey(), GetECDsaPrivateKey()|
| PH2079  | Namespace prefix                             | Specify a namespace prefix in the .editorconfig file Eg. dotnet_code_quality.PH2079.namespace_prefix = [OrganizationName].[ProductName] and use that prefix for all projects/namespaces|
| PH2080  | Avoid hardcoded absolute paths               | Avoid hardcoded absolute paths (Windows only) |
| PH2081  | Avoid #regions within methods                | A #region cannot start or end within a method. Consider refactoring long methods instead.|
| PH2082  | Positive naming                              | Name properties, fields and variables after the positive case. People can handle double negations poorly, and by using positive naming this is prevented.|
| PH2083  | Avoid Pass By Reference                      | Parameters that are passed by reference should be written to, otherwise they should not be passed by reference|
| PH2084  | Don't lock on "new x()"                      | Locking on a newly created object has no effect|
| PH2085  | Order property accessors                     | Consistently order property accessors as get, set, init|
