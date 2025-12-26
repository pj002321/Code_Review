using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TerrainUtils;
using System;
using System.Collections.Generic;

namespace Kirist.EditorTool
{
    public partial class KiristWindow
    {
        [System.Serializable]
        public class SceneAnalysisResult
        {
            public string sceneName;
            public string scenePath;
            public DateTime analysisTime;

            [System.NonSerialized]
            public Texture2D sceneSnapshot;

            public int totalObjects;
            public int activeObjects;
            public int totalComponents;
            public int totalScripts;
            public int totalRenderers;
            public int totalMaterials;
            public int errorCount;

            public int highSeverityErrors;
            public int mediumSeverityErrors;
            public int lowSeverityErrors;

            public List<GameObjectInfo> gameObjects;
            public Dictionary<string, int> componentTypes;
            public EnvironmentInfo environmentInfo;
            public List<SceneError> errors;

            public SceneAnalysisResult()
            {
                analysisTime = DateTime.Now;
                gameObjects = new List<GameObjectInfo>();
                componentTypes = new Dictionary<string, int>();
                environmentInfo = new EnvironmentInfo();
                errors = new List<SceneError>();
            }
        }
        
        [System.Serializable]
        public class GameObjectInfo
        {
            public GameObject gameObject;
            public string name;
            public bool isActive;
            public int layer;
            public string tag;
            public int childCount;
            public List<ComponentInfo> components;
        }
        
        
        [System.Serializable]
        public class EnvironmentInfo
        {
            // Lighting
            public int lightCount;
            public List<LightInfo> lights;
            public int lightmapCount;
            public string reflectionMode;
            
            // Cameras
            public int cameraCount;
            public List<CameraInfo> cameras;
            
            // Terrain
            public int terrainCount;
            public List<TerrainInfo> terrains;
            
            // Post Processing
            public int postProcessingVolumeCount;
            public List<PostProcessingInfo> postProcessingVolumes;
        }
        
        [System.Serializable]
        public class LightInfo
        {
            public Light light;
            public LightType type;
            public float intensity;
            public float range;
            public Color color;
            public bool isActive;
        }
        
        [System.Serializable]
        public class CameraInfo
        {
            public Camera camera;
            public float fieldOfView;
            public float nearClipPlane;
            public float farClipPlane;
            public CameraClearFlags clearFlags;
            public Color backgroundColor;
            public bool isActive;
        }
        
        [System.Serializable]
        public class TerrainInfo
        {
            public Terrain terrain;
            public TerrainData terrainData;
            public int heightmapResolution;
            public int detailResolution;
            public int alphamapResolution;
            public bool isActive;
        }
        
        [System.Serializable]
        public class PostProcessingInfo
        {
            public Volume volume;
            public bool isGlobal;
            public float priority;
            public float blendDistance;
            public float weight;
            public bool isActive;
            
            public ScriptableObject profile;
            public int settingsCount;
            public List<string> activeSettings;
            public List<string> inactiveSettings;
        }
        
        [System.Serializable]
        public class SceneError
        {
            public SceneErrorType type;
            public SceneErrorSeverity severity;
            public GameObject gameObject;
            public Component component;
            public string message;
            public int componentIndex = -1;
            public int materialIndex = -1;
            
            public string gameObjectName;
            public string gameObjectPath;
            public string componentTypeName;
        }
        
        public enum SceneErrorType
        {
            MissingScript,
            MissingMaterial,
            MissingPrefab,
            MissingPrefabAsset,
            BrokenPrefabConnection,
            PrefabInstanceIssue,
            PerformanceIssue,
            LightingIssue,
            CameraIssue,
            TerrainIssue,
            PostProcessingIssue,
            ErrorShader
        }
        
        public enum SceneErrorSeverity
        {
            Low,
            Medium,
            High,
            Critical
        }
        
        public enum SceneAnalysisMode
        {
            CurrentScene,
            SpecificScene
        }
    }
}
