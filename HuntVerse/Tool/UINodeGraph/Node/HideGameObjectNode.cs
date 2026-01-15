using System;
using UnityEngine;

namespace Hunt
{
    [Serializable]
    public class HideGameObjectNode : UINode
    {
        public GameObject[] targetGameObjects;
        
        public HideGameObjectNode() => nodeName = "Hide GameObject";
        public override UINodeType GetNodeType() => UINodeType.HideGameObject;
        
        public override UIGraphExecutionStep CreateExecutionStep(UINodeGraph graph)
        {
            var step = new UIGraphExecutionStep { nodeType = UINodeType.HideGameObject, nodeGuid = guid };
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
