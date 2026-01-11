using System;
using UnityEngine;

namespace Hunt
{
    [Serializable]
    public class UINodeConnection
    {
        public string fromNodeGuid;
        public string toNodeGuid;
        public int fromPortIndex;
        public int toPortIndex;
    }
}

