using System;
using UnityEngine;

namespace Hunt
{
    [Serializable]
    public class DelayNode : UINode
    {
        public float delaySeconds = 1.0f;
        
        public DelayNode() => nodeName = "Delay";
        public override UINodeType GetNodeType() => UINodeType.Delay;
        
        public override UIGraphExecutionStep CreateExecutionStep(UINodeGraph graph)
        {
            var step = new UIGraphExecutionStep { nodeType = UINodeType.Delay, nodeGuid = guid };
            step.floatParams.Add("delaySeconds", delaySeconds);
            return step;
        }
    }
}
