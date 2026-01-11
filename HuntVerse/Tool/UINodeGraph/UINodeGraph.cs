using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
#endif

namespace Hunt
{
    [CreateAssetMenu(fileName = "NewUIGraph", menuName = "Hunt/UI Node Graph")]
    public class UINodeGraph : ScriptableObject
    {
        [SerializeReference] public List<UINode> nodes = new List<UINode>();
        public List<UINodeConnection> connections = new List<UINodeConnection>();
        [SerializeField, HideInInspector] private UIGraphRuntimeData bakedData;
        
        public UIGraphRuntimeData GetBakedData() => bakedData;
        public List<UINodeConnection> GetConnections() => connections;
        
        public void Bake()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            
            bakedData = new UIGraphRuntimeData();
            BakeNodes();
            BakeButtonEvents();
            
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
#endif
        }
        
        private void BakeNodes()
        {
            if (nodes == null || nodes.Count == 0) return;
            
            var executionOrder = CalculateExecutionOrder();
            bakedData.executionSteps.Clear();
            
            foreach (var nodeGuid in executionOrder)
            {
                var node = nodes.Find(n => n.guid == nodeGuid);
                if (node != null)
                {
                    PreserveNodeReferences(node);
                    var step = node.CreateExecutionStep(this);
                    if (step != null) bakedData.executionSteps.Add(step);
#if UNITY_EDITOR
                    EditorUtility.SetDirty(this);
#endif
                }
            }
        }
        
        private void BakeButtonEvents()
        {
#if UNITY_EDITOR
            foreach (var node in nodes)
            {
                if (node is ButtonClickNode btnNode && btnNode.targetButton != null)
                {
                    var button = btnNode.targetButton.GetComponent<UnityEngine.UI.Button>();
                    if (button == null) continue;
                    
                    var existingEvent = btnNode.targetButton.GetComponent<UIGraphBakedEvent>();
                    if (existingEvent != null)
                    {
                        UnityEventTools.RemovePersistentListener(button.onClick, existingEvent.OnButtonClick);
                        Undo.DestroyObjectImmediate(existingEvent);
                    }
                    
                    var bakedEvent = btnNode.targetButton.AddComponent<UIGraphBakedEvent>();
                    bakedEvent.SetGraph(this);
                    bakedEvent.SetStartNodeGuid(btnNode.guid);
                    UnityEventTools.AddPersistentListener(button.onClick, bakedEvent.OnButtonClick);
                    
                    EditorUtility.SetDirty(bakedEvent);
                    EditorUtility.SetDirty(button);
                    EditorUtility.SetDirty(btnNode.targetButton);
                }
            }
#endif
        }
        
        private void PreserveNodeReferences(UINode node)
        {
#if UNITY_EDITOR
            if (node is ButtonClickNode btnNode && btnNode.targetButton != null)
                EditorUtility.SetDirty(btnNode.targetButton);
            else if (node is HideGameObjectNode hideNode && hideNode.targetGameObjects != null)
            {
                foreach (var obj in hideNode.targetGameObjects) if (obj != null) EditorUtility.SetDirty(obj);
            }
            else if (node is ShowGameObjectNode showNode && showNode.targetGameObjects != null)
            {
                foreach (var obj in showNode.targetGameObjects) if (obj != null) EditorUtility.SetDirty(obj);
            }
            else if (node is ToggleGameObjectNode toggleNode && toggleNode.targetGameObjects != null)
            {
                foreach (var obj in toggleNode.targetGameObjects) if (obj != null) EditorUtility.SetDirty(obj);
            }
#endif
        }
        
        public string GetOrCreateTargetId(GameObject obj)
        {
            if (obj == null) return string.Empty;
            
#if UNITY_EDITOR
            var target = obj.GetComponent<UIGraphTarget>();
            if (target == null) { target = obj.AddComponent<UIGraphTarget>(); EditorUtility.SetDirty(obj); }
            
            if (string.IsNullOrEmpty(target.TargetId))
            {
                target.SetTargetId(Guid.NewGuid().ToString());
                EditorUtility.SetDirty(target);
                EditorUtility.SetDirty(obj);
            }
            
            return target.TargetId;
#else
            var target = obj.GetComponent<UIGraphTarget>();
            return target != null ? target.TargetId : string.Empty;
#endif
        }
        
        private List<string> CalculateExecutionOrder()
        {
            var result = new List<string>();
            var visited = new HashSet<string>();
            var processing = new HashSet<string>();
            var nodesWithoutInput = new HashSet<string>();
            
            foreach (var node in nodes) nodesWithoutInput.Add(node.guid);
            if (connections != null)
                foreach (var connection in connections) nodesWithoutInput.Remove(connection.toNodeGuid);
            
            foreach (var node in nodes)
                if (nodesWithoutInput.Contains(node.guid) && !visited.Contains(node.guid))
                    TopologicalSort(node.guid, visited, processing, result);
            
            foreach (var node in nodes)
                if (!visited.Contains(node.guid)) result.Add(node.guid);
            
            return result;
        }
        
        private void TopologicalSort(string nodeGuid, HashSet<string> visited, HashSet<string> processing, List<string> result)
        {
            if (processing.Contains(nodeGuid) || visited.Contains(nodeGuid)) return;
            
            processing.Add(nodeGuid);
            visited.Add(nodeGuid);
            result.Add(nodeGuid);
            
            if (connections != null)
            {
                var outgoingConnections = connections.FindAll(c => c.fromNodeGuid == nodeGuid);
                foreach (var connection in outgoingConnections)
                    TopologicalSort(connection.toNodeGuid, visited, processing, result);
            }
            
            processing.Remove(nodeGuid);
        }
    }
}
