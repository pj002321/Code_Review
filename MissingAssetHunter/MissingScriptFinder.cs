using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace Kirist.EditorTool
{
        /// <summary>
        /// 누락된 스크립트를 찾는 클래스 (핵심 구현 내용만 포함)
        /// </summary>
        public class MissingScriptFinder : BaseFinderBehaviour
        {
            #region Fields
            
            // 검사 결과 저장
            private List<MissingScriptInfo> missingScriptResults = new List<MissingScriptInfo>();
            
            #endregion
            
            #region Constructor
            
            public MissingScriptFinder(KiristWindow parent) : base(parent)
            {
            }
            
            #endregion
            
            #region Core Detection Logic
            
            /// <summary>
            /// Missing Script 검사를 시작합니다
            /// </summary>
            /// <param name="searchMode">검사 모드 (Scene 또는 Prefab)</param>
            /// <param name="targets">검사 대상 리스트 (Scene 또는 GameObject)</param>
            public void FindMissingScripts(ScriptSearchMode searchMode, List<Object> targets)
            {
                missingScriptResults.Clear();
                
                if (searchMode == ScriptSearchMode.Scene)
                {
                    FindMissingScriptsInScenes(targets);
                }
                else
                {
                    FindMissingScriptsInPrefabs(targets);
                }
                
            }
            
            /// <summary>
            /// 여러 씬에서 Missing Script를 찾습니다
            /// </summary>
            private void FindMissingScriptsInScenes(List<Object> sceneTargets)
            {
                if (sceneTargets == null || sceneTargets.Count == 0)
                {
                    // 현재 열린 모든 씬 검사
                    for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                    {
                        var scene = EditorSceneManager.GetSceneAt(i);
                        if (scene.IsValid())
                        {
                            FindMissingScriptsInScene(scene);
                        }
                    }
                }
                else
                {
                    // 지정된 씬들 검사
                    foreach (var target in sceneTargets)
                    {
                        if (target is SceneAsset sceneAsset)
                        {
                            string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                            FindMissingScriptsInScene(scene);
                            EditorSceneManager.CloseScene(scene, true);
                        }
                    }
                }
            }
            
            /// <summary>
            /// 여러 프리팹에서 Missing Script를 찾습니다
            /// </summary>
            private void FindMissingScriptsInPrefabs(List<Object> prefabTargets)
            {
                
                if (prefabTargets == null || prefabTargets.Count == 0)
                {
                    return;
                }

                foreach (var target in prefabTargets)
                {
                    if (target is GameObject prefab)
                    {
                        FindMissingScriptsInPrefab(prefab);
                    }
                }
            }
            
            /// <summary>
            /// 단일 씬에서 Missing Script를 찾습니다
            /// </summary>
            private void FindMissingScriptsInScene(Scene scene)
            {
                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (GameObject obj in rootObjects)
                {
                    CheckGameObjectForMissingScripts(obj, scene.name, scene.path);
                }
            }
            
            /// <summary>
            /// 단일 프리팹에서 Missing Script를 찾습니다
            /// </summary>
            private void FindMissingScriptsInPrefab(GameObject prefab)
            {
                string prefabPath = AssetDatabase.GetAssetPath(prefab);
                
                CheckGameObjectForMissingScripts(prefab, "Prefab", prefabPath);
            }
            
            /// <summary>
            /// GameObject와 그 자식들을 재귀적으로 검사하여 Missing Script를 찾습니다
            /// 핵심 검사 로직 
            /// </summary>
            /// <param name="obj">검사할 GameObject</param>
            /// <param name="locationName">위치 이름 (씬 또는 프리팹 이름)</param>
            /// <param name="locationPath">위치 경로</param>
            private void CheckGameObjectForMissingScripts(GameObject obj, string locationName, string locationPath)
            {
                // 1. GameObject의 모든 컴포넌트를 가져옴
                Component[] components = obj.GetComponents<Component>();
                
                // 2. 각 컴포넌트를 검사
                for (int i = 0; i < components.Length; i++)
                {
                    // 3. 컴포넌트가 null이면 Missing Script!
                    if (components[i] == null)
                    {
                        AddMissingScriptInfo(obj, i, locationName, locationPath);
                    }
                }
                
                // 4. 자식 GameObject들도 재귀적으로 검사
                foreach (Transform child in obj.transform)
                {
                    CheckGameObjectForMissingScripts(child.gameObject, locationName, locationPath);
                }
            }
            
            /// <summary>
            /// 발견한 Missing Script 정보를 결과 리스트에 추가합니다
            /// </summary>
            private void AddMissingScriptInfo(GameObject obj, int componentIndex, string locationName, string locationPath)
            {
                var info = new MissingScriptInfo
                {
                    gameObject = obj,
                    componentIndex = componentIndex,
                    sceneName = locationName,
                    scenePath = locationPath,
                    assetPath = locationPath,
                    gameObjectName = obj.name,
                    instanceID = obj.GetInstanceID().ToString(),
                    componentTypeName = "Missing Script"
                };
                
                missingScriptResults.Add(info);
            }
            
            #endregion
            
            #region Public API
            
            /// <summary>
            /// 검사 결과를 반환합니다
            /// </summary>
            public List<MissingScriptInfo> GetResults()
            {
                return missingScriptResults;
            }
            
            /// <summary>
            /// 검사 결과를 초기화합니다
            /// </summary>
            public override void ClearResults()
            {
                missingScriptResults.Clear();
            }
            
            #endregion
        }
}
