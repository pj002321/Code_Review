using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.TerrainTools;
using UnityEngine.TerrainUtils;

namespace Kirist.EditorTool
{
        /// <summary>
        /// 씬을 분석하고 문제를 찾아내는 클래스 (핵심 구현 내용만 포함)
        /// </summary>
        public partial class SceneAnalyzer : BaseFinderBehaviour
        {
            #region Fields
            
            // 상수
            private const int SNAPSHOT_SIZE = 512;
            private const int HIGH_POLY_THRESHOLD = 10000;
            
            // 분석 결과
            private SceneAnalysisResult currentAnalysisResult = null;
            private bool isAnalyzing = false;
            private float analysisProgress = 0f;
            private string analysisStatus = "";
            
            // UI 스크롤 위치
            private Vector2 sceneAnalysisScrollPos = Vector2.zero;
            private Vector2 objectListScrollPos = Vector2.zero;
            private Vector2 environmentScrollPos = Vector2.zero;
            private Vector2 errorScrollPos = Vector2.zero;
            
            // 분석 옵션
            private bool analyzeGameObjects = true;
            private bool analyzeEnvironment = true;
            private bool analyzeErrors = true;
            private bool analyzePerformance = true;
            private bool includeInactiveObjects = false;
            
            // 에러 체크 옵션
            private bool checkMissingScripts = true;
            private bool checkMissingMaterials = true;
            private bool checkMissingPrefabs = true;
            private bool autoFixErrors = false;
            
            // 씬 선택
            private SceneAnalysisMode analysisMode = SceneAnalysisMode.CurrentScene;
            private SceneAsset selectedSceneAsset = null;
            
            // 선택된 오브젝트
            private int selectedObjectIndex = -1;
            private GameObject selectedGameObject = null;
            
            // 씬 복원 정보
            private string originalScenePath = null;
            private bool wasAnalyzingSpecificScene = false;
            
            #endregion
            
            public SceneAnalyzer(KiristWindow parent) : base(parent)
            {
            }
            
            
            #region Utility Methods
            
            /// <summary>
            /// 그라데이션 텍스처를 생성합니다
            /// </summary>
            /// <param name="topColor">상단 색상</param>
            /// <param name="bottomColor">하단 색상</param>
            /// <returns>생성된 그라데이션 텍스처</returns>
            private Texture2D CreateGradientTexture(Color topColor, Color bottomColor)
            {
                var texture = new Texture2D(1, 2);
                texture.SetPixel(0, 0, topColor);
                texture.SetPixel(0, 1, bottomColor);
                texture.Apply();
                return texture;
            }
            
            #endregion
            
            
            
            
            
            
            

            
            public override void ClearResults()
            {
                currentAnalysisResult = null;
                isAnalyzing = false;
                analysisProgress = 0f;
                analysisStatus = "";
                selectedObjectIndex = -1;
                selectedGameObject = null;
                selectedSceneAsset = null;
                
                System.GC.Collect();
                AssetDatabase.Refresh();
                
            }
            
            private void AnalyzeCurrentScene()
            {
                if (isAnalyzing) return;
                
                currentAnalysisResult = null;
                System.GC.Collect();
                AssetDatabase.Refresh();
                
                isAnalyzing = true;
                analysisProgress = 0f;
                analysisStatus = "Starting analysis...";
                
                try
                {
                    currentAnalysisResult = new SceneAnalysisResult();
                    currentAnalysisResult.sceneName = SceneManager.GetActiveScene().name;
                    currentAnalysisResult.scenePath = SceneManager.GetActiveScene().path;
                    
                    EditorApplication.update += UpdateAnalysis;
                }
                catch (System.Exception e)
                {
                    isAnalyzing = false;
                }
            }
            
            private void AnalyzeSpecificScene(SceneAsset sceneAsset)
            {
                if (isAnalyzing) return;
                
                isAnalyzing = true;
                analysisProgress = 0f;
                analysisStatus = "Starting analysis...";
                
                try
                {
                    var currentScene = SceneManager.GetActiveScene();
                    originalScenePath = currentScene.path;
                    wasAnalyzingSpecificScene = true;
                    
                    currentAnalysisResult = new SceneAnalysisResult();
                    currentAnalysisResult.sceneName = sceneAsset.name;
                    currentAnalysisResult.scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                    
                    var scene = EditorSceneManager.OpenScene(currentAnalysisResult.scenePath, OpenSceneMode.Single);
                    
                    
                    EditorApplication.update += UpdateAnalysis;
                }
                catch (System.Exception e)
                {
                    isAnalyzing = false;
                    wasAnalyzingSpecificScene = false;
                    originalScenePath = null;
                }
            }
            
