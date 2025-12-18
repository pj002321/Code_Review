using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace Kirist.EditorTool
{

        /// <summary>
        /// 누락된 Material과 Shader 오류를 찾는 클래스 (핵심 구현 내용만 포함)
        /// </summary>
        public class MissingMaterialFinder : BaseFinderBehaviour
        {
            #region Fields
            
            // 검사 결과 저장
            private List<MissingMaterialInfo> missingMaterialResults = new List<MissingMaterialInfo>();
            
            #endregion
            
            #region Constructor
            
            public MissingMaterialFinder(KiristWindow parent) : base(parent)
            {
            }
            
            #endregion
            
            #region Core Detection Logic
            
            /// <summary>
            /// Missing Material 검사를 시작합니다
            /// </summary>
            /// <param name="searchMode">검사 모드 (Scene 또는 Prefab)</param>
            /// <param name="targets">검사 대상 리스트 (Scene 또는 GameObject)</param>
            public void FindMissingMaterials(MaterialSearchMode searchMode, List<Object> targets)
            {
                missingMaterialResults.Clear();
                
                if (searchMode == MaterialSearchMode.Scene)
                {
                    FindMissingMaterialsInScenes(targets);
                }
                else
                {
                    FindMissingMaterialsInPrefabs(targets);
                }
            }
            
            /// <summary>
            /// 여러 씬에서 Missing Material을 찾습니다
            /// </summary>
            private void FindMissingMaterialsInScenes(List<Object> sceneTargets)
            {
                if (sceneTargets == null || sceneTargets.Count == 0)
                {
                    // 현재 열린 모든 씬 검사
                    for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                    {
                        var scene = EditorSceneManager.GetSceneAt(i);
                        if (scene.IsValid())
                        {
                            FindMissingMaterialsInScene(scene);
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
                            FindMissingMaterialsInScene(scene);
                            EditorSceneManager.CloseScene(scene, true);
                        }
                    }
                }
            }
            
            /// <summary>
            /// 여러 프리팹에서 Missing Material을 찾습니다
            /// </summary>
            private void FindMissingMaterialsInPrefabs(List<Object> prefabTargets)
            {
                if (prefabTargets == null || prefabTargets.Count == 0)
                {
                    return;
                }
                
                foreach (var target in prefabTargets)
                {
                    if (target is GameObject prefab)
                    {
                        FindMissingMaterialsInPrefab(prefab);
                    }
                }
            }
            
            /// <summary>
            /// 단일 씬에서 Missing Material을 찾습니다
            /// </summary>
            private void FindMissingMaterialsInScene(Scene scene)
            {
                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (GameObject obj in rootObjects)
                {
                    string sceneFileName = System.IO.Path.GetFileNameWithoutExtension(scene.path);
                    CheckGameObjectForMissingMaterials(obj, sceneFileName, scene.path);
                }
            }
            
            /// <summary>
            /// 단일 프리팹에서 Missing Material을 찾습니다
            /// </summary>
            private void FindMissingMaterialsInPrefab(GameObject prefab)
            {
                string prefabPath = AssetDatabase.GetAssetPath(prefab);
                CheckGameObjectForMissingMaterials(prefab, "Prefab", prefabPath);
            }
            
            /// <summary>
            /// GameObject와 그 자식들을 재귀적으로 검사하여 Missing Material을 찾습니다
            /// 핵심 검사 로직
            /// </summary>
            /// <param name="obj">검사할 GameObject</param>
            /// <param name="locationName">위치 이름 (씬 또는 프리팹 이름)</param>
            /// <param name="locationPath">위치 경로</param>
            private void CheckGameObjectForMissingMaterials(GameObject obj, string locationName, string locationPath)
            {
                // 1. 다양한 Renderer 타입 검사
                
                // MeshRenderer
                var meshRenderer = obj.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    CheckRendererForMissingMaterials(meshRenderer, obj, locationName, locationPath);
                }
                
                // SkinnedMeshRenderer
                var skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer != null)
                {
                    CheckRendererForMissingMaterials(skinnedMeshRenderer, obj, locationName, locationPath);
                }
                
                // SpriteRenderer
                var spriteRenderer = obj.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    CheckRendererForMissingMaterials(spriteRenderer, obj, locationName, locationPath);
                }
                
                // LineRenderer
                var lineRenderer = obj.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    CheckRendererForMissingMaterials(lineRenderer, obj, locationName, locationPath);
                }
                
                // TrailRenderer
                var trailRenderer = obj.GetComponent<TrailRenderer>();
                if (trailRenderer != null)
                {
                    CheckRendererForMissingMaterials(trailRenderer, obj, locationName, locationPath);
                }
                
                // ParticleSystemRenderer
                var particleSystemRenderer = obj.GetComponent<ParticleSystemRenderer>();
                if (particleSystemRenderer != null)
                {
                    CheckRendererForMissingMaterials(particleSystemRenderer, obj, locationName, locationPath);
                }
                
                // 2. 자식 GameObject들도 재귀적으로 검사
                foreach (Transform child in obj.transform)
                {
                    CheckGameObjectForMissingMaterials(child.gameObject, locationName, locationPath);
                }
            }
            
            /// <summary>
            /// Renderer의 Material을 검사합니다
            /// </summary>
            private void CheckRendererForMissingMaterials(Renderer renderer, GameObject obj, string locationName, string locationPath)
            {
                Material[] materials = renderer.sharedMaterials;
                
                for (int i = 0; i < materials.Length; i++)
                {
                    // Material이 null인 경우
                    if (materials[i] == null)
                    {
                        AddMissingMaterialInfo(obj, renderer, i, locationName, locationPath, "Missing Material");
                    }
                    // Material은 있지만 Shader에 오류가 있는 경우
                    else if (IsErrorMaterial(materials[i]))
                    {
                        string errorReason = GetShaderErrorReason(materials[i]);
                        AddMissingMaterialInfo(obj, renderer, i, locationName, locationPath, errorReason);
                    }
                }
            }
            
            /// <summary>
            /// Material의 Shader에 오류가 있는지 확인합니다
            /// 핵심 Shader 검증 로직
            /// </summary>
            private bool IsErrorMaterial(Material material)
            {
                if (material == null)
                    return false;
                
                if (material.shader == null)
                    return true;
                
                var shader = material.shader;
                var shaderName = shader.name;
                
                // 1. Unity Error Shader 확인 (마젠타 핑크 머티리얼)
                if (shaderName == "Hidden/InternalErrorShader" ||
                    shaderName == "Hidden/InternalError" ||
                    shaderName.Contains("InternalErrorShader") ||
                    shaderName.Contains("Internal-Error"))
                {
                    return true;
                }
                
                // 2. 플랫폼 지원 확인
                if (!shader.isSupported)
                {
                    return true;
                }
                
                // 3. Shader 컴파일 에러 확인
                if (UnityVersionHelper.ShaderHasError(shader))
                {
                    return true;
                }
                
                // 4. Render Pipeline 불일치 확인
                if (IsRenderPipelineMismatch(shader, shaderName))
                {
                    return true;
                }
                
                return false;
            }
            
            /// <summary>
            /// Render Pipeline 불일치를 확인합니다
            /// </summary>
            private bool IsRenderPipelineMismatch(Shader shader, string shaderName)
            {
                var currentRP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
                bool isHDRP = currentRP != null && currentRP.GetType().Name.Contains("HDRenderPipelineAsset");
                bool isURP = currentRP != null && currentRP.GetType().Name.Contains("UniversalRenderPipelineAsset");
                bool isBuiltIn = currentRP == null;
                
                // HDRP 환경에서 URP/Built-in 셰이더 사용
                if (isHDRP && (shaderName.StartsWith("Universal Render Pipeline/") || 
                               shaderName.StartsWith("URP/") || 
                               shaderName == "Standard"))
                {
                    return true;
                }
                
                // URP 환경에서 HDRP/Built-in 셰이더 사용
                if (isURP && (shaderName.StartsWith("HDRP/") || 
                              shaderName == "Standard"))
                {
                    return true;
                }
                
                // Built-in 환경에서 HDRP/URP 셰이더 사용
                if (isBuiltIn && (shaderName.StartsWith("HDRP/") || 
                                  shaderName.StartsWith("Universal Render Pipeline/") || 
                                  shaderName.StartsWith("URP/")))
                {
                    return true;
                }
                
                return false;
            }
            
            /// <summary>
            /// Shader 오류 원인을 문자열로 반환합니다
            /// </summary>
            private string GetShaderErrorReason(Material material)
            {
                if (material == null || material.shader == null)
                    return "Missing Shader";
                
                var shader = material.shader;
                var shaderName = shader.name;
                
                if (shaderName.Contains("InternalErrorShader"))
                    return "Unity Error Shader";
                
                if (!shader.isSupported)
                    return "Unsupported Shader";
                
                if (UnityVersionHelper.ShaderHasError(shader))
                    return "Shader Compilation Failed";
                
                if (IsRenderPipelineMismatch(shader, shaderName))
                    return "Render Pipeline Mismatch";
                
                return "Shader Error";
            }
            
            /// <summary>
            /// 발견한 Missing Material 정보를 결과 리스트에 추가합니다
            /// </summary>
            private void AddMissingMaterialInfo(GameObject obj, Renderer renderer, int materialIndex, 
                                                string locationName, string locationPath, string errorReason)
            {
                var info = new MissingMaterialInfo
                {
                    gameObject = obj,
                    renderer = renderer,
                    materialIndex = materialIndex,
                    sceneName = locationName,
                    assetPath = locationPath,
                    gameObjectName = obj.name,
                    rendererType = renderer.GetType().Name,
                    errorReason = errorReason
                };
                
                missingMaterialResults.Add(info);
            }
            
            #endregion
            
            #region Public API
            
            /// <summary>
            /// 검사 결과를 반환합니다
            /// </summary>
            public List<MissingMaterialInfo> GetResults()
            {
                return missingMaterialResults;
            }
            
            /// <summary>
            /// 검사 결과를 초기화합니다
            /// </summary>
            public override void ClearResults()
            {
                missingMaterialResults.Clear();
            }
            
            #endregion
        }
}
