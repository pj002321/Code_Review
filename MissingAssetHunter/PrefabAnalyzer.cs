using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.IO;

namespace Kirist.EditorTool
{
        /// <summary>
        /// 프리팹을 분석하고 문제를 찾아내는 클래스 (핵심 구현 내용만 포함)
        /// </summary>

        private string dragDropMessage = "";
        private float dragDropMessageTime = 0f;

        private float loadingAnimationTime = 0f;
        private bool isAnalyzing = false;

        private GameObject selectedPrefab = null;
        private PrefabAnalysisResult currentAnalysisResult = null;

        public class PrefabAnalyzer : BaseFinderBehaviour
        {
            private List<PrefabAnalysisResult> analysisResults = new List<PrefabAnalysisResult>();
            private bool showAnalysisResults = false;
            private AnalysisMode analysisMode = AnalysisMode.DependencyAnalysis;

            private List<GameObject> prefabsInFolder = new List<GameObject>();
            private List<bool> selectedPrefabs = new List<bool>();
            private string selectedPrefabFolder = "";
            
            private bool autoScanFolder = false;
            private bool includeSubfolders = true;
            private string scanFolderPath = "Assets";

            private Vector2 analysisResultsScrollPos = Vector2.zero;
            private int selectedAnalysisResultIndex = -1;
            private int visibleItemsCount = 50;
            private int scrollOffset = 0;

            private int totalPrefabsAnalyzed = 0;
            private int prefabsWithIssues = 0;
            private int totalComponents = 0;
            private int totalMaterials = 0;
            private int totalTextures = 0;

            public PrefabAnalyzer(KiristWindow parent) : base(parent)
            {
            }

            public override void ClearResults()
            {
                analysisResults.Clear();
                showAnalysisResults = false;
                selectedAnalysisResultIndex = -1;
                scrollOffset = 0;
                totalPrefabsAnalyzed = 0;
                prefabsWithIssues = 0;
                totalComponents = 0;
                totalMaterials = 0;
                totalTextures = 0;
                parentWindow.selectedPrefab = null;
                parentWindow.currentAnalysisResult = null;
            }
            
            
            
            
            private bool IsPackageScene(string scenePath)
            {
                return scenePath.Contains("Packages/") || scenePath.Contains("Library/");
            }
            
            private void AnalyzeMissingMaterials(GameObject prefab, PrefabAnalysisResult result)
            {
                try
                {
                    
                    var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                    
                    int totalMaterialsChecked = 0;
                    int errorMaterialsFound = 0;
                    
                    foreach (var renderer in renderers)
                    {
                        if (renderer == null) continue;
                        
                        
                        for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                        {
                            var material = renderer.sharedMaterials[i];
                            totalMaterialsChecked++;
                            
                            
                            if (material == null)
                            {
                                result.missingMaterials.Add($"Missing Material at index {i} in {renderer.name}");
                                errorMaterialsFound++;
                            }
                            else
                            {
                                
                                bool isError = IsErrorMaterial(material);
                                
                                bool hasIssues = false;
                                
                                if (material.shader == null)
                                {
                                    result.missingMaterials.Add($"Material '{material.name}' has NULL shader in {renderer.name}");
                                    hasIssues = true;
                                }
                                else
                                {
                                    string shaderPath = AssetDatabase.GetAssetPath(material.shader);
                                    
                                    if (string.IsNullOrEmpty(shaderPath) || !System.IO.File.Exists(shaderPath))
                                    {
                                        result.missingMaterials.Add($"Material '{material.name}' references missing shader: {material.shader.name} in {renderer.name}");
                                        hasIssues = true;
                                    }
                                    else if (!material.shader.isSupported)
                                    {
                                        result.missingMaterials.Add($"Material '{material.name}' shader not supported: {material.shader.name} in {renderer.name}");
                                        hasIssues = true;
                                    }
                                    else if (material.shader.passCount == 0)
                                    {
                                        result.missingMaterials.Add($"Material '{material.name}' shader compilation failed: {material.shader.name} in {renderer.name}");
                                        hasIssues = true;
                                    }
                                }
                                
                                if (isError)
                                {
                                    result.missingMaterials.Add($"Error Material '{material.name}' in {renderer.name}");
                                    hasIssues = true;
                                }
                                
                                
                                if (hasIssues)
                                {
                                    errorMaterialsFound++;
                                }
                                else
                                {
                                }
                            }
                        }
                    }
                    
                    
                    var particleSystems = prefab.GetComponentsInChildren<ParticleSystem>(true);
                    foreach (var ps in particleSystems)
                    {
                        if (ps == null) continue;
                        
                        var renderer = ps.GetComponent<ParticleSystemRenderer>();
                        if (renderer != null)
                        {
                            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                            {
                                var material = renderer.sharedMaterials[i];
                                
                                if (material == null)
                                {
                                    result.missingMaterials.Add($"Missing Particle Material at index {i} in {ps.name}");
                                }
                                else if (IsErrorMaterial(material))
                                {
                                    result.missingMaterials.Add($"Error Particle Material '{material.name}' in {ps.name}");
                                }
                            }
                        }
                    }
                    
                    var lineRenderers = prefab.GetComponentsInChildren<LineRenderer>(true);
                    foreach (var lr in lineRenderers)
                    {
                        if (lr == null) continue;
                        
                        var material = lr.material;
                        if (material == null)
                        {
                            result.missingMaterials.Add($"Missing Line Renderer Material in {lr.name}");
                        }
                        else if (IsErrorMaterial(material))
                        {
                            result.missingMaterials.Add($"Error Line Renderer Material '{material.name}' in {lr.name}");
                        }
                    }
                    
                    var trailRenderers = prefab.GetComponentsInChildren<TrailRenderer>(true);
                    foreach (var tr in trailRenderers)
                    {
                        if (tr == null) continue;
                        
                        var material = tr.material;
                        if (material == null)
                        {
                            result.missingMaterials.Add($"Missing Trail Renderer Material in {tr.name}");
                        }
                        else if (IsErrorMaterial(material))
                        {
                            result.missingMaterials.Add($"Error Trail Renderer Material '{material.name}' in {tr.name}");
                        }
                    }
                    
                }
                catch (System.Exception e)
                {
                }
            }
            
