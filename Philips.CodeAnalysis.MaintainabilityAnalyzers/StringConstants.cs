// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	public static class StringConstants
	{
		public const string ThrownExceptionPropertyKey = "MissingExceptionDocumentation";
		public const string ToStringMethodName = "ToString";
		public const string ToListMethodName = "ToList";
		public const string ToArrayMethodName = "ToArray";
		public const string TaskFullyQualifiedName = "System.Threading.Tasks.Task";
		public const string TupleFullyQualifiedName = "System.ValueTuple";
		public const string Value = "value";
		public const string Set = "set";
		public const string IDictionaryInterfaceName = "IDictionary";
		public const string IListInterfaceName = "IList";
		public const string DictionaryClassName = "Dictionary";
		public const string QueueClassName = "Queue";
		public const string StackClassName = "Stack";
		public const string SortedListClassName = "SortedList";
		public const string WindowsNewLine = "\r\n";
		public const string List = "List";
		public const string Dispose = "Dispose";
		public const string GetResourceString = "System.String System.SR::GetResourceString(System.String";
		public const string GetExceptionForWin32Error =
			"System.Exception System.IO.Win32Marshal::GetExceptionForWin32Error(System.Int32,System.String)";
		public const string GetExceptionForLastWin32Error =
			"System.Exception System.IO.Win32Marshal::GetExceptionForLastWin32Error(System.String)";
		public const string GetExceptionForIoErrno =
			"System.Exception Interop::GetExceptionForIoErrno(Interop/ErrorInfo,System.String,System.Boolean)";
		public const string Exception = "Exception";
		public const string SystemException = "System.Exception";
		public const string IoException = "System.IO.IOException";
		public const string FileNotFoundException = "System.IO.FileNotFoundException";
		public const string DirectoryNotFoundException = "System.IO.DirectoryNotFoundException";
		public const string PathTooLongException = "System.IO.PathTooLongException";
		public const string UnauthorizedException = "System.UnauthorizedException";
	}
}
