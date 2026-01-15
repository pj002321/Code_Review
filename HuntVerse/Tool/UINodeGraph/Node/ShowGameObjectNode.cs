using System;
using UnityEngine;

namespace Hunt
{
    [Serializable]
    public class ShowGameObjectNode : UINode
    {
        public GameObject[] targetGameObjects;
        
        public ShowGameObjectNode() => nodeName = "Show GameObject";
        public override UINodeType GetNodeType() => UINodeType.ShowGameObject;
        
        public override UIGraphExecutionStep CreateExecutionStep(UINodeGraph graph)
        {
            var step = new UIGraphExecutionStep { nodeType = UINodeType.ShowGameObject, nodeGuid = guid };
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
