using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;
using Hunt;

namespace Hunt
{
    public class UINodeGraphEditor : EditorWindow
    {
        private UINodeGraph currentGraph;
        private Vector2 panOffset;
        private UINode selectedNode;
        private bool isConnecting;
        private string connectingFromNodeGuid;
        
        private const float NODE_WIDTH = 250f, NODE_HEIGHT = 150f, GRID_SIZE = 20f;
        
        [MenuItem("Tools/Hunt/UI Node Graph Editor")]
        public static void OpenWindow() => GetWindow<UINodeGraphEditor>("UI Node Graph").Show();
        
        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
        }
        
        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
            SaveCurrentGraph();
        }
        
        private void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode) => TryRestoreDelayed();
        private void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene oldScene, UnityEngine.SceneManagement.Scene newScene) => TryRestoreDelayed();
        
        private void TryRestoreDelayed()
        {
            if (currentGraph != null)
                for (int i = 0; i < 3; i++)
                    EditorApplication.delayCall += () => { if (RestoreNodeReferences()) { EditorUtility.SetDirty(currentGraph); Repaint(); } };
        }
        
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode && currentGraph != null)
            {
                for (int i = 0; i < 5; i++)
                    EditorApplication.delayCall += () => { if (RestoreNodeReferences()) { EditorUtility.SetDirty(currentGraph); AssetDatabase.SaveAssets(); Repaint(); } };
            }
        }
        
        private bool RestoreNodeReferences()
        {
            var bakedData = currentGraph?.GetBakedData();
            if (currentGraph?.nodes == null || bakedData?.executionSteps == null) return false;
            
            var targetMap = new Dictionary<string, GameObject>();
            foreach (var target in Resources.FindObjectsOfTypeAll<UIGraphTarget>())
                if (!string.IsNullOrEmpty(target.TargetId) && target.gameObject.scene.isLoaded)
                    targetMap[target.TargetId] = target.gameObject;
            
            bool restored = false;
            foreach (var step in bakedData.executionSteps)
            {
                if (step.gameObjectIds == null || step.gameObjectIds.Length == 0) continue;
                
                var node = currentGraph.nodes.Find(n => n.guid == step.nodeGuid);
                if (node == null) continue;
                
                if (node is ButtonClickNode btnNode && (btnNode.targetButton == null || !btnNode.targetButton) && step.gameObjectIds.Length > 0 &&
                    targetMap.TryGetValue(step.gameObjectIds[0], out var btnObj))
                {
                    btnNode.targetButton = btnObj;
                    restored = true;
                }
                else if (node is HideGameObjectNode hideNode && step.gameObjectIds.Length > 0)
                {
                    var restoredArray = RestoreGameObjectArray(step.gameObjectIds, targetMap);
                    if (restoredArray != null && (hideNode.targetGameObjects == null || hideNode.targetGameObjects.Length != restoredArray.Length || 
                        Array.Exists(hideNode.targetGameObjects, go => go == null || !go)))
                    {
                        hideNode.targetGameObjects = restoredArray;
                        restored = true;
                    }
                }
                else if (node is ShowGameObjectNode showNode && step.gameObjectIds.Length > 0)
                {
                    var restoredArray = RestoreGameObjectArray(step.gameObjectIds, targetMap);
                    if (restoredArray != null && (showNode.targetGameObjects == null || showNode.targetGameObjects.Length != restoredArray.Length || 
                        Array.Exists(showNode.targetGameObjects, go => go == null || !go)))
                    {
                        showNode.targetGameObjects = restoredArray;
                        restored = true;
                    }
                }
                else if (node is ToggleGameObjectNode toggleNode && step.gameObjectIds.Length > 0)
                {
                    var restoredArray = RestoreGameObjectArray(step.gameObjectIds, targetMap);
                    if (restoredArray != null && (toggleNode.targetGameObjects == null || toggleNode.targetGameObjects.Length != restoredArray.Length || 
                        Array.Exists(toggleNode.targetGameObjects, go => go == null || !go)))
                    {
                        toggleNode.targetGameObjects = restoredArray;
                        restored = true;
                    }
                }
                else if (node is ExecuteMethodNode execNode && (execNode.targetObject == null || !execNode.targetObject) && step.gameObjectIds.Length > 0 &&
                    targetMap.TryGetValue(step.gameObjectIds[0], out var execObj))
                {
                    execNode.targetObject = execObj;
                    restored = true;
                }
            }
            
            return restored;
        }
        
        private GameObject[] RestoreGameObjectArray(string[] ids, Dictionary<string, GameObject> targetMap)
        {
            if (ids == null || ids.Length == 0) return null;
            
            var result = new GameObject[ids.Length];
            for (int i = 0; i < ids.Length; i++)
                if (!string.IsNullOrEmpty(ids[i]) && targetMap.TryGetValue(ids[i], out var obj))
                    result[i] = obj;
            
            return result;
        }
        
        private void SaveCurrentGraph()
        {
            if (currentGraph != null) { EditorUtility.SetDirty(currentGraph); AssetDatabase.SaveAssets(); }
        }
        
        private void OnGUI()
        {
            DrawToolbar();
            
            if (currentGraph == null)
            {
                EditorGUILayout.HelpBox("노드 그래프를 선택하거나 생성하세요.", MessageType.Info);
                return;
            }
            
            if (currentGraph.nodes == null || currentGraph.nodes.Count == 0)
                EditorGUILayout.HelpBox("노드가 없습니다. Add Node 버튼을 눌러 노드를 추가하세요.", MessageType.Warning);
            else
                EditorGUILayout.HelpBox($"노드 개수: {currentGraph.nodes.Count}개", MessageType.Info);
            
            var bakedData = currentGraph.GetBakedData();
            if (bakedData?.executionSteps?.Count > 0)
                EditorGUILayout.HelpBox($"Bake 완료: {bakedData.executionSteps.Count}개의 실행 단계", MessageType.Info);
            else if (currentGraph.nodes?.Count > 0)
                EditorGUILayout.HelpBox("Bake되지 않았습니다. Bake 버튼을 눌러주세요.", MessageType.Warning);
            
            HandleEvents();
            DrawGrid();
            DrawConnections();
            DrawNodes();
            
            if (GUI.changed) EditorUtility.SetDirty(currentGraph);
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            currentGraph = EditorGUILayout.ObjectField(currentGraph, typeof(UINodeGraph), false, GUILayout.Width(200)) as UINodeGraph;
            if (GUILayout.Button("New Graph", EditorStyles.toolbarButton)) CreateNewGraph();
            if (GUILayout.Button("Add Node", EditorStyles.toolbarButton)) ShowNodeMenu();
            if (GUILayout.Button("Bake", EditorStyles.toolbarButton))
            {
                if (currentGraph?.nodes?.Count == 0)
                {
                    EditorUtility.DisplayDialog("Bake 실패", "노드가 없습니다. 노드를 추가한 후 Bake해주세요.", "OK");
                    return;
                }
                if (currentGraph != null)
                {
                    currentGraph.Bake();
                    EditorUtility.SetDirty(currentGraph);
                    AssetDatabase.SaveAssets();
                    Repaint();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void CreateNewGraph()
        {
            string path = null;
            try { path = EditorUtility.SaveFilePanelInProject("새 노드 그래프 생성", "NewUIGraph", "asset", "노드 그래프를 저장할 위치를 선택하세요."); }
            catch { path = "Assets/NewUIGraph.asset"; }
            
            if (!string.IsNullOrEmpty(path))
            {
                var graph = CreateInstance<UINodeGraph>();
                AssetDatabase.CreateAsset(graph, path);
                AssetDatabase.SaveAssets();
                currentGraph = graph;
            }
        }
        
        private void DrawGrid()
        {
            int widthDivs = Mathf.CeilToInt(position.width / GRID_SIZE);
            int heightDivs = Mathf.CeilToInt(position.height / GRID_SIZE);
            Handles.BeginGUI();
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
            Vector3 offset = new Vector3(panOffset.x % GRID_SIZE, panOffset.y % GRID_SIZE, 0);
            
            for (int i = 0; i < widthDivs; i++)
                Handles.DrawLine(new Vector3(GRID_SIZE * i, -GRID_SIZE, 0) + offset, new Vector3(GRID_SIZE * i, position.height, 0) + offset);
            for (int i = 0; i < heightDivs; i++)
                Handles.DrawLine(new Vector3(-GRID_SIZE, GRID_SIZE * i, 0) + offset, new Vector3(position.width, GRID_SIZE * i, 0) + offset);
            
            Handles.color = Color.white;
            Handles.EndGUI();
        }
        
        private void DrawNodes()
        {
            if (currentGraph?.nodes == null) return;
            BeginWindows();
            
            for (int i = 0; i < currentGraph.nodes.Count; i++)
            {
                var node = currentGraph.nodes[i];
                if (node == null) continue;
                
                Rect nodeRect = new Rect(node.position.x + panOffset.x, node.position.y + panOffset.y, NODE_WIDTH, NODE_HEIGHT);
                nodeRect = GUI.Window(i, nodeRect, (id) => DrawNodeWindow(id, node), node.nodeName);
                
                var newPosition = new Vector2(nodeRect.x - panOffset.x, nodeRect.y - panOffset.y);
                if (node.position != newPosition) { node.position = newPosition; EditorUtility.SetDirty(currentGraph); }
            }
            EndWindows();
        }
        
        private void DrawNodeWindow(int id, UINode node)
        {
            if (node == null) return;
            GUILayout.BeginVertical();
            DrawNodeContent(node);
            GUILayout.Space(5);
            
            if (GUILayout.Button("Connect")) { isConnecting = true; connectingFromNodeGuid = node.guid; Repaint(); }
            if (GUILayout.Button("Edit")) { Selection.activeObject = currentGraph; selectedNode = node; EditorGUIUtility.PingObject(currentGraph); Repaint(); }
            if (GUILayout.Button("Delete")) { DeleteNode(node); Repaint(); }
            
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
        
        private void DrawNodeContent(UINode node)
        {
            if (node == null) return;
            EditorGUI.BeginChangeCheck();
            
            switch (node.GetNodeType())
            {
                case UINodeType.ButtonClick: DrawButtonClickNode(node as ButtonClickNode); break;
                case UINodeType.KeyboardInput: DrawKeyboardInputNode(node as KeyboardInputNode); break;
                case UINodeType.HideLayer:
                case UINodeType.ShowLayer:
                case UINodeType.ToggleLayer: DrawLayerArrayInNode(node); break;
                case UINodeType.HideGameObject:
                case UINodeType.ShowGameObject:
                case UINodeType.ToggleGameObject: DrawGameObjectArrayInNode(node); break;
                case UINodeType.Delay: DrawDelayNode(node as DelayNode); break;
                case UINodeType.ExecuteMethod: DrawExecuteMethodNode(node as ExecuteMethodNode); break;
            }
            
            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(currentGraph);
        }
        
        private void DrawButtonClickNode(ButtonClickNode node)
        {
            if (node == null) return;
            GUILayout.Label("Button Click", EditorStyles.boldLabel);
            var rect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
            node.targetButton = EditorGUI.ObjectField(rect, "Button", node.targetButton, typeof(GameObject), true) as GameObject;
        }
        
        private void DrawKeyboardInputNode(KeyboardInputNode node)
        {
            if (node == null) return;
            GUILayout.Label("Keyboard Input", EditorStyles.boldLabel);
            var objRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
            node.targetGameObject = EditorGUI.ObjectField(objRect, "Target GameObject", node.targetGameObject, typeof(GameObject), true) as GameObject;
            var keyRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
            node.targetKeyCode = (KeyCode)EditorGUI.EnumPopup(keyRect, "Key Code", node.targetKeyCode);
        }
        
        private void DrawDelayNode(DelayNode node)
        {
            if (node == null) return;
            GUILayout.Label("Delay", EditorStyles.boldLabel);
            var rect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
            node.delaySeconds = EditorGUI.FloatField(rect, "Seconds", node.delaySeconds);
        }
        
        private void DrawExecuteMethodNode(ExecuteMethodNode node)
        {
            if (node == null) return;
            GUILayout.Label("Execute Method", EditorStyles.boldLabel);
            
            var objRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
            var newObj = EditorGUI.ObjectField(objRect, "Target Object", node.targetObject, typeof(GameObject), true) as GameObject;
            if (newObj != node.targetObject)
            {
                node.targetObject = newObj;
                node.componentTypeName = "";
                node.methodName = "";
                EditorUtility.SetDirty(currentGraph);
            }
            
            if (node.targetObject != null)
            {
                var components = node.targetObject.GetComponents<Component>();
                var componentNames = new List<string> { "None" };
                var componentTypes = new List<System.Type> { null };
                
                foreach (var comp in components)
                {
                    if (comp == null) continue;
                    var type = comp.GetType();
                    componentNames.Add($"{type.Name} ({type.Namespace})");
                    componentTypes.Add(type);
                }
                
                int currentIndex = 0;
                if (!string.IsNullOrEmpty(node.componentTypeName) && componentTypes.Count > 1)
                {
                    var targetType = System.Type.GetType(node.componentTypeName);
                    if (targetType == null) targetType = System.Reflection.Assembly.GetExecutingAssembly().GetType(node.componentTypeName);
                    if (targetType == null)
                    {
                        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                        {
                            targetType = asm.GetType(node.componentTypeName);
                            if (targetType != null) break;
                        }
                    }
                    if (targetType != null)
                    {
                        var foundIndex = componentTypes.FindIndex(t => t == targetType);
                        if (foundIndex >= 0 && foundIndex < componentTypes.Count)
                            currentIndex = foundIndex;
                    }
                }
                
                if (currentIndex < 0 || currentIndex >= componentNames.Count)
                    currentIndex = 0;
                
                var compRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
                int newIndex = EditorGUI.Popup(compRect, "Component", currentIndex, componentNames.ToArray());
                
                if (newIndex >= 0 && newIndex < componentNames.Count && newIndex < componentTypes.Count)
                {
                    if (newIndex != currentIndex)
                    {
                        if (newIndex > 0 && newIndex < componentTypes.Count)
                        {
                            node.componentTypeName = componentTypes[newIndex]?.FullName ?? "";
                            node.methodName = "";
                            EditorUtility.SetDirty(currentGraph);
                        }
                        else if (newIndex == 0)
                        {
                            node.componentTypeName = "";
                            node.methodName = "";
                            EditorUtility.SetDirty(currentGraph);
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(node.componentTypeName) && newIndex > 0 && newIndex < componentTypes.Count)
                    {
                        var selectedType = componentTypes[newIndex];
                        if (selectedType != null)
                        {
                            var methods = selectedType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                                .Where(m => m.GetParameters().Length == 0 && !m.IsSpecialName && m.ReturnType == typeof(void))
                                .ToList();
                            
                            var methodNames = new List<string> { "None" };
                            methods.ForEach(m => methodNames.Add(m.Name));
                            
                            int methodIndex = 0;
                            if (!string.IsNullOrEmpty(node.methodName) && methods.Count > 0)
                            {
                                var foundMethodIndex = methods.FindIndex(m => m.Name == node.methodName);
                                if (foundMethodIndex >= 0 && foundMethodIndex < methods.Count)
                                    methodIndex = foundMethodIndex + 1;
                            }
                            
                            if (methodIndex < 0 || methodIndex >= methodNames.Count)
                                methodIndex = 0;
                            
                            var methodRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
                            int newMethodIndex = EditorGUI.Popup(methodRect, "Method", methodIndex, methodNames.ToArray());
                            
                            if (newMethodIndex >= 0 && newMethodIndex < methodNames.Count)
                            {
                                if (newMethodIndex != methodIndex)
                                {
                                    if (newMethodIndex > 0 && newMethodIndex <= methods.Count)
                                        node.methodName = methods[newMethodIndex - 1].Name;
                                    else
                                        node.methodName = "";
                                    EditorUtility.SetDirty(currentGraph);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private void DrawLayerArrayInNode(UINode node)
        {
            var (layers, title, setter) = GetLayerArrayData(node);
            if (layers == null) { layers = new UILayer[0]; setter(layers); EditorUtility.SetDirty(currentGraph); }
            
            GUILayout.Label(title, EditorStyles.boldLabel);
            DrawArraySizeField(layers.Length, newSize => { System.Array.Resize(ref layers, newSize); setter(layers); EditorUtility.SetDirty(currentGraph); });
            
            layers = GetLayerArrayData(node).layers;
            for (int i = 0; i < layers.Length; i++)
            {
                var rect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
                var newValue = (UILayer)EditorGUI.EnumPopup(rect, $"Layer {i}", layers[i]);
                if (newValue != layers[i]) { layers[i] = newValue; setter(layers); EditorUtility.SetDirty(currentGraph); }
            }
        }
        
        private (UILayer[] layers, string title, Action<UILayer[]> setter) GetLayerArrayData(UINode node)
        {
            if (node is HideLayerNode h) return (h.targetLayers, "Hide Layer", v => h.targetLayers = v);
            if (node is ShowLayerNode s) return (s.targetLayers, "Show Layer", v => s.targetLayers = v);
            if (node is ToggleLayerNode t) return (t.targetLayers, "Toggle Layer", v => t.targetLayers = v);
            return (null, "", null);
        }
        
        private void DrawGameObjectArrayInNode(UINode node)
        {
            var (gameObjects, title, setter) = GetGameObjectArrayData(node);
            if (gameObjects == null) return;
            
            GUILayout.Label(title, EditorStyles.boldLabel);
            DrawArraySizeField(gameObjects.Length, newSize => { System.Array.Resize(ref gameObjects, newSize); setter(gameObjects); EditorUtility.SetDirty(currentGraph); });
            
            for (int i = 0; i < gameObjects.Length; i++)
            {
                var rect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
                var newValue = EditorGUI.ObjectField(rect, $"Object {i}", gameObjects[i], typeof(GameObject), true) as GameObject;
                if (newValue != gameObjects[i]) { gameObjects[i] = newValue; setter(gameObjects); EditorUtility.SetDirty(currentGraph); }
            }
        }
        
        private (GameObject[] gameObjects, string title, Action<GameObject[]> setter) GetGameObjectArrayData(UINode node)
        {
            if (node is HideGameObjectNode h) return (h.targetGameObjects, "Hide GameObject", v => h.targetGameObjects = v);
            if (node is ShowGameObjectNode s) return (s.targetGameObjects, "Show GameObject", v => s.targetGameObjects = v);
            if (node is ToggleGameObjectNode t) return (t.targetGameObjects, "Toggle GameObject", v => t.targetGameObjects = v);
            return (null, "", null);
        }
        
        private void DrawArraySizeField(int currentSize, Action<int> onSizeChanged)
        {
            var sizeRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
            int newSize = EditorGUI.IntField(sizeRect, "Size", currentSize);
            if (newSize != currentSize) onSizeChanged(newSize);
        }
        
        private void DrawConnections()
        {
            if (currentGraph?.connections == null) return;
            Handles.BeginGUI();
            
            foreach (var connection in currentGraph.connections)
                DrawConnection(connection);
            
            if (isConnecting)
            {
                var fromNode = currentGraph.nodes.Find(n => n.guid == connectingFromNodeGuid);
                if (fromNode != null)
                {
                    Vector2 fromPos = new Vector2(fromNode.position.x + panOffset.x + NODE_WIDTH, fromNode.position.y + panOffset.y + NODE_HEIGHT / 2);
                    DrawConnectionLine(fromPos, Event.current.mousePosition, Color.yellow);
                }
            }
            Handles.EndGUI();
        }
        
        private void DrawConnection(UINodeConnection connection)
        {
            var fromNode = currentGraph.nodes.Find(n => n.guid == connection.fromNodeGuid);
            var toNode = currentGraph.nodes.Find(n => n.guid == connection.toNodeGuid);
            if (fromNode == null || toNode == null) return;
            
            Vector2 fromPos = new Vector2(fromNode.position.x + panOffset.x + NODE_WIDTH, fromNode.position.y + panOffset.y + NODE_HEIGHT / 2);
            Vector2 toPos = new Vector2(toNode.position.x + panOffset.x, toNode.position.y + panOffset.y + NODE_HEIGHT / 2);
            DrawConnectionLine(fromPos, toPos, Color.white);
        }
        
        private void DrawConnectionLine(Vector2 from, Vector2 to, Color color)
        {
            Handles.color = color;
            Handles.DrawLine(new Vector3(from.x, from.y, 0), new Vector3(to.x, to.y, 0));
            Handles.color = Color.white;
        }
        
        private void HandleEvents()
        {
            Event e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 1) ShowContextMenu(e.mousePosition);
                    else if (e.button == 0 && isConnecting) CompleteConnection(e.mousePosition);
                    break;
                case EventType.MouseDrag:
                    if (e.button == 2) { panOffset += e.delta; Repaint(); }
                    break;
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    HandleDragAndDrop(e);
                    break;
            }
        }
        
        private void HandleDragAndDrop(Event e)
        {
            if (DragAndDrop.objectReferences.Length > 0 && DragAndDrop.objectReferences[0] is GameObject gameObject)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (e.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    CreateNodeFromGameObject(gameObject, e.mousePosition - panOffset);
                }
            }
        }
        
        private void CreateNodeFromGameObject(GameObject obj, Vector2 position)
        {
            if (currentGraph == null || obj.GetComponent<UIButtonControlBase>() == null) return;
            if (currentGraph.nodes == null) currentGraph.nodes = new List<UINode>();
            
            var node = new ButtonClickNode { guid = Guid.NewGuid().ToString(), position = position, nodeName = obj.name, targetButton = obj };
            currentGraph.nodes.Add(node);
            EditorUtility.SetDirty(currentGraph);
            AssetDatabase.SaveAssets();
            Repaint();
        }
        
        private void ShowContextMenu(Vector2 mousePosition)
        {
            var menu = new GenericMenu();
            var nodeTypes = new[] { UINodeType.ButtonClick, UINodeType.KeyboardInput, UINodeType.HideLayer, UINodeType.ShowLayer, UINodeType.ToggleLayer, 
                UINodeType.HideGameObject, UINodeType.ShowGameObject, UINodeType.ToggleGameObject, UINodeType.Delay, UINodeType.ExecuteMethod };
            var nodeNames = new[] { "Button Click", "Keyboard Input", "Hide Layer", "Show Layer", "Toggle Layer", 
                "Hide GameObject", "Show GameObject", "Toggle GameObject", "Delay", "Execute Method" };
            
            for (int i = 0; i < nodeTypes.Length; i++)
            {
                int index = i;
                menu.AddItem(new GUIContent($"Add {nodeNames[index]} Node"), false, () => CreateNode(nodeTypes[index], mousePosition - panOffset));
            }
            
            menu.ShowAsContext();
        }
        
        private void ShowNodeMenu() => ShowContextMenu(new Vector2(position.width / 2, position.height / 2));
        
        private void CreateNode(UINodeType type, Vector2 position)
        {
            if (currentGraph == null) return;
            
            UINode node = type switch
            {
                UINodeType.ButtonClick => new ButtonClickNode(),
                UINodeType.KeyboardInput => new KeyboardInputNode { targetKeyCode = KeyCode.None },
                UINodeType.HideLayer => new HideLayerNode { targetLayers = new UILayer[0] },
                UINodeType.ShowLayer => new ShowLayerNode { targetLayers = new UILayer[0] },
                UINodeType.ToggleLayer => new ToggleLayerNode { targetLayers = new UILayer[0] },
                UINodeType.HideGameObject => new HideGameObjectNode { targetGameObjects = new GameObject[0] },
                UINodeType.ShowGameObject => new ShowGameObjectNode { targetGameObjects = new GameObject[0] },
                UINodeType.ToggleGameObject => new ToggleGameObjectNode { targetGameObjects = new GameObject[0] },
                UINodeType.Delay => new DelayNode { delaySeconds = 1f },
                UINodeType.ExecuteMethod => new ExecuteMethodNode(),
                _ => null
            };
            
            if (node != null)
            {
                node.guid = Guid.NewGuid().ToString();
                node.position = position;
                if (string.IsNullOrEmpty(node.nodeName)) node.nodeName = type.ToString();
                
                if (currentGraph.nodes == null) currentGraph.nodes = new List<UINode>();
                currentGraph.nodes.Add(node);
                EditorUtility.SetDirty(currentGraph);
                AssetDatabase.SaveAssets();
                Repaint();
            }
        }
        
        private void DeleteNode(UINode node)
        {
            if (currentGraph == null || node == null) return;
            currentGraph.nodes.Remove(node);
            if (currentGraph.connections != null)
                currentGraph.connections.RemoveAll(c => c.fromNodeGuid == node.guid || c.toNodeGuid == node.guid);
            EditorUtility.SetDirty(currentGraph);
        }
        
        private void CompleteConnection(Vector2 mousePosition)
        {
            if (!isConnecting) return;
            
            var toNode = currentGraph.nodes.FirstOrDefault(n =>
            {
                Rect nodeRect = new Rect(n.position.x + panOffset.x, n.position.y + panOffset.y, NODE_WIDTH, NODE_HEIGHT);
                return nodeRect.Contains(mousePosition);
            });
            
            if (toNode != null && toNode.guid != connectingFromNodeGuid)
            {
                if (currentGraph.connections == null) currentGraph.connections = new List<UINodeConnection>();
                currentGraph.connections.Add(new UINodeConnection
                {
                    fromNodeGuid = connectingFromNodeGuid,
                    toNodeGuid = toNode.guid,
                    fromPortIndex = 0,
                    toPortIndex = 0
                });
                EditorUtility.SetDirty(currentGraph);
            }
            
            isConnecting = false;
            connectingFromNodeGuid = null;
        }
    }
}
