using System;
using UnityEngine;

namespace Hunt
{
    [Serializable]
    public class ButtonClickNode : UINode
    {
        public GameObject targetButton;
        
        public ButtonClickNode() => nodeName = "Button Click";
        public override UINodeType GetNodeType() => UINodeType.ButtonClick;
        
        public override UIGraphExecutionStep CreateExecutionStep(UINodeGraph graph)
        {
            var step = new UIGraphExecutionStep { nodeType = UINodeType.ButtonClick, nodeGuid = guid };
            if (targetButton != null && graph != null)
                step.gameObjectIds = new[] { graph.GetOrCreateTargetId(targetButton) };
            return step;
        }
    }
}
