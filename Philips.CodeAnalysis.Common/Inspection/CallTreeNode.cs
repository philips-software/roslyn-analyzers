// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Philips.CodeAnalysis.Common.Inspection
{
	public class CallTreeNode
	{
		private const int CallOpcode = 0x28;
		private const int VirtualCallOpcode = 0x6F;
		private const int NewObjectCallOpcode = 0x73;
		private readonly List<CallTreeNode> _children;
		private static readonly Dictionary<string, CallTreeNode> Cache = new();

		public CallTreeNode(MethodDefinition method, CallTreeNode parent)
		{
			_children = new();
			Method = method;
			Parent = parent;
		}

		public static CallTreeNode CreateCallTree(MethodDefinition entryPoint)
		{
			Cache.Clear();
			var root = new CallTreeNode(entryPoint, null);
			CreateCallTree(root);
			return root;
		}

		private static void CreateCallTree(CallTreeNode node)
		{
			MethodDefinition methodDef = node.Method;
			if (methodDef is { HasBody: true })
			{
				MethodBody body = methodDef.Body;
				if (Cache.TryGetValue(methodDef.FullName, out CallTreeNode cached))
				{
					node.CopyChildrenFrom(cached);
					return;
				}

				foreach (Instruction instruction in body.Instructions.Where(IsCallInstruction))
				{
					if (instruction.Operand is not MethodDefinition called)
					{
						continue;
					}
					// Check for recursive call patterns.
					if (!node.HasAncestor(called) && node.Children.All(n => n.Method != called))
					{
						CallTreeNode child = node.AddChild(called);
						CreateCallTree(child);
					}
				}
				Cache[methodDef.FullName] = node;
			}
		}

		public static bool IsCallInstruction(Instruction instruction)
		{
			var opCode = instruction.OpCode.Op2;
			return opCode is CallOpcode or VirtualCallOpcode or NewObjectCallOpcode;
		}

		public CallTreeNode Parent { get; }

		public IReadOnlyList<CallTreeNode> Children => _children;

		public MethodDefinition Method { get; }

		public object Tag { get; set; }

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
			List<CallTreeNode> siblings = Parent._children;
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
			CallTreeNode current = Parent;
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
			foreach (CallTreeNode child in source._children)
			{
				var newChild = new CallTreeNode(child.Method, this);
				_children.Add(newChild);
				newChild.CopyChildrenFrom(child);
			}
		}
	}
}
