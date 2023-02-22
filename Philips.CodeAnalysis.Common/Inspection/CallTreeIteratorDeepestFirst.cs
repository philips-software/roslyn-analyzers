// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Philips.CodeAnalysis.Common.Inspection
{
	/// <summary>
	/// Traverse the <see cref="CallTreeNode"/> in Port-Order Depth First, as described for LRN in <see href="https://en.wikipedia.org/wiki/Tree_traversal"/>.
	/// </summary>
	public sealed class CallTreeIteratorDeepestFirst : IEnumerator<CallTreeNode>, IEnumerable<CallTreeNode>
	{
		private readonly CallTreeNode _root;
		private readonly HashSet<CallTreeNode> _visited;

		public CallTreeIteratorDeepestFirst(CallTreeNode root)
		{
			_root = root;
			_visited = new HashSet<CallTreeNode>();
			Reset();
		}

		public bool MoveNext()
		{
			// Check the end status.
			if (ReferenceEquals(_root, Current))
			{
				return false;
			}
			// Check for start status.
			if (Current is null)
			{
				Current = FindDeepest(_root);
				_ = _visited.Add(Current);
				return true;
			}

			CallTreeNode nextSibling = Current.GetNextSibling();
			CallTreeNode next = FindDeepest(nextSibling) ?? BackTrack();
			Current = next;
			_ = _visited.Add(Current);
			return Current != null;
		}

		public void Reset()
		{
			Current = null;
			_visited.Clear();
		}

		public CallTreeNode Current { get; private set; }

		object IEnumerator.Current => Current;

		public void Dispose()
		{
			// Nothing to be done here
		}

		private CallTreeNode FindDeepest(CallTreeNode start)
		{
			if (start == null)
			{
				return null;
			}
			CallTreeNode current;
			CallTreeNode candidate = start;
			do
			{
				current = candidate;
				candidate = current.Children.FirstOrDefault(child => !_visited.Contains(child));
			}
			while (candidate != null);

			return current;
		}

		private CallTreeNode BackTrack()
		{
			CallTreeNode parent = Current.Parent;
			return FindDeepest(parent);
		}

		public IEnumerator<CallTreeNode> GetEnumerator()
		{
			return this;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this;
		}
	}
}
