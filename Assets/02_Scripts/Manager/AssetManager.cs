using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
/// <summary>
/// Addressables 시스템을 이용해 게임 에셋의 로드 및 인스턴스화를 관리하는 싱글톤 클래스입니다.
/// </summary>
public class AssetManager
{
    #region Singleton
    private static AssetManager _instance;

    /// <summary> 전역 접근을 위한 싱글톤 인스턴스 </summary>
    public static AssetManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new AssetManager();
            }
            return _instance;
        }
    }
    #endregion

    /// <summary>
    /// 에셋 로드 시 필요한 인자들을 담는 구조체입니다.
    /// </summary>
    /// <typeparam name="T">로드할 에셋의 타입</typeparam>
    public struct AssetArguments<T>
    {
        /// <summary> 어드레서블 에셋 주소 </summary>
        public string address;
        /// <summary> 로드 성공 시 호출될 콜백 </summary>
        public Action<T> successCallback;
        /// <summary> 로드 실패 시 호출될 콜백 </summary>
        public Action failedCallback;
    }

    /// <summary> 로드된 에셋들을 주소별로 캐싱하는 딕셔너리 </summary>
    private Dictionary<string, object> _addressablePacket = new Dictionary<string, object>();

    /// <summary> Addressables 해제를 위해 로드 핸들을 주소별로 보관하는 딕셔너리 </summary>
    private Dictionary<string, AsyncOperationHandle> _handlePacket = new Dictionary<string, AsyncOperationHandle>();

    /// <summary> 씬 전환 시에도 캐시에서 유지할 에셋 주소 목록 (프리팹 등 공용 에셋) </summary>
    private HashSet<string> _persistentAddresses = new HashSet<string>();

    /// <summary>
    /// 에셋을 비동기적으로 로드합니다. 이미 캐싱된 경우 즉시 성공 콜백을 호출합니다.
    /// </summary>
    /// <typeparam name="T">에셋 타입</typeparam>
    /// <param name="args">로드 설정 인자</param>
    internal void LoadAssetAsync<T>(AssetArguments<T> args)
    {
        if (string.IsNullOrEmpty(args.address))
        {
            Debug.LogError("Addressable 주소가 비어 있습니다.");
            return;
        }

        if (_addressablePacket.TryGetValue(args.address, out object reObj))
        {
            args.successCallback?.Invoke((T)reObj);
            return;
        }

        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(args.address);
        handle.Completed += (op) =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                if (!_addressablePacket.ContainsKey(args.address))
                {
                    _addressablePacket.Add(args.address, op.Result);
                    _handlePacket[args.address] = handle;
                }

                args.successCallback?.Invoke(op.Result);
            }
            else
            {
                Debug.LogError($"에셋 로드 실패! 타입: {typeof(T)}, 주소: {args.address}");
                args.failedCallback?.Invoke();
            }
        };
    }

    /// <summary>
    /// 프리팹 에셋을 비동기적으로 로드하고 실제 게임 오브젝트로 생성(Instantiate)합니다.
    /// </summary>
    /// <param name="args">로드 설정 인자</param>
    /// <param name="parent">생성될 오브젝트의 부모 Transform</param>
    internal void LoadGameObjectAsync(AssetArguments<GameObject> args, Transform parent = null)
    {
        if (string.IsNullOrEmpty(args.address))
        {
            Debug.LogError("Addressable 주소가 비어 있습니다.");
            args.failedCallback?.Invoke();
            return;
        }

        Action<GameObject> originalSuccessCallback = args.successCallback;
        args.successCallback = (loadedPrefab) =>
        {
            if (loadedPrefab != null)
            {
                GameObject instance = UnityEngine.Object.Instantiate(loadedPrefab, parent);
                originalSuccessCallback?.Invoke(instance);
            }
            else
            {
                Debug.LogError($"프리팹 인스턴스화 실패! 주소: {args.address}");
                args.failedCallback?.Invoke();
            }
        };

        LoadAssetAsync(args);
    }

    /// <summary>
    /// 에셋을 동기적으로 로드합니다. (Wait Until Completion)
    /// </summary>
    /// <typeparam name="T">에셋 타입</typeparam>
    /// <param name="address">에셋 주소</param>
    /// <returns>로드된 에셋 혹은 null</returns>
    internal T LoadAsset<T>(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError($"[AssetManager] LoadAsset<{typeof(T).Name}> 호출 시 주소가 비어 있습니다.");
            return default;
        }

        if (_addressablePacket.TryGetValue(address, out object reObj))
        {
            return (T)reObj;
        }
        
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
        T result = handle.WaitForCompletion();

        if (result != null && !_addressablePacket.ContainsKey(address))
        {
            _addressablePacket.Add(address, result);
            _handlePacket[address] = handle;
        }
        return result;
    }

    /// <summary>
    /// 특정 에셋 주소를 씬 전환 시에도 유지되도록 등록합니다. (프리팹 등 공용 에셋)
    /// </summary>
    /// <param name="address">유지할 에셋 주소</param>
    internal void MarkPersistent(string address)
    {
        _persistentAddresses.Add(address);
    }

    /// <summary>
    /// 씬 전환 시 호출하여 Persistent로 등록되지 않은 에셋 캐시를 모두 해제합니다.
    /// </summary>
    /// <summary> ReleaseAll() 내부에서 재사용하는 임시 리스트 (GC 할당 방지) </summary>
    private readonly List<string> _releaseBuffer = new List<string>();

    internal void ReleaseAll()
    {
        _releaseBuffer.Clear();
        foreach (string key in _handlePacket.Keys)
        {
            if (!_persistentAddresses.Contains(key))
            {
                _releaseBuffer.Add(key);
            }
        }

        for (int i = 0; i < _releaseBuffer.Count; i++)
        {
            string key = _releaseBuffer[i];
            if (_handlePacket.TryGetValue(key, out AsyncOperationHandle handle))
            {
                Addressables.Release(handle);
            }
            _handlePacket.Remove(key);
            _addressablePacket.Remove(key);
        }
    }

    /// <summary>
    /// 프리팹을 동기적으로 로드하고 실제 게임 오브젝트로 생성합니다.
    /// </summary>
    /// <param name="address">에셋 주소</param>
    /// <param name="parent">부모 Transform</param>
    /// <returns>생성된 게임 오브젝트</returns>
    internal GameObject LoadGameObject(string address, Transform parent = null)
    {
        GameObject prefab = LoadAsset<GameObject>(address);
        if (prefab != null)
        {
            return UnityEngine.Object.Instantiate(prefab, parent);
        }
        
        Debug.LogError($"게임 오브젝트 로드 실패! 주소: {address}");
        return null;
    }
}
