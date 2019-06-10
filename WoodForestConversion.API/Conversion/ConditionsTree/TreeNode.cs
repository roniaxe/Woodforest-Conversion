using System;
using System.Collections;
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
        public readonly Guid Id;
        [JsonConverter(typeof(StringEnumConverter))]
        public ConditionMatch ConditionMatch { get; set; }
        public List<Condition> Conditions { get; } = new List<Condition>();
        public HashSet<TreeNode> Children { get; } = new HashSet<TreeNode>();
        public TreeNode Parent { get; set; }
        public List<SetFreqDto> NodeScheduleTrigger { get; set; } = new List<SetFreqDto>();

        public TreeNode(Guid id)
        {
            Id = id;
        }
        public int ChildrenCount => Children.Count;
        public static TreeNode BuildTree(ConditionSet root, IQueryable<ConditionSet> conditionSets, IQueryable<Condition> conditions)
        {
            var treeNode = new TreeNode(root.SetUID)
            {
                ConditionMatch = (ConditionMatch) root.Matching
            };

            var nodeConditions = conditions.Where(c => c.SetUID == root.SetUID);
            treeNode.Conditions.AddRange(nodeConditions);

            var childrenSets = conditionSets.Where(cs => cs.ParentSet == root.SetUID);
            foreach (var set in childrenSets)
            {
                var childNode = BuildTree(set, conditionSets, conditions);
                childNode.Parent = treeNode;
                treeNode.Children.Add(childNode);
            }
            
            return treeNode;
        }
    }
}
