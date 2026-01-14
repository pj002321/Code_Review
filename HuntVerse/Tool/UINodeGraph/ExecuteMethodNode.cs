using System;
using UnityEngine;

namespace Hunt
{
    [Serializable]
    public class ExecuteMethodNode : UINode
    {
        public GameObject targetObject;
        public string componentTypeName;
        public string methodName;
        
        public ExecuteMethodNode() => nodeName = "Execute Method";
        public override UINodeType GetNodeType() => UINodeType.ExecuteMethod;
        
        public override UIGraphExecutionStep CreateExecutionStep(UINodeGraph graph)
        {
            var step = new UIGraphExecutionStep { nodeType = UINodeType.ExecuteMethod, nodeGuid = guid };
            if (targetObject != null && graph != null)
            {
                step.gameObjectIds = new[] { graph.GetOrCreateTargetId(targetObject) };
                step.stringParams.Add("componentType", componentTypeName ?? "");
                step.stringParams.Add("methodName", methodName ?? "");
            }
            return step;
        }
    }
}






