using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WoodForestConversion.API.Conversion.DTOs;
using WoodForestConversion.API.Conversion.Enums;
using WoodForestConversion.Data;

namespace WoodForestConversion.API.Conversion.ConditionsTree
{
    [Serializable]
    public class TreeNode
    {
        public List<Condition> Conditions { get; } = new List<Condition>();
        public HashSet<TreeNode> Children { get; } = new HashSet<TreeNode>();
        public TreeNode Parent { get; private set; }
        public List<SetFreqDto> NodeScheduleTrigger { get; set; } = new List<SetFreqDto>();

        private TreeNode() { }

        public int ChildrenCount => Children.Count;

        public static TreeNode GetConditionTree(ConditionSet root, IQueryable<ConditionSet> conditionSets, IQueryable<Condition> conditions)
        {
            // Create the tree root 
            var treeNode = new TreeNode();

            // Add node conditions
            treeNode.Conditions.AddRange(conditions.Where(c => c.SetUID == root.SetUID));

            // find child nodes
            var childrenSets = conditionSets.Where(cs => cs.ParentSet == root.SetUID);
            foreach (var set in childrenSets)
            {
                // regressive create child nodes
                var childNode = GetConditionTree(set, conditionSets, conditions);
                childNode.Parent = treeNode;
                treeNode.Children.Add(childNode);
            }

            return treeNode;
        }
    }
}