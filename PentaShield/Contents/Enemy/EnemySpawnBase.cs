using chaos;
using Cysharp.Threading.Tasks;
using PentaShield;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public class EnemySpawnBase<T> : MonoBehaviour where T : class
{
    protected List<T> spawnTable;
    protected SpawnInfo SpawnInfo { get; private set; } = new SpawnInfo();

    public List<GameObject> spawnPoint = new List<GameObject>();

    public bool IsSpawn = true;

    private GameObject spawnObjParent = null;

    protected StageData stageData = null;

    #region LifeCycle
    protected virtual void Awake()
    {
        spawnObjParent = new GameObject("SpawnObjParent");
        stageData = new StageData();

        if (spawnPoint.Count == 0)
        {
            GameObject largeObj = FindObjectWithLargestFootprint(transform.GetTopParent().gameObject);
            List<Vector3> points = GenerateSpawnPoints(largeObj, 3, 2, 5);
            spawnPoint = GenerateSpawnPointObject(points);
        }
    }
    protected virtual void Start()
    {
        spawnTable = SheetManager.GetSheetObject()?.GetList<T>();

        if (RoundSystem.Shared != null)
        {
            RoundSystem.Shared.OnRoundStart += RoundSpawnInit;
            RoundSystem.Shared.OnRoundChange += RoundSpawnInit;
            RoundSystem.Shared.OnRoundEnd += RoundEndClear;
        }
    }

    protected virtual void Update()
    {       
        if (IsSpawn == false || RoundSystem.Shared?.OngameOver == true) { return; }
        float dt = Time.deltaTime;

        foreach (SpawnOperation op in SpawnInfo.SpawnOperations)
        {
            if (op.isSpawning == false) { continue; }

            if (op.SpawnPrefab == null || op.SpawnCount != -1 && op.curSpawnCount >= op.SpawnCount)
            {      
                SpawnInfo.WaitRemoveOperations.Add(op);
            }

            op.TimerUpdate(dt);
            if (op.IsSpawnable == false || spawnPoint == null || spawnPoint.Count == 0) { continue; }

            Vector3 spawnPos = spawnPoint[UnityEngine.Random.Range(0, spawnPoint.Count)].transform.position;
            GameObject spawnObj = Instantiate(op.SpawnPrefab, spawnPos, Quaternion.identity, spawnObjParent.transform);

            op.curSpawnCount++;
            op.spawnAni?.ExcuteAni(spawnObj.transform);
            op.TimerReset();
        }     

        for (int i = SpawnInfo.WaitRemoveOperations.Count - 1; i >= 0; i--)
        {  
            SpawnInfo.SpawnOperations.Remove(SpawnInfo.WaitRemoveOperations[i]);
            SpawnInfo.WaitRemoveOperations.RemoveAt(i);
        }

    }

    protected virtual void OnDestroy()
    {

    }

    #endregion LifeCycle

    // T(Table) 을 활용해야하기에 상속받은곳에서 재정의
    protected virtual List<bool> RoundSpawnInit(int curRound)
    {
        $"[EnemySpawnBase] : Override 함수만 호출되도록 해주세요!".DError();
        var _outParam = new List<bool>();
        _outParam.Add(false);
        return _outParam;
    }     
    protected virtual void RoundEndClear(int curRound)
    {
        DestroySpawnedObject();
        ClearSpawnOperation();
    }


    public async virtual UniTask AddSpawn(SpawnRequest request)
    {
        if (AbHelper.Shared == null) return;

        GameObject prefab = await AbHelper.Shared.LoadAssetAsync<GameObject>(request.SpawnObjKey);
        if (prefab == null) return;

        SpawnOperation operation = new SpawnOperation(request, prefab);
        SpawnInfo.SpawnOperations.Add(operation);
    }

    public void ClearSpawnOperation() => SpawnInfo.SpawnOperations.Clear();

    public void DestroySpawnedObject()
    {
        if (spawnObjParent == null) return;

        Transform[] childTransforms = new Transform[spawnObjParent.transform.childCount];
        for (int i = 0; i < childTransforms.Length; i++)
        {
            childTransforms[i] = spawnObjParent.transform.GetChild(i);
        }
        foreach (Transform obj in childTransforms)
        {
            if (obj != null) Destroy(obj.gameObject);
        }
    }

    protected virtual void StageEndTrriger(int curRound)
    {
        curRound--;
        stageData.Round = curRound;
        stageData.SaveTime = DateTime.UtcNow;
        stageData.Score = RewardUI.Shared?.Score ?? 0;

        var userData = UserDataManager.Shared?.Data;
        if (userData != null)
        {
            userData.StageDatas.Add(stageData);
        }

        RoundSystem.Shared?.OnStageSpawnEnd();
    }

    #region Private Methods



    /// <summary>
    /// 지정된 부모와 그 모든 자식 중에서 X-Z 평면 면적이 가장 넓은 오브젝트를 찾습니다.
    /// </summary>
    /// <param name="parent">검색을 시작할 최상위 부모 오브젝트</param>
    /// <returns>가장 넓은 바닥 면적을 가진 게임 오브젝트. 렌더러가 없으면 null을 반환합니다.</returns>
    private GameObject FindObjectWithLargestFootprint(GameObject parent)
    {
        if (parent == null) return null;

        Renderer[] renderers = parent.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0) return null;

        GameObject largestObject = null;
        float maxArea = -1f;

        foreach (Renderer renderer in renderers)
        {
            Vector3 size = renderer.bounds.size;
            float currentArea = size.x * size.z;

            if (currentArea > maxArea)
            {
                maxArea = currentArea;
                largestObject = renderer.gameObject;
            }
        }

        return largestObject;
    }

    /// <summary>
    /// 주어진 오브젝트의 바운드를 기준으로 그리드 형태의 스폰 포인트를 생성합니다.
    /// </summary>
    /// <param name="floorObject">기준이 될 바닥 오브젝트</param>
    /// <param name="gridX">가로 방향으로 나눌 개수</param>
    /// <param name="gridZ">세로 방향으로 나눌 개수</param>
    /// <param name="margin">바운드 가장자리에서 안쪽으로 둘 여백</param>
    /// <returns>생성된 스폰 포인트 목록</returns>
    private List<Vector3> GenerateSpawnPoints(GameObject floorObject, int gridX, int gridZ, float margin)
    {
        var points = new List<Vector3>();
        if (floorObject == null) return points;

        var renderer = floorObject.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("오브젝트에 Renderer가 없습니다.", floorObject);
            return points;
        }

        // 1. 원본 바운드를 가져옵니다.
        Bounds originalBounds = renderer.bounds;

        // 2. margin을 적용하여 '안전 영역'의 min, max를 계산합니다.
        Vector3 safeMin = originalBounds.min + new Vector3(margin, 0, margin);
        Vector3 safeMax = originalBounds.max - new Vector3(margin, 0, margin);

        // 안전 영역의 전체 크기를 계산합니다.
        Vector3 safeSize = safeMax - safeMin;
        if (safeSize.x < 0 || safeSize.z < 0)
        {
            Debug.LogWarning("Margin이 너무 커서 안전 영역을 만들 수 없습니다.", floorObject);
            return points;
        }

        // 3. 중첩 for문을 이용해 그리드를 순회하며 포인트를 계산합니다.
        for (int z = 0; z < gridZ; z++)
        {
            // 0.0 ~ 1.0 사이의 정규화된 z 위치
            float tZ = (gridZ == 1) ? 0.5f : (float)z / (gridZ - 1);

            for (int x = 0; x < gridX; x++)
            {
                // 0.0 ~ 1.0 사이의 정규화된 x 위치
                float tX = (gridX == 1) ? 0.5f : (float)x / (gridX - 1);

                // 정규화된 위치를 이용해 안전 영역 내의 실제 월드 좌표를 계산
                Vector3 point = new Vector3(
                    safeMin.x + safeSize.x * tX,
                    originalBounds.max.y, // 스폰될 높이는 바닥의 최상단으로 설정
                    safeMin.z + safeSize.z * tZ
                );
                points.Add(point);
            }
        }
        return points;
    }

    private List<GameObject> GenerateSpawnPointObject(List<Vector3> points)
    {
        List<GameObject> result = new List<GameObject>();
        if (points == null) { return result; }

        float halfCount = points.Count / 2;
        for (int i = 0; i < points.Count; i++)
        {
            GameObject obj = new GameObject($"AutoSpawnPoint_{i}");
            obj.transform.parent = transform;
            obj.transform.position = points[i];
            if (halfCount < i)
            {
                obj.transform.rotation = Quaternion.Euler(new Vector3(0f, 180f, 0f));
            }

            result.Add(obj);
        }

        return result;
    }
    #endregion 

}    