            private bool HasShaderFunctionMismatch(string shaderContent)
            {
                try
                {
                    var vertexMatch = System.Text.RegularExpressions.Regex.Match(shaderContent, @"#pragma\s+vertex\s+(\w+)");
                    if (vertexMatch.Success)
                    {
                        string declaredVertexFunction = vertexMatch.Groups[1].Value;
                        
                        var vertexFunctionMatch = System.Text.RegularExpressions.Regex.Match(shaderContent, @"(\w+)\s+vert\s*\(");
                        if (vertexFunctionMatch.Success)
                        {
                            string actualVertexFunction = vertexFunctionMatch.Groups[1].Value;
                            
                            if (declaredVertexFunction != actualVertexFunction)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                    
                    var fragmentMatch = System.Text.RegularExpressions.Regex.Match(shaderContent, @"#pragma\s+fragment\s+(\w+)");
                    if (fragmentMatch.Success)
                    {
                        string declaredFragmentFunction = fragmentMatch.Groups[1].Value;
                        
                        var fragmentFunctionMatch = System.Text.RegularExpressions.Regex.Match(shaderContent, @"(\w+)\s+frag\s*\(");
                        if (fragmentFunctionMatch.Success)
                        {
                            string actualFragmentFunction = fragmentFunctionMatch.Groups[1].Value;
                            
                            if (declaredFragmentFunction != actualFragmentFunction)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                    
                    return false;
                }
                catch (System.Exception e)
                {
                    return true;
                }
            }
            
            private bool HasBasicShaderSyntaxErrors(string shaderContent)
            {
                try
                {
                    int cgProgramCount = System.Text.RegularExpressions.Regex.Matches(shaderContent, @"CGPROGRAM").Count;
                    int endCgCount = System.Text.RegularExpressions.Regex.Matches(shaderContent, @"ENDCG").Count;
                    
                    if (cgProgramCount != endCgCount)
                    {
                        return true;
                    }
                    
                    int passCount = System.Text.RegularExpressions.Regex.Matches(shaderContent, @"Pass\s*\{").Count;
                    if (passCount == 0)
                    {
                        return true;
                    }
                    
                    if (!shaderContent.Contains("Shader") || !shaderContent.Contains("SubShader"))
                    {
                        return true;
                    }
                    
                    return false;
                }
                catch (System.Exception e)
                {
                    return true;
                }
            }

            private List<string> AnalyzeTransformSymmetry(GameObject prefab)
            {
                var issues = new List<string>();
                if (prefab == null) return issues;

                var allTransforms = prefab.GetComponentsInChildren<Transform>();

                var leftRightPairs = new Dictionary<string, string>
                {
                    {"LeftHand", "RightHand"},
                    {"LeftArm", "RightArm"},
                    {"LeftShoulder", "RightShoulder"},
                    {"LeftFoot", "RightFoot"},
                    {"LeftLeg", "RightLeg"},
                    {"LeftThigh", "RightThigh"},
                    {"LeftKnee", "RightKnee"},
                    {"LeftAnkle", "RightAnkle"},
                    {"LeftToe", "RightToe"},
                    {"LeftEye", "RightEye"},
                    {"LeftEar", "RightEar"},
                    {"LeftFinger", "RightFinger"},
                    {"LeftThumb", "RightThumb"},
                    {"LeftIndex", "RightIndex"},
                    {"LeftMiddle", "RightMiddle"},
                    {"LeftRing", "RightRing"},
                    {"LeftPinky", "RightPinky"}
                };

                foreach (var pair in leftRightPairs)
                {
                    var leftTransform = FindTransformByName(allTransforms, pair.Key);
                    var rightTransform = FindTransformByName(allTransforms, pair.Value);

                    if (leftTransform != null && rightTransform != null)
                    {
                        var leftScale = leftTransform.localScale;
                        var rightScale = rightTransform.localScale;

                        var scaleDifference = Vector3.Distance(leftScale, rightScale);
                        if (scaleDifference > 0.1f)
                        {
                            issues.Add($"{pair.Key} vs {pair.Value}: Scale asymmetry (Left: {leftScale}, Right: {rightScale}, Diff: {scaleDifference:F2})");
                        }

                        if (leftScale.x < 0.5f || leftScale.x > 2.0f || leftScale.y < 0.5f || leftScale.y > 2.0f || leftScale.z < 0.5f || leftScale.z > 2.0f)
                        {
                            issues.Add($"{pair.Key}: Scale out of ideal range (0.5~2.0): {leftScale}");
                        }
                        if (rightScale.x < 0.5f || rightScale.x > 2.0f || rightScale.y < 0.5f || rightScale.y > 2.0f || rightScale.z < 0.5f || rightScale.z > 2.0f)
                        {
                            issues.Add($"{pair.Value}: Scale out of ideal range (0.5~2.0): {rightScale}");
                        }
                    }
                    else if (leftTransform != null || rightTransform != null)
                    {
                        var missing = leftTransform == null ? pair.Key : pair.Value;
                        var existing = leftTransform == null ? pair.Value : pair.Key;
                        issues.Add($"Missing counterpart: {missing} (found {existing})");
                    }
                }

                return issues;
            }

            private Transform FindTransformByName(Transform[] transforms, string name)
            {
                return transforms.FirstOrDefault(t => t.name.Contains(name));
            }


            private List<string> ExtractTransformNamesFromIssue(string issue)
            {
                var names = new List<string>();

                if (issue.Contains(" vs "))
                {
                    var parts = issue.Split(new[] { " vs " }, StringSplitOptions.None);
                    if (parts.Length >= 2)
                    {
                        var leftName = parts[0].Split(':')[0].Trim();
                        var rightName = parts[1].Split(':')[0].Trim();
                        names.Add(leftName);
                        names.Add(rightName);
                    }
                }
                else if (issue.Contains(": "))
                {
                    var name = issue.Split(':')[0].Trim();
                    names.Add(name);
                }

                return names;
            }

            private void OpenPrefabAndHighlightObject(GameObject targetObject)
            {
                try
                {
                    if (parentWindow.currentAnalysisResult != null && parentWindow.currentAnalysisResult.prefab != null)
                    {
                        var prefabAsset = parentWindow.currentAnalysisResult.prefab;

                        AssetDatabase.OpenAsset(prefabAsset);

                        var allObjects = prefabAsset.GetComponentsInChildren<Transform>();
                        var matchingObject = allObjects.FirstOrDefault(t => t.name == targetObject.name);

                        if (matchingObject != null)
                        {
                            Selection.activeObject = matchingObject.gameObject;
                            EditorGUIUtility.PingObject(matchingObject.gameObject);

                            FocusOnObjectInSceneView(matchingObject.gameObject);
                        }
                        else
                        {
                            var targetPath = GetGameObjectPath(targetObject);
                            matchingObject = FindObjectByPath(allObjects, targetPath);

                            if (matchingObject != null)
                            {
                                Selection.activeObject = matchingObject.gameObject;
                                EditorGUIUtility.PingObject(matchingObject.gameObject);

                                FocusOnObjectInSceneView(matchingObject.gameObject);
                            }
                        }
                    }
                    else
                    {
                        Selection.activeObject = targetObject;
                        EditorGUIUtility.PingObject(targetObject);

                        FocusOnObjectInSceneView(targetObject);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to open prefab and highlight object: {e.Message}");

                    Selection.activeObject = targetObject;
                    EditorGUIUtility.PingObject(targetObject);
                    FocusOnObjectInSceneView(targetObject);
                }
            }

            private void FocusOnObjectInSceneView(GameObject targetObject)
            {
                try
                {
                    if (targetObject == null) return;

                    Selection.activeObject = targetObject;

                    var hierarchyWindow = EditorWindow.GetWindow<EditorWindow>("Hierarchy");
                    if (hierarchyWindow == null)
                    {
                        EditorApplication.ExecuteMenuItem("Window/General/Hierarchy");
                    }
                    else
                    {
                        hierarchyWindow.Focus();
                    }

                    EditorGUIUtility.PingObject(targetObject);

                    EditorApplication.delayCall += () =>
                    {
                        try
                        {
                            Selection.activeObject = targetObject;
                            EditorGUIUtility.PingObject(targetObject);

                            var hierarchy = EditorWindow.GetWindow<EditorWindow>("Hierarchy");
                            if (hierarchy != null)
                            {
                                hierarchy.Focus();
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"Error in FocusOnObjectInSceneView delayCall: {e.Message}");
                        }
                    };
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in FocusOnObjectInSceneView: {e.Message}");
                }
            }



            private string GetGameObjectPath(GameObject obj)
            {
                try
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
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in GetGameObjectPath: {e.Message}");
                    return "Error";
                }
            }

            private Transform FindObjectByPath(Transform[] transforms, string path)
            {
                try
                {
                    if (transforms == null || string.IsNullOrEmpty(path)) return null;

                    var pathParts = path.Split('/');
                    var current = transforms.FirstOrDefault(t => t.name == pathParts[0]);

                    for (int i = 1; i < pathParts.Length && current != null; i++)
                    {
                        current = current.GetComponentsInChildren<Transform>()
                            .FirstOrDefault(t => t.name == pathParts[i] && t.IsChildOf(current));
                    }

                    return current;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in FindObjectByPath: {e.Message}");
                    return null;
                }
            }

            private string DetermineObjectType(GameObject obj)
            {
                if (obj == null)
                {
                    return "Invalid GameObject";
                }

                var renderer = obj.GetComponent<Renderer>();
                var animator = obj.GetComponent<Animator>();
                var rigidbody = obj.GetComponent<Rigidbody>();
                var rigidbody2D = obj.GetComponent<Rigidbody2D>();

                if (rigidbody2D != null || obj.GetComponent<SpriteRenderer>() != null)
                {
                    return "2D Object";
                }
                else if (animator != null)
                {
                    return "Rigging Object";
                }
                else if (renderer != null)
                {
                    return "Static 3D Object";
                }
                else
                {
                    return "Empty GameObject";
                }
            }

            private void HandleDragAndDrop(Rect dropArea)
            {
                Event evt = Event.current;
                if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
                {
                    if (dropArea.Contains(evt.mousePosition))
                    {
                        bool isValidPrefab = false;
                        if (DragAndDrop.objectReferences.Length > 0)
                        {
                            var obj = DragAndDrop.objectReferences[0];
                            if (obj is GameObject gameObject)
                            {
                                bool isPrefab = UnityVersionHelper.IsPrefab(gameObject);
                                bool isModelPrefab = UnityVersionHelper.IsModelPrefab(gameObject);
                                isValidPrefab = isPrefab && !isModelPrefab;

                                if (isValidPrefab)
                                {
                                    var assetPath = AssetDatabase.GetAssetPath(gameObject);
                                    if (!string.IsNullOrEmpty(assetPath) && !assetPath.EndsWith(".prefab"))
                                    {
                                        isValidPrefab = false;
                                    }
                                }
                            }
                        }

                        if (isValidPrefab)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                            if (evt.type == EventType.DragPerform)
                            {
                                DragAndDrop.AcceptDrag();
                                if (DragAndDrop.objectReferences.Length > 0)
                                {
                                    var prefab = DragAndDrop.objectReferences[0] as GameObject;
                                    if (prefab != null)
                                    {
                                        SetPrefab(prefab);
                                        parentWindow.dragDropMessage = "✅ Prefab loaded successfully!";
                                        parentWindow.dragDropMessageTime = 2f;
                                    }
                                }
                            }
                        }
                        else
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;

                            if (evt.type == EventType.DragPerform)
                            {
                                if (DragAndDrop.objectReferences.Length > 0)
                                {
                                    var obj = DragAndDrop.objectReferences[0];
                                    if (obj is GameObject gameObject)
                                    {
                                        bool isModelPrefab = UnityVersionHelper.IsModelPrefab(gameObject);
                                        if (isModelPrefab)
                                        {
                                            parentWindow.dragDropMessage = "❌ FBX/Model files are not supported! Use .prefab files only.";
                                        }
                                        else
                                        {
                                            parentWindow.dragDropMessage = "❌ Only .prefab files are supported!";
                                        }
                                    }
                                    else
                                    {
                                        parentWindow.dragDropMessage = "❌ Only .prefab files are supported!";
                                    }
                                }
                                else
                                {
                                    parentWindow.dragDropMessage = "❌ Only .prefab files are supported!";
                                }
                                parentWindow.dragDropMessageTime = 3f;
                            }
                        }
                    }
                }
            }


            private void SetPrefab(GameObject prefab)
            {
                try
                {
                    parentWindow.selectedPrefab = prefab;
                    showAnalysisResults = false;
                    parentWindow.currentAnalysisResult = null;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in SetPrefab: {e.Message}");
                }
            }


            private PrefabAnalysisResult AnalyzePrefabDetailed(GameObject prefab)
            {
                try
                {
                    if (prefab == null)
                    {
                        Debug.LogError("AnalyzePrefabDetailed: prefab is null");
                        return new PrefabAnalysisResult
                        {
                            prefab = null,
                            prefabName = "Invalid Prefab",
                            prefabPath = "",
                            objectType = "Invalid",
                            componentCount = 0,
                            materialCount = 0,
                            shaderCount = 0,
                            uniqueShaders = new List<Shader>(),
                            scriptCount = 0,
                            uniqueScriptTypes = new List<System.Type>(),
                            animationCount = 0,
                            referenceCount = 0,
                            hasRenderer = false,
                            textureCount = 0,
                            meshInfo = new MeshInfo(),
                            boneIssues = new List<string>(),
                            bonePositions = new Dictionary<string, Vector3>(),
                            boneMap = new Dictionary<string, string[]>(),
                            materialInfo = new List<MaterialInfo>(),
                            scriptInfo = new List<ScriptInfo>(),
                            animationInfo = new List<AnimationInfo>(),
                            referenceInfo = new List<ReferenceInfo>(),
                            missingComponents = new List<string>(),
                            missingMaterials = new List<string>(),
                            missingScripts = new List<string>(),
                            missingPrefabs = new List<string>(),
                            componentInfo = new List<ComponentInfo>(),
                            referencedByPaths = new List<string>(),
                            referencedObjects = new List<ReferenceInfo>()
                        };
                    }

                    var result = new PrefabAnalysisResult
                    {
                        prefab = prefab,
                        prefabName = prefab.name,
                        prefabPath = AssetDatabase.GetAssetPath(prefab),
                        componentCount = 0,
                        materialCount = 0,
                        shaderCount = 0,
                        uniqueShaders = new List<Shader>(),
                        scriptCount = 0,
                        uniqueScriptTypes = new List<System.Type>(),
                        animationCount = 0,
                        referenceCount = 0,
                        hasRenderer = false,
                        textureCount = 0,
                        meshInfo = new MeshInfo(),
                        boneIssues = new List<string>(),
                        bonePositions = new Dictionary<string, Vector3>(),
                        boneMap = new Dictionary<string, string[]>(),
                        materialInfo = new List<MaterialInfo>(),
                        scriptInfo = new List<ScriptInfo>(),
                        animationInfo = new List<AnimationInfo>(),
                        referenceInfo = new List<ReferenceInfo>(),
                        missingComponents = new List<string>(),
                        missingMaterials = new List<string>(),
                        missingScripts = new List<string>(),
                        missingPrefabs = new List<string>(),
                        componentInfo = new List<ComponentInfo>(),
                        referencedByPaths = new List<string>(),
                        referencedObjects = new List<ReferenceInfo>()
                    };

                    result.objectType = DetermineObjectType(prefab);

                    AnalyzeAllComponents(prefab, result);

                    AnalyzeMissingPrefabs(prefab, result);

                    AnalyzeMissingMaterials(prefab, result);

                    AnalyzeAnimatorDetails(prefab, result);

                    AnalyzeReferences(prefab, result);

                    return result;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in AnalyzePrefabDetailed: {e.Message}");
                    return new PrefabAnalysisResult
                    {
                        prefab = prefab,
                        prefabName = prefab?.name ?? "Error",
                        prefabPath = prefab != null ? AssetDatabase.GetAssetPath(prefab) : "",
                        objectType = "Error",
                        componentCount = 0,
                        materialCount = 0,
                        shaderCount = 0,
                        uniqueShaders = new List<Shader>(),
                        scriptCount = 0,
                        uniqueScriptTypes = new List<System.Type>(),
                        animationCount = 0,
                        referenceCount = 0,
                        hasRenderer = false,
                        textureCount = 0,
                        meshInfo = new MeshInfo(),
                        boneIssues = new List<string>(),
                        bonePositions = new Dictionary<string, Vector3>(),
                        boneMap = new Dictionary<string, string[]>(),
                        materialInfo = new List<MaterialInfo>(),
                        scriptInfo = new List<ScriptInfo>(),
                        animationInfo = new List<AnimationInfo>(),
                        referenceInfo = new List<ReferenceInfo>(),
                        missingComponents = new List<string>(),
                        missingMaterials = new List<string>(),
                        missingScripts = new List<string>(),
                        missingPrefabs = new List<string>(),
                        componentInfo = new List<ComponentInfo>(),
                        referencedByPaths = new List<string>(),
                        referencedObjects = new List<ReferenceInfo>()
                    };
                }
            }

            private void AnalyzeAllComponents(GameObject prefab, PrefabAnalysisResult result)
            {
                try
                {
                    if (prefab == null || result == null) return;

                    var allComponents = prefab.GetComponentsInChildren<Component>();
                    result.componentCount = allComponents.Length;

                    var materials = new HashSet<Material>();
                    var shaders = new HashSet<Shader>();
                    var textures = new HashSet<Texture>();

                    AnalyzeGameObjectComponentsRecursive(prefab, result, materials, shaders, textures);

                    foreach (var component in allComponents)
                    {
                        if (component == null)
                        {

                            continue;
                        }

                        var componentInfo = new ComponentInfo
                        {
                            component = component,
                            componentType = component.GetType(),
                            isMissing = false
                        };
                        result.componentInfo.Add(componentInfo);

                        if (component is Renderer renderer)
                        {
                            result.hasRenderer = true;
                            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                            {
                                var material = renderer.sharedMaterials[i];
                                if (material == null)
                                {
                                    result.missingMaterials.Add($"Missing Material on {renderer.name} (Slot {i})");
                                }
                                else if (IsErrorMaterial(material))
                                {
                                    result.missingMaterials.Add($"Error Shader Material on {renderer.name} (Slot {i}): {material.name}");
                                }
                                else
                                {
                                    materials.Add(material);
                                    if (material.shader != null)
                                    {
                                        shaders.Add(material.shader);

                                        var shader = material.shader;
                                        for (int j = 0; j < ShaderUtil.GetPropertyCount(shader); j++)
                                        {
                                            if (ShaderUtil.GetPropertyType(shader, j) == ShaderUtil.ShaderPropertyType.TexEnv)
                                            {
                                                var texture = material.GetTexture(ShaderUtil.GetPropertyName(shader, j));
                                                if (texture != null)
                                                {
                                                    textures.Add(texture);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if (component is MeshFilter meshFilter && meshFilter.sharedMesh != null)
                        {
                            var mesh = meshFilter.sharedMesh;
                            result.meshInfo = new MeshInfo
                            {
                                vertexCount = mesh.vertexCount,
                                triangleCount = mesh.triangles.Length / 3,
                                hasNormals = mesh.normals != null && mesh.normals.Length > 0,
                                hasUVs = mesh.uv != null && mesh.uv.Length > 0,
                                hasColors = mesh.colors != null && mesh.colors.Length > 0
                            };
                        }
                        else if (component is Animator animator && animator.runtimeAnimatorController != null)
                        {
                            var animInfo = new AnimationInfo
                            {
                                animator = animator,
                                controller = animator.runtimeAnimatorController,
                                animationCount = animator.runtimeAnimatorController.animationClips.Length
                            };
                            result.animationInfo.Add(animInfo);
                            result.animationCount += animInfo.animationCount;
                        }
                        else if (component is Animation animation)
                        {
                            int clipCount = 0;
                            foreach (AnimationState state in animation)
                            {
                                clipCount++;
                            }
                            var animInfo = new AnimationInfo
                            {
                                animation = animation,
                                animationCount = clipCount
                            };
                            result.animationInfo.Add(animInfo);
                            result.animationCount += clipCount;
                        }
                        else if (component is AudioSource audioSource)
                        {
                            if (audioSource.clip == null)
                            {
                                result.missingComponents.Add($"Missing AudioClip on AudioSource '{audioSource.name}'");
                            }
                            else
                            {
                            }
                        }
                        else if (component is Light light)
                        {
                        }
                        else if (component is ParticleSystem particleSystem)
                        {
                            var psRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
                            if (psRenderer != null)
                            {
                                for (int i = 0; i < psRenderer.sharedMaterials.Length; i++)
                                {
                                    var material = psRenderer.sharedMaterials[i];
                                    if (material != null && !IsErrorMaterial(material))
                                    {
                                        materials.Add(material);
                                        if (material.shader != null)
                                        {
                                            shaders.Add(material.shader);
                                        }
                                    }
                                }
                            }
                        }
                    
                        else if (component.GetType().Namespace == "UnityEngine.UI")
                        {
                            AnalyzeUIComponent(component, result, materials, shaders, textures);
                        }
                        else if (component is Canvas canvas)
                        {
                        }
                        else if (component is Collider collider)
                        {
                        }
                        else if (component is Rigidbody rigidbody)
                        {
                        }

                        if (component is MonoBehaviour monoBehaviour)
                        {
                            if (monoBehaviour != null)
                            {
                                var scriptType = monoBehaviour.GetType();
                                if (!result.uniqueScriptTypes.Contains(scriptType))
                                {
                                    result.uniqueScriptTypes.Add(scriptType);
                                }
                            }
                        }
                    }

                    result.materialCount = materials.Count;
                    result.shaderCount = shaders.Count;
                    result.uniqueShaders = shaders.ToList();
                    result.textureCount = textures.Count;
                    result.scriptCount = result.uniqueScriptTypes.Count;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in AnalyzeAllComponents: {e.Message}");
                }
            }

            private void AnalyzeGameObjectComponentsRecursive(GameObject obj, PrefabAnalysisResult result, HashSet<Material> materials, HashSet<Shader> shaders, HashSet<Texture> textures)
            {
                if (obj == null) return;

                Component[] components = obj.GetComponents<Component>();

                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        result.missingScripts.Add($"Missing Script on '{obj.name}' (Component index {i})");
                    }
                }

                foreach (Transform child in obj.transform)
                {
                    AnalyzeGameObjectComponentsRecursive(child.gameObject, result, materials, shaders, textures);
                }
            }

            private void AnalyzeUIComponent(Component component, PrefabAnalysisResult result, HashSet<Material> materials, HashSet<Shader> shaders, HashSet<Texture> textures)
            {
                try
                {
                    var componentType = component.GetType();

                    if (componentType.Name == "Image")
                    {
                        var spriteProperty = componentType.GetProperty("sprite");
                        var materialProperty = componentType.GetProperty("material");

                        if (spriteProperty != null)
                        {
                            var sprite = spriteProperty.GetValue(component) as UnityEngine.Sprite;
                            if (sprite == null)
                            {
                                result.missingComponents.Add($"Missing Sprite on UI.Image '{component.name}'");
                            }
                            else if (sprite.texture != null)
                            {
                                textures.Add(sprite.texture);
                            }
                        }

                        if (materialProperty != null)
                        {
                            var material = materialProperty.GetValue(component) as Material;
                            if (material != null && !IsErrorMaterial(material))
                            {
                                materials.Add(material);
                                if (material.shader != null)
                                {
                                    shaders.Add(material.shader);
                                }
                            }
                        }
                    }
                    else if (componentType.Name == "RawImage")
                    {
                        var textureProperty = componentType.GetProperty("texture");
                        var materialProperty = componentType.GetProperty("material");

                        if (textureProperty != null)
                        {
                            var texture = textureProperty.GetValue(component) as UnityEngine.Texture;
                            if (texture == null)
                            {
                                result.missingComponents.Add($"Missing Texture on UI.RawImage '{component.name}'");
                            }
                            else
                            {
                                textures.Add(texture);
                            }
                        }

                        if (materialProperty != null)
                        {
                            var material = materialProperty.GetValue(component) as Material;
                            if (material != null && !IsErrorMaterial(material))
                            {
                                materials.Add(material);
                                if (material.shader != null)
                                {
                                    shaders.Add(material.shader);
                                }
                            }
                        }
                    }
                    else if (componentType.Name == "Text")
                    {
                        var fontProperty = componentType.GetProperty("font");
                        var textProperty = componentType.GetProperty("text");

                        if (fontProperty != null)
                        {
                            var font = fontProperty.GetValue(component) as UnityEngine.Font;
                            if (font == null)
                            {
                                result.missingComponents.Add($"Missing Font on UI.Text '{component.name}'");
                            }
                            else
                            {
                            }
                        }

                        if (textProperty != null)
                        {
                            var text = textProperty.GetValue(component) as string;
                            if (string.IsNullOrEmpty(text))
                            {
                            }
                        }
                    }
                    else if (componentType.Name == "Button")
                    {
                    }
                }
                catch (System.Exception e)
                {
                }
            }

            private void AnalyzeMissingPrefabs(GameObject prefab, PrefabAnalysisResult result)
            {
                try
                {
                    if (prefab == null || result == null) return;


                    // 프리팹 자체와 모든 자식을 재귀적으로 검사
                    CheckGameObjectForMissingPrefabsRecursive(prefab, result);

                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in AnalyzeMissingPrefabs: {e.Message}");
                }
            }

            private void CheckGameObjectForMissingPrefabsRecursive(GameObject obj, PrefabAnalysisResult result)
            {
                if (obj == null) return;

                if (PrefabUtility.IsPrefabAssetMissing(obj))
                {
                    result.missingPrefabs.Add($"Missing Prefab Asset on '{obj.name}'");
                }

                if (PrefabUtility.IsPartOfPrefabInstance(obj))
                {
                    var prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                    if (prefabSource == null)
                    {
                        result.missingPrefabs.Add($"Broken Prefab Instance on '{obj.name}'");
                    }
                }

                if (PrefabUtility.IsPartOfPrefabInstance(obj) && PrefabUtility.IsOutermostPrefabInstanceRoot(obj))
                {
                    var outerPrefab = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                    if (outerPrefab == null)
                    {
                        result.missingPrefabs.Add($"Missing Nested Prefab on '{obj.name}'");
                    }
                }

                foreach (Transform child in obj.transform)
                {
                    CheckGameObjectForMissingPrefabsRecursive(child.gameObject, result);
                }
            }

            private void AnalyzeAnimatorDetails(GameObject prefab, PrefabAnalysisResult result)
            {
                try
                {
                    if (prefab == null || result == null) return;

                    var animator = prefab.GetComponentInChildren<Animator>();
                    if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
                    {
                        return;
                    }

                    var avatar = animator.avatar;
                    var humanDescription = avatar.humanDescription;
                    var humanBones = humanDescription.human;

                    var boneMap = new Dictionary<string, string[]>
                {
                    {"Head", new[] {"Head"}},
                    {"Neck", new[] {"Neck"}},
                    {"Shoulders", new[] {"LeftShoulder", "RightShoulder"}},
                    {"Arms", new[] {"LeftUpperArm", "RightUpperArm", "LeftLowerArm", "RightLowerArm"}},
                    {"Hands", new[] {"LeftHand", "RightHand"}},
                    {"Chest", new[] {"Chest"}},
                    {"Spine", new[] {"Spine"}},
                    {"Hips", new[] {"Hips"}},
                    {"Legs", new[] {"LeftUpperLeg", "RightUpperLeg", "LeftLowerLeg", "RightLowerLeg"}},
                    {"Feet", new[] {"LeftFoot", "RightFoot"}}
                };

                    result.boneMap = boneMap;

                    var bonePositions = new Dictionary<string, Vector3>();
                    var boneIssues = new List<string>();

                    foreach (var humanBone in humanBones)
                    {
                        var boneName = humanBone.humanName;

                        if (System.Enum.TryParse<HumanBodyBones>(boneName, out HumanBodyBones bodyBone))
                        {
                            var boneTransform = animator.GetBoneTransform(bodyBone);

                            if (boneTransform != null)
                            {
                                var position = boneTransform.localPosition;
                                var rotation = boneTransform.localRotation;
                                var scale = boneTransform.localScale;

                                bonePositions[boneName] = position;

                                var issue = CheckBonePositionIssues(boneName, position, rotation, scale, boneMap);
                                if (!string.IsNullOrEmpty(issue))
                                {
                                    boneIssues.Add($"{boneName}: {issue}");
                                }
                            }
                        }
                    }

                    result.bonePositions = bonePositions;
                    result.boneIssues = boneIssues;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in AnalyzeAnimatorDetails: {e.Message}");
                }
            }

            private void AnalyzeReferences(GameObject prefab, PrefabAnalysisResult result)
            {
                try
                {
                    if (prefab == null || result == null) return;

                    var prefabPath = AssetDatabase.GetAssetPath(prefab);

                    FindUsageInScenes(prefab, result);

                    FindUsageInPrefabs(prefab, prefabPath, result);

                    var allComponents = prefab.GetComponentsInChildren<Component>();
                    foreach (var component in allComponents)
                    {
                        if (component == null) continue;

                        var serializedObject = new SerializedObject(component);
                        var iterator = serializedObject.GetIterator();

                        while (iterator.NextVisible(true))
                        {
                            if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue != null)
                            {
                                var refInfo = new ReferenceInfo
                                {
                                    reference = iterator.objectReferenceValue,
                                    referenceType = iterator.objectReferenceValue.GetType().Name,
                                    referencePath = AssetDatabase.GetAssetPath(iterator.objectReferenceValue)
                                };
                                result.referencedObjects.Add(refInfo);
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in AnalyzeReferences: {e.Message}");
                }
            }

            private void FindUsageInScenes(GameObject prefab, PrefabAnalysisResult result)
            {
                try
                {
                    if (prefab == null || result == null) return;
                    var prefabPath = AssetDatabase.GetAssetPath(prefab);

                    var sceneGuids = AssetDatabase.FindAssets("t:Scene");

                    foreach (var guid in sceneGuids)
                    {
                        var scenePath = AssetDatabase.GUIDToAssetPath(guid);

                        if (scenePath.StartsWith("Packages/"))
                        {
                            continue;
                        }

                        try
                        {
                            var dependencies = AssetDatabase.GetDependencies(scenePath, true);
                            if (dependencies.Contains(prefabPath))
                            {
                                var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                                result.referencedByPaths.Add($"Scene: {sceneName} ({scenePath})");
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"Could not check scene dependencies {scenePath}: {e.Message}");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in FindUsageInScenes: {e.Message}");
                }
            }

            private void FindUsageInPrefabs(GameObject prefab, string prefabPath, PrefabAnalysisResult result)
            {
                try
                {
                    if (prefab == null || result == null) return;

                    var prefabGuids = AssetDatabase.FindAssets("t:Prefab");

                    foreach (var guid in prefabGuids)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        if (path == prefabPath) continue;

                        var otherPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (otherPrefab != null)
                        {
                            CheckGameObjectForPrefabUsage(otherPrefab, prefab, "Prefab", path, result);
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in FindUsageInPrefabs: {e.Message}");
                }
            }

            private void CheckGameObjectForPrefabUsage(GameObject obj, GameObject targetPrefab, string locationName, string locationPath, PrefabAnalysisResult result)
            {
                try
                {
                    if (obj == null || targetPrefab == null || result == null) return;

                    var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                    if (prefabAsset == targetPrefab)
                    {
                        result.referencedByPaths.Add($"{locationName}: {obj.name} ({locationPath})");
                    }

                    foreach (Transform child in obj.transform)
                    {
                        CheckGameObjectForPrefabUsage(child.gameObject, targetPrefab, locationName, locationPath, result);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in CheckGameObjectForPrefabUsage: {e.Message}");
                }
            }




            private void GoToUsageLocation(string usageInfo)
            {
                try
                {
                    if (string.IsNullOrEmpty(usageInfo)) return;

                    var pathStart = usageInfo.LastIndexOf('(');
                    var pathEnd = usageInfo.LastIndexOf(')');

                    if (pathStart > 0 && pathEnd > pathStart)
                    {
                        var path = usageInfo.Substring(pathStart + 1, pathEnd - pathStart - 1);

                        if (path.EndsWith(".unity"))
                        {
                            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                            if (scene.IsValid())
                            {
                                Debug.Log($"Opened scene: {scene.name}");
                            }
                        }
                        else if (path.EndsWith(".prefab"))
                        {
                            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                            if (prefab != null)
                            {
                                Selection.activeObject = prefab;
                                EditorGUIUtility.PingObject(prefab);
                                AssetDatabase.OpenAsset(prefab);
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to go to usage location: {e.Message}");
                }
            }

            private GameObject FindGameObjectByName(GameObject[] rootObjects, string name)
            {
                try
                {
                    if (rootObjects == null || string.IsNullOrEmpty(name)) return null;

                    foreach (var rootObj in rootObjects)
                    {
                        var found = FindGameObjectByNameRecursive(rootObj, name);
                        if (found != null) return found;
                    }
                    return null;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in FindGameObjectByName: {e.Message}");
                    return null;
                }
            }

            private GameObject FindGameObjectByNameRecursive(GameObject obj, string name)
            {
                try
                {
                    if (obj == null || string.IsNullOrEmpty(name)) return null;

                    if (obj.name == name) return obj;

                    foreach (Transform child in obj.transform)
                    {
                        var found = FindGameObjectByNameRecursive(child.gameObject, name);
                        if (found != null) return found;
                    }
                    return null;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in FindGameObjectByNameRecursive: {e.Message}");
                    return null;
                }
            }

            private void GoToReferencedPrefab(string prefabPath)
            {
                try
                {
                    if (string.IsNullOrEmpty(prefabPath)) return;

                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab != null)
                    {
                        Selection.activeObject = prefab;
                        EditorGUIUtility.PingObject(prefab);

                        AssetDatabase.OpenAsset(prefab);
                    }
                    else
                    {
                        Debug.LogWarning($"Could not load prefab at path: {prefabPath}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to go to referenced prefab: {e.Message}");
                }
            }

            private void GoToReferencedObject(ReferenceInfo refInfo)
            {
                try
                {
                    if (refInfo == null || refInfo.reference == null) return;

                    if (refInfo.reference != null)
                    {
                        Selection.activeObject = refInfo.reference;
                        EditorGUIUtility.PingObject(refInfo.reference);

                        if (refInfo.reference is GameObject gameObj)
                        {
                            FocusOnObjectInSceneView(gameObj);
                        }
                        else
                        {
                            var assetPath = AssetDatabase.GetAssetPath(refInfo.reference);
                            if (!string.IsNullOrEmpty(assetPath))
                            {
                                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                                if (asset != null)
                                {
                                    Selection.activeObject = asset;
                                    EditorGUIUtility.PingObject(asset);
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Referenced object is null: {refInfo.referenceType}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to go to referenced object: {e.Message}");
                }
            }

            private void OpenAvatarConfigureWindow(Animator animator)
            {
                try
                {
                    if (animator != null && animator.avatar != null)
                    {
                        Selection.activeObject = animator.avatar;
                        EditorGUIUtility.PingObject(animator.avatar);

                        EditorApplication.ExecuteMenuItem("Window/Animation/Avatar Setup");
                        Selection.activeObject = animator.gameObject;
                        EditorGUIUtility.PingObject(animator.gameObject);

                        EditorApplication.delayCall += () =>
                        {
                            try
                            {
                                var avatarSetupWindow = EditorWindow.focusedWindow;
                                if (avatarSetupWindow != null && avatarSetupWindow.GetType().Name.Contains("Avatar"))
                                {
                                    Debug.Log("Avatar Setup 창이 열렸습니다. 뼈 매핑을 확인하세요.");
                                }
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogWarning($"Avatar Setup 창에서 뼈 매핑 표시 실패: {ex.Message}");
                            }
                        };
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to open Avatar Configure window: {e.Message}");

                    if (animator != null && animator.avatar != null)
                    {
                        Selection.activeObject = animator.avatar;
                        EditorGUIUtility.PingObject(animator.avatar);
                    }
                }
            }


            private string CheckBonePositionIssues(string humanBoneName, Vector3 position, Quaternion rotation, Vector3 scale, Dictionary<string, string[]> boneMap)
            {
                try
                {
                    if (string.IsNullOrEmpty(humanBoneName) || boneMap == null) return null;

                    var issues = new List<string>();

                    var centerBones = new HashSet<string> { "Head", "Neck", "Chest", "Spine", "Hips" };

                    if (centerBones.Contains(humanBoneName))
                    {
                        if (Mathf.Abs(position.x) > 0.1f)
                        {
                            issues.Add($"Center bone X offset: {position.x:F3}");
                        }
                    }

                    if (Mathf.Abs(position.x) > 10f || Mathf.Abs(position.y) > 10f || Mathf.Abs(position.z) > 10f)
                    {
                        issues.Add($"Extreme position: {position}");
                    }

                    if (Mathf.Abs(scale.x - 1f) > 0.5f || Mathf.Abs(scale.y - 1f) > 0.5f || Mathf.Abs(scale.z - 1f) > 0.5f)
                    {
                        issues.Add($"Extreme scale: {scale}");
                    }

                    var eulerAngles = rotation.eulerAngles;
                    if (Mathf.Abs(eulerAngles.y) > 180f)
                    {
                        issues.Add($"Extreme Y rotation: {eulerAngles.y:F1}°");
                    }

                    return issues.Count > 0 ? string.Join(", ", issues) : null;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in CheckBonePositionIssues: {e.Message}");
                    return null;
                }
            }
        }

        public enum AnalysisMode
        {
            DependencyAnalysis,
            PerformanceAnalysis,
            UsageAnalysis
        }

        [System.Serializable]
        public class PrefabAnalysisResult
        {
            public GameObject prefab;
            public string prefabName;
            public string prefabPath;
            public string objectType;
            public int componentCount;
            public int materialCount;
            public int shaderCount;
            public List<Shader> uniqueShaders;
            public int scriptCount;
            public List<System.Type> uniqueScriptTypes;
            public int animationCount;
            public int referenceCount;
            public bool hasRenderer;
            public int textureCount;
            public MeshInfo meshInfo;
            public List<string> boneIssues;
            public Dictionary<string, Vector3> bonePositions;
            public Dictionary<string, string[]> boneMap;
            public List<MaterialInfo> materialInfo;
            public List<ScriptInfo> scriptInfo;
            public List<AnimationInfo> animationInfo;
            public List<ReferenceInfo> referenceInfo;
            public List<string> missingComponents;
            public List<string> missingMaterials;
            public List<string> missingScripts;
            public List<string> missingPrefabs;
            public List<ComponentInfo> componentInfo;
            public List<string> referencedByPaths;
            public List<ReferenceInfo> referencedObjects;
        }
        [System.Serializable]
        public class MeshInfo
        {
            public int vertexCount;
            public int triangleCount;
            public bool hasNormals;
            public bool hasUVs;
            public bool hasColors;
        }

        [System.Serializable]
        public class MaterialInfo
        {
            public Material material;
            public Shader shader;
            public int textureCount;
        }

        [System.Serializable]
        public class ScriptInfo
        {
            public MonoBehaviour script;
            public System.Type scriptType;
            public bool isEnabled;
        }

        [System.Serializable]
        public class AnimationInfo
        {
            public Animator animator;
            public Animation animation;
            public RuntimeAnimatorController controller;
            public int animationCount;
        }

        [System.Serializable]
        public class ReferenceInfo
        {
            public UnityEngine.Object reference;
            public string referenceType;
            public string referencePath;
        }

        [System.Serializable]
        public class ComponentInfo
        {
            public Component component;
            public System.Type componentType;
            public bool isMissing;
        }
}