            private void UpdateAnalysis()
            {
                try
                {
                    if (!isAnalyzing) return;
                    
                    if (currentAnalysisResult == null)
                    {
                        isAnalyzing = false;
                        EditorApplication.update -= UpdateAnalysis;
                        return;
                    }
                    
                    if (analysisProgress < 0.2f)
                    {
                        analysisStatus = "Analyzing GameObjects...";
                        AnalyzeGameObjects();
                        analysisProgress = 0.2f;
                    }
                    else if (analysisProgress < 0.4f)
                    {
                        analysisStatus = "Analyzing Components...";
                        AnalyzeComponents();
                        analysisProgress = 0.4f;
                    }
                    else if (analysisProgress < 0.6f)
                    {
                        analysisStatus = "Analyzing Environment...";
                        AnalyzeEnvironment();
                        analysisProgress = 0.6f;
                    }
                    else if (analysisProgress < 0.8f)
                    {
                        analysisStatus = "Detecting Errors...";
                        AnalyzeErrors();
                        analysisProgress = 0.8f;
                    }
                    else if (analysisProgress < 0.9f)
                    {
                        analysisStatus = "Capturing Scene Snapshot...";
                        CaptureSceneSnapshot();
                        analysisProgress = 0.9f;
                    }
                    else if (analysisProgress < 1.0f)
                    {
                        analysisStatus = "Finalizing Analysis...";
                        FinalizeAnalysis();
                        analysisProgress = 1.0f;
                    }
                    else
                    {
                        analysisStatus = "Analysis Complete!";
                        isAnalyzing = false;
                        EditorApplication.update -= UpdateAnalysis;
                        
                        if (wasAnalyzingSpecificScene && !string.IsNullOrEmpty(originalScenePath))
                        {
                            try
                            {
                                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
                            }
                            catch (System.Exception e)
                            {
                            }
                            finally
                            {
                                wasAnalyzingSpecificScene = false;
                                originalScenePath = null;
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    isAnalyzing = false;
                    EditorApplication.update -= UpdateAnalysis;
                    
                    if (wasAnalyzingSpecificScene && !string.IsNullOrEmpty(originalScenePath))
                    {
                        try
                        {
                            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
                        }
                        catch (System.Exception returnError)
                        {
                        }
                        finally
                        {
                            wasAnalyzingSpecificScene = false;
                            originalScenePath = null;
                        }
                    }
                }
            }
            
            private void AnalyzeGameObjects()
            {
                var scene = SceneManager.GetActiveScene();
                var rootObjects = scene.GetRootGameObjects();
                
                currentAnalysisResult.totalObjects = 0;
                currentAnalysisResult.activeObjects = 0;
                currentAnalysisResult.gameObjects = new List<GameObjectInfo>();
                
                foreach (var rootObj in rootObjects)
                {
                    AnalyzeGameObjectRecursive(rootObj);
                }
                
            }
            
            private void AnalyzeGameObjectRecursive(GameObject obj)
            {
                if (!includeInactiveObjects && !obj.activeInHierarchy) return;
                
                currentAnalysisResult.totalObjects++;
                if (obj.activeInHierarchy) currentAnalysisResult.activeObjects++;
                
                bool isPrefab = PrefabUtility.IsPartOfPrefabInstance(obj);
                bool isMissingPrefab = PrefabUtility.IsPrefabAssetMissing(obj);
                var prefabType = PrefabUtility.GetPrefabAssetType(obj);
                var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(obj);
                
                
                var objInfo = new GameObjectInfo
                {
                    gameObject = obj,
                    name = obj.name,
                    isActive = obj.activeInHierarchy,
                    layer = obj.layer,
                    tag = obj.tag,
                    components = new List<ComponentInfo>(),
                    childCount = obj.transform.childCount
                };
                
                var components = obj.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null) continue;
                    
                    var compInfo = new ComponentInfo
                    {
                        component = component,
                        componentType = component.GetType(),
                        isMissing = false
                    };
                    
                    objInfo.components.Add(compInfo);
                }
                
                currentAnalysisResult.gameObjects.Add(objInfo);
                
                foreach (Transform child in obj.transform)
                {
                    AnalyzeGameObjectRecursive(child.gameObject);
                }
            }
            
            private void AnalyzeComponents()
            {
                currentAnalysisResult.totalComponents = 0;
                currentAnalysisResult.totalScripts = 0;
                currentAnalysisResult.totalRenderers = 0;
                currentAnalysisResult.totalMaterials = 0;
                currentAnalysisResult.componentTypes = new Dictionary<string, int>();
                
                foreach (var objInfo in currentAnalysisResult.gameObjects)
                {
                    foreach (var compInfo in objInfo.components)
                    {
                        currentAnalysisResult.totalComponents++;
                        
                        var typeName = compInfo.componentType.Name;
                        if (currentAnalysisResult.componentTypes.ContainsKey(typeName))
                            currentAnalysisResult.componentTypes[typeName]++;
                        else
                            currentAnalysisResult.componentTypes[typeName] = 1;
                        
                        if (compInfo.componentType == typeof(MonoBehaviour))
                            currentAnalysisResult.totalScripts++;
                        else if (compInfo.component is Renderer)
                            currentAnalysisResult.totalRenderers++;
                    }
                }
            }
            
            private void AnalyzeEnvironment()
            {
                currentAnalysisResult.environmentInfo = new EnvironmentInfo();
                
                AnalyzeLighting();
                
                AnalyzeCameras();
                
                AnalyzeTerrains();
                
                AnalyzePostProcessing();
            }
            
            private void AnalyzeLighting()
            {
                var lights = FindObjectsOfType<Light>();
                currentAnalysisResult.environmentInfo.lightCount = lights.Length;
                currentAnalysisResult.environmentInfo.lights = new List<LightInfo>();
                
                foreach (var light in lights)
                {
                    var lightInfo = new LightInfo
                    {
                        light = light,
                        type = light.type,
                        intensity = light.intensity,
                        range = light.range,
                        color = light.color,
                        isActive = light.gameObject.activeInHierarchy
                    };
                    
                    currentAnalysisResult.environmentInfo.lights.Add(lightInfo);
                }
                
                var lightingSettings = LightmapSettings.lightmaps;
                currentAnalysisResult.environmentInfo.lightmapCount = lightingSettings.Length;
                
                var renderSettings = RenderSettings.defaultReflectionMode;
                currentAnalysisResult.environmentInfo.reflectionMode = renderSettings.ToString();
            }
            
            private void AnalyzeCameras()
            {
                var cameras = FindObjectsOfType<Camera>();
                currentAnalysisResult.environmentInfo.cameraCount = cameras.Length;
                currentAnalysisResult.environmentInfo.cameras = new List<CameraInfo>();
                
                foreach (var camera in cameras)
                {
                    var cameraInfo = new CameraInfo
                    {
                        camera = camera,
                        fieldOfView = camera.fieldOfView,
                        nearClipPlane = camera.nearClipPlane,
                        farClipPlane = camera.farClipPlane,
                        clearFlags = camera.clearFlags,
                        backgroundColor = camera.backgroundColor,
                        isActive = camera.gameObject.activeInHierarchy
                    };
                    
                    currentAnalysisResult.environmentInfo.cameras.Add(cameraInfo);
                }
            }
            
            private void AnalyzeTerrains()
            {
                var terrains = FindObjectsOfType<Terrain>();
                currentAnalysisResult.environmentInfo.terrainCount = terrains.Length;
                currentAnalysisResult.environmentInfo.terrains = new List<TerrainInfo>();
                
                foreach (var terrain in terrains)
                {
                    var terrainInfo = new TerrainInfo
                    {
                        terrain = terrain,
                        terrainData = terrain.terrainData,
                        heightmapResolution = terrain.terrainData?.heightmapResolution ?? 0,
                        detailResolution = terrain.terrainData?.detailResolution ?? 0,
                        alphamapResolution = terrain.terrainData?.alphamapResolution ?? 0,
                        isActive = terrain.gameObject.activeInHierarchy
                    };
                    
                    currentAnalysisResult.environmentInfo.terrains.Add(terrainInfo);
                }
            }
            
            private void AnalyzePostProcessing()
            {
                
                var volumes = FindObjectsOfType<Volume>();
                currentAnalysisResult.environmentInfo.postProcessingVolumeCount = volumes.Length;
                currentAnalysisResult.environmentInfo.postProcessingVolumes = new List<PostProcessingInfo>();
                
                foreach (var volume in volumes)
                {
                    var ppInfo = new PostProcessingInfo
                    {
                        volume = volume,
                        isGlobal = volume.isGlobal,
                        priority = volume.priority,
                        blendDistance = volume.blendDistance,
                        weight = volume.weight,
                        isActive = volume.gameObject.activeInHierarchy,
                        profile = null, 
                        settingsCount = 0,
                        activeSettings = new List<string>(),
                        inactiveSettings = new List<string>()
                    };
                    
                
                    try
                    {
                    
                        var profileProperty = volume.GetType().GetProperty("profile");
                        if (profileProperty != null)
                        {
                            ppInfo.profile = profileProperty.GetValue(volume) as ScriptableObject;
                            
                            if (ppInfo.profile != null)
                            {
                                AnalyzeVolumeProfile(ppInfo);
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                    }
                    
                    currentAnalysisResult.environmentInfo.postProcessingVolumes.Add(ppInfo);
                }
            }
            
            private void AnalyzeVolumeProfile(PostProcessingInfo ppInfo)
            {
                if (ppInfo.profile == null) return;
                
                try
                {
                    
                    var componentsProperty = ppInfo.profile.GetType().GetProperty("components");
                    if (componentsProperty != null)
                    {
                        var settings = componentsProperty.GetValue(ppInfo.profile) as System.Collections.IList;
                        if (settings != null)
                        {
                            ppInfo.settingsCount = settings.Count;
                            
                            foreach (var setting in settings)
                            {
                                if (setting == null) continue;
                                
                                var settingName = setting.GetType().Name;
                                
                               
                                var isActive = IsPostProcessingSettingActive(setting);
                                
                                if (isActive)
                                {
                                    ppInfo.activeSettings.Add(settingName);
                                }
                                else
                                {
                                    ppInfo.inactiveSettings.Add(settingName);
                                }
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                }
            }
            
            private bool IsPostProcessingSettingActive(object setting)
            {
                try
                {
                 
                    var type = setting.GetType();
                    var enabledField = type.GetField("enabled", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    
                    if (enabledField != null)
                    {
                        return (bool)enabledField.GetValue(setting);
                    }
                    
                
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            
            private void AnalyzeErrors()
            {
                currentAnalysisResult.errors = new List<SceneError>();
                
                
                AnalyzeMissingScripts();
                
                AnalyzeMissingMaterials();
                
                AnalyzeErrorShaders();
                
                CheckForMissingPrefabs();
                
                AnalyzeMissingPrefabs();
                
                AnalyzePrefabReferences();
                
                if (analyzePerformance)
                {
                    AnalyzePerformanceIssues();
                }
                
            }
            
            private void AnalyzeMissingScripts()
            {
                int missingScriptCount = 0;
                
                foreach (var objInfo in currentAnalysisResult.gameObjects)
                {
                    var components = objInfo.gameObject.GetComponents<Component>();
                    for (int i = 0; i < components.Length; i++)
                    {
                        if (components[i] == null)
                        {
                            missingScriptCount++;
                            
                            var error = new SceneError
                            {
                                type = SceneErrorType.MissingScript,
                                severity = SceneErrorSeverity.High,
                                gameObject = objInfo.gameObject,
                                message = $"Missing Script at component index {i}",
                                componentIndex = i,
                                gameObjectName = objInfo.gameObject.name,
                                gameObjectPath = GetGameObjectPath(objInfo.gameObject),
                                componentTypeName = "Missing Script"
                            };
                            
                            currentAnalysisResult.errors.Add(error);
                        }
                    }
                }
                
            }
            
            private void AnalyzeMissingMaterials()
            {
                int missingMaterialCount = 0;
                
                foreach (var objInfo in currentAnalysisResult.gameObjects)
                {
                    var renderers = objInfo.gameObject.GetComponents<Renderer>();
                    foreach (var renderer in renderers)
                    {
                        if (renderer == null) continue;
                        
                        var materials = renderer.sharedMaterials;
                        for (int i = 0; i < materials.Length; i++)
                        {
                            if (materials[i] == null)
                            {
                                missingMaterialCount++;
                                
                                var error = new SceneError
                                {
                                    type = SceneErrorType.MissingMaterial,
                                    severity = SceneErrorSeverity.Medium,
                                    gameObject = objInfo.gameObject,
                                    message = $"Missing Material at index {i}",
                                    component = renderer,
                                    materialIndex = i,
                                    gameObjectName = objInfo.gameObject.name,
                                    gameObjectPath = GetGameObjectPath(objInfo.gameObject),
                                    componentTypeName = renderer.GetType().Name
                                };
                                
                                currentAnalysisResult.errors.Add(error);
                            }
                        }
                    }
                }
                
            }
            
            private void AnalyzeErrorShaders()
            {
                int errorShaderCount = 0;
                int totalMaterials = 0;
                

                foreach (var objInfo in currentAnalysisResult.gameObjects)
                {
                    var renderers = objInfo.gameObject.GetComponents<Renderer>();
                    
                    foreach (var renderer in renderers)
                    {
                        if (renderer == null) continue;
                        
                        var materials = renderer.sharedMaterials;
                        
                        for (int i = 0; i < materials.Length; i++)
                        {
                            var material = materials[i];
                            if (material == null) continue;
                            
                            totalMaterials++;
                            
                            if (IsErrorMaterial(material))
                            {
                                errorShaderCount++;
                                
                                var error = new SceneError
                                {
                                    type = SceneErrorType.ErrorShader,
                                    severity = SceneErrorSeverity.Medium,
                                    gameObject = objInfo.gameObject,
                                    message = $"Error Shader detected: {material.shader?.name ?? "Unknown"}",
                                    component = renderer,
                                    materialIndex = i,
                                    gameObjectName = objInfo.gameObject.name,
                                    gameObjectPath = GetGameObjectPath(objInfo.gameObject),
                                    componentTypeName = renderer.GetType().Name
                                };
                                
                                currentAnalysisResult.errors.Add(error);
                            }
                        }
                    }
                }
                
            }
            
            /// <summary>
            /// 머티리얼에 에러가 있는지 확인합니다
            /// </summary>
            /// <param name="material">확인할 머티리얼</param>
            /// <returns>에러가 있으면 true</returns>
            
            /// <summary>
            /// Unity 에러 셰이더인지 확인합니다
            /// </summary>

            /// <summary>
            /// 렌더 파이프라인 불일치를 확인합니다
            /// </summary>
            
            /// <summary>
            /// URP 또는 Built-in 셰이더인지 확인
            /// </summary>
            private bool IsURPOrBuiltInShader(string shaderName)
            {
                return shaderName.StartsWith("Universal Render Pipeline/") ||
                       shaderName.StartsWith("URP/") ||
                       shaderName == "Standard" ||
                       shaderName.StartsWith("Legacy Shaders/") ||
                       shaderName.StartsWith("Mobile/");
            }
            
            /// <summary>
            /// HDRP 또는 Built-in 셰이더인지 확인
            /// </summary>
            private bool IsHDRPOrBuiltInShader(string shaderName)
            {
                return shaderName.StartsWith("HDRP/") ||
                       shaderName == "Standard" ||
                       shaderName.StartsWith("Legacy Shaders/") ||
                       shaderName.StartsWith("Mobile/");
            }
            
            /// <summary>
            /// HDRP 또는 URP 셰이더인지 확인
            /// </summary>
            private bool IsHDRPOrURPShader(string shaderName)
            {
                return shaderName.StartsWith("HDRP/") ||
                       shaderName.StartsWith("Universal Render Pipeline/") ||
                       shaderName.StartsWith("URP/");
            }
            
            /// <summary>
            /// Unity 내장 셰이더인지 확인합니다
            /// </summary>

            private void AnalyzeMissingPrefabs()
            {
                int totalObjects = currentAnalysisResult.gameObjects.Count;
                
                foreach (var objInfo in currentAnalysisResult.gameObjects)
                {
                    var gameObject = objInfo.gameObject;
                    
                    bool isMissing = PrefabUtility.IsPrefabAssetMissing(gameObject);
                    
                    if (isMissing)
                    {
                        var error = new SceneError
                        {
                            type = SceneErrorType.MissingPrefabAsset,
                            severity = SceneErrorSeverity.Critical,
                            gameObject = gameObject,
                            message = "Missing Prefab Asset - The prefab file cannot be found"
                        };
                        
                        currentAnalysisResult.errors.Add(error);
                        continue;
                    }
                    
                    var prefabType = PrefabUtility.GetPrefabAssetType(gameObject);
                    var prefabInstanceStatus = PrefabUtility.GetPrefabInstanceStatus(gameObject);
                    var isPartOfPrefab = PrefabUtility.IsPartOfPrefabInstance(gameObject);
                    
                    
                    if (prefabType != PrefabAssetType.NotAPrefab && 
                        prefabInstanceStatus == PrefabInstanceStatus.MissingAsset)
                    {
                        var error = new SceneError
                        {
                            type = SceneErrorType.BrokenPrefabConnection,
                            severity = SceneErrorSeverity.High,
                            gameObject = gameObject,
                            message = "Broken Prefab Connection - Prefab asset reference is broken"
                        };
                        
                        currentAnalysisResult.errors.Add(error);
                    }
                    
                    if (prefabType != PrefabAssetType.NotAPrefab)
                    {
                        if (prefabInstanceStatus == PrefabInstanceStatus.Disconnected)
                        {
                            var error = new SceneError
                            {
                                type = SceneErrorType.PrefabInstanceIssue,
                                severity = SceneErrorSeverity.Medium,
                                gameObject = gameObject,
                                message = "Disconnected Prefab Instance - Prefab connection is lost"
                            };
                            
                            currentAnalysisResult.errors.Add(error);
                        }
                        
                        if (prefabInstanceStatus == PrefabInstanceStatus.Connected && 
                            PrefabUtility.IsPartOfPrefabInstance(gameObject) && 
                            PrefabUtility.GetCorrespondingObjectFromSource(gameObject) == null)
                        {
                            var error = new SceneError
                            {
                                type = SceneErrorType.PrefabInstanceIssue,
                                severity = SceneErrorSeverity.Medium,
                                gameObject = gameObject,
                                message = "Invalid Prefab Instance - Corrupted prefab hierarchy"
                            };
                            
                            currentAnalysisResult.errors.Add(error);
                        }
                    }
                    
                    if (PrefabUtility.IsPartOfPrefabInstance(gameObject))
                    {
                        var rootPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
                        
                        if (rootPrefab != null && PrefabUtility.IsPrefabAssetMissing(rootPrefab))
                        {
                        var error = new SceneError
                        {
                            type = SceneErrorType.MissingPrefab,
                            severity = SceneErrorSeverity.High,
                                gameObject = gameObject,
                                message = $"Nested Prefab Issue - Root prefab '{rootPrefab.name}' is missing"
                        };
                        
                        currentAnalysisResult.errors.Add(error);
                    }
                }
                }
                
            }
            
            private void AnalyzePrefabReferences()
            {
                int prefabRefErrors = 0;
                
                foreach (var objInfo in currentAnalysisResult.gameObjects)
                {
                    var gameObject = objInfo.gameObject;
                    var components = gameObject.GetComponents<Component>();
                    
                    foreach (var component in components)
                    {
                        if (component == null) continue;
                        
                        try
                        {
                            var serializedObject = new UnityEditor.SerializedObject(component);
                            var iterator = serializedObject.GetIterator();
                            
                            while (iterator.NextVisible(true))
                            {
                                if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                                {
                                    var referencedObject = iterator.objectReferenceValue;
                                    
                                    if (referencedObject != null && PrefabUtility.IsPartOfPrefabAsset(referencedObject))
                                    {
                                        
                                        var prefabPath = AssetDatabase.GetAssetPath(referencedObject);
                                        
                                        if (string.IsNullOrEmpty(prefabPath) || !System.IO.File.Exists(prefabPath))
                                        {
                                            var error = new SceneError
                                            {
                                                type = SceneErrorType.MissingPrefabAsset,
                                                severity = SceneErrorSeverity.High,
                                                gameObject = gameObject,
                                                component = component,
                                                message = $"Missing Prefab Reference in {component.GetType().Name}.{iterator.name} - Referenced prefab '{referencedObject.name}' not found"
                                            };
                                            
                                            currentAnalysisResult.errors.Add(error);
                                            prefabRefErrors++;
                                        }
                                    }
                                }
                            }
                        }
                        catch (System.Exception e)
                        {
                            continue;
                        }
                    }
                }
                
            }
            
            private void AnalyzePerformanceIssues()
            {
                foreach (var objInfo in currentAnalysisResult.gameObjects)
                {
                    var meshFilter = objInfo.gameObject.GetComponent<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        var vertexCount = meshFilter.sharedMesh.vertexCount;
                        if (vertexCount > 10000) 
                        {
                            var error = new SceneError
                            {
                                type = SceneErrorType.PerformanceIssue,
                                severity = SceneErrorSeverity.Medium,
                                gameObject = objInfo.gameObject,
                                message = $"High polygon mesh: {vertexCount} vertices"
                            };
                            
                            currentAnalysisResult.errors.Add(error);
                        }
                    }
                }
            }
            
            private void CaptureSceneSnapshot()
            {
                try
                {

                    Camera mainCamera = Camera.main;
                    if (mainCamera == null)
                    {
                        Camera[] cameras = UnityEngine.Object.FindObjectsOfType<Camera>();
                        if (cameras.Length > 0)
                        {
                            mainCamera = cameras[0];
                        }
                    }

                    if (mainCamera == null)
                    {
                        GameObject tempCameraObj = new GameObject("TempSnapshotCamera");
                        mainCamera = tempCameraObj.AddComponent<Camera>();

                        var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                        if (rootObjects.Length > 0)
                        {
                            Bounds sceneBounds = new Bounds(Vector3.zero, Vector3.zero);
                            bool boundsInitialized = false;

                            foreach (var obj in rootObjects)
                            {
                                Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
                                foreach (var renderer in renderers)
                                {
                                    if (!boundsInitialized)
                                    {
                                        sceneBounds = renderer.bounds;
                                        boundsInitialized = true;
                                    }
                                    else
                                    {
                                        sceneBounds.Encapsulate(renderer.bounds);
                                    }
                                }
                            }

                            if (boundsInitialized)
                            {
                                Vector3 center = sceneBounds.center;
                                float distance = sceneBounds.size.magnitude * 1.5f;
                                tempCameraObj.transform.position = center + new Vector3(distance, distance * 0.5f, -distance);
                                tempCameraObj.transform.LookAt(center);
                            }
                        }
                    }

                    int width = 512;
                    int height = 512;
                    RenderTexture renderTexture = new RenderTexture(width, height, 24);
                    RenderTexture previousRT = mainCamera.targetTexture;

                    mainCamera.targetTexture = renderTexture;
                    mainCamera.Render();

                    RenderTexture.active = renderTexture;
                    Texture2D snapshot = new Texture2D(width, height, TextureFormat.RGB24, false);
                    snapshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    snapshot.Apply();

                    mainCamera.targetTexture = previousRT;
                    RenderTexture.active = null;
                    UnityEngine.Object.DestroyImmediate(renderTexture);

                    if (mainCamera.gameObject.name == "TempSnapshotCamera")
                    {
                        UnityEngine.Object.DestroyImmediate(mainCamera.gameObject);
                    }

                    currentAnalysisResult.sceneSnapshot = snapshot;
                }
                catch (System.Exception e)
                {
                }
            }

            private void FinalizeAnalysis()
            {

                var validErrors = currentAnalysisResult.errors.Where(e => e.gameObject != null).ToList();
                var removedCount = currentAnalysisResult.errors.Count - validErrors.Count;
                if (removedCount > 0)
                {
                    currentAnalysisResult.errors = validErrors;
                }
            
                currentAnalysisResult.totalMaterials = CalculateTotalMaterials();
                currentAnalysisResult.errorCount = currentAnalysisResult.errors.Count;
                
                currentAnalysisResult.highSeverityErrors = currentAnalysisResult.errors.Count(e => e.severity == SceneErrorSeverity.High || e.severity == SceneErrorSeverity.Critical);
                currentAnalysisResult.mediumSeverityErrors = currentAnalysisResult.errors.Count(e => e.severity == SceneErrorSeverity.Medium);
                currentAnalysisResult.lowSeverityErrors = currentAnalysisResult.errors.Count(e => e.severity == SceneErrorSeverity.Low);
                
                
                foreach (var errorType in System.Enum.GetValues(typeof(SceneErrorType)))
                {
                    var count = currentAnalysisResult.errors.Count(e => e.type == (SceneErrorType)errorType);
                    if (count > 0)
                    {
                    }
                }
            }
            
            private int CalculateTotalMaterials()
            {
                var materialSet = new HashSet<Material>();
                
                foreach (var objInfo in currentAnalysisResult.gameObjects)
                {
                    var renderers = objInfo.gameObject.GetComponents<Renderer>();
                    foreach (var renderer in renderers)
                    {
                        if (renderer == null) continue;
                        
                        var materials = renderer.sharedMaterials;
                        foreach (var material in materials)
                        {
                            if (material != null)
                                materialSet.Add(material);
                        }
                    }
                }
                
                return materialSet.Count;
            }
            

            private void CheckForMissingScripts()
            {
                if (!checkMissingScripts) return;
                
                foreach (var objInfo in currentAnalysisResult.gameObjects)
                {
                    var components = objInfo.gameObject.GetComponents<Component>();
                    foreach (var component in components)
                    {
                        if (component == null)
                        {
                            var error = new SceneError
                            {
                                type = SceneErrorType.MissingScript,
                                severity = SceneErrorSeverity.High,
                                gameObject = objInfo.gameObject,
                                message = "Missing Script Component"
                            };
                            
                            currentAnalysisResult.errors.Add(error);
                            
                            if (autoFixErrors)
                            {
                                var serializedObject = new SerializedObject(objInfo.gameObject);
                                var componentsProperty = serializedObject.FindProperty("m_Component");
                                
                                for (int i = componentsProperty.arraySize - 1; i >= 0; i--)
                                {
                                    var componentProperty = componentsProperty.GetArrayElementAtIndex(i);
                                    var componentRef = componentProperty.FindPropertyRelative("component");
                                    
                                    if (componentRef.objectReferenceValue == null)
                                    {
                                        componentsProperty.DeleteArrayElementAtIndex(i);
                                    }
                                }
                                
                                serializedObject.ApplyModifiedProperties();
                            }
                        }
                    }
                }
            }
            
            private void CheckForMissingMaterials()
            {
                if (!checkMissingMaterials) return;
                
                foreach (var objInfo in currentAnalysisResult.gameObjects)
                {
                    var renderers = objInfo.gameObject.GetComponents<Renderer>();
                    foreach (var renderer in renderers)
                    {
                        if (renderer == null) continue;
                        
                        CheckRendererMaterials(renderer, objInfo.gameObject);
                    }
                    
                    var particleSystems = objInfo.gameObject.GetComponents<ParticleSystem>();
                    foreach (var ps in particleSystems)
                    {
                        if (ps == null) continue;
                        
                        CheckParticleSystemMaterials(ps, objInfo.gameObject);
                    }
                    
                    var lineRenderer = objInfo.gameObject.GetComponent<LineRenderer>();
                    if (lineRenderer != null)
                    {
                        CheckLineRendererMaterials(lineRenderer, objInfo.gameObject);
                    }
                    
                    var trailRenderer = objInfo.gameObject.GetComponent<TrailRenderer>();
                    if (trailRenderer != null)
                    {
                        CheckTrailRendererMaterials(trailRenderer, objInfo.gameObject);
                    }
                }
            }
            
            private void CheckRendererMaterials(Renderer renderer, GameObject gameObject)
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                    {
                        var error = new SceneError
                        {
                            type = SceneErrorType.MissingMaterial,
                            severity = SceneErrorSeverity.Medium,
                            gameObject = gameObject,
                            message = $"Missing Material at index {i} in {renderer.GetType().Name}"
                        };
                        
                        currentAnalysisResult.errors.Add(error);
                        
                        if (autoFixErrors)
                        {
                            var newMaterials = new Material[materials.Length];
                            for (int j = 0; j < materials.Length; j++)
                            {
                                newMaterials[j] = materials[j] ?? GetDefaultMaterial();
                            }
                            renderer.sharedMaterials = newMaterials;
                        }
                    }
                }
            }
            
            private void CheckParticleSystemMaterials(ParticleSystem ps, GameObject gameObject)
            {
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    CheckRendererMaterials(renderer, gameObject);
                }
            }
            
            private void CheckLineRendererMaterials(LineRenderer lineRenderer, GameObject gameObject)
            {
                var material = lineRenderer.material;
                if (material == null)
                {
                    var error = new SceneError
                    {
                        type = SceneErrorType.MissingMaterial,
                        severity = SceneErrorSeverity.Medium,
                        gameObject = gameObject,
                        message = "Missing Material in LineRenderer"
                    };
                    
                    currentAnalysisResult.errors.Add(error);
                    
                    if (autoFixErrors)
                    {
                        lineRenderer.material = GetDefaultMaterial();
                    }
                }
            }
            
            private void CheckTrailRendererMaterials(TrailRenderer trailRenderer, GameObject gameObject)
            {
                var material = trailRenderer.material;
                if (material == null)
                {
                    var error = new SceneError
                    {
                        type = SceneErrorType.MissingMaterial,
                        severity = SceneErrorSeverity.Medium,
                        gameObject = gameObject,
                        message = "Missing Material in TrailRenderer"
                    };
                    
                    currentAnalysisResult.errors.Add(error);
                    
                    if (autoFixErrors)
                    {
                        trailRenderer.material = GetDefaultMaterial();
                    }
                }
            }
            
            private void CheckForMissingPrefabs()
            {
                if (!checkMissingPrefabs) return;
                
                
                foreach (var objInfo in currentAnalysisResult.gameObjects)
                {
                    var gameObject = objInfo.gameObject;
                    
                    bool isMissing = PrefabUtility.IsPrefabAssetMissing(gameObject);
                    var prefabType = PrefabUtility.GetPrefabAssetType(gameObject);
                    var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(gameObject);
                    
                    
                    if (isMissing)
                    {
                        var error = new SceneError
                        {
                            type = SceneErrorType.MissingPrefabAsset,
                            severity = SceneErrorSeverity.Critical,
                            gameObject = gameObject,
                            message = "Missing Prefab Asset - The prefab file cannot be found"
                        };
                        
                        currentAnalysisResult.errors.Add(error);
                        
                        if (autoFixErrors)
                        {
                            PrefabUtility.UnpackPrefabInstance(gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                        }
                    }
                    else if (prefabType != PrefabAssetType.NotAPrefab && 
                             prefabStatus == PrefabInstanceStatus.MissingAsset)
                    {
                        var error = new SceneError
                        {
                            type = SceneErrorType.BrokenPrefabConnection,
                            severity = SceneErrorSeverity.High,
                            gameObject = gameObject,
                            message = "Broken Prefab Connection - Prefab asset reference is broken"
                        };
                        
                        currentAnalysisResult.errors.Add(error);
                    }
                }
                
            }
            
            private Material GetDefaultMaterial()
            {
                var defaultMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
                if (defaultMaterial == null)
                {
                    var material = new Material(Shader.Find("Standard"));
                    material.name = "Default Material";
                    return material;
                }
                return defaultMaterial;
            }
            
            
            
            private string GetGameObjectPath(GameObject obj)
            {
                if (obj == null) return "Unknown";
                
                var path = obj.name;
                var parent = obj.transform.parent;
                
                while (parent != null)
                {
                    path = parent.name + "/" + path;
                    parent = parent.parent;
                }
                
                return path;
            }
            
            private string GetErrorTypeDisplayName(SceneErrorType errorType)
            {
                return errorType switch
                {
                    SceneErrorType.MissingScript => "Missing Script",
                    SceneErrorType.MissingMaterial => "Missing Material",
                    SceneErrorType.MissingPrefab => "Missing Prefab",
                    SceneErrorType.MissingPrefabAsset => "Missing Prefab Asset",
                    SceneErrorType.BrokenPrefabConnection => "Broken Prefab Connection",
                    SceneErrorType.PrefabInstanceIssue => "Prefab Instance Issue",
                    SceneErrorType.ErrorShader => "Error Shader",
                    SceneErrorType.PerformanceIssue => "Performance Issue",
                    SceneErrorType.LightingIssue => "Lighting Issue",
                    SceneErrorType.CameraIssue => "Camera Issue",
                    SceneErrorType.TerrainIssue => "Terrain Issue",
                    SceneErrorType.PostProcessingIssue => "Post Processing Issue",
                    _ => errorType.ToString()
                };
            }
            
            private void RemoveMissingScript(SceneError error)
            {
                try
            {
                if (error.gameObject != null)
                {
                    var serializedObject = new SerializedObject(error.gameObject);
                    var componentsProperty = serializedObject.FindProperty("m_Component");
                    
                    for (int i = componentsProperty.arraySize - 1; i >= 0; i--)
                    {
                        var componentProperty = componentsProperty.GetArrayElementAtIndex(i);
                        var componentRef = componentProperty.FindPropertyRelative("component");
                        
                        if (componentRef.objectReferenceValue == null)
                        {
                            componentsProperty.DeleteArrayElementAtIndex(i);
                        }
                    }
                    
                    serializedObject.ApplyModifiedProperties();
             
                    currentAnalysisResult.errors.Remove(error);
                    currentAnalysisResult.errorCount = currentAnalysisResult.errors.Count;
                }
                    else
                    {
                    }
                }
                catch (System.Exception e)
                {
                }
            }
        }
}
