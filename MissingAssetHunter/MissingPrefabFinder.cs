using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace Kirist.EditorTool
{
        /// <summary>
        /// 누락된 프리팹을 찾는 클래스 (핵심 구현 내용만 포함)
        /// </summary>
        public class MissingPrefabFinder : BaseFinderBehaviour
        {
            #region Fields
            
            // 검사 결과 저장
            private List<MissingPrefabInfo> missingPrefabResults = new List<MissingPrefabInfo>();
            
            #endregion
            
            #region Constructor
            
            public MissingPrefabFinder(KiristWindow parent) : base(parent)
            {
            }
            
            #endregion
            
            #region Core Detection Logic
            
            /// <summary>
            /// Missing Prefab 검사를 시작합니다
            /// </summary>
            /// <param name="searchMode">검사 모드 (Scene 또는 Prefab)</param>
            /// <param name="targets">검사 대상 리스트 (Scene 또는 GameObject)</param>
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
            
            /// <summary>
            /// 여러 씬에서 Missing Prefab을 찾습니다
            /// </summary>
            private void FindMissingPrefabsInScenes(List<Object> sceneTargets)
            {
                if (sceneTargets == null || sceneTargets.Count == 0)
                {
                    // 현재 열린 모든 씬 검사
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
                    // 지정된 씬들 검사
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
            
            /// <summary>
            /// 여러 프리팹에서 Missing Prefab을 찾습니다
            /// </summary>
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
            
            /// <summary>
            /// 단일 씬에서 Missing Prefab을 찾습니다
            /// </summary>
            private void FindMissingPrefabsInScene(Scene scene)
            {
                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (GameObject obj in rootObjects)
                {
                    CheckGameObjectForMissingPrefabs(obj, scene.name, scene.path);
                }
            }
            
            /// <summary>
            /// 단일 프리팹에서 Missing Prefab을 찾습니다
            /// </summary>
            private void FindMissingPrefabsInPrefab(GameObject prefab)
            {
                string prefabPath = AssetDatabase.GetAssetPath(prefab);
                CheckGameObjectForMissingPrefabs(prefab, "Prefab", prefabPath);
            }
            
            /// <summary>
            /// GameObject와 그 자식들을 재귀적으로 검사하여 Missing Prefab을 찾습니다
            /// 핵심 검사 로직
            /// </summary>
            /// <param name="obj">검사할 GameObject</param>
            /// <param name="locationName">위치 이름 (씬 또는 프리팹 이름)</param>
            /// <param name="locationPath">위치 경로</param>
            private void CheckGameObjectForMissingPrefabs(GameObject obj, string locationName, string locationPath)
            {
                // 1. 프리팹 에셋이 Missing인지 확인
                if (PrefabUtility.IsPrefabAssetMissing(obj))
                {
                    AddMissingPrefabInfo(obj, locationName, locationPath, "Missing Prefab Asset");
                }
                // 2. 프리팹 인스턴스인 경우 원본 연결 확인
                else if (PrefabUtility.IsPartOfPrefabInstance(obj))
                {
                    var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                    if (prefabAsset == null)
                    {
                        AddMissingPrefabInfo(obj, locationName, locationPath, "Broken Prefab Instance");
                    }
                }
                
                // 3. 자식 GameObject들도 재귀적으로 검사
                foreach (Transform child in obj.transform)
                {
                    CheckGameObjectForMissingPrefabs(child.gameObject, locationName, locationPath);
                }
            }
            
            /// <summary>
            /// 발견한 Missing Prefab 정보를 결과 리스트에 추가합니다
            /// </summary>
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
            
            #endregion
            
            #region Public API
            
            /// <summary>
            /// 검사 결과를 반환합니다
            /// </summary>
            public List<MissingPrefabInfo> GetResults()
            {
                return missingPrefabResults;
            }
            
            /// <summary>
            /// 검사 결과를 초기화합니다
            /// </summary>
            public override void ClearResults()
            {
                missingPrefabResults.Clear();
            }
            
            #endregion
        }
}
