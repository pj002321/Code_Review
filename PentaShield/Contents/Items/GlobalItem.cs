using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using PentaShield;
using Random = UnityEngine.Random;
using Chaos;
using System.Collections.ObjectModel;
using System.Linq;

namespace chaos
{
    public enum GlobalItemSkillType
    {
        PlayerInfinity,
        EnemiesStop,
        EnemiesSlow,
        IceMeteo,
        FireMeteo,
        StoneRadial,
        MultiCurse,
        ThunderCrash
    }

    /// <summary>
    /// 글로벌 아이템 시스템 관리 (주요 로직)
    /// - 플레이어 버프 아이템 (Heal, Haste, God, Fever)
    /// - 적 디버프 아이템 (Slow)
    /// - 스킬 아이템 (IceMeteo, FireMeteo, StoneRadial, ThunderCrash, MultiCurse)
    /// - 아이템 개수 관리 (임시/영구)
    /// </summary>
    public partial class GlobalItem : MonoBehaviourSingleton<GlobalItem>, IDestroyOnThisScene
    {
        private Dictionary<ItemType, int> temporaryItems = null;

        [Header("TIMING SETTINGS")]
        [SerializeField] private float waitTimeforEnemyGod = 3f;
        [SerializeField] private float waitTimeforEnemySlow = 5f;
        [SerializeField] private float waitTimeforPlayerFever = 5f;
        [SerializeField] private float waitTimeforPlayerHaste = 5f;

        [Header("COOLDOWN SETTINGS")]
        [SerializeField] private float cooldownHeal = 5f;
        [SerializeField] private float cooldownHaste = 8f;
        [SerializeField] private float cooldownGod = 15f;
        [SerializeField] private float cooldownFever = 15f;
        [SerializeField] private float cooldownRandomBox = 10f;

        private float healCooldownRemaining;
        private float hasteCooldownRemaining;
        private float godCooldownRemaining;
        private float feverCooldownRemaining;
        private float randomBoxCooldownRemaining;

        public float HealCooldownRemaining => healCooldownRemaining;
        public float HasteCooldownRemaining => hasteCooldownRemaining;
        public float GodCooldownRemaining => godCooldownRemaining;
        public float FeverCooldonwRemaining => feverCooldownRemaining;
        public float RandomBoxCooldownRemaining => randomBoxCooldownRemaining;

        public float HealCooldownMax => cooldownHeal;
        public float HasteCooldownMax => cooldownHaste;
        public float GodCooldownMax => cooldownGod;
        public float FeverCooldownMax => cooldownFever;
        public float RandomBoxCooldownMax => cooldownRandomBox;

        public bool IsHealOnCooldown => healCooldownRemaining > 0;
        public bool IsHasteOnCooldown => hasteCooldownRemaining > 0;
        public bool IsGodOnCooldown => godCooldownRemaining > 0;
        public bool IsFeverOnCooldown => feverCooldownRemaining > 0;
        public bool IsRandomBoxOnCooldown => randomBoxCooldownRemaining > 0;


        private List<Coroutine> activeCoroutines = new List<Coroutine>();
        private List<GameObject> activeSkillObjects = new List<GameObject>();
        private System.Threading.CancellationTokenSource skillCancellationTokenSource;

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();

            Array itemtypes = Enum.GetValues(typeof(ItemType));
            temporaryItems = new Dictionary<ItemType, int>(itemtypes.Length);
            foreach (ItemType type in itemtypes)
            {
                temporaryItems.Add(type, 0);
            }

            skillCancellationTokenSource = new System.Threading.CancellationTokenSource();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            skillCancellationTokenSource?.Cancel();
            skillCancellationTokenSource?.Dispose();
        }

        private void Update()
        {
            if (healCooldownRemaining > 0)
                healCooldownRemaining -= Time.deltaTime;
            if (hasteCooldownRemaining > 0)
                hasteCooldownRemaining -= Time.deltaTime;
            if (godCooldownRemaining > 0)
                godCooldownRemaining -= Time.deltaTime;
            if (feverCooldownRemaining > 0)
                feverCooldownRemaining -= Time.deltaTime;
            if (randomBoxCooldownRemaining > 0)
                randomBoxCooldownRemaining -= Time.deltaTime;
        }
        #endregion

