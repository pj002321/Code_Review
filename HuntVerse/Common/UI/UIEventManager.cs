using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hunt
{
    /// <summary>
    /// UI 이벤트 관리 (Keyboard, Button, Input Action)
    /// </summary>
    public class UIEventManager
    {
        private readonly UILayerManager layerManager;
        private readonly UIGraphExecutor graphExecutor;
        
        private Dictionary<InputAction, UILayer> inputLayerMap = new Dictionary<InputAction, UILayer>();
        private Dictionary<KeyCode, List<UIGraphBakedKeyboardEvent>> keyboardEventMap = new Dictionary<KeyCode, List<UIGraphBakedKeyboardEvent>>();

        public UIEventManager(UILayerManager layerManager, UIGraphExecutor graphExecutor)
        {
            this.layerManager = layerManager;
            this.graphExecutor = graphExecutor;
        }

        #region Input Action Events
        public void RegisterInputEvent(InputAction inputAction, UILayer targetLayer)
        {
            if (inputAction == null) return;

            if(inputLayerMap.ContainsKey(inputAction))
            {
                inputAction.performed -= OnInputPerformed;
            }

            inputLayerMap[inputAction] = targetLayer;
            inputAction.performed += OnInputPerformed;
        }

        public void UnregisterInputEvent(InputAction inputAction)
        {
            if(inputAction == null || !inputLayerMap.ContainsKey(inputAction)) return;

            inputAction.performed -= OnInputPerformed;
            inputLayerMap.Remove(inputAction);
        }

        private void OnInputPerformed(InputAction.CallbackContext context)
        {
            if (!inputLayerMap.TryGetValue(context.action, out var targetLayer)) return;
            layerManager.HideLayer(targetLayer);
        }

        public void UnregisterAllInputEvents()
        {
            foreach(var kvp in inputLayerMap)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.performed -= OnInputPerformed;
                }
            }

            inputLayerMap.Clear();
        }
        #endregion

        #region Keyboard Events
        public void RegisterKeyboardEvent(UIGraphBakedKeyboardEvent keyboardEvent)
        {
            if (keyboardEvent == null || keyboardEvent.targetKeyCode == KeyCode.None) return;
            
            if (!keyboardEventMap.ContainsKey(keyboardEvent.targetKeyCode))
            {
                keyboardEventMap[keyboardEvent.targetKeyCode] = new List<UIGraphBakedKeyboardEvent>();
            }
            
            if (!keyboardEventMap[keyboardEvent.targetKeyCode].Contains(keyboardEvent))
            {
                keyboardEventMap[keyboardEvent.targetKeyCode].Add(keyboardEvent);
            }
        }

        public void UnregisterKeyboardEvent(UIGraphBakedKeyboardEvent keyboardEvent)
        {
            if (keyboardEvent == null || keyboardEvent.targetKeyCode == KeyCode.None) return;
            
            if (keyboardEventMap.TryGetValue(keyboardEvent.targetKeyCode, out var events))
            {
                events.Remove(keyboardEvent);
                if (events.Count == 0)
                {
                    keyboardEventMap.Remove(keyboardEvent.targetKeyCode);
                }
            }
        }

        public void RegisterAllKeyboardEventsInScene()
        {
            var allKeyboardEvents = Object.FindObjectsOfType<UIGraphBakedKeyboardEvent>(true);
            foreach (var keyboardEvent in allKeyboardEvents)
            {
                if (keyboardEvent != null && keyboardEvent.targetKeyCode != KeyCode.None)
                {
                    keyboardEvent.RegisterToManager();
                }
            }
        }

        public void UnregisterAllKeyboardEvents()
        {
            keyboardEventMap.Clear();
        }
        #endregion

        #region Button Events
        public void RegisterAllButtonEventsInScene()
        {
            var allButtonEvents = Object.FindObjectsOfType<UIGraphBakedEvent>(true);
            foreach (var buttonEvent in allButtonEvents)
            {
                if (buttonEvent != null && buttonEvent.graph != null && !string.IsNullOrEmpty(buttonEvent.startNodeGuid))
                {
                    buttonEvent.RegisterToButton();
                }
            }
        }

        public void UnregisterAllButtonEvents()
        {
            var allButtonEvents = Object.FindObjectsOfType<UIGraphBakedEvent>(true);
            foreach (var buttonEvent in allButtonEvents)
            {
                if (buttonEvent != null)
                {
                    buttonEvent.UnregisterFromButton();
                }
            }
        }
        #endregion

        #region Scene Event Registration
        public void RegisterAllBakedEventsInScene()
        {
            RegisterAllKeyboardEventsInScene();
            RegisterAllButtonEventsInScene();
        }

        public void UnregisterAllBakedEvents()
        {
            UnregisterAllKeyboardEvents();
            UnregisterAllButtonEvents();
        }

        public void UnregisterBakedEventsInScene(UnityEngine.SceneManagement.Scene scene)
        {
            var rootObjects = scene.GetRootGameObjects();
            foreach (var rootObj in rootObjects)
            {
                UnregisterBakedEventsRecursive(rootObj);
            }
        }

        private void UnregisterBakedEventsRecursive(GameObject obj)
        {
            var keyboardEvent = obj.GetComponent<UIGraphBakedKeyboardEvent>();
            if (keyboardEvent != null)
            {
                UnregisterKeyboardEvent(keyboardEvent);
            }
            
            var buttonEvent = obj.GetComponent<UIGraphBakedEvent>();
            if (buttonEvent != null)
            {
                buttonEvent.UnregisterFromButton();
            }
            
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                UnregisterBakedEventsRecursive(obj.transform.GetChild(i).gameObject);
            }
        }
        #endregion

        #region Update
        /// <summary>
        /// 매 프레임 키보드 입력 감지
        /// </summary>
        public void Update()
        {
            foreach (var kvp in keyboardEventMap)
            {
                if (Input.GetKeyDown(kvp.Key))
                {
                    foreach (var keyboardEvent in kvp.Value)
                    {
                        if (keyboardEvent != null && keyboardEvent.gameObject != null && keyboardEvent.gameObject.activeInHierarchy && 
                            keyboardEvent.graph != null && !string.IsNullOrEmpty(keyboardEvent.startNodeGuid))
                        {
                            graphExecutor.ExecuteGraphFromNode(keyboardEvent.graph, keyboardEvent.startNodeGuid).Forget();
                        }
                    }
                }
            }
        }
        #endregion

        public void Clear()
        {
            UnregisterAllInputEvents();
            UnregisterAllKeyboardEvents();
        }
    }
}

