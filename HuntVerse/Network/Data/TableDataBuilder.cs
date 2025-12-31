using Hunt.Data;
using Hunt.Table;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Hunt.Ed
{

    public class TableDataBuilder : OdinEditorWindow
    {
        [MenuItem("Tools/Hunt/Open TableDataBuilder")]
        public static void OpenWindow()
        {
            GetWindow<TableDataBuilder>("Table Data Builder");
        }

        [Title("Table Data Builder")]
        [InfoBox("Batch 파일을 실행하여 BIN 파일을 생성하고 게임 데이터를 검증합니다.", InfoMessageType.Info)]

        [BoxGroup("Build")]
        [Button("Execute Batch", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 1f)]
        [LabelText("RUN SerializeProto.bat")]
        private void ExecuteBatch()
        {
            if (EditorApplication.isCompiling)
            {
                EditorUtility.DisplayDialog("ERROR", "Plase wait for compilation to finish", "OK");
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("Executing Batch", "Starting batch file...", 0f);

                var batPath = Path.Combine(Application.dataPath, "..", "DataBuild", "serializeProto.bat");
                if (!File.Exists(batPath))
                {
                    EditorUtility.DisplayDialog("ERROR", $"Batch file not foun:\n {batPath}", "OK");
                    EditorUtility.ClearProgressBar();
                    return;
                }

                this.DLog($"Executing batch file: {batPath}");

                EditorUtility.DisplayProgressBar("Executing Batch", "Running serializeProto.bat...", 0.3f);

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = batPath,
                    WorkingDirectory = Path.GetDirectoryName(batPath),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var process = Process.Start(startInfo);
                process.WaitForExit();

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                EditorUtility.DisplayProgressBar("Executing Batch", "Validating generated Bin files...", 0.7f);

                var validationResult = ValidateBinFiles();

                EditorUtility.DisplayProgressBar("Executing Batch", "Refeshing assets...", 0.9f);

                AssetDatabase.Refresh();
                EditorUtility.DisplayProgressBar("Executing Batch", "Importing BIN files...", 0.95f);
                ImportBinFiles();
                EditorUtility.ClearProgressBar();

                if (validationResult.SuccessCount == validationResult.TotalCount)
                {
                    EditorUtility.DisplayDialog("Success",
                        $"Batch file executed successfully!\n" +
                        $"All {validationResult.SuccessCount} BIN files generated.", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Warning",
                                            $"Batch file executed.\n" +
                                            $"Generated {validationResult.SuccessCount}/{validationResult.TotalCount} BIN files.\n" +
                                            $"Failed: {validationResult.FailCount}", "OK");
                }

                this.DLog($"Batch execution output : \n {output}");

               
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Error", $"Batch file execution failed:\n{e.Message}", "OK");
                this.DError($"{e}");
            }
        }
        private void ImportBinFiles()
        {
            string resourcesPath = Path.Combine(Application.dataPath, "Resources", "Data");
            string streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets", "Data");
            var expectedTables = GetExpectedTableNames();

            // StreamingAssets/Data 폴더가 없으면 생성
            if (!Directory.Exists(streamingAssetsPath))
            {
                Directory.CreateDirectory(streamingAssetsPath);
            }

            int importedCount = 0;
            foreach (var tableName in expectedTables)
            {
                string binFile = Path.Combine(resourcesPath, $"{tableName}.bin");
                if (File.Exists(binFile))
                {
                    // StreamingAssets에 .bin 파일로 복사
                    string streamingBinFile = Path.Combine(streamingAssetsPath, $"{tableName}.bin");
                    if (!File.Exists(streamingBinFile) || File.GetLastWriteTime(binFile) > File.GetLastWriteTime(streamingBinFile))
                    {
                        File.Copy(binFile, streamingBinFile, overwrite: true);
                        this.DLog($"Copied to StreamingAssets: {tableName}.bin");
                    }
                    
                    importedCount++;
                }
            }

            this.DLog($"Imported {importedCount} BIN files to StreamingAssets");
            AssetDatabase.Refresh();
        }
        [BoxGroup("Validation")]
        [Button("VALIDATE RUNTIME DATA", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.4f)]
        [EnableIf("@UnityEngine.Application.isPlaying")]
        private void ValidataData()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("ERROR", "Please start Play Mode first", "OK");
                return;
            }

            var manager = TableDataManager.Shared;
            if (manager == null)
            {
                EditorUtility.DisplayDialog("ERROR",
                    "TableDataManager not found. Please add it to the scene", "OK");
                return;
            }

            validationResults.Clear();
            ValidateAllTables(manager);

            EditorUtility.DisplayDialog("Validation Complete",
                $"Validated {validationResults.Count} tables. \n Check the results below.", "OK");
        }

        [BoxGroup("Validation")]
        [InfoBox("Play Mode must be active to validate data.", InfoMessageType.Warning, "@!UnityEngine.Application.isPlaying")]
        [Space(10)]

        [FoldoutGroup("Validation Results")]
        [ShowInInspector]
        [ReadOnly]
        [TableList(ShowIndexLabels = true)]
        private List<ValidationResult> validationResults = new List<ValidationResult>();

        [Serializable]
        public class ValidationResult
        {
            [TableColumnWidth(150)]
            public string TableName { get; set; }

            [TableColumnWidth(80)]
            [LabelText("Status")]
            public ValidationStatus Status { get; set; }

            [TableColumnWidth(100)]
            [LabelText("Item Count")]
            public int ItemCount { get; set; }

            [TableColumnWidth(150)]
            [LabelText("Sample ID")]
            [ShowIf("@this.ItemCount > 0")]
            public string SampleId { get; set; }

            [TableColumnWidth(150)]
            [LabelText("Message")]
            [ShowIf("@this.Status != ValidationStatus.Success")]
            public string Message { get; set; }
        }
        public enum ValidationStatus
        {
            [LabelText("✔ Success")]
            Success,
            [LabelText("❌ Failed")]
            Failed,
            [LabelText("⚠ Warning")]
            Warning
        }

        /// <summary>생성된 BIN 파일 검증</summary>
        private (int SuccessCount, int FailCount, int TotalCount) ValidateBinFiles()
        {
            string binPath = Path.Combine(Application.dataPath, "Resources", "Data");
            var expectedTables = GetExpectedTableNames();

            int successCount = 0;
            int failCount = 0;

            foreach (var tName in expectedTables)
            {
                var binField = Path.Combine(binPath, $"{tName}.bin");
                if (File.Exists(binField))
                {
                    FileInfo fileInfo = new FileInfo(binField);
                    this.DLog($"✔ {tName}.bin generated ({fileInfo.Length}) bytes");
                    successCount++;
                }
                else
                {
                    this.DError($"❌ {tName}.bin - NOT FOUND");
                    failCount++;
                }
            }

            return (successCount, failCount, expectedTables.Length);
        }

        private void ValidateAllTables(TableDataManager manager)
        {
            var tableValidators = new Dictionary<string, Action<TableDataManager>>
            {   {"Item", (m) => ValidateTable<ItemTable>("Item", m) },
                { "NPC", (m) => ValidateTable<NPCTable>("NPC", m) },
                { "EquipItem", (m) => ValidateTable<EquipItemTable>("EquipItem", m) },
                { "UsingItem", (m) => ValidateTable<UsingItemTable>("UsingItem", m) },
                { "BasicStat", (m) => ValidateTable<BasicStatTable>("BasicStat", m) },
                { "Map", (m) => ValidateTable<MapTable>("Map", m) },
                { "Job", (m) => ValidateTable<JobTable>("Job", m) },
                { "JobDefault", (m) => ValidateTable<JobDefaultTable>("JobDefault", m) },
                { "Role", (m) => ValidateTable<RoleTable>("Role", m) },
                { "Script", (m) => ValidateTable<ScriptTable>("Script", m) },
                { "ScriptNPC", (m) => ValidateTable<ScriptNPCTable>("ScriptNPC", m) },
                { "QuestNPC", (m) => ValidateTable<QuestNPCTable>("QuestNPC", m) },
                { "BattleNPC", (m) => ValidateTable<BattleNPCTable>("BattleNPC", m) },
                { "ShopNPC", (m) => ValidateTable<ShopNPCTable>("ShopNPC", m) },
                { "ShopMapping", (m) => ValidateTable<ShopMappingTable>("ShopMapping", m) },
                { "ItemCategory", (m) => ValidateTable<ItemCategoryTable>("ItemCategory", m) },
                { "EquipType", (m) => ValidateTable<EquipTypeTable>("EquipType", m) },
                { "LimitLevel", (m) => ValidateTable<LimitLevelTable>("LimitLevel", m) },
                { "StatApplyType", (m) => ValidateTable<StatApplyTypeTable>("StatApplyType", m) },
                { "SpecialStat", (m) => ValidateTable<SpecialStatTable>("SpecialStat", m) },
                { "BufGroup", (m) => ValidateTable<BufGroupTable>("BufGroup", m) },
                { "Trigger", (m) => ValidateTable<TriggerTable>("Trigger", m) },
                { "Function", (m) => ValidateTable<FunctionTable>("Function", m) }
            };

            foreach (var validator in tableValidators)
            {
                try
                {
                    validator.Value(manager);
                }
                catch (Exception e)
                {
                    validationResults.Add(new ValidationResult
                    {
                        TableName = validator.Key,
                        Status = ValidationStatus.Failed,
                        ItemCount = 0,
                        Message = $"Exception: {e.Message}"
                    });
                }
            }
        }

        private void ValidateTable<TTable>(string tableName, TableDataManager manager) where TTable : class
        {
            var result = new ValidationResult { TableName = tableName };

            try
            {
                var table = manager.GetTable<TTable>();
                if (table == null)
                {
                    result.Status = ValidationStatus.Failed;
                    result.Message = "Table is null";
                    validationResults.Add(result);
                    return;
                }

                var infosProperty = typeof(TTable).GetProperty("Infos");
                if (infosProperty == null)
                {
                    result.Status = ValidationStatus.Failed;
                    result.Message = "Infos property not found";
                    validationResults.Add(result);
                    return;
                }

                var infos = infosProperty.GetValue(table);
                if (infos == null)
                {
                    result.Status = ValidationStatus.Failed;
                    result.Message = "Infos is null";
                    validationResults.Add(result);
                    return;
                }

                var countProperty = infos.GetType().GetProperty("Count");
                if (countProperty == null)
                {
                    result.Status = ValidationStatus.Warning;
                    result.Message = "Count property not found";
                    validationResults.Add(result);
                    return;
                }

                int count = (int)countProperty.GetValue(infos);
                result.ItemCount = count;

                if (count > 0)
                {
                    var firstItem = infos.GetType().GetMethod("get_Item", new[] { typeof(int) });
                    if (firstItem != null)
                    {
                        var item = firstItem.Invoke(infos, new object[] { 0 });
                        var idProperty = item.GetType().GetProperty("Id");
                        if (idProperty != null)
                        {
                            var id = idProperty.GetValue(item);
                            result.SampleId = id.ToString();
                        }
                    }
                    result.Status = ValidationStatus.Success;
                }
                else
                {
                    result.Status = ValidationStatus.Warning;
                    result.Message = "Table is empty";
                }

                validationResults.Add(result);
            }
            catch (Exception e)
            {
                result.Status = ValidationStatus.Failed;
                result.Message = $"Error: {e.Message}";
                validationResults.Add(result);
            }


        }

        /// <summary>예상 테이블 이름 목록</summary>
        private string[] GetExpectedTableNames()
        {
            return new[]
            {
                "Item", "NPC", "EquipItem", "UsingItem", "BasicStat",
                "Map", "Job", "JobDefault", "Role", "Script",
                "ScriptNPC", "QuestNPC", "BattleNPC", "ShopNPC",
                "ShopMapping", "ItemCategory", "EquipType", "LimitLevel",
                "StatApplyType", "SpecialStat", "BufGroup", "Trigger", "Function"
            };
        }
    }
}
