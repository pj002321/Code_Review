using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace Kirist.EditorTool
{
    public abstract class BaseFinderBehaviour
    {
        protected KiristWindow parentWindow;
        
        protected static class UIColors
        {
            public static readonly Color Primary = new Color(0.2f, 0.6f, 1f, 1f);
            public static readonly Color Success = new Color(0.2f, 0.8f, 0.3f, 1f);
            public static readonly Color Warning = new Color(1f, 0.6f, 0.2f, 1f);
            public static readonly Color Danger = new Color(0.9f, 0.3f, 0.3f, 1f);
            public static readonly Color Info = new Color(0.4f, 0.7f, 0.9f, 1f);
            public static readonly Color Dark = new Color(0.8f, 0.8f, 1f, 1f);
            public static readonly Color Light = new Color(0.9f, 0.9f, 0.9f, 1f);
            public static readonly Color Accent = new Color(0.8f, 0.4f, 0.9f, 1f);
            
            public static readonly Color ModernBlue = new Color(0.6f, 0.9f, 1f, 1f);
            public static readonly Color ModernGreen = new Color(0.7f, 1f, 0.8f, 1f);
            public static readonly Color ModernPurple = new Color(1f, 0.7f, 1f, 1f);
            public static readonly Color ModernOrange = new Color(1f, 0.8f, 0.5f, 1f);
            public static readonly Color ModernTeal = new Color(0.6f, 1f, 1f, 1f);
            public static readonly Color ModernPink = new Color(1f, 0.7f, 0.9f, 1f);
            
            public static readonly Color DarkBackground = new Color(0.10f, 0.20f, 0.15f, 1f);
            public static readonly Color DarkCard = new Color(0.15f, 0.25f, 0.20f, 1f);
            public static readonly Color DarkSection = new Color(0.20f, 0.30f, 0.25f, 1f);
            public static readonly Color DarkAccent = new Color(0.25f, 0.35f, 0.30f, 1f);
        }
        
        protected static class UIStyles
        {
            private static GUIStyle _cardStyle;
            private static GUIStyle _headerStyle;
            private static GUIStyle _buttonStyle;
            private static GUIStyle _largeButtonStyle;
            private static GUIStyle _helpStyle;
            private static GUIStyle _titleStyle;
            private static GUIStyle _subtitleStyle;
            private static GUIStyle _compactButtonStyle;
            private static GUIStyle _searchButtonStyle;
            private static GUIStyle _backgroundStyle;
            private static GUIStyle _sectionBackgroundStyle;
            private static GUIStyle _gradientBackgroundStyle;
            private static GUIStyle _windowBackgroundStyle;
            
            public static GUIStyle CardStyle
            {
                get
                {
                    if (_cardStyle == null)
                    {
                        _cardStyle = new GUIStyle(GUI.skin.box)
                        {
                            padding = new RectOffset(8, 8, 8, 8),
                            margin = new RectOffset(2, 2, 2, 2),
                            border = new RectOffset(1, 1, 1, 1)
                        };
                    }
                    return _cardStyle;
                }
            }
            
            public static GUIStyle HeaderStyle
            {
                get
                {
                    if (_headerStyle == null)
                    {
                        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                        {
                            fontSize = 13,
                            fontStyle = FontStyle.Bold,
                            alignment = TextAnchor.MiddleLeft,
                            normal = { textColor = UIColors.Primary }
                        };
                    }
                    return _headerStyle;
                }
            }
            
            public static GUIStyle ButtonStyle
            {
                get
                {
                    if (_buttonStyle == null)
                    {
                        _buttonStyle = new GUIStyle(GUI.skin.button)
                        {
                            fontSize = 11,
                            fontStyle = FontStyle.Normal,
                            padding = new RectOffset(6, 6, 3, 3),
                            border = new RectOffset(1, 1, 1, 1)
                        };
                    }
                    return _buttonStyle;
                }
            }
            
            public static GUIStyle CompactButtonStyle
            {
                get
                {
                    if (_compactButtonStyle == null)
                    {
                        _compactButtonStyle = new GUIStyle(GUI.skin.button)
                        {
                            fontSize = 10,
                            fontStyle = FontStyle.Normal,
                            padding = new RectOffset(4, 4, 2, 2),
                            border = new RectOffset(1, 1, 1, 1)
                        };
                    }
                    return _compactButtonStyle;
                }
            }
            
            public static GUIStyle SearchButtonStyle
            {
                get
                {
                    if (_searchButtonStyle == null)
                    {
                        _searchButtonStyle = new GUIStyle(GUI.skin.button)
                        {
                            fontSize = 12,
                            fontStyle = FontStyle.Bold,
                            padding = new RectOffset(12, 12, 6, 6),
                            border = new RectOffset(2, 2, 2, 2)
                        };
                    }
                    return _searchButtonStyle;
                }
            }
            
            public static GUIStyle LargeButtonStyle
            {
                get
                {
                    if (_largeButtonStyle == null)
                    {
                        _largeButtonStyle = new GUIStyle(GUI.skin.button)
                        {
                            fontSize = 12,
                            fontStyle = FontStyle.Bold,
                            padding = new RectOffset(10, 10, 5, 5),
                            border = new RectOffset(2, 2, 2, 2)
                        };
                    }
                    return _largeButtonStyle;
                }
            }
            
            public static GUIStyle HelpStyle
            {
                get
                {
                    if (_helpStyle == null)
                    {
                        _helpStyle = new GUIStyle(EditorStyles.helpBox)
                        {
                            fontSize = 10,
                            wordWrap = true,
                            padding = new RectOffset(6, 6, 6, 6),
                            normal = { textColor = UIColors.Info }
                        };
                    }
                    return _helpStyle;
                }
            }
            
            public static GUIStyle TitleStyle
            {
                get
                {
                    if (_titleStyle == null)
                    {
                        _titleStyle = new GUIStyle(EditorStyles.boldLabel)
                        {
                            fontSize = 16,
                            fontStyle = FontStyle.Bold,
                            alignment = TextAnchor.MiddleLeft,
                            normal = { textColor = UIColors.Dark }
                        };
                    }
                    return _titleStyle;
                }
            }
            
            public static GUIStyle SubtitleStyle
            {
                get
                {
                    if (_subtitleStyle == null)
                    {
                        _subtitleStyle = new GUIStyle(EditorStyles.label)
                        {
                            fontSize = 11,
                            fontStyle = FontStyle.Normal,
                            alignment = TextAnchor.MiddleLeft,
                            normal = { textColor = UIColors.Dark }
                        };
                    }
                    return _subtitleStyle;
                }
            }
            
            public static GUIStyle BackgroundStyle
            {
                get
                {
                    if (_backgroundStyle == null)
                    {
                        _backgroundStyle = new GUIStyle(GUI.skin.box)
                        {
                            normal = { background = CreateGradientTexture(UIColors.DarkCard, UIColors.DarkSection) },
                            border = new RectOffset(1, 1, 1, 1),
                            padding = new RectOffset(10, 10, 10, 10)
                        };
                    }
                    return _backgroundStyle;
                }
            }
            
            public static GUIStyle SectionBackgroundStyle
            {
                get
                {
                    if (_sectionBackgroundStyle == null)
                    {
                        _sectionBackgroundStyle = new GUIStyle(GUI.skin.box)
                        {
                            normal = { background = CreateGradientTexture(UIColors.DarkSection, UIColors.DarkAccent) },
                            border = new RectOffset(2, 2, 2, 2),
                            padding = new RectOffset(8, 8, 8, 8),
                            margin = new RectOffset(2, 2, 2, 2)
                        };
                    }
                    return _sectionBackgroundStyle;
                }
            }
            
            public static GUIStyle GradientBackgroundStyle
            {
                get
                {
                    if (_gradientBackgroundStyle == null)
                    {
                        _gradientBackgroundStyle = new GUIStyle(GUI.skin.box)
                        {
                            normal = { background = CreateGradientTexture(UIColors.DarkAccent, UIColors.DarkCard) },
                            border = new RectOffset(1, 1, 1, 1),
                            padding = new RectOffset(12, 12, 12, 12)
                        };
                    }
                    return _gradientBackgroundStyle;
                }
            }
            
            public static GUIStyle WindowBackgroundStyle
            {
                get
                {
                    if (_windowBackgroundStyle == null)
                    {
                        _windowBackgroundStyle = new GUIStyle(GUI.skin.box)
                        {
                            normal = { background = CreateGradientTexture(new Color(0.98f, 0.99f, 1f, 1f), new Color(0.94f, 0.96f, 1f, 1f)) },
                            border = new RectOffset(0, 0, 0, 0),
                            padding = new RectOffset(0, 0, 0, 0)
                        };
                    }
                    return _windowBackgroundStyle;
                }
            }
            
            private static Texture2D CreateGradientTexture(Color topColor, Color bottomColor)
            {
                var texture = new Texture2D(1, 2);
                texture.SetPixel(0, 0, topColor);
                texture.SetPixel(0, 1, bottomColor);
                texture.Apply();
                return texture;
            }
        }
        
        public BaseFinderBehaviour(KiristWindow parent)
        {
            parentWindow = parent;
        }
        
        protected void DrawSectionCard(string title, System.Action content)
        {
            EditorGUILayout.BeginVertical(UIStyles.CardStyle);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(title, UIStyles.HeaderStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            content?.Invoke();
            
            EditorGUILayout.EndVertical();
        }
        
        protected bool DrawStyledButton(string text, Color color, int width = 0, int height = 0)
        {
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            
            bool result;
            if (width > 0 && height > 0)
            {
                result = GUILayout.Button(text, UIStyles.ButtonStyle, GUILayout.Width(width), GUILayout.Height(height));
            }
            else if (width > 0)
            {
                result = GUILayout.Button(text, UIStyles.ButtonStyle, GUILayout.Width(width));
            }
            else if (height > 0)
            {
                result = GUILayout.Button(text, UIStyles.ButtonStyle, GUILayout.Height(height));
            }
            else
            {
                result = GUILayout.Button(text, UIStyles.ButtonStyle);
            }
            
            GUI.backgroundColor = originalColor;
            return result;
        }
        
        protected bool DrawLargeStyledButton(Rect rect, string text, Color color)
        {
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            
            bool result = GUI.Button(rect, text, UIStyles.LargeButtonStyle);
            
            GUI.backgroundColor = originalColor;
            return result;
        }
        
        protected void DrawHelpBox(string message, MessageType messageType = MessageType.Info)
        {
            EditorGUILayout.HelpBox(message, messageType);
        }
        
        protected void DrawTitle(string title)
        {
            GUILayout.Label(title, UIStyles.TitleStyle);
        }
        
        protected void DrawSubtitle(string subtitle)
        {
            GUILayout.Label(subtitle, UIStyles.SubtitleStyle);
        }
        
        protected void LogInfo(string message)
        {
            Debug.Log($"[{GetType().Name}] {message}");
        }
        
        protected void LogWarning(string message)
        {
            Debug.LogWarning($"[{GetType().Name}] {message}");
        }
        
        protected void LogError(string message)
        {
            Debug.LogError($"[{GetType().Name}] {message}");
        }
        
        protected void FindInScene(GameObject gameObject, string sceneName, string finderName)
        {
            FindInScene(gameObject, sceneName, null, finderName);
        }
        
        protected void FindInScene(GameObject gameObject, string objectName, string sceneName, string scenePath, string finderName)
        {
            LogInfo($"FindInScene called - GameObject: {(gameObject != null ? gameObject.name : "null")}, ObjectName: {objectName}, SceneName: {sceneName}, ScenePath: {scenePath}");
            
            if (gameObject == null)
            {
                LogInfo($"GameObject is null, attempting to find '{objectName}' in scene: {sceneName}");
                
                if (!string.IsNullOrEmpty(scenePath))
                {
                    LogInfo($"Opening scene: {scenePath}");
                    try
                    {
                        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                        if (scene.IsValid())
                        {
                            LogInfo($"Successfully opened scene: {scene.name} (path: {scene.path})");
                            
                            LogInfo($"Searching for GameObject: '{objectName}' in scene: {scene.name}");
                            GameObject foundObject = FindGameObjectInScene(scene, objectName);
                            if (foundObject != null)
                            {
                                LogInfo($"Found GameObject: {foundObject.name}, selecting and highlighting...");
                                Selection.activeGameObject = foundObject;
                                
                                if (SceneView.lastActiveSceneView != null)
                                {
                                    LogInfo("Framing GameObject in SceneView...");
                                    SceneView.lastActiveSceneView.FrameSelected();
                                }
                                else
                                {
                                    LogWarning("No active SceneView found for framing");
                                }
                                
                                EditorGUIUtility.PingObject(foundObject);
                                LogInfo($"Successfully selected and highlighted GameObject: {foundObject.name} in opened scene: {scene.name}");
                                return;
                            }
                            else
                            {
                                LogWarning($"Could not find GameObject '{objectName}' in opened scene {scene.name}");
                                
                                GameObject[] allObjects = scene.GetRootGameObjects();
                                LogInfo($"Scene has {allObjects.Length} root GameObjects:");
                                foreach (var obj in allObjects)
                                {
                                    LogInfo($"  - {obj.name}");
                                }
                            }
                        }
                        else
                        {
                            LogError($"Failed to open scene: {scenePath}");
                        }
                    }
                    catch (System.Exception e)
                    {
                        LogError($"Error opening scene {scenePath}: {e.Message}");
                    }
                }
                else
                {
                    LogError($"Scene path is null or empty for scene: {sceneName}");
                }
                
                EditorUtility.DisplayDialog("Error", $"Could not find GameObject '{objectName}' in scene '{sceneName}'. Scene may not be accessible.", "OK");
                return;
            }

            LogInfo($"GameObject exists, using existing logic");
            FindInScene(gameObject, sceneName, scenePath, finderName);
        }
        
        protected void FindInScene(GameObject gameObject, string sceneName, string scenePath, string finderName)
        {
            if (gameObject == null)
            {
                LogInfo($"GameObject is null, attempting to find it in scene: {sceneName}");
                
                if (!string.IsNullOrEmpty(scenePath))
                {
                    try
                    {
                        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                        if (scene.IsValid())
                        {
                            LogInfo($"Successfully opened scene: {scene.name}");
                            
                            string objectName = sceneName;
                            if (sceneName.Contains("Scene: "))
                            {
                                LogWarning($"Cannot determine GameObject name from scene name: {sceneName}");
                                EditorUtility.DisplayDialog("Error", $"Cannot find GameObject in scene '{sceneName}'. GameObject name is not specified.", "OK");
                                return;
                            }
                            
                            GameObject foundObject = FindGameObjectInScene(scene, objectName);
                            if (foundObject != null)
                            {
                                Selection.activeGameObject = foundObject;
                                
                                if (SceneView.lastActiveSceneView != null)
                                {
                                    SceneView.lastActiveSceneView.FrameSelected();
                                }
                                
                                EditorGUIUtility.PingObject(foundObject);
                                LogInfo($"Found and selected GameObject: {foundObject.name} in opened scene: {scene.name}");
                                return;
                            }
                            else
                            {
                                LogWarning($"Could not find GameObject {objectName} in opened scene {scene.name}");
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        LogError($"Error opening scene {scenePath}: {e.Message}");
                    }
                }
                
                EditorUtility.DisplayDialog("Error", $"Could not find GameObject in scene '{sceneName}'. Scene may not be accessible.", "OK");
                return;
            }

            LogInfo($"Attempting to find GameObject: {gameObject.name} in scene: {sceneName}");

            if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                Selection.activeObject = gameObject;
                EditorGUIUtility.PingObject(gameObject);
                LogInfo($"Selected Prefab Asset: {gameObject.name} in Project window (Find in Folder)");
                return;
            }

            if (!gameObject.scene.IsValid())
            {
                LogInfo($"GameObject is not in an open scene. Attempting to open scene: {sceneName}");
                
                string targetScenePath = scenePath;
                if (string.IsNullOrEmpty(targetScenePath))
                {
                    targetScenePath = FindScenePath(sceneName);
                }
                
                if (!string.IsNullOrEmpty(targetScenePath))
                {
                    try
                    {
                        Scene scene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);
                        if (scene.IsValid())
                        {
                            LogInfo($"Successfully opened scene: {scene.name}");
                            
                            GameObject foundObject = FindGameObjectInScene(scene, gameObject.name);
                            if (foundObject != null)
                            {
                                Selection.activeGameObject = foundObject;
                                
                                if (SceneView.lastActiveSceneView != null)
                                {
                                    SceneView.lastActiveSceneView.FrameSelected();
                                }
                                
                                EditorGUIUtility.PingObject(foundObject);
                                LogInfo($"Found and selected GameObject: {foundObject.name} in opened scene: {scene.name}");
                                return;
                            }
                            else
                            {
                                LogWarning($"Could not find GameObject {gameObject.name} in opened scene {scene.name}");
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        LogError($"Error opening scene {targetScenePath}: {e.Message}");
                    }
                }
                
                EditorUtility.DisplayDialog("Error", $"Could not find GameObject '{gameObject.name}' in scene '{sceneName}'. Scene may not be accessible.", "OK");
                return;
            }

            Selection.activeGameObject = gameObject;
            
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }
            
            EditorGUIUtility.PingObject(gameObject);
            
            LogInfo($"Found and selected GameObject: {gameObject.name} in scene: {sceneName}");
        }
        
        private string FindScenePath(string sceneName)
        {
            LogInfo($"Looking for scene: '{sceneName}'");
            
            string cleanSceneName = sceneName;
            if (sceneName.StartsWith("Scene: "))
            {
                cleanSceneName = sceneName.Substring("Scene: ".Length);
            }
            
            LogInfo($"Clean scene name: '{cleanSceneName}'");
            
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
            LogInfo($"Found {sceneGuids.Length} scene files");
            
            foreach (string sceneGuid in sceneGuids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                if (!string.IsNullOrEmpty(scenePath))
                {
                    if (scenePath.Contains("Packages/"))
                        continue;
                    
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                    LogInfo($"Checking scene: '{fileName}' at '{scenePath}'");
                        
                    if (scenePath.Contains(cleanSceneName) || fileName == cleanSceneName)
                    {
                        LogInfo($"Found matching scene: '{scenePath}'");
                        return scenePath;
                    }
                }
            }
            
            LogWarning($"No scene found matching: '{cleanSceneName}'");
            return null;
        }
        
        private GameObject FindGameObjectInScene(Scene scene, string objectName)
        {
            LogInfo($"Searching for GameObject '{objectName}' in scene '{scene.name}'");
            GameObject[] rootObjects = scene.GetRootGameObjects();
            LogInfo($"Scene has {rootObjects.Length} root GameObjects");
            
            foreach (GameObject rootObj in rootObjects)
            {
                LogInfo($"Checking root object: {rootObj.name}");
                GameObject found = FindGameObjectRecursive(rootObj, objectName);
                if (found != null)
                {
                    LogInfo($"Found GameObject: {found.name} under root: {rootObj.name}");
                    return found;
                }
            }
            
            LogWarning($"GameObject '{objectName}' not found in scene '{scene.name}'");
            return null;
        }
        
        private GameObject FindGameObjectRecursive(GameObject obj, string objectName)
        {
            if (obj.name == objectName)
                return obj;
                
            foreach (Transform child in obj.transform)
            {
                GameObject found = FindGameObjectRecursive(child.gameObject, objectName);
                if (found != null)
                    return found;
            }
            
            return null;
        }
        
        protected void DrawScrollControls(string itemCountText, System.Action findInSceneAction = null)
        {
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.Label(itemCountText, EditorStyles.centeredGreyMiniLabel, GUILayout.Width(150));
            
            EditorGUILayout.Space(10);
            
            if (findInSceneAction != null)
            {
                if (GUILayout.Button("🔍 Find in Scene", UIStyles.ButtonStyle, GUILayout.Width(120)))
                {
                    findInSceneAction.Invoke();
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        protected void DrawResultCard<T>(T result, int index, bool isSelected, 
            System.Func<T, string> getName, 
            System.Func<T, string> getScene, 
            System.Func<T, string> getError,
            System.Action<T, int> onSelect,
            System.Action<T, int> onRemove)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(200), GUILayout.Height(150));
            
            if (isSelected)
            {
                EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 150), UIColors.Primary * 0.2f);
            }
            
            var nameStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                wordWrap = true,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = UIColors.Dark }
            };
            GUILayout.Label($"🎯 {getName(result)}", nameStyle, GUILayout.Height(30));
            
            var sceneStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = UIColors.Info }
            };
            GUILayout.Label($"📍 {getScene(result)}", sceneStyle, GUILayout.Height(20));
            
            GUILayout.Label($"❌ {getError(result)}", sceneStyle, GUILayout.Height(20));
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button(isSelected ? "✅ Selected" : "Select", UIStyles.ButtonStyle, GUILayout.Height(25)))
            {
                onSelect?.Invoke(result, index);
            }
            
            if (GUILayout.Button("🗑️ Remove", UIStyles.ButtonStyle, GUILayout.Height(25)))
            {
                onRemove?.Invoke(result, index);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        public abstract void DrawUI();
        public abstract void ClearResults();
    }
}