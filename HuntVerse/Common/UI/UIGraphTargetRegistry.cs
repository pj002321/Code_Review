using System.Collections.Generic;
using UnityEngine;

namespace Hunt
{
    /// <summary>
    /// UIGraphTarget 등록 및 검색 관리
    /// </summary>
    public class UIGraphTargetRegistry
    {
        private Dictionary<string, UIGraphTarget> graphTargetMap = new Dictionary<string, UIGraphTarget>();

        public void RegisterGraphTarget(UIGraphTarget target)
        {
            if (target == null || string.IsNullOrEmpty(target.TargetId))
            {
                this.DWarnning("RegisterGraphTarget: target이 null이거나 TargetId가 비어있습니다.");
                return;
            }
            
            this.DLog($"RegisterGraphTarget: {target.gameObject.name} (ID: {target.TargetId}) 등록 중...");
            graphTargetMap[target.TargetId] = target;
            this.DLog($"RegisterGraphTarget 완료. 현재 등록된 개수: {graphTargetMap.Count}");
        }
        
        public void UnregisterGraphTarget(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return;
            graphTargetMap.Remove(targetId);
        }
        
        public GameObject FindGameObjectById(string targetId)
        {
            if (string.IsNullOrEmpty(targetId))
            {
                this.DWarnning("FindGameObjectById: targetId가 비어있습니다.");
                return null;
            }
         
            if (graphTargetMap.TryGetValue(targetId, out var target) && target != null)
            {
                return target.gameObject;
            }
            
            this.DWarnning($"FindGameObjectById: ID {targetId}를 찾을 수 없습니다.");
            return null;
        }

        public void RegisterAllGraphTargetsInScene()
        {
            var allTargets = Object.FindObjectsOfType<UIGraphTarget>(true);
            this.DLog($"씬에서 {allTargets.Length}개의 UIGraphTarget 발견");
            
            foreach (var target in allTargets)
            {
                if (target != null && !string.IsNullOrEmpty(target.TargetId))
                {
                    RegisterGraphTarget(target);
                }
            }
        }

        public void Clear()
        {
            graphTargetMap.Clear();
        }
    }
}

