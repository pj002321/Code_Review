using System.Collections.Generic;
using UnityEngine;

namespace Hunt
{
    /// <summary>
    /// UILayer와 UILayerGroup 관리
    /// </summary>
    public class UILayerManager
    {
        private Dictionary<UILayer, HashSet<UILayerGroup>> layerGroupMap = new Dictionary<UILayer, HashSet<UILayerGroup>>();

        public void RegisterGroup(UILayerGroup group)
        {
            if (group == null)
            {
                this.DWarnning("RegisterGroup: group이 null입니다.");
                return;
            }
            
            if (group.Layer == UILayer.None)
            {
                this.DWarnning($"RegisterGroup: {group.gameObject.name}의 Layer가 UILayer.None입니다.");
                return;
            }

            if (!layerGroupMap.ContainsKey(group.Layer))
            {
                layerGroupMap[group.Layer] = new HashSet<UILayerGroup>();
            }
            
            layerGroupMap[group.Layer].Add(group);
            this.DLog($"RegisterGroup: {group.gameObject.name} (Layer: {group.Layer}) 등록 완료. 현재 {group.Layer} 레이어에 {layerGroupMap[group.Layer].Count}개 그룹 등록됨");
        }

        public void UnregisterGroup(UILayerGroup group)
        {
            if (group == null || group.Layer == UILayer.None) return;   

            if(layerGroupMap.TryGetValue(group.Layer, out var groupSet))
            {
                groupSet.Remove(group);
            }
        }

        public void HideLayer(UILayer layer)
        {
            if (layer == UILayer.None)
            {
                this.DWarnning("HideLayer: UILayer.None은 처리할 수 없습니다.");
                return;
            }
            
            if (!layerGroupMap.TryGetValue(layer, out var groups))
            {
                this.DWarnning($"HideLayer: {layer} 레이어에 등록된 그룹이 없습니다. UILayerGroup 컴포넌트가 씬에 있는지 확인해주세요.");
                return;
            }

            this.DLog($"HideLayer: {layer} 레이어의 {groups.Count}개 그룹 비활성화 중...");
            
            foreach (var group in groups)
            {
                if (group != null && group.gameObject != null)
                {
                    this.DLog($"HideLayer: {group.gameObject.name} 및 모든 자식 비활성화");
                    group.gameObject.SetActive(false);
                }
            }
        }

        public void ShowLayer(UILayer layer)
        {
            if (layer == UILayer.None)
            {
                this.DWarnning("ShowLayer: UILayer.None은 처리할 수 없습니다.");
                return;
            }
            
            if (!layerGroupMap.TryGetValue(layer, out var groups))
            {
                this.DWarnning($"ShowLayer: {layer} 레이어에 등록된 그룹이 없습니다. UILayerGroup 컴포넌트가 씬에 있는지 확인해주세요.");
                return;
            }

            this.DLog($"ShowLayer: {layer} 레이어의 {groups.Count}개 그룹 활성화 중...");
            
            foreach (var group in groups)
            {
                if (group != null && group.gameObject != null)
                {
                    this.DLog($"ShowLayer: {group.gameObject.name} 및 모든 자식 활성화");
                    group.gameObject.SetActive(true);
                }
            }
        }

        public void HideLayers(params UILayer[] layers)
        {
            if (layers == null) return;
            foreach (var layer in layers)
            {
                HideLayer(layer);
            }
        }

        public void ShowLayers(params UILayer[] layers)
        {
            if (layers == null) return;
            foreach (var layer in layers)
            {
                ShowLayer(layer);
            }
        }

        public void ToggleLayers(params UILayer[] layers)
        {
            if (layers == null) return;
            foreach (var layer in layers)
            {
                if (layer == UILayer.None || !layerGroupMap.TryGetValue(layer, out var groups)) continue;
                
                bool anyActive = false;
                foreach (var group in groups)
                {
                    if (group != null && group.gameObject != null && group.gameObject.activeSelf)
                    {
                        anyActive = true;
                        break;
                    }
                }
                
                if (anyActive)
                {
                    HideLayer(layer);
                }
                else
                {
                    ShowLayer(layer);
                }
            }
        }

        public void RegisterAllLayerGroupsInScene()
        {
            var allGroups = Object.FindObjectsOfType<UILayerGroup>(true);
            this.DLog($"씬에서 {allGroups.Length}개의 UILayerGroup 발견");
            
            if (allGroups.Length == 0)
            {
                this.DWarnning("씬에 UILayerGroup이 없습니다. GameObject에 UILayerGroup 컴포넌트를 추가해주세요.");
                return;
            }
            
            foreach (var group in allGroups)
            {
                if (group == null)
                {
                    this.DWarnning("null인 UILayerGroup 발견");
                    continue;
                }
                
                this.DLog($"UILayerGroup 처리 중: {group.gameObject.name}, Layer: {group.Layer}");
                
                if (group.Layer != UILayer.None)
                {
                    RegisterGroup(group);
                }
                else
                {
                    this.DWarnning($"{group.gameObject.name}의 Layer가 UILayer.None입니다. Inspector에서 Layer를 설정해주세요.");
                }
            }
            
            this.DLog($"layerGroupMap 등록 완료. 총 {layerGroupMap.Count}개 레이어에 그룹 등록됨");
            foreach (var kvp in layerGroupMap)
            {
                this.DLog($"  - {kvp.Key}: {kvp.Value.Count}개 그룹");
            }
        }

        public void Clear()
        {
            layerGroupMap.Clear();
        }
    }
}

