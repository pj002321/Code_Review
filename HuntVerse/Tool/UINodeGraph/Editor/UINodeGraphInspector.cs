using UnityEngine;
using UnityEditor;
using Hunt;

namespace Hunt
{
    [CustomEditor(typeof(UINodeGraph))]
    public class UINodeGraphInspector : Editor
    {
        private UINode selectedNode;
        
        public override void OnInspectorGUI()
        {
            var graph = (UINodeGraph)target;
            
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("노드 편집", EditorStyles.boldLabel);
            
            if (graph.nodes == null || graph.nodes.Count == 0)
            {
                EditorGUILayout.HelpBox("노드가 없습니다.", MessageType.Info);
                return;
            }
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                var node = graph.nodes[i];
                if (node == null) continue;
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                bool isSelected = selectedNode == node;
                bool newSelected = EditorGUILayout.Foldout(isSelected, $"{i}: {node.nodeName} ({node.GetNodeType()})");
                
                if (newSelected != isSelected)
                {
                    selectedNode = newSelected ? node : null;
                }
                
                if (isSelected)
                {
                    EditorGUI.indentLevel++;
                    DrawNodeEditor(node);
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndVertical();
            
            if (GUI.changed)
            {
                EditorUtility.SetDirty(graph);
            }
        }
        
        private void DrawNodeEditor(UINode node)
        {
            if (node == null) return;
            
            EditorGUILayout.LabelField("GUID", node.guid);
            EditorGUILayout.LabelField("Position", node.position.ToString());
            
            switch (node.GetNodeType())
            {
                case UINodeType.ButtonClick:
                    var btnNode = node as ButtonClickNode;
                    if (btnNode != null)
                    {
                        btnNode.targetButton = EditorGUILayout.ObjectField(
                            "Target Button",
                            btnNode.targetButton,
                            typeof(GameObject),
                            true
                        ) as GameObject;
                    }
                    break;
                    
                case UINodeType.KeyboardInput:
                    var keyNode = node as KeyboardInputNode;
                    if (keyNode != null)
                    {
                        keyNode.targetGameObject = EditorGUILayout.ObjectField(
                            "Target GameObject",
                            keyNode.targetGameObject,
                            typeof(GameObject),
                            true
                        ) as GameObject;
                        keyNode.targetKeyCode = (KeyCode)EditorGUILayout.EnumPopup("Key Code", keyNode.targetKeyCode);
                    }
                    break;
                    
                case UINodeType.HideLayer:
                case UINodeType.ShowLayer:
                case UINodeType.ToggleLayer:
                    DrawLayerArrayField(node);
                    break;
                    
                case UINodeType.HideGameObject:
                case UINodeType.ShowGameObject:
                case UINodeType.ToggleGameObject:
                    DrawGameObjectArrayField(node);
                    break;
                    
                case UINodeType.Delay:
                    var delayNode = node as DelayNode;
                    if (delayNode != null)
                    {
                        delayNode.delaySeconds = EditorGUILayout.FloatField("Delay (sec)", delayNode.delaySeconds);
                    }
                    break;
            }
        }
        
        private void DrawLayerArrayField(UINode node)
        {
            UILayer[] layers = null;
            
            if (node is HideLayerNode hideNode)
            {
                layers = hideNode.targetLayers;
            }
            else if (node is ShowLayerNode showNode)
            {
                layers = showNode.targetLayers;
            }
            else if (node is ToggleLayerNode toggleNode)
            {
                layers = toggleNode.targetLayers;
            }
            
            if (layers == null) return;
            
            int size = layers.Length;
            int newSize = EditorGUILayout.IntField("Size", size);
            
            if (newSize != size)
            {
                System.Array.Resize(ref layers, newSize);
                
                if (node is HideLayerNode h)
                    h.targetLayers = layers;
                else if (node is ShowLayerNode s)
                    s.targetLayers = layers;
                else if (node is ToggleLayerNode t)
                    t.targetLayers = layers;
            }
            
            for (int i = 0; i < layers.Length; i++)
            {
                layers[i] = (UILayer)EditorGUILayout.EnumPopup($"Layer {i}", layers[i]);
            }
        }
        
        private void DrawGameObjectArrayField(UINode node)
        {
            GameObject[] gameObjects = null;
            
            if (node is HideGameObjectNode hideNode)
            {
                gameObjects = hideNode.targetGameObjects;
            }
            else if (node is ShowGameObjectNode showNode)
            {
                gameObjects = showNode.targetGameObjects;
            }
            else if (node is ToggleGameObjectNode toggleNode)
            {
                gameObjects = toggleNode.targetGameObjects;
            }
            
            if (gameObjects == null) return;
            
            int size = gameObjects.Length;
            int newSize = EditorGUILayout.IntField("Size", size);
            
            if (newSize != size)
            {
                System.Array.Resize(ref gameObjects, newSize);
                
                if (node is HideGameObjectNode h)
                    h.targetGameObjects = gameObjects;
                else if (node is ShowGameObjectNode s)
                    s.targetGameObjects = gameObjects;
                else if (node is ToggleGameObjectNode t)
                    t.targetGameObjects = gameObjects;
            }
            
            for (int i = 0; i < gameObjects.Length; i++)
            {
                gameObjects[i] = EditorGUILayout.ObjectField(
                    $"GameObject {i}",
                    gameObjects[i],
                    typeof(GameObject),
                    true
                ) as GameObject;
            }
        }
    }
}

