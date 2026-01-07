using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace penta
{
    public enum RegionConst
    {
        Korea = 0,
        Japan = 1,
        China = 2,
        English = 3,
    }

    public static class RegionConstHelper
    {
        public static RegionConst GetRegionName(SystemLanguage lang)
        {
            switch (lang)
            {
                case SystemLanguage.Korean:
                    return RegionConst.Korea;
                case SystemLanguage.Japanese:
                    return RegionConst.Japan;
                case SystemLanguage.Chinese:
                    return RegionConst.China;
                case SystemLanguage.English:
                    return RegionConst.English;
                default:
                    $"Can't Mapping Region : {lang}".EWarning();
                    return RegionConst.English;
            }
        }
        public static string GetNationCode(SystemLanguage lang)
        {
            switch (lang)
            {
                case SystemLanguage.Korean:
                    return "KOR";
                case SystemLanguage.Japanese:
                    return "JAP";
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                    return "CHI";
                case SystemLanguage.English:
                default:
                    return "USA";
            }
        }
        public static string GetNationCode(RegionConst region)
        {
            switch (region)
            {
                case RegionConst.Korea:
                    return "KOR";
                case RegionConst.Japan:
                    return "JAP";
                case RegionConst.China:
                    return "CHI";
                case RegionConst.English:
                default:
                    return "USA";
            }
        }

        public static async UniTask<Sprite> GetRegionSprite(string region)
        {
            string resourceKey = string.Empty;
            if (region == RegionConst.English.ToString().ToLower())
            {
                resourceKey = "america@region";
            }
            else
            {
                resourceKey = region.ToString().ToLower() + "@region";
            }
            return await AbHelper.Shared.LoadAssetAsync<Sprite>(resourceKey);
        }
    }
}