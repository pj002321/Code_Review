using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Hunt
{
    /// <summary>
    /// UI 시스템 통합 관리자 - 각 서브시스템 조율
    /// </summary>
    public class UIManager : MonoBehaviourSingleton<UIManager>
    {
        private UILayerManager layerManager;
        private UIGraphExecutor graphExecutor;
        private UIEventManager eventManager;
        private UIGraphTargetRegistry targetRegistry;

        protected override bool DontDestroy => base.DontDestroy;

        protected override void Awake()
        {
            base.Awake();
            InitializeSubsystems();
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            RegisterAllSystemsInScene();
        }

        private void InitializeSubsystems()
        {
            layerManager = new UILayerManager();
            targetRegistry = new UIGraphTargetRegistry();
            graphExecutor = new UIGraphExecutor(layerManager, targetRegistry);
            eventManager = new UIEventManager(layerManager, graphExecutor);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RegisterAllSystemsInScene();
        }

        private void OnSceneUnloaded(Scene scene)
        {
            eventManager.UnregisterBakedEventsInScene(scene);
        }

        private void RegisterAllSystemsInScene()
        {
            targetRegistry.RegisterAllGraphTargetsInScene();
            layerManager.RegisterAllLayerGroupsInScene();
            eventManager.RegisterAllBakedEventsInScene();
        }

        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            eventManager?.Clear();
            targetRegistry?.Clear();
            layerManager?.Clear();
            base.OnDestroy();
        }

        private void Update()
        {
            eventManager?.Update();
        }

        #region Layer Management (Facade)
        public void RegisterGroup(UILayerGroup group) => layerManager?.RegisterGroup(group);
        public void UnregisterGroup(UILayerGroup group) => layerManager?.UnregisterGroup(group);
        public void HideLayer(UILayer layer) => layerManager?.HideLayer(layer);
        public void ShowLayer(UILayer layer) => layerManager?.ShowLayer(layer);
        public void HideLayers(params UILayer[] layers) => layerManager?.HideLayers(layers);
        public void ShowLayers(params UILayer[] layers) => layerManager?.ShowLayers(layers);
        public void ToggleLayers(params UILayer[] layers) => layerManager?.ToggleLayers(layers);
        #endregion

        #region GameObject Management
        public void HideGameObjects(params GameObject[] targets)
        {
            if (targets == null) return;
            foreach (var target in targets)
            {
                if (target != null)
                {
                    target.SetActive(false);
                }
            }
        }

        public void ShowGameObjects(params GameObject[] targets)
        {
            if (targets == null) return;
            foreach (var target in targets)
            {
                if (target != null)
                {
                    target.SetActive(true);
                }
            }
        }

        public void ToggleGameObjects(params GameObject[] targets)
        {
            if (targets == null) return;
            foreach (var target in targets)
            {
                if (target != null)
                {
                    target.SetActive(!target.activeSelf);
                }
            }
        }
        #endregion

        #region GraphTarget Management (Facade)
        public void RegisterGraphTarget(UIGraphTarget target) => targetRegistry?.RegisterGraphTarget(target);
        public void UnregisterGraphTarget(string targetId) => targetRegistry?.UnregisterGraphTarget(targetId);
        public GameObject FindGameObjectById(string targetId) => targetRegistry?.FindGameObjectById(targetId);
        #endregion

        #region Graph Execution (Facade)
        public async UniTask ExecuteGraph(UINodeGraph graph)
        {
            if (graphExecutor != null)
            {
                await graphExecutor.ExecuteGraph(graph);
            }
        }

        public async UniTask ExecuteGraphFromNode(UINodeGraph graph, string startNodeGuid)
        {
            if (graphExecutor != null)
            {
                await graphExecutor.ExecuteGraphFromNode(graph, startNodeGuid);
            }
        }
        #endregion

        #region Event Management (Facade)
        public void RegisterInputEvent(InputAction inputAction, UILayer targetLayer) 
            => eventManager?.RegisterInputEvent(inputAction, targetLayer);
        
        public void UnregisterInputEvent(InputAction inputAction) 
            => eventManager?.UnregisterInputEvent(inputAction);
        
        public void RegisterKeyboardEvent(UIGraphBakedKeyboardEvent keyboardEvent) 
            => eventManager?.RegisterKeyboardEvent(keyboardEvent);
        
        public void UnregisterKeyboardEvent(UIGraphBakedKeyboardEvent keyboardEvent) 
            => eventManager?.UnregisterKeyboardEvent(keyboardEvent);
        #endregion
    }
}
