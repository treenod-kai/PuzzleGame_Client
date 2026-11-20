using System;
using UnityEngine;

/// <summary>
/// 각 씬에서 팝업의 생성과 제거를 직접 담당하는 컨트롤러 베이스 클래스.
/// 씬별 팝업 루트 Transform을 보유하며, DomainManager의 요청을 받아 실제 오브젝트를 관리합니다.
/// </summary>
public abstract class PopupController : MonoBehaviour
{
    /// <summary> 컨트롤러 식별 이름 (씬 이름과 대응) </summary>
    public abstract string ControllerName { get; }

    /// <summary>
    /// 활성화 시 DomainManager에 등록
    /// </summary>
    protected virtual void OnEnable()
    {
        if (DomainManager.Instance != null)
        {
            DomainManager.Instance.Register(this);
        }
    }

    /// <summary>
    /// 비활성화 시 DomainManager에서 해제
    /// </summary>
    protected virtual void OnDisable()
    {
        if (DomainManager.Instance != null)
        {
            DomainManager.Instance.Unregister(this);
        }
    }

    /// <summary><paramref name="popupName"/> 프리팹을 불러와 <paramref name="onCreated"/>로 전달하고, 실패하거나 씬을 떠났으면 <paramref name="onFailed"/>를 호출한다.</summary>
    public void CreatePopup(string popupName, Action<PopupBase> onCreated, Action onFailed = null)
    {
        string address = $"Popup/{popupName}";

        /// <summary>비동기로 받은 <paramref name="prefab"/>을 요청한 팝업 루트에 생성한다.</summary>
        void OnPrefabLoaded(GameObject prefab)
        {
            if (this == null || !isActiveAndEnabled || prefab == null)
            {
                OnLoadFailed();
                return;
            }
            GameObject instance = Instantiate(prefab, transform);
            PopupBase popup = instance.GetComponent<PopupBase>();
            if (popup == null)
            {
                popup = instance.AddComponent<PopupBase>();
            }
            popup.SetPopupName(popupName);
            if (onCreated != null)
            {
                onCreated.Invoke(popup);
            }
        }

        /// <summary>로드 실패를 기록하고 요청자에게 입력을 복구할 기회를 준다.</summary>
        void OnLoadFailed()
        {
            if (this != null && isActiveAndEnabled)
            {
                Debug.LogError($"[{ControllerName}PopupController] 팝업 로드 실패: {popupName} (주소: {address})");
            }
            if (onFailed != null)
            {
                onFailed.Invoke();
            }
        }

        AssetManager.Instance.LoadAssetAsync(new AssetManager.AssetArguments<GameObject>
        {
            address = address,
            successCallback = OnPrefabLoaded,
            failedCallback = OnLoadFailed
        });
    }

    /// <summary>
    /// 팝업의 닫힘 연출을 실행한 뒤 오브젝트를 파괴합니다.
    /// </summary>
    /// <param name="popup">제거할 팝업</param>
    public void DestroyPopup(PopupBase popup)
    {
        if (popup == null || popup.gameObject == null)
        {
            return;
        }

        /// <summary> 닫힘 연출이 끝난 팝업을 파괴합니다. </summary>
        void OnPopupClosed()
        {
            if (popup != null && popup.gameObject != null)
            {
                Destroy(popup.gameObject);
            }
        }

        // 열림 연출 중의 뒤로 가기도 닫힘 완료 이벤트를 거치게 한다.
        if (popup.IsAnimating)
        {
            popup.OnOpened += CloseAfterOpen;
        }
        else
        {
            popup.Close(OnPopupClosed);
        }

        /// <summary>열림 연출을 마친 <paramref name="opened"/>의 닫힘 연출을 시작한다.</summary>
        void CloseAfterOpen(PopupBase opened)
        {
            opened.OnOpened -= CloseAfterOpen;
            opened.Close(OnPopupClosed);
        }
    }
}