        #region Properties
        public ItemData UserItem => UserDataManager.Shared != null && UserDataManager.Shared.Data != null
            ? UserDataManager.Shared.Data.Item
            : null;
        #endregion

        #region Player Buff Skills
        /// <summary> 플레이어 무적 버프 - 모든 적 행동 정지 </summary>
        public IEnumerator Co_PlayerGod(bool _islevelupreward = false)
        {
            if (!_islevelupreward) ReduceItemCount(ItemType.God);
            IsSkillIcon.Shared.OnCreatePlayerIcon(PentaConst.KGIconGod).Forget();
            AudioHelper.PlaySFX(AudioConst.GLOBAL_ITEM_GOD,2f);
            GodFx().Forget();
            godCooldownRemaining = cooldownGod;

            var dummyEnemies = new List<Dummy>();
            var enemies = new List<Enemy>();

            // Dummy 타입 적들 멈춤
            foreach (var enemy in FindObjectsOfType<Dummy>())
            {
                if (enemy.gameObject != null)
                {
                    enemy.SetBehaviourStop();
                    dummyEnemies.Add(enemy);
                }
            }

            // Enemy 타입 적들 멈춤
            foreach (var enemy in FindObjectsOfType<Enemy>())
            {
                if (enemy != null && enemy.gameObject != null)
                {
                    enemy.SetBehaviourStop();
                    enemies.Add(enemy);
                }
            }

            yield return new WaitForSeconds(waitTimeforEnemyGod);

            // Dummy 타입 적들 재개
            foreach (var enemy in dummyEnemies)
            {
                if (enemy != null)
                {
                    enemy.ResumBehaviour();
                }
            }

            // Enemy 타입 적들 재개
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.ResumBehaviour();
                }
            }
        }

        /// <summary> 플레이어 피버 모드 - 무적 상태 및 원소 스폰 </summary>
        public IEnumerator Co_PlayerFever(bool _islevelupreward = false)
        {
            if (!_islevelupreward) ReduceItemCount(ItemType.Fiver);
            IsSkillIcon.Shared.OnCreatePlayerIcon(PentaConst.KGIconFever).Forget();

            feverCooldownRemaining = cooldownFever;
            var player = PlayerController.Shared;
            if (player == null) yield break;

            var originalHealth = player.CurHeath;
            var originalMaxHealth = player.CurMaxHeath;

            player.IsInvincible = true;

            player.CurMaxHeath = 5000;
            player.CurHeath = 5000;

            if (player.healthSlider != null)
            {
                player.healthSlider.SetMaxHealth(5000);
                player.healthSlider.SetCurrentHealth(5000);
                player.healthSlider.isFever = true;
                player.healthSlider.UpdateSlider();
            }

            // Fever BGM 재생
            AudioHelper.PlayBGM(AudioConst.FEVERTIME_BGM, 0.5f);

            // 5가지 원소 객체를 플레이어 주위에 생성하여 빙글빙글 돌면서 적 공격
            _ = SpawnFeverElementals(waitTimeforPlayerFever);

            yield return new WaitForSeconds(waitTimeforPlayerFever);

            player.IsInvincible = false;

            player.CurMaxHeath = originalMaxHealth;
            player.CurHeath = originalHealth;

            if (player.healthSlider != null)
            {
                player.healthSlider.SetMaxHealth(originalMaxHealth);
                player.healthSlider.SetCurrentHealth(originalHealth);
                player.healthSlider.isFever = false;
                player.healthSlider.UpdateSlider();
            }

            // 원래 BGM으로 복원
            AudioHelper.RestorePreviousBGM(0.5f);
        }

        /// <summary> 피버 모드 원소 스폰 - 플레이어 주위에 5개 원소 생성 </summary>
        private async UniTask SpawnFeverElementals(float duration)
        {
            var playerTransform = PlayerBehaviour.Shared?.gameObject.transform;
            if (playerTransform == null)
            {
                $"[SpawnFeverElementals] PlayerBehaviour not found".DError();
                return;
            }

            var elementTables = SheetManager.GetSheetObject()?.ElementTableList;
            if (elementTables == null || elementTables.Count == 0)
            {
                $"[SpawnFeverElementals] ElementTable not found in sheet".DError();
                return;
            }

            var level1Elementals = elementTables.Where(e => e.Level == 1).Take(5).ToList();
            if (level1Elementals.Count == 0)
            {
                $"[SpawnFeverElementals] No level 1 elementals found in sheet".DError();
                return;
            }

            List<Elemental> spawnedElementals = new List<Elemental>();

            foreach (var elementTable in level1Elementals)
            {
                try
                {
                    var elemental = await UpgradeTable.Shared.ElementalUpgrade.CreateElemental(elementTable.Name);
                    if (elemental != null)
                    {
                        spawnedElementals.Add(elemental);
                        elemental.enabled = false;
                        elemental.gameObject.AddComponent<FeverElementalController>().Initialize(playerTransform, duration, elemental);
                    }
                }
                catch (System.Exception ex)
                {
                    $"[SpawnFeverElementals] Failed to create {elementTable.Name}: {ex.Message}".DError();
                }
            }

            await UniTask.Delay(Mathf.RoundToInt(duration * 1000f), cancellationToken: this.GetCancellationTokenOnDestroy());

            foreach (var elemental in spawnedElementals)
            {
                if (elemental != null)
                {
                    Destroy(elemental.gameObject);
                }
            }
        }

        /// <summary> 플레이어 이동속도 증가 버프 </summary>
        public IEnumerator Co_PlayerHaste(bool _islevelupreward = false)
        {
            if (!_islevelupreward) ReduceItemCount(ItemType.Haste);
            IsSkillIcon.Shared.OnCreatePlayerIcon(PentaConst.KGIconHaste).Forget();
            AudioHelper.PlaySFX(AudioConst.GLOBAL_ITEM_HASTE,2f);
            
            hasteCooldownRemaining = cooldownHaste;

            var player = PlayerController.Shared;
            if (player == null) yield break;
            HasteFx().Forget();
            var originalSpeed = player.CurSpeed;
            player.moveSpeed *= 2f;
            yield return new WaitForSeconds(waitTimeforPlayerHaste);
            player.moveSpeed = originalSpeed; 
        }

        /// <summary> 플레이어 체력 회복 - 최대 체력의 30% 회복 </summary>
        public IEnumerator Co_PlayerHeal(bool _islevelupreward=false)
        {
            if(!_islevelupreward) ReduceItemCount(ItemType.Potion);
            IsSkillIcon.Shared.OnCreatePlayerIcon(PentaConst.KGIconHeal).Forget();
            AudioHelper.PlaySFX(AudioConst.GLOBAL_ITEM_HEAL,2f);
            HealFx().Forget();
            healCooldownRemaining = cooldownHeal;
            var player = PlayerController.Shared;
            if (player == null) yield break;

            int targetHealth = Mathf.Min(player.CurHeath + (int)(player.MaxHealth * 0.3f), player.CurMaxHeath);
            int startHealth = player.CurHeath;

            float healDuration = 2f;
            float elapsedTime = 0f;

            while (elapsedTime < healDuration && player.CurHeath < targetHealth)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / healDuration;

                int newHealth = Mathf.RoundToInt(Mathf.Lerp(startHealth, targetHealth, progress));
                player.CurHeath = newHealth;

                if (player.healthSlider != null)
                {
                    player.healthSlider.SetCurrentHealth(newHealth);
                }

                yield return null;
            }

            player.CurHeath = targetHealth;
            if (player.healthSlider != null)
            {
                player.healthSlider.SetCurrentHealth(targetHealth);
            }
        }
        #endregion

        #region Enemy Debuff Skills
        /// <summary> 적 이동속도 감소 디버프 </summary>
        public IEnumerator Co_EnemiesSlow()
        {            
            var enemies = new List<Dummy>();

            foreach (var enemy in FindObjectsOfType<Dummy>())
            {
                if (enemy.gameObject != null)
                {
                    enemy.SlowMove();
                    enemies.Add(enemy);
                }
            }

            yield return new WaitForSeconds(waitTimeforEnemySlow);

            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.ResumBehaviour();
                }
            }
        }
        #endregion

        #region Skill Items
        /// <summary> 아이스 메테오 스킬 - 적 타겟으로 사선 낙하 </summary>
        public async UniTask Co_IceMeteo()
        {
            try
            {
                await IsSkillIcon.Shared.OnChangeIcon(GlobalItemSkillType.IceMeteo, PentaConst.kGIconIce);

                var meteoPrefab = await AbHelper.Shared.LoadAssetAsync<GameObject>(PentaConst.kGIceMeteo);

                if (meteoPrefab == null)
                {
                    $"[IceMeteo] Prefab not found".DError();
                    return;
                }

                int meteoCount = Random.Range(40, 50);
                float spawnHeight = 20f;
                float spawnRadius = 15f;

                Dummy[] activeEnemies = FindObjectsOfType<Dummy>();
                List<Dummy> validEnemies = new List<Dummy>();

                foreach (var enemy in activeEnemies)
                {
                    if (enemy != null && enemy.gameObject.activeInHierarchy)
                    {
                        validEnemies.Add(enemy);
                    }
                }

                for (int i = 0; i < meteoCount; i++)
                {
                    if (skillCancellationTokenSource.Token.IsCancellationRequested)
                        return;

                    Vector3 playerPos = Guard.Shared != null ? Guard.Shared.transform.position : Vector3.zero;
                    Vector3 targetPosition;
                    Vector3 spawnPosition;
                    Quaternion spawnRotation;

                    validEnemies.RemoveAll(enemy => enemy == null);

                    if (validEnemies.Count > 0)
                    {
                        Dummy targetEnemy = validEnemies[Random.Range(0, validEnemies.Count)];
                        targetPosition = targetEnemy.transform.position;
                    }
                    else
                    {
                        Vector3 randomOffset = new Vector3(
                            Random.Range(-spawnRadius, spawnRadius),
                            -0.5f,
                            Random.Range(-spawnRadius, spawnRadius)
                        );
                        targetPosition = playerPos + randomOffset;
                    }

                    Vector3 direction = (targetPosition - (playerPos + Vector3.up * spawnHeight)).normalized;
                    spawnPosition = playerPos + Vector3.up * spawnHeight + direction * Random.Range(5f, 15f);
                    spawnRotation = Quaternion.LookRotation(direction);

                    GameObject meteo = Instantiate(meteoPrefab, spawnPosition, spawnRotation);

                    IceGlobalItemObject iceMeteoComponent = meteo.GetComponent<IceGlobalItemObject>();
                    if (iceMeteoComponent == null)
                    {
                        iceMeteoComponent = meteo.AddComponent<IceGlobalItemObject>();
                    }

                    iceMeteoComponent.SetTargetPosition(targetPosition);

                    await UniTask.Delay(150, cancellationToken: skillCancellationTokenSource.Token);
                }
            }
            catch (System.OperationCanceledException)
            {
                $"[IceMeteo] Cancelled".DLog();
            }
            catch (System.Exception e)
            {
                $"[IceMeteo] Error : {e.Message}\n{e.StackTrace}".DError();
            }
        }

        /// <summary> 파이어 메테오 스킬 - 적 타겟으로 사선 낙하 및 도트 데미지 </summary>
        public async UniTask Co_FireMeteo()
        {
            try
            {
                await IsSkillIcon.Shared.OnChangeIcon(GlobalItemSkillType.FireMeteo, PentaConst.kGIconFlame);
                var fireBallPrefab = await AbHelper.Shared.LoadAssetAsync<GameObject>(PentaConst.kGfireMeteo);

                if (fireBallPrefab == null)
                {
                    $"[FireMeteo] Prefab not found".DError();
                    return;
                }

                int fireBallCount = Random.Range(40, 50);
                float spawnHeight = 20f;
                float spawnRadius = 15f;

                Dummy[] activeEnemies = FindObjectsOfType<Dummy>();
                List<Dummy> validEnemies = new List<Dummy>();

                foreach (var enemy in activeEnemies)
                {
                    if (enemy != null && enemy.gameObject.activeInHierarchy)
                    {
                        validEnemies.Add(enemy);
                    }
                }

                for (int i = 0; i < fireBallCount; i++)
                {
                    if (skillCancellationTokenSource.Token.IsCancellationRequested)
                        return;

                    Vector3 playerPos = Guard.Shared != null ? Guard.Shared.transform.position : Vector3.zero;
                    Vector3 targetPosition;
                    Vector3 spawnPosition;
                    Quaternion spawnRotation;

                    validEnemies.RemoveAll(enemy => enemy == null);

                    if (validEnemies.Count > 0)
                    {
                        Dummy targetEnemy = validEnemies[Random.Range(0, validEnemies.Count)];

                        targetPosition = targetEnemy.transform.position;
                    }
                    else
                    {
                        Vector3 randomOffset = new Vector3(
                            Random.Range(-spawnRadius, spawnRadius),
                            1f,
                            Random.Range(-spawnRadius, spawnRadius)
                        );
                        targetPosition = playerPos + randomOffset;
                    }

                    Vector3 direction = (targetPosition - (playerPos + Vector3.up * spawnHeight)).normalized;
                    spawnPosition = playerPos + Vector3.up * spawnHeight + direction * Random.Range(5f, 15f);
                    spawnRotation = Quaternion.LookRotation(direction);

                    GameObject fireBall = Instantiate(fireBallPrefab, spawnPosition, spawnRotation);

                    FireGlobalItemObject fireBallComponent = fireBall.GetComponent<FireGlobalItemObject>();
                    if (fireBallComponent == null)
                    {
                        fireBallComponent = fireBall.AddComponent<FireGlobalItemObject>();
                    }

                    fireBallComponent.SetTargetPosition(targetPosition);

                    await UniTask.Delay(Random.Range(100, 300), cancellationToken: skillCancellationTokenSource.Token);
                }
            }
            catch (System.OperationCanceledException)
            {
                $"[FireMeteo] Cancelled".DLog();
            }
        }

        /// <summary> 스톤 레이디얼 스킬 - 가드 주위 원형으로 낙하 </summary>
        public async UniTask Co_StoneRadial()
        {
            try
            {
                await IsSkillIcon.Shared.OnChangeIcon(GlobalItemSkillType.StoneRadial, PentaConst.kGIconStone);

                var stonePrefab = await AbHelper.Shared.LoadAssetAsync<GameObject>(PentaConst.kGstoneRadial);

                if (stonePrefab == null)
                {
                    "[StoneRadial] Stone prefab is null!".DError();
                    return;
                }

                Vector3 guardPosition = Guard.Shared.transform.localPosition;
                float spawnHeight = 10f;

                int[] stoneCountsPerWave = { 10, 16, 22, 28 };
                float[] radiusPerWave = { 4f, 10f, 16f, 20f };
                float delayBetweenWaves = 0.5f;

                for (int wave = 0; wave < stoneCountsPerWave.Length; wave++)
                {
                    if (skillCancellationTokenSource.Token.IsCancellationRequested)
                        return;

                    int stoneCount = stoneCountsPerWave[wave];
                    float radius = radiusPerWave[wave];

                    List<int> randomIndices = new List<int>();
                    for (int i = 0; i < stoneCount; i++)
                    {
                        randomIndices.Add(i);
                    }

                    // Fisher-Yates
                    for (int i = randomIndices.Count - 1; i > 0; i--)
                    {
                        int randomIndex = Random.Range(0, i + 1);
                        int temp = randomIndices[i];
                        randomIndices[i] = randomIndices[randomIndex];
                        randomIndices[randomIndex] = temp;
                    }

                    for (int idx = 0; idx < randomIndices.Count; idx++)
                    {
                        if (skillCancellationTokenSource.Token.IsCancellationRequested)
                            return;

                        int i = randomIndices[idx];

                        float angle = (360f / stoneCount) * i;
                        float radian = angle * Mathf.Deg2Rad;

                        Vector3 offset = new Vector3(
                            Mathf.Cos(radian) * radius,
                            0f,
                            Mathf.Sin(radian) * radius
                        );

                        Vector3 targetPosition = guardPosition + offset;
                        Vector3 spawnPosition = targetPosition + Vector3.up * spawnHeight;

                        Vector3 directionFromCenter = offset.normalized;
                        Quaternion rotation = Quaternion.LookRotation(directionFromCenter);

                        GameObject stone = Instantiate(stonePrefab, spawnPosition, rotation);

                        StoneGlobalItemObject stoneComponent = stone.GetComponent<StoneGlobalItemObject>();
                        if (stoneComponent == null)
                        {
                            stoneComponent = stone.AddComponent<StoneGlobalItemObject>();
                        }

                        await UniTask.Delay(Random.Range(30, 100), cancellationToken: skillCancellationTokenSource.Token);
                    }

                    await UniTask.Delay((int)(delayBetweenWaves * 1000), cancellationToken: skillCancellationTokenSource.Token);
                }
            }
            catch (System.OperationCanceledException)
            {
                $"[StoneRadial] Cancelled".DLog();
            }
        }

        /// <summary> 썬더 크래시 스킬 - 적 머리 위에서 발사체 생성 </summary>
        public async UniTask Co_ThuderCrash()
        {
            try
            {
                await IsSkillIcon.Shared.OnChangeIcon(GlobalItemSkillType.ThunderCrash, PentaConst.kGIconThunder);

                var thunderPrefab = await AbHelper.Shared.LoadAssetAsync<GameObject>(PentaConst.kGthunderCrash);

                if (thunderPrefab == null)
                {
                    $"[ThunderCrash] Prefab not found".DError();
                    return;
                }

                Dummy[] activeEnemies = FindObjectsOfType<Dummy>();
                List<Dummy> validEnemies = new List<Dummy>();

                foreach (var enemy in activeEnemies)
                {
                    if (enemy != null && enemy.gameObject.activeInHierarchy)
                    {
                        validEnemies.Add(enemy);
                    }
                }

                if (validEnemies.Count == 0)
                {
                    $"[ThunderCrash] No enemies found".DWarnning();
                    return;
                }

                foreach (var enemy in validEnemies)
                {
                    if (skillCancellationTokenSource.Token.IsCancellationRequested)
                        return;

                    Vector3 spawnPosition = enemy.transform.position;
                    spawnPosition.y += 3f;

                    GameObject thunder = Instantiate(thunderPrefab, spawnPosition, Quaternion.identity);

                    ThunderGlobalItemObject thunderComponent = thunder.GetComponent<ThunderGlobalItemObject>();
                    if (thunderComponent == null)
                    {
                        thunderComponent = thunder.AddComponent<ThunderGlobalItemObject>();
                    }

                    thunderComponent.SetTarget(enemy);

                    await UniTask.Delay(50, cancellationToken: skillCancellationTokenSource.Token);
                }
            }
            catch (System.OperationCanceledException)
            {
                $"[ThunderCrash] Cancelled".DLog();
            }
        }

        /// <summary> 멀티 커스 스킬 - 범위 내 적에게 저주 적용 </summary>
        public async UniTask Co_MultiCurse()
        {
            try
            {
                await IsSkillIcon.Shared.OnChangeIcon(GlobalItemSkillType.MultiCurse, PentaConst.kGIconCurse);
                var cursePrefab = await AbHelper.Shared.LoadAssetAsync<GameObject>(PentaConst.kGmultiCurse);

                if (cursePrefab == null)
                {
                    $"[MultiCurse] Prefab not found".DError();
                    return;
                }

                if (skillCancellationTokenSource.Token.IsCancellationRequested)
                    return;

                Dummy[] activeEnemies = FindObjectsOfType<Dummy>();
                List<Dummy> validEnemies = new List<Dummy>();

                foreach (var enemy in activeEnemies)
                {
                    if (enemy != null && enemy.gameObject.activeInHierarchy)
                    {
                        validEnemies.Add(enemy);
                    }
                }

                if (validEnemies.Count == 0)
                {
                    $"[MultiCurse] No enemies found to spawn curse object".DWarnning();
                    return;
                }

                int spawnCount = Mathf.Min(5, validEnemies.Count);

                List<Dummy> shuffledEnemies = new List<Dummy>(validEnemies);
                for (int i = 0; i < shuffledEnemies.Count; i++)
                {
                    int randomIndex = Random.Range(i, shuffledEnemies.Count);
                    Dummy temp = shuffledEnemies[i];
                    shuffledEnemies[i] = shuffledEnemies[randomIndex];
                    shuffledEnemies[randomIndex] = temp;
                }

                for (int i = 0; i < spawnCount; i++)
                {
                    if (skillCancellationTokenSource.Token.IsCancellationRequested)
                        return;

                    Dummy targetEnemy = shuffledEnemies[i];
                    Vector3 spawnPosition = targetEnemy.transform.position;
                    spawnPosition.y = 2.5f;

                    GameObject curseObject = Instantiate(cursePrefab, spawnPosition, Quaternion.identity);

                    CurseGlobalItemObject curseComponent = curseObject.GetComponent<CurseGlobalItemObject>();
                    if (curseComponent == null)
                    {
                        curseComponent = curseObject.AddComponent<CurseGlobalItemObject>();
                    }
                }
            }
            catch (System.OperationCanceledException)
            {
                $"[MultiCurse] Cancelled".DLog();
            }
        }
        #endregion

        #region Random Box
        /// <summary> 랜덤 박스 아이템 실행 - 랜덤 스킬 선택 </summary>
        public void ExecuteRandomBoxItem()
        {
            ReduceItemCount(ItemType.RandomBox, 1);

            randomBoxCooldownRemaining = cooldownRandomBox;

            var availableMethods = new List<GlobalItemSkillType>
            {
                GlobalItemSkillType.IceMeteo,
                GlobalItemSkillType.FireMeteo,
                GlobalItemSkillType.StoneRadial,
                GlobalItemSkillType.ThunderCrash,
                GlobalItemSkillType.MultiCurse,
            };


            int randomIndex = UnityEngine.Random.Range(0, availableMethods.Count);
            GlobalItemSkillType selectedMethod = availableMethods[randomIndex];

            ExecuteRandomBoxType(selectedMethod);
        }

        /// <summary> 랜덤 박스 타입별 실행 </summary>
        public void ExecuteRandomBoxType(GlobalItemSkillType method)
        {
            switch (method)
            {
                case GlobalItemSkillType.IceMeteo:
                    _ = Co_IceMeteo();
                    break;
                case GlobalItemSkillType.FireMeteo:
                    _ = Co_FireMeteo();
                    break;
                case GlobalItemSkillType.StoneRadial:
                    _ = Co_StoneRadial();
                    break;
                case GlobalItemSkillType.MultiCurse:
                    _ = Co_MultiCurse();
                    break;
                case GlobalItemSkillType.ThunderCrash:
                    _ = Co_ThuderCrash();
                    break;
                default:
                    $"[GlobalItem] Unknown method: {method}".DWarnning();
                    break;
            }
        }
        #endregion

        #region Cleanup
        /// <summary> 게임 오버 시 정리 - 모든 스킬 오브젝트 및 코루틴 정리 </summary>
        public void CleanupOnGameOver()
        {
            if (skillCancellationTokenSource != null && !skillCancellationTokenSource.IsCancellationRequested)
            {
                skillCancellationTokenSource.Cancel();
                skillCancellationTokenSource.Dispose();
                skillCancellationTokenSource = new System.Threading.CancellationTokenSource();
            }

            StopAllCoroutines();
            activeCoroutines.Clear();

            foreach (var skillObj in activeSkillObjects)
            {
                if (skillObj != null)
                {
                    Destroy(skillObj);
                }
            }
            activeSkillObjects.Clear();

            CleanupSkillObjectsByType<IceGlobalItemObject>();
            CleanupSkillObjectsByType<FireGlobalItemObject>();
            CleanupSkillObjectsByType<StoneGlobalItemObject>();
            CleanupSkillObjectsByType<CurseGlobalItemObject>();
            CleanupSkillObjectsByType<ThunderGlobalItemObject>();

            ResetPlayerState();

            healCooldownRemaining = 0;
            hasteCooldownRemaining = 0;
            godCooldownRemaining = 0;
            feverCooldownRemaining = 0;
            randomBoxCooldownRemaining = 0;
        }

        private void CleanupSkillObjectsByType<T>() where T : MonoBehaviour
        {
            T[] objects = FindObjectsOfType<T>();
            foreach (var obj in objects)
            {
                if (obj != null)
                {
                    Destroy(obj.gameObject);
                }
            }
        }

        private void ResetPlayerState()
        {
            var player = PlayerController.Shared;
            if (player != null)
            {
                player.IsInvincible = false;
            }
        }
        #endregion

        #region ItemCount Methods
        /// <summary> 임시 아이템 추가 </summary>
        public int AddTemporaryItem(ItemType itemType, int addCount = 1)
        {
            if (addCount <= 0)
            {
                int safetyValue = 1;
                $"[GlobalItem] Count Param Is Wrong Set Safety value\nparam Add Count : {addCount}\nsafety set value : {safetyValue}".DWarnning();
                addCount = safetyValue;
            }

            int resultCount;
            if (temporaryItems.TryGetValue(itemType, out int curCount))
            {
                resultCount = curCount + addCount;
            }
            else
            {
                resultCount = addCount;
            }
            temporaryItems[itemType] = resultCount;
            return resultCount;
        }

        /// <summary> 아이템 개수 차감 - 임시 아이템 우선 차감 후 영구 아이템 차감 </summary>
        public int ReduceItemCount(ItemType reduceItemType, int reduceCount = 1)
        {
            if (UseAbleItem(reduceItemType) == false)
            {
                $"[GlobalItem] 차감할 아이템 타입의 갯수가 존재하지 않아 차감 할 수 없습니다".DWarnning();
                return 0;
            }            
            if (reduceCount <= 0)
            {
                int safetyCount = 1;
                $"[GlobalItem] 차감 개수가 잘못되었습니다. 안전값으로 설정합니다\n요청 개수: {reduceCount} → 안전값: {safetyCount}".DWarnning();
                reduceCount = safetyCount;
            }

            int userDataItemCount = UserItem.GetItemCount(reduceItemType);
            int temporaryItemCount = temporaryItems.TryGetValue(reduceItemType, out int tempCount) ? tempCount : 0;
            int totalItemCount = userDataItemCount + temporaryItemCount;

            if (totalItemCount <= 0)
            {
                $"[GlobalItem] {reduceItemType} 아이템이 없어 차감할 수 없습니다".DWarnning();
                return 0;
            }
            if (reduceCount > totalItemCount)
            {
                $"[GlobalItem] 차감 요청 개수({reduceCount})가 보유 개수({totalItemCount})보다 많습니다. 보유한 만큼만 차감합니다".DWarnning();
                reduceCount = totalItemCount;
            }

            int actualReducedCount = 0;
            int remainingToReduce = reduceCount;
            
            if (temporaryItemCount > 0)
            {
                if (temporaryItemCount >= remainingToReduce)
                {                    
                    temporaryItems[reduceItemType] -= remainingToReduce;
                    actualReducedCount += remainingToReduce;
                    remainingToReduce = 0;
                }
                else
                {
                    temporaryItems[reduceItemType] = 0;
                    actualReducedCount += temporaryItemCount;
                    remainingToReduce -= temporaryItemCount;
                }
            }

            if (remainingToReduce > 0 && userDataItemCount > 0)
            {
                int permanentReduceCount = Mathf.Min(remainingToReduce, userDataItemCount);
                bool success = UserItem.UseItem(reduceItemType, permanentReduceCount);

                if (success)
                {
                    actualReducedCount += permanentReduceCount;
                    remainingToReduce -= permanentReduceCount;

                    if (UserDataManager.Shared != null)
                    {
                        UserDataManager.Shared.UpdateUserDataAsync().Forget();
                    }
                    else
                    {
                        $"[GlobalItem] UserDataManager.Shared가 null이어서 DB 동기화를 건너뜁니다".DWarnning();
                    }
                }
                else
                {
                    $"[GlobalItem] UserData 아이템 차감 실패".DError();
                }
            }            

            if (actualReducedCount != reduceCount)
            {
                $"[GlobalItem] 요청 차감 개수({reduceCount})와 실제 차감 개수({actualReducedCount})가 다릅니다".DWarnning();
            }

            return actualReducedCount;
        }

        /// <summary> 사용 가능한 아이템 체크 - 임시/영구 아이템 개수 확인 </summary>
        public bool UseAbleItem(ItemType targetItem)
        {
            bool isUseAble = false;
            if (temporaryItems.TryGetValue(targetItem, out int curCount))
            {
                isUseAble = 0 < curCount;
            }
            if (isUseAble == false && UserItem != null)
            {
                isUseAble = 0 < UserItem.GetItemCount(targetItem);
            }
            return isUseAble;
        }
        #endregion
    }
}