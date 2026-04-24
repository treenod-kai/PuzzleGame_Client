using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 오브젝트의 재사용을 관리하여 성능을 최적화하는 풀링 매니저 클래스입니다.
/// </summary>
public class PoolManager : MonoBehaviour
{
    #region Singleton
    private static PoolManager _instance;

    /// <summary> 전역 접근을 위한 싱글톤 인스턴스 </summary>
    public static PoolManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject obj = new GameObject("PoolManager");
                _instance = obj.AddComponent<PoolManager>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }
    #endregion

    /// <summary> 프리팹별로 비활성화된 오브젝트들을 담아두는 저장소 </summary>
    private Dictionary<GameObject, Queue<GameObject>> _pools = new Dictionary<GameObject, Queue<GameObject>>();

    /// <summary> 생성된 인스턴스가 어떤 프리팹으로부터 만들어졌는지 기록 (반납 시 활용) </summary>
    private Dictionary<GameObject, GameObject> _instanceToPrefab = new Dictionary<GameObject, GameObject>();

    /// <summary> 프리팹당 풀에 보관할 수 있는 최대 오브젝트 수 </summary>
    private const int MAX_POOL_SIZE = 50;

    /// <summary>
    /// 특정 프리팹의 인스턴스를 가져옵니다. 풀에 있다면 꺼내오고, 없다면 새로 생성합니다.
    /// </summary>
    /// <param name="prefab">가져올 프리팹 원본</param>
    /// <param name="parent">부모 Transform</param>
    /// <returns>활성화된 게임 오브젝트 인스턴스</returns>
    public GameObject Get(GameObject prefab, Transform parent = null)
    {
        if (prefab == null)
        {
            return null;
        }

        if (!_pools.ContainsKey(prefab))
        {
            _pools.Add(prefab, new Queue<GameObject>());
        }

        GameObject instance;
        if (_pools[prefab].Count > 0)
        {
            instance = _pools[prefab].Dequeue();
            if (instance != null)
            {
                instance.transform.SetParent(parent);
                instance.SetActive(true);
            }
            else
            {
                // 풀 내부의 오브젝트가 파괴된 경우 새로 생성
                instance = Instantiate(prefab, parent);
                _instanceToPrefab[instance] = prefab;
            }
        }
        else
        {
            instance = Instantiate(prefab, parent);
            _instanceToPrefab[instance] = prefab;
        }

        return instance;
    }

    /// <summary>
    /// 사용이 끝난 인스턴스를 풀로 반납하고 비활성화합니다.
    /// </summary>
    /// <param name="instance">반납할 게임 오브젝트 인스턴스</param>
    public void Release(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        if (_instanceToPrefab.TryGetValue(instance, out GameObject prefab))
        {
            if (!_pools.ContainsKey(prefab))
            {
                _pools.Add(prefab, new Queue<GameObject>());
            }

            if (_pools[prefab].Count >= MAX_POOL_SIZE)
            {
                _instanceToPrefab.Remove(instance);
                Destroy(instance);
                return;
            }

            instance.SetActive(false);
            instance.transform.SetParent(this.transform);
            _pools[prefab].Enqueue(instance);
        }
        else
        {
            // 풀을 통해 생성된 오브젝트가 아니라면 그냥 파괴
            Debug.LogWarning($"[PoolManager] 풀에 등록되지 않은 오브젝트를 반납 시도했습니다. 파괴 처리합니다: {instance.name}");
            Destroy(instance);
        }
    }

    /// <summary>
    /// 모든 풀을 비우고 생성된 오브젝트들을 제거합니다.
    /// </summary>
    public void Clear()
    {
        foreach (var pool in _pools.Values)
        {
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }
        _pools.Clear();
        _instanceToPrefab.Clear();
    }
}
