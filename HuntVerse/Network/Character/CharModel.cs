using Hunt.Game;
using Hunt.Login;
using System.Collections.Generic;
using UnityEngine;

namespace Hunt
{
    public class CharModel
    {
        public uint worldId;
        public ulong charId;
        public string name;
        public ClassType classtype;
        public ulong level;
        public ulong mapId;
        public List<StatInfo> stats;
        
        public Sprite icon;

        public bool IsCreated => !string.IsNullOrEmpty(name);

        public static CharModel FromCharacterInfo(SimpleCharacterInfo inp)
        {
            var statList = inp.StatInfos != null && inp.StatInfos.Count > 0
            ? new List<StatInfo>(inp.StatInfos)
            : CreateDefaultStats();
            return new CharModel
            {
                worldId = inp.WorldId,
                charId = inp.CharId,
                name = inp.Name,
                classtype = BindKeyConst.GetClassTypeByJobId(inp.ClassType),
                level = inp.Level,
                mapId = inp.MapId,
                stats = statList

            };


        }
        private static List<StatInfo> CreateDefaultStats()
        {
            return new List<StatInfo>
            {
                new StatInfo { Type = (uint)CharStatType.HP, Point = 30 },
                new StatInfo { Type = (uint)CharStatType.MP, Point = 20 },
                new StatInfo { Type = (uint)CharStatType.STR, Point = 50 },
                new StatInfo { Type = (uint)CharStatType.INT, Point = 50 },
                new StatInfo { Type = (uint)CharStatType.DEF, Point = 50 }
            };
        }
    }
}