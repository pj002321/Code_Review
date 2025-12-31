using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hunt
{
    /// <summary> NPC 대사 데이터 </summary>
    [Serializable]
    public class DialogData
    {
        public int npcId;
        public string npcName;
        public string speakerIconkey;
        public List<DialogNode> nodes;
    }

    /// <summary> 대사 노드 (이전 노드를 참조할 수 있음) </summary>
    [Serializable]
    public class DialogNode
    {
        public int nodeId;
        public string dialogText;
        public List<DialogChoice> choices;
        public bool allowPrev = false;
    }

    /// <summary> 대사 선택 </summary>
    [Serializable]
    public class DialogChoice
    {
        public string choiceText;
        public int nextNodeId;
        public string choiceId = "";
    }
}
