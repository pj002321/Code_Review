using System;
using UnityEngine;

namespace Hunt
{
    [Serializable]
    public class KeyboardInputNode : UINode
    {
        public GameObject targetGameObject;
        public KeyCode targetKeyCode = KeyCode.None;
        
        public KeyboardInputNode() => nodeName = "Keyboard Input";
        public override UINodeType GetNodeType() => UINodeType.KeyboardInput;
        
        public override UIGraphExecutionStep CreateExecutionStep(UINodeGraph graph)
        {
            var step = new UIGraphExecutionStep { nodeType = UINodeType.KeyboardInput, nodeGuid = guid };
            if (targetKeyCode != KeyCode.None)
                step.intParams.Add("keyCode", (int)targetKeyCode);
            if (targetGameObject != null && graph != null)
                step.gameObjectIds = new[] { graph.GetOrCreateTargetId(targetGameObject) };
            return step;
        }
    }
}

