using Hunt.Table;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace Hunt.Data
{
    public class TableDataManager : MonoBehaviourSingleton<TableDataManager>
    {
        private Dictionary<Type, object> tableCache = new();

        protected override void Awake()
        {
            base.Awake();
            LoadAllTables();
        }

        /// <summary>BIN 파일 로드 및 Protocol Buffers 역직렬화</summary>
        private TTable LoadTableFromBin<TTable>(string tableName) where TTable : class
        {
            try
            {
                this.DLog($"Attempting to load: {tableName}");

                byte[] bytes = null;

                string streamingPath = Path.Combine(Application.streamingAssetsPath, "Data", $"{tableName}.bin");
                if (File.Exists(streamingPath))
                {
                    bytes = File.ReadAllBytes(streamingPath);
                    this.DLog($"Loaded from StreamingAssets: {tableName}.bin ({bytes.Length} bytes)");
                }

                if (bytes == null || bytes.Length == 0)
                {
                    this.DError($"Binary File Not Found or Empty: {tableName}");
                    return null;
                }

    
                var tableType = typeof(TTable);
                var parserProperty = tableType.GetProperty("Parser", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                if (parserProperty == null)
                {
                    this.DError($"Parser Not Found: {tableName}");
                    return null;
                }

                var parser = parserProperty.GetValue(null);
                if (parser == null)
                {
                    this.DError($"Parser Instance is Null: {tableName}");
                    return null;
                }

                var parseMethod = parser.GetType().GetMethod("ParseFrom", new[] { typeof(byte[]) });
                if (parseMethod == null)
                {
                    this.DError($"ParseFrom Method Not Found: {tableName}");
                    return null;
                }

                this.DLog($"Calling ParseFrom for: {tableName}");
                var table = parseMethod.Invoke(parser, new object[] { bytes }) as TTable;

                if (table != null)
                {
                    this.DLog($"✓ Successfully loaded {tableName} from Binary: {bytes.Length} bytes");
                }
                else
                {
                    this.DError($"Failed to Deserialize: {tableName} - ParseFrom returned null");
                }
                return table;
            }
            catch (Exception e)
            {
                this.DError($"Failed to Load Binary {tableName}: {e.Message}");
                this.DError($"Stack trace: {e.StackTrace}");
                return null;
            }
        }
        private void LoadAndCacheTable<TTable>(string tableName) where TTable : class
        {
            var table = LoadTableFromBin<TTable>(tableName);
            if (table != null)
            {
                tableCache[typeof(TTable)] = table;
            }
        }

        public TTable GetTable<TTable>() where TTable : class
        {
            if (tableCache.TryGetValue(typeof(TTable), out var table))
            {
                return table as TTable;
            }

            this.DError($"Table Not Found in Cache:{typeof(TTable).Name}");
            return null;
        }

        public void LoadAllTables()
        {
            LoadAndCacheTable<ItemTable>("Item");
            LoadAndCacheTable<NPCTable>("NPC");
            LoadAndCacheTable<EquipItemTable>("EquipItem");
            LoadAndCacheTable<UsingItemTable>("UsingItem");
            LoadAndCacheTable<BasicStatTable>("BasicStat");
            LoadAndCacheTable<MapTable>("Map");
            LoadAndCacheTable<JobTable>("Job");
            LoadAndCacheTable<JobDefaultTable>("JobDefault");
            LoadAndCacheTable<RoleTable>("Role");
            LoadAndCacheTable<ScriptTable>("Script");
            LoadAndCacheTable<ScriptNPCTable>("ScriptNPC");
            LoadAndCacheTable<QuestNPCTable>("QuestNPC");
            LoadAndCacheTable<BattleNPCTable>("BattleNPC");
            LoadAndCacheTable<ShopNPCTable>("ShopNPC");
            LoadAndCacheTable<ShopMappingTable>("ShopMapping");
            LoadAndCacheTable<ItemCategoryTable>("ItemCategory");
            LoadAndCacheTable<EquipTypeTable>("EquipType");
            LoadAndCacheTable<LimitLevelTable>("LimitLevel");
            LoadAndCacheTable<StatApplyTypeTable>("StatApplyType");
            LoadAndCacheTable<SpecialStatTable>("SpecialStat");
            LoadAndCacheTable<BufGroupTable>("BufGroup");
            LoadAndCacheTable<TriggerTable>("Trigger");
            LoadAndCacheTable<FunctionTable>("Function");

            $"Loaded {tableCache.Count} tables".DLog();
        }

        /// <summary>Item 테이블 조회</summary>
        public ItemTable GetItemTable() => GetTable<ItemTable>();

        /// <summary>NPC 테이블 조회</summary>
        public NPCTable GetNPCTable() => GetTable<NPCTable>();

        /// <summary>EquipItem 테이블 조회</summary>
        public EquipItemTable GetEquipItemTable() => GetTable<EquipItemTable>();

        /// <summary>UsingItem 테이블 조회</summary>
        public UsingItemTable GetUsingItemTable() => GetTable<UsingItemTable>();
    }
}
