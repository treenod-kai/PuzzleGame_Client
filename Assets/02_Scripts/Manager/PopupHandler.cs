using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 개별 팝업의 컨텐츠 로직을 담당하는 핸들러 베이스 클래스.
/// PopupBase와 같은 게임오브젝트에 부착되어 팝업별 고유 로직을 구현합니다.
/// 버튼 콜백은 UIButton의 SendMessage 방식으로 인스펙터에서 직접 연결합니다.
/// </summary>
[RequireComponent(typeof(PopupBase))]
public abstract class PopupHandler : MonoBehaviour
{
    /// <summary> 연결된 PopupBase 인스턴스 </summary>
    protected PopupBase _popup;

    /// <summary>
    /// PopupBase를 가져오고 이벤트를 구독합니다.
    /// 하위 클래스에서 override 시 반드시 base.Awake()를 호출해야 합니다.
    /// </summary>
    protected virtual void Awake()
    {
        _popup = GetComponent<PopupBase>();
    }

    #region 공용 팝업 조작

    /// <summary>
    /// 현재 팝업을 닫습니다. PopupBase를 통해 DomainManager에 닫기를 요청합니다.
    /// 애니메이션 진행 중에는 무시됩니다.
    /// </summary>
    protected void ClosePopup()
    {
        if (_popup.IsAnimating)
        {
            return;
        }

        _popup.RequestClose();
    }

    /// <summary>
    /// 최상위 팝업을 닫고 이전 상태로 돌아갑니다. PopupBase를 통해 요청합니다.
    /// 애니메이션 진행 중에는 무시됩니다.
    /// </summary>
    protected void BackPopup()
    {
        if (_popup.IsAnimating)
        {
            return;
        }

        _popup.RequestBack();
    }

    #endregion

}
