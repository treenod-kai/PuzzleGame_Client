using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬 내 카메라를 관리하며, 메인 카메라 외 중복 카메라를 자동 제거하는 컨트롤러
/// </summary>
public class CameraController : MonoBehaviour
{
    /// <summary> 이 카메라를 메인 카메라로 지정할지 여부 </summary>
    [SerializeField] private bool _isMainCamera;

    /// <summary> 메인 카메라로 등록된 인스턴스 </summary>
    private static CameraController _mainInstance;

    /// <summary> 등록된 모든 CameraController 목록 </summary>
    private static readonly List<CameraController> _instances = new List<CameraController>();

    /// <summary>
    /// 메인 카메라를 반환합니다. 메인 카메라가 없으면 null을 반환합니다.
    /// </summary>
    public static Camera MainCamera
    {
        get
        {
            if (_mainInstance != null)
            {
                return _mainInstance.GetComponent<Camera>();
            }
            return null;
        }
    }

    /// <summary>
    /// 카메라 등록 및 메인 카메라 외 중복 카메라 제거
    /// </summary>
    private void Awake()
    {
        _instances.Add(this);

        if (_isMainCamera)
        {
            if (_mainInstance != null && _mainInstance != this)
            {
                Destroy(gameObject);
                return;
            }

            _mainInstance = this;
            DontDestroyOnLoad(gameObject);
            RemoveDuplicateCameras();
        }
        else
        {
            // 메인 카메라가 이미 존재하면 자신을 제거
            if (_mainInstance != null)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// 인스턴스 목록에서 제거
    /// </summary>
    private void OnDestroy()
    {
        _instances.Remove(this);

        if (_mainInstance == this)
        {
            _mainInstance = null;
        }
    }

    /// <summary>
    /// 메인 카메라가 아닌 모든 CameraController를 제거합니다.
    /// </summary>
    private void RemoveDuplicateCameras()
    {
        for (int i = _instances.Count - 1; i >= 0; i--)
        {
            if (_instances[i] != this && _instances[i] != null)
            {
                Destroy(_instances[i].gameObject);
            }
        }
    }
}
