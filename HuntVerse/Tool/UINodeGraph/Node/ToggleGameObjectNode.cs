using System;
using UnityEngine;

namespace Hunt
{
    [Serializable]
    public class ToggleGameObjectNode : UINode
    {
        public GameObject[] targetGameObjects;
        
        public ToggleGameObjectNode() => nodeName = "Toggle GameObject";
        public override UINodeType GetNodeType() => UINodeType.ToggleGameObject;
        
        public override UIGraphExecutionStep CreateExecutionStep(UINodeGraph graph)
        {
            var step = new UIGraphExecutionStep { nodeType = UINodeType.ToggleGameObject, nodeGuid = guid };
            if (targetGameObjects != null && targetGameObjects.Length > 0 && graph != null)
            {
                step.gameObjectIds = new string[targetGameObjects.Length];
                for (int i = 0; i < targetGameObjects.Length; i++)
                    if (targetGameObjects[i] != null)
                        step.gameObjectIds[i] = graph.GetOrCreateTargetId(targetGameObjects[i]);
            }
            return step;
        }
    }
}
