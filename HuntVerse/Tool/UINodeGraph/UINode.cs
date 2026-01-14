using System;
using UnityEngine;

namespace Hunt
{
    [Serializable]
    public abstract class UINode
    {
        public string guid;
        public Vector2 position;
        public string nodeName;
        
        public abstract UINodeType GetNodeType();
        public abstract UIGraphExecutionStep CreateExecutionStep(UINodeGraph graph);
    }
    
    public enum UINodeType
    {
        ButtonClick,
        KeyboardInput,
        HideLayer,
        ShowLayer,
        ToggleLayer,
        HideGameObject,
        ShowGameObject,
        ToggleGameObject,
        Delay,
        ExecuteMethod
    }
}
