// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Philips.CodeAnalysis.Common.Inspection
{
	public class CallTreeNode
	{
		private readonly List<CallTreeNode> _children;
		private static readonly Dictionary<string, CallTreeNode> _cache = new();

		public CallTreeNode(MethodDefinition method, CallTreeNode parent)
		{
			_children = new();
			Method = method;
			Parent = parent;
		}

		public static CallTreeNode CreateCallTree(MethodDefinition entryPoint)
		{
			var root = new CallTreeNode(entryPoint, null);
			CreateCallTree(root);
			return root;
		}

		private static void CreateCallTree(CallTreeNode node)
		{
			var methodDef = node.Method;
			if (methodDef is { HasBody: true })
			{
				var body = methodDef.Body;
				if (_cache.TryGetValue(methodDef.FullName, out var cached))
				{
					node.CopyChildrenFrom(cached);
					return;
				}

				foreach (var instruction in body.Instructions.Where(IsCallInstruction))
				{
					if (instruction.Operand is not MethodDefinition called)
					{
						continue;
					}
					// Check for recursive call patterns.
					if (!node.HasAncestor(called) && !node.Children.Any(n => n.Method == called))
					{
						var child = node.AddChild(called);
						CreateCallTree(child);
					}
				}
				// TODO: Investigate why object.ToString() is added in between.
				if (!_cache.ContainsKey(methodDef.FullName))
				{
					_cache.Add(methodDef.FullName, node);
				}
			}
		}

		private static bool IsCallInstruction(Instruction instruction)
		{
			var opCode = instruction.OpCode.Op2;
			return opCode is 0x28 or 0x6F or 0x73;
		}

		public CallTreeNode Parent { get; }

		public IReadOnlyList<CallTreeNode> Children => _children;

		public MethodDefinition Method { get; }

		public CallTreeNode AddChild(MethodDefinition method)
		{
			var child = new CallTreeNode(method, this);
			_children.Add(child);
			return child;
		}

		/// <summary>
		/// Gets the node that is next in the children list of this node's Parent. Or null if no such node exists.
		/// </summary>
		public CallTreeNode GetNextSibling()
		{
			if (Parent == null)
			{
				return null;
			}
			var siblings = Parent._children;
			var index = siblings.IndexOf(this);
			if (index < siblings.Count - 1)
			{
				return siblings[index + 1];
			}
			return null;
		}

		/// <summary>
		/// Returns true if any of the Parent (or Parent of Parent, etc) have the specified <see cref="MethodDefinition"/>. Returns false otherwise.
		/// </summary>
		public bool HasAncestor(MethodDefinition method)
		{
			var current = Parent;
			while (current != null)
			{
				if (current.Method == method)
				{
					return true;
				}
				current = current.Parent;
			}
			return false;
		}

		public override string ToString()
		{
			return Method.ToString();
		}

		private void CopyChildrenFrom(CallTreeNode source)
		{
			foreach(var child in source._children)
			{
				var newChild = new CallTreeNode(child.Method, this);
				_children.Add(newChild);
				newChild.CopyChildrenFrom(child);
			}
		}
	}
}
