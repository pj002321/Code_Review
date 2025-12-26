using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace Kirist.EditorTool
{
    public partial class KiristWindow
    {
        public class MissingPrefabFinder : BaseFinderBehaviour
        {
            private List<MissingPrefabInfo> missingPrefabResults = new List<MissingPrefabInfo>();
            
            public MissingPrefabFinder(KiristWindow parent) : base(parent)
            {
            }
            
            public override void DrawUI()
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                DrawTitle("📦 MISSING PREFAB FINDER");
                EditorGUILayout.Space(5);
                
                if (GUILayout.Button("🔍 FIND MISSING PREFABS", UIStyles.SearchButtonStyle, GUILayout.Height(30)))
                {
                    FindMissingPrefabs();
                }
                
                if (missingPrefabResults.Count > 0)
                {
                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField($"Found {missingPrefabResults.Count} missing prefabs", EditorStyles.boldLabel);
                    
                    EditorGUILayout.BeginScrollView(Vector2.zero);
                    for (int i = 0; i < missingPrefabResults.Count; i++)
                    {
                        var result = missingPrefabResults[i];
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"{result.gameObjectName} - {result.sceneName}");
                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            Selection.activeGameObject = result.gameObject;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();
                }
                
                EditorGUILayout.EndVertical();
            }
            
            public void FindMissingPrefabs(PrefabSearchMode searchMode, List<Object> targets)
            {
                missingPrefabResults.Clear();
                
                if (searchMode == PrefabSearchMode.Scene)
                {
                    FindMissingPrefabsInScenes(targets);
                }
                else
                {
                    FindMissingPrefabsInPrefabs(targets);
                }
            }
            
            private void FindMissingPrefabs()
            {
                missingPrefabResults.Clear();
                
                if (parentWindow.prefabSearchMode == PrefabSearchMode.Scene)
                {
                    FindMissingPrefabsInScenes(null);
                }
                else
                {
                    FindMissingPrefabsInPrefabs(null);
                }
            }
            
            private void FindMissingPrefabsInScenes(List<Object> sceneTargets)
            {
                if (sceneTargets == null || sceneTargets.Count == 0)
                {
                    for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                    {
                        var scene = EditorSceneManager.GetSceneAt(i);
                        if (scene.IsValid())
                        {
                            FindMissingPrefabsInScene(scene);
                        }
                    }
                }
                else
                {
                    foreach (var target in sceneTargets)
                    {
                        if (target is SceneAsset sceneAsset)
                        {
                            string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                            FindMissingPrefabsInScene(scene);
                            EditorSceneManager.CloseScene(scene, true);
                        }
                    }
                }
            }
            
            private void FindMissingPrefabsInPrefabs(List<Object> prefabTargets)
            {
                if (prefabTargets == null || prefabTargets.Count == 0)
                {
                    return;
                }
                
                foreach (var target in prefabTargets)
                {
                    if (target is GameObject prefab)
                    {
                        FindMissingPrefabsInPrefab(prefab);
                    }
                }
            }
            
            private void FindMissingPrefabsInScene(Scene scene)
            {
                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (GameObject obj in rootObjects)
                {
                    CheckGameObjectForMissingPrefabs(obj, scene.name, scene.path);
                }
            }
            
            private void FindMissingPrefabsInPrefab(GameObject prefab)
            {
                string prefabPath = AssetDatabase.GetAssetPath(prefab);
                CheckGameObjectForMissingPrefabs(prefab, "Prefab", prefabPath);
            }
            
            private void CheckGameObjectForMissingPrefabs(GameObject obj, string locationName, string locationPath)
            {
                if (PrefabUtility.IsPrefabAssetMissing(obj))
                {
                    AddMissingPrefabInfo(obj, locationName, locationPath, "Missing Prefab Asset");
                }
                else if (PrefabUtility.IsPartOfPrefabInstance(obj))
                {
                    var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                    if (prefabAsset == null)
                    {
                        AddMissingPrefabInfo(obj, locationName, locationPath, "Broken Prefab Instance");
                    }
                }
                
                foreach (Transform child in obj.transform)
                {
                    CheckGameObjectForMissingPrefabs(child.gameObject, locationName, locationPath);
                }
            }
            
            private void AddMissingPrefabInfo(GameObject obj, string locationName, string locationPath, string errorReason)
            {
                var info = new MissingPrefabInfo
                {
                    gameObject = obj,
                    sceneName = locationName,
                    assetPath = locationPath,
                    gameObjectName = obj.name,
                    instanceID = obj.GetInstanceID().ToString(),
                    prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj),
                    errorReason = errorReason
                };
                
                missingPrefabResults.Add(info);
            }
            
            public List<MissingPrefabInfo> GetResults()
            {
                return missingPrefabResults;
            }
            
            public override void ClearResults()
            {
                missingPrefabResults.Clear();
            }
        }
    }
}
