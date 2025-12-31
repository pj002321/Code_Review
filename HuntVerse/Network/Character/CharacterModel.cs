using Hunt.Game;
using Hunt.Login;
using System.Collections.Generic;
using UnityEngine;

namespace Hunt
{


    public class CharacterModel
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

        public static CharacterModel FromCharacterInfo(SimpleCharacterInfo inp)
        {
            return new CharacterModel
            {
                worldId = inp.WorldId,
                charId = inp.CharId,
                name = inp.Name,
                classtype = (ClassType)inp.ClassType,
                level = inp.Level,
                mapId = inp.MapId,
                stats = new List<StatInfo>(inp.StatInfos)

            };


        }

    }
}