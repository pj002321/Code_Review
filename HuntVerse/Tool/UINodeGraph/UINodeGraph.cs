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
            $"[Bake] 시작 - Graph: {name}".DLog();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            
            CleanupOldBakedEvents();
            
            bakedData = new UIGraphRuntimeData();
            BakeNodes();
            BakeButtonEvents();
            
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            $"[Bake] 완료 - Graph: {name}".DLog();
#endif
        }
        
        private void CleanupOldBakedEvents()
        {
#if UNITY_EDITOR
            var processedButtons = new HashSet<GameObject>();
            var processedKeyboardObjects = new HashSet<GameObject>();
            int totalRemoved = 0;
            
            foreach (var node in nodes)
            {
                if (node is ButtonClickNode btnNode && btnNode.targetButton != null)
                {
                    if (!processedButtons.Add(btnNode.targetButton)) continue;
                    
                    var allComponents = btnNode.targetButton.GetComponents<UIGraphBakedEvent>();
                    foreach (var comp in allComponents)
                    {
                        if (comp != null)
                        {
                            UnityEngine.Object.DestroyImmediate(comp);
                            totalRemoved++;
                        }
                    }
                }
                else if (node is KeyboardInputNode keyNode && keyNode.targetGameObject != null)
                {
                    if (!processedKeyboardObjects.Add(keyNode.targetGameObject)) continue;
                    
                    var allComponents = keyNode.targetGameObject.GetComponents<UIGraphBakedKeyboardEvent>();
                    foreach (var comp in allComponents)
                    {
                        if (comp != null)
                        {
                            UnityEngine.Object.DestroyImmediate(comp);
                            totalRemoved++;
                        }
                    }
                }
            }
            
            $"[Cleanup] 제거된 컴포넌트: {totalRemoved}개".DLog();
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
            var processedButtons = new HashSet<GameObject>();
            var processedKeyboardObjects = new HashSet<GameObject>();
            int buttonCount = 0;
            int keyboardCount = 0;
            
            foreach (var node in nodes)
            {
                if (node is ButtonClickNode btnNode && btnNode.targetButton != null)
                {
                    if (!processedButtons.Add(btnNode.targetButton)) continue;
                    
                    var button = btnNode.targetButton.GetComponent<UnityEngine.UI.Button>();
                    if (button == null) continue;
                    
                    var bakedEvent = btnNode.targetButton.AddComponent<UIGraphBakedEvent>();
                    bakedEvent.SetGraph(this);
                    bakedEvent.SetStartNodeGuid(btnNode.guid);
                    
                    EditorUtility.SetDirty(bakedEvent);
                    EditorUtility.SetDirty(button);
                    EditorUtility.SetDirty(btnNode.targetButton);
                    buttonCount++;
                }
                else if (node is KeyboardInputNode keyNode && keyNode.targetGameObject != null && keyNode.targetKeyCode != KeyCode.None)
                {
                    if (!processedKeyboardObjects.Add(keyNode.targetGameObject)) continue;
                    
                    var bakedKeyboardEvent = keyNode.targetGameObject.AddComponent<UIGraphBakedKeyboardEvent>();
                    bakedKeyboardEvent.SetGraph(this);
                    bakedKeyboardEvent.SetStartNodeGuid(keyNode.guid);
                    bakedKeyboardEvent.SetKeyCode(keyNode.targetKeyCode);
                    
                    EditorUtility.SetDirty(bakedKeyboardEvent);
                    EditorUtility.SetDirty(keyNode.targetGameObject);
                    keyboardCount++;
                }
            }
            
            $"[Bake] 추가된 컴포넌트 - Button: {buttonCount}개, Keyboard: {keyboardCount}개".DLog();
#endif
        }
        
        private void PreserveNodeReferences(UINode node)
        {
#if UNITY_EDITOR
            if (node is ButtonClickNode btnNode && btnNode.targetButton != null)
                EditorUtility.SetDirty(btnNode.targetButton);
            else if (node is KeyboardInputNode keyNode && keyNode.targetGameObject != null)
                EditorUtility.SetDirty(keyNode.targetGameObject);
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
            else if (node is ExecuteMethodNode execNode && execNode.targetObject != null)
                EditorUtility.SetDirty(execNode.targetObject);
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
