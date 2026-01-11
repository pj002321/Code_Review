using System;
using UnityEngine;

namespace Hunt
{
    [Serializable]
    public class HideLayerNode : UINode
    {
        public UILayer[] targetLayers;
        
        public HideLayerNode() => nodeName = "Hide Layer";
        public override UINodeType GetNodeType() => UINodeType.HideLayer;
        
        public override UIGraphExecutionStep CreateExecutionStep(UINodeGraph graph)
        {
            var step = new UIGraphExecutionStep { nodeType = UINodeType.HideLayer, nodeGuid = guid };
            if (targetLayers != null && targetLayers.Length > 0)
            {
                step.layerParams = new string[targetLayers.Length];
                for (int i = 0; i < targetLayers.Length; i++)
                    step.layerParams[i] = targetLayers[i].ToString();
            }
            else step.layerParams = new string[0];
            return step;
        }
    }
}
