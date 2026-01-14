using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace Hunt
{
    /// <summary>
    /// UINodeGraph 실행 로직
    /// </summary>
    public class UIGraphExecutor
    {
        private readonly UILayerManager layerManager;
        private readonly UIGraphTargetRegistry targetRegistry;

        public UIGraphExecutor(UILayerManager layerManager, UIGraphTargetRegistry targetRegistry)
        {
            this.layerManager = layerManager;
            this.targetRegistry = targetRegistry;
        }

        public async UniTask ExecuteGraph(UINodeGraph graph)
        {
            try
            {
                if (graph == null)
                {
                    this.DError("UINodeGraph가 null입니다.");
                    return;
                }
                
                var runtimeData = graph.GetBakedData();
                if (runtimeData == null)
                {
                    this.DError("Bake된 데이터가 없습니다. 노드 그래프를 Bake해주세요.");
                    return;
                }
                
                if (runtimeData.executionSteps == null || runtimeData.executionSteps.Count == 0)
                {
                    this.DWarnning("실행할 단계가 없습니다. 노드 그래프에 노드를 추가하고 Bake해주세요.");
                    return;
                }
                
                for (int i = 0; i < runtimeData.executionSteps.Count; i++)
                {
                    var step = runtimeData.executionSteps[i];
                    if (step == null) continue;
                    
                    try
                    {
                        await ExecuteStep(step);
                    }
                    catch (System.Exception e)
                    {
                        this.DError($"ExecuteStep 오류 (단계 {i + 1}): {e.Message}");
                        Debug.LogException(e);
                    }
                }
            }
            catch (System.Exception e)
            {
                this.DError($"ExecuteGraph 오류: {e.Message}");
                Debug.LogException(e);
            }
        }
        
        public async UniTask ExecuteGraphFromNode(UINodeGraph graph, string startNodeGuid)
        {
            try
            {
                this.DLog($"ExecuteGraphFromNode 호출됨 - graph: {graph?.name}, startNodeGuid: {startNodeGuid}");
                
                if (graph == null || string.IsNullOrEmpty(startNodeGuid))
                {
                    this.DWarnning("ExecuteGraphFromNode: graph 또는 startNodeGuid가 null입니다.");
                    return;
                }
                
                var runtimeData = graph.GetBakedData();
                if (runtimeData == null || runtimeData.executionSteps == null)
                {
                    this.DWarnning("ExecuteGraphFromNode: runtimeData가 null이거나 executionSteps가 없습니다.");
                    return;
                }
                
                this.DLog($"executionSteps 개수: {runtimeData.executionSteps.Count}, connections 개수: {graph.GetConnections()?.Count ?? 0}");
                
                var stepsToExecute = GetStepsFromNode(runtimeData.executionSteps, startNodeGuid, graph.GetConnections());
                
                this.DLog($"실행할 step 개수: {stepsToExecute.Count}");
                
                if (stepsToExecute.Count == 0)
                {
                    this.DWarnning("실행할 step이 없습니다. ButtonClickNode에서 연결된 노드가 있는지 확인해주세요.");
                    return;
                }
                
                foreach (var step in stepsToExecute)
                {
                    if (step == null) continue;
                    
                    try
                    {
                        this.DLog($"Step 실행 중: {step.nodeType}, nodeGuid: {step.nodeGuid}");
                        await ExecuteStep(step);
                    }
                    catch (System.Exception e)
                    {
                        this.DError($"ExecuteStep 오류: {e.Message}");
                        Debug.LogException(e);
                    }
                }
            }
            catch (System.Exception e)
            {
                this.DError($"ExecuteGraphFromNode 오류: {e.Message}");
                Debug.LogException(e);
            }
        }
        
        private List<UIGraphExecutionStep> GetStepsFromNode(List<UIGraphExecutionStep> allSteps, string startGuid, List<UINodeConnection> connections)
        {
            var result = new List<UIGraphExecutionStep>();
            var visited = new HashSet<string>();
            var stepMap = new Dictionary<string, UIGraphExecutionStep>();
            
            this.DLog($"GetStepsFromNode 시작 - startGuid: {startGuid}, allSteps: {allSteps?.Count ?? 0}, connections: {connections?.Count ?? 0}");
            
            foreach (var step in allSteps)
            {
                if (step != null && !string.IsNullOrEmpty(step.nodeGuid))
                {
                    stepMap[step.nodeGuid] = step;
                    this.DLog($"Step 매핑: {step.nodeGuid} -> {step.nodeType}");
                }
            }
            
            var queue = new Queue<string>();
            visited.Add(startGuid);
            
            if (connections != null)
            {
                int connectionCount = 0;
                foreach (var connection in connections)
                {
                    this.DLog($"Connection 확인: {connection.fromNodeGuid} -> {connection.toNodeGuid}");
                    if (connection.fromNodeGuid == startGuid && !visited.Contains(connection.toNodeGuid))
                    {
                        visited.Add(connection.toNodeGuid);
                        queue.Enqueue(connection.toNodeGuid);
                        connectionCount++;
                        this.DLog($"시작 노드에서 연결된 노드 발견: {connection.toNodeGuid}");
                    }
                }
                
                if (connectionCount == 0)
                {
                    this.DWarnning($"시작 노드({startGuid})에서 연결된 노드가 없습니다.");
                }
            }
            else
            {
                this.DWarnning("connections가 null입니다.");
            }
            
            while (queue.Count > 0)
            {
                var currentGuid = queue.Dequeue();
                
                if (stepMap.TryGetValue(currentGuid, out var step) && step.nodeType != UINodeType.ButtonClick && step.nodeType != UINodeType.KeyboardInput)
                {
                    result.Add(step);
                    this.DLog($"실행할 step 추가: {step.nodeType} ({currentGuid})");
                }
                else
                {
                    this.DWarnning($"Step을 찾을 수 없거나 ButtonClick 타입입니다: {currentGuid}");
                }
                
                if (connections != null)
                {
                    foreach (var connection in connections)
                    {
                        if (connection.fromNodeGuid == currentGuid && !visited.Contains(connection.toNodeGuid))
                        {
                            visited.Add(connection.toNodeGuid);
                            queue.Enqueue(connection.toNodeGuid);
                        }
                    }
                }
            }
            
            this.DLog($"GetStepsFromNode 완료 - 결과 개수: {result.Count}");
            return result;
        }

        private async UniTask ExecuteStep(UIGraphExecutionStep step)
        {
            if (step == null) return;
            
            try
            {
                switch (step.nodeType)
                {
                    case UINodeType.HideLayer:
                        ExecuteHideLayer(step);
                        break;
                        
                    case UINodeType.ShowLayer:
                        ExecuteShowLayer(step);
                        break;
                        
                    case UINodeType.ToggleLayer:
                        ExecuteToggleLayer(step);
                        break;
                        
                    case UINodeType.HideGameObject:
                        ExecuteHideGameObject(step);
                        break;
                        
                    case UINodeType.ShowGameObject:
                        ExecuteShowGameObject(step);
                        break;
                        
                    case UINodeType.ToggleGameObject:
                        ExecuteToggleGameObject(step);
                        break;
                        
                    case UINodeType.Delay:
                        await ExecuteDelay(step);
                        break;
                        
                    case UINodeType.ButtonClick:
                    case UINodeType.KeyboardInput:
                        break;
                        
                    case UINodeType.ExecuteMethod:
                        ExecuteMethod(step);
                        break;
                }
            }
            catch (System.Exception e)
            {
                this.DError($"ExecuteStep 오류 ({step?.nodeType}): {e.Message}");
                Debug.LogException(e);
            }
        }

        private void ExecuteHideLayer(UIGraphExecutionStep step)
        {
            if (step.layerParams != null && step.layerParams.Length > 0)
            {
                foreach (var layerStr in step.layerParams)
                {
                    if (System.Enum.TryParse<UILayer>(layerStr, out var layer) && layer != UILayer.None)
                    {
                        layerManager.HideLayer(layer);
                    }
                }
            }
        }

        private void ExecuteShowLayer(UIGraphExecutionStep step)
        {
            if (step.layerParams != null && step.layerParams.Length > 0)
            {
                foreach (var layerStr in step.layerParams)
                {
                    if (System.Enum.TryParse<UILayer>(layerStr, out var layer) && layer != UILayer.None)
                    {
                        layerManager.ShowLayer(layer);
                    }
                }
            }
        }

        private void ExecuteToggleLayer(UIGraphExecutionStep step)
        {
            if (step.layerParams != null && step.layerParams.Length > 0)
            {
                foreach (var layerStr in step.layerParams)
                {
                    if (System.Enum.TryParse<UILayer>(layerStr, out var layer) && layer != UILayer.None)
                    {
                        layerManager.ToggleLayers(layer);
                    }
                }
            }
        }

        private void ExecuteHideGameObject(UIGraphExecutionStep step)
        {
            if (step.gameObjectIds == null || step.gameObjectIds.Length == 0)
            {
                this.DWarnning("HideGameObject: gameObjectIds가 null이거나 비어있습니다.");
                return;
            }
            
            this.DLog($"HideGameObject: {step.gameObjectIds.Length}개 GameObject 처리 시작");
            
            foreach (var id in step.gameObjectIds)
            {
                this.DLog($"HideGameObject: ID {id}로 GameObject 찾는 중...");
                var go = targetRegistry.FindGameObjectById(id);
                if (go != null)
                {
                    this.DLog($"HideGameObject: {go.name} 찾음, 비활성화 중...");
                    go.SetActive(false);
                    this.DLog($"HideGameObject: {go.name} 비활성화 완료 (activeSelf: {go.activeSelf})");
                }
                else
                {
                    this.DWarnning($"HideGameObject: ID {id}로 GameObject를 찾을 수 없습니다.");
                }
            }
        }

        private void ExecuteShowGameObject(UIGraphExecutionStep step)
        {
            if (step.gameObjectIds != null && step.gameObjectIds.Length > 0)
            {
                foreach (var id in step.gameObjectIds)
                {
                    var go = targetRegistry.FindGameObjectById(id);
                    if (go != null)
                    {
                        go.SetActive(true);
                    }
                }
            }
        }

        private void ExecuteToggleGameObject(UIGraphExecutionStep step)
        {
            if (step.gameObjectIds != null && step.gameObjectIds.Length > 0)
            {
                foreach (var id in step.gameObjectIds)
                {
                    var go = targetRegistry.FindGameObjectById(id);
                    if (go != null)
                    {
                        go.SetActive(!go.activeSelf);
                    }
                }
            }
        }

        private async UniTask ExecuteDelay(UIGraphExecutionStep step)
        {
            if (step.floatParams.TryGetValue("delaySeconds", out var delay))
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(delay));
            }
        }

        private void ExecuteMethod(UIGraphExecutionStep step)
        {
            if (step.gameObjectIds != null && step.gameObjectIds.Length > 0 && 
                step.stringParams.TryGetValue("componentType", out var compType) &&
                step.stringParams.TryGetValue("methodName", out var methodName) &&
                !string.IsNullOrEmpty(compType) && !string.IsNullOrEmpty(methodName))
            {
                var go = targetRegistry.FindGameObjectById(step.gameObjectIds[0]);
                if (go != null)
                {
                    var componentType = System.Type.GetType(compType);
                    if (componentType == null) componentType = System.Reflection.Assembly.GetExecutingAssembly().GetType(compType);
                    if (componentType == null)
                    {
                        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                        {
                            componentType = asm.GetType(compType);
                            if (componentType != null) break;
                        }
                    }
                    
                    if (componentType != null)
                    {
                        var component = go.GetComponent(componentType);
                        if (component != null)
                        {
                            var method = componentType.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                            if (method != null && method.GetParameters().Length == 0)
                                method.Invoke(component, null);
                        }
                    }
                }
            }
        }
    }
}

