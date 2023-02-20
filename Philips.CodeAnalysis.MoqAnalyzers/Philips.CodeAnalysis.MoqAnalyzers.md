| Rule ID | Title                                       | Description                                                  |
| ------- | ------------------------------------------- | ------------------------------------------------------------ |
| [PH2048](../Documentation/Diagnostics/PH2048.md)  | Mock arguments must match constructor       | Mock&lt;T> construction must call an existing constructor.   |
| [PH2053](../Documentation/Diagnostics/PH2053.md)  | Mock raise arguments must match event       | Mock&lt;T>.Raise(x => x.Event += null, sender, args) must have correct parameters.  Their types must match. |
| [PH2054](../Documentation/Diagnostics/PH2054.md)  | Mock raise arguments must match event count | Mock&lt;T>.Raise(x => x.Event += null, sender, args) must have correct parameters.  The argument count must match. |


