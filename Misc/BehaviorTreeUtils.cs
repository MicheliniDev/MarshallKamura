using BepInEx.Logging;
using DHUtil.SerializableDictionary;
using System.Collections.Generic;
using System.Text;
using Object = UnityEngine.Object;

namespace KamuraPrime.Misc
{
    public static class BehaviorTreeUtils
    {
        public static void ForceNode(OneStepBranchNode branchNode, BossBlackboard bossBlackboard, NodeBT targetNode)
        {
            branchNode.branches.TryAdd(targetNode.Label, targetNode);
            bossBlackboard.SetBranchTrigger(targetNode.Label);
        }

        public static BehaviourTree CloneTree(BehaviourTree baseTree)
        {
            if (!baseTree) 
                return null;

            BehaviourTree newTree = Object.Instantiate(baseTree);
            newTree.name = baseTree.name + "_Clone";

            Dictionary<NodeBT, NodeBT> map = new Dictionary<NodeBT, NodeBT>();
            newTree.nodes = new List<NodeBT>();

            if (baseTree.rootNodeNode != null)
            {
                newTree.rootNodeNode = CloneNodeRecursive(baseTree.rootNodeNode, map, newTree.nodes) as RootNode;
            }

            return newTree;
        }

        private static NodeBT CloneNodeRecursive(NodeBT original, Dictionary<NodeBT, NodeBT> map, List<NodeBT> flatList)
        {
            if (!original) 
                return null;

            if (map.ContainsKey(original)) 
                return map[original];

            NodeBT clone = Object.Instantiate(original);
            clone.name = original.name;
            clone.Label = original.Label;

            map[original] = clone;
            flatList.Add(clone);

            if (original is RootNode rootOrig && clone is RootNode rootClone)
            {
                rootClone.child = CloneNodeRecursive(rootOrig.child, map, flatList);
            }
            else if (original is DecoratorNodeBt decOrig && clone is DecoratorNodeBt decClone)
            {
                decClone.child = CloneNodeRecursive(decOrig.child, map, flatList);
            }
            else if (original is CompositeNodeBt compOrig && clone is CompositeNodeBt compClone)
            {
                int childCount = compOrig.children != null ? compOrig.children.Count : 0;
                compClone.children = new List<NodeBT>(childCount);

                if (compOrig.children != null)
                {
                    foreach (var child in compOrig.children)
                    {
                        compClone.children.Add(CloneNodeRecursive(child, map, flatList));
                    }
                }
            }
            else if (original is BranchNode branchOrig && clone is BranchNode branchClone)
            {
                branchClone.defaultBranch = CloneNodeRecursive(branchOrig.defaultBranch, map, flatList);

                branchClone.branches = new SerializableDictionary<string, NodeBT>();
                if (branchOrig.branches != null)
                {
                    foreach (var branch in branchOrig.branches)
                    {
                        branchClone.branches.Add(branch.Key, CloneNodeRecursive(branch.Value, map, flatList));
                    }
                }
            }

            return clone;
        }

        public static void IndexTree(BossBlackboard bossBlackboard, Dictionary<string, NodeBT> registry, NodeBT node)
        {
            string key = !string.IsNullOrEmpty(node.Label) ? node.Label : node.name;

            if (!registry.ContainsKey(key))
            {
                registry.Add(key, node);
            }

            if (node is RootNode root)
            {
                IndexTree(bossBlackboard, registry, root.child);
            }
            else if (node is CompositeNodeBt composite)
            {
                if (composite.children != null)
                {
                    foreach (var child in composite.children)
                        IndexTree(bossBlackboard, registry, child);
                }
            }
            else if (node is DecoratorNodeBt decorator)
            {
                IndexTree(bossBlackboard, registry, decorator.child);
            }
            else if (node is BranchNode branch)
            {
                IndexTree(bossBlackboard, registry, branch.defaultBranch);
                if (branch.branches != null)
                {
                    foreach (var braanch in branch.branches)
                        IndexTree(bossBlackboard, registry, braanch.Value);
                }
            }
        }

        public static void BindNodeRecursive(BossBlackboard bossBlackboard, NodeBT node)
        {
            node.Blackboard = bossBlackboard;

            if (node is CompositeNodeBt composite)
            {
                foreach (var child in composite.children)
                    BindNodeRecursive(bossBlackboard, child);
            }
        }

        public static void DumpFsm(BehaviourTree tree)
        {
            if (tree == null)
            {
                Plugin.Instance.Log("[FSM Dump] Cannot dump: Tree object is NULL.", LogLevel.Error);
                return;
            }

            if (tree.rootNodeNode == null)
            {
                Plugin.Instance.Log($"[FSM Dump] Tree '{tree.name}' has no Root Node assigned!", LogLevel.Error);
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"FSM DUMP: {tree.name}");

            DumpNodeRecursive(tree.rootNodeNode, 0, sb, "Root");

            Plugin.Instance.Log(sb.ToString());
        }

        private static void DumpNodeRecursive(NodeBT node, int depth, StringBuilder sb, string prefix)
        {
            string indent = new string(' ', depth * 4);
            string arrow = (depth > 0) ? "└─ " : "";

            if (node == null)
            {
                sb.AppendLine($"{indent}{arrow}[{prefix}]: NULL");
                return;
            }

            bool isClone = node.name.Contains("(Clone)") || !node.name.Contains("Asset");

            sb.AppendLine($"{indent}{arrow}[{prefix}] {node.GetType().Name} : {node.name}");

            if (node is RootNode root)
            {
                DumpNodeRecursive(root.child, depth + 1, sb, "Child");
            }
            else if (node is CompositeNodeBt composite)
            {
                if (composite.children == null || composite.children.Count == 0)
                {
                    sb.AppendLine($"{indent}    (No Children)");
                }
                else
                {
                    for (int i = 0; i < composite.children.Count; i++)
                    {
                        DumpNodeRecursive(composite.children[i], depth + 1, sb, $"Index {i}");
                    }
                }
            }
            else if (node is BranchNode branch)
            {
                DumpNodeRecursive(branch.defaultBranch, depth + 1, sb, "Default");

                if (branch.branches != null)
                {
                    foreach (var kvp in branch.branches)
                    {
                        DumpNodeRecursive(kvp.Value, depth + 1, sb, $"Trigger: '{kvp.Key}'");
                    }
                }
            }
            else if (node is DecoratorNodeBt decorator)
            {
                DumpNodeRecursive(decorator.child, depth + 1, sb, "Child");
            }
        }
    }
}
