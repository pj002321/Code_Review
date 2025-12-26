namespace penta
{
    [CreateAssetMenu(fileName = "New RoundSpawn Data", menuName = "penta/RoundSpawn Data")]
    public class RoundSpawnData : ScriptableObject
    {
        [System.Serializable]
        public class RoundInfo
        {
            public int roundIndex;
            public int maxSpawnCount = 30;
            public float spawnInterval = 2f;
            public bool randomPath = true;
        }

        public List<RoundInfo> rounds = new List<RoundInfo>();
    }
}