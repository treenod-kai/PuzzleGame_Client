using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 모든 팝업 프리팹에 부착하는 공통 베이스 클래스.
/// 도메인 경로 추적, 공용 열림/닫힘 애니메이션(딤 배경 + 컨텐츠 스케일)을 담당합니다.
/// 개별 팝업 로직은 PopupHandler를 통해 구현합니다.
/// </summary>
public class PopupBase : MonoBehaviour, IDomainNode
{
    #region 팝업 도메인 관련

    /// <summary> 팝업 식별 이름 (도메인 경로 추적에 사용) </summary>
    public string PopupName { get; private set; }

    /// <inheritdoc/>
    public string DomainName => PopupName;

    /// <inheritdoc/>
    public DomainType DomainType => DomainType.Popup;

    #endregion

    /// <summary> 팝업 배경 어둡게 처리할 이미지 </summary>
    [SerializeField]
    private Image _dimBackground;

    /// <summary> 팝업 컨텐츠 패널 (스케일 애니메이션 대상) </summary>
    [SerializeField]
    private RectTransform _contentPanel;

    /// <summary>중앙 정렬된 본문을 감싸는 안전 영역 프레임과 터치 높이 기준 버튼.</summary>
    [SerializeField] private RectTransform _safeFrame;
    [SerializeField] private Button _referenceButton;
    private const float MinimumTouchHeight = 44f;

    /// <summary> 열림/닫힘 연출 시간 (초) </summary>
    [SerializeField]
    private float _animationDuration = 0.3f;

    /// <summary> 팝업 열림 완료 시 발생하는 이벤트 </summary>
    public event Action<PopupBase> OnOpened;

    /// <summary> 팝업 닫힘 완료 시 발생하는 이벤트 </summary>
    public event Action<PopupBase> OnClosed;

    /// <summary> 현재 연출 진행 중 여부 </summary>
    public bool IsAnimating { get; private set; }

    /// <summary> 현재 진행 중인 DOTween 시퀀스 </summary>
    private Sequence _currentSequence;

    /// <summary>팝업이 열려 있는 동안 해상도와 기기 안전 영역 변경을 반영한다.</summary>
    private void LateUpdate() => FitSafeArea();

    /// <summary>안전 영역에 본문을 맞추고, 여유가 있으면 가장 작은 버튼도 누를 수 있는 크기로 확대한다.</summary>
    private void FitSafeArea()
    {
        if (_safeFrame == null || _referenceButton == null)
        {
            return;
        }
        Vector2 screen = new Vector2(Screen.width, Screen.height);
        Rect safe = Screen.safeArea;
        Vector2 size = _safeFrame.rect.size;
        if (screen.x <= 0f || screen.y <= 0f || safe.width <= 0f || safe.height <= 0f || size.x <= 0f || size.y <= 0f)
        {
            return;
        }

        RectTransform root = (RectTransform)_safeFrame.parent;
        Vector2 unitsPerPixel = root.rect.size / screen;
        Vector2 available = Vector2.Scale(safe.size, unitsPerPixel);
        float fit = Mathf.Min((available.x - 24f) / size.x, (available.y - 24f) / size.y);
        float buttonHeight = ((RectTransform)_referenceButton.transform).rect.height;
        float readable = Mathf.Max(1f, MinimumTouchHeight * unitsPerPixel.y / Mathf.Max(1f, buttonHeight));
        float scale = Mathf.Clamp(fit, 0f, readable);
        Vector2 center = Vector2.Scale(safe.center - screen * 0.5f, unitsPerPixel);
        if (_safeFrame.anchoredPosition != center || !Mathf.Approximately(_safeFrame.localScale.x, scale))
        {
            _safeFrame.anchoredPosition = center;
            _safeFrame.localScale = Vector3.one * scale;
        }
    }

    /// <summary>
    /// 팝업 이름을 설정합니다. DomainManager에서 생성 시 호출됩니다.
    /// </summary>
    /// <param name="popupName">팝업 식별 이름</param>
    public void SetPopupName(string popupName)
    {
        PopupName = popupName;
    }

    /// <summary>
    /// 팝업 열림 처리를 시작합니다. DomainManager에서 호출됩니다.
    /// 공용 애니메이션(딤 페이드인 + 컨텐츠 스케일) 후 OnOpened 이벤트가 발생합니다.
    /// </summary>
    public void Open()
    {
        if (IsAnimating)
        {
            return;
        }

        gameObject.SetActive(true);
        FitSafeArea();
        InitializeOpenState();
        StartCoroutine(OpenRoutine());
    }

    /// <summary>
    /// 팝업 닫힘 처리를 시작합니다. DomainManager에서 호출됩니다.
    /// 공용 애니메이션(컨텐츠 스케일 축소 + 딤 페이드아웃) 후 OnClosed 이벤트가 발생합니다.
    /// </summary>
    /// <param name="onComplete">닫힘 연출 완료 후 호출되는 콜백 (오브젝트 파괴 등)</param>
    public void Close(Action onComplete = null)
    {
        if (IsAnimating)
        {
            return;
        }

        StartCoroutine(CloseRoutine(onComplete));
    }

    /// <summary>
    /// 팝업 닫기를 요청합니다. PopupHandler에서 호출됩니다.
    /// DomainManager를 통해 도메인 경로를 정리하고 닫힘 연출을 수행합니다.
    /// </summary>
    public void RequestClose()
    {
        DomainManager.Instance.Close(PopupName);
    }

    /// <summary>
    /// 최상위 도메인을 닫고 이전 상태로 돌아갑니다. PopupHandler에서 호출됩니다.
    /// </summary>
    public void RequestBack()
    {
        DomainManager.Instance.Back();
    }

    /// <summary>
    /// 오브젝트 파괴 시 진행 중인 DOTween 시퀀스를 정리합니다.
    /// </summary>
    private void OnDestroy()
    {
        if (_currentSequence != null)
        {
            _currentSequence.Kill();
        }
        OnOpened = null;
        OnClosed = null;
    }

    /// <summary>
    /// 열림 연출 시작 전 초기 상태를 설정합니다.
    /// 딤 배경 투명, 컨텐츠 패널 스케일 0으로 세팅합니다.
    /// </summary>
    private void InitializeOpenState()
    {
        if (_dimBackground != null)
        {
            Color color = _dimBackground.color;
            color.a = 0f;
            _dimBackground.color = color;
        }

        if (_contentPanel != null)
        {
            _contentPanel.localScale = Vector3.zero;
        }
    }

    /// <summary>
    /// 열림 연출 코루틴. 딤 페이드인 + 컨텐츠 스케일 팝 연출을 수행합니다.
    /// </summary>
    /// <returns>코루틴 열거자</returns>
    private IEnumerator OpenRoutine()
    {
        IsAnimating = true;

        _currentSequence = DOTween.Sequence();

        if (_dimBackground != null)
        {
            _currentSequence.Join(_dimBackground.DOFade(0.5f, _animationDuration));
        }

        if (_contentPanel != null)
        {
            _currentSequence.Join(
                _contentPanel.DOScale(Vector3.one, _animationDuration)
                    .SetEase(_safeFrame != null ? Ease.OutCubic : Ease.OutBack)
            );
        }

        yield return _currentSequence.WaitForCompletion();

        _currentSequence = null;
        IsAnimating = false;
        if (OnOpened != null)
        {
            OnOpened.Invoke(this);
        }
    }

    /// <summary>
    /// 닫힘 연출 코루틴. 컨텐츠 스케일 축소 + 딤 페이드아웃 연출을 수행합니다.
    /// </summary>
    /// <param name="onComplete">연출 완료 후 호출되는 콜백</param>
    /// <returns>코루틴 열거자</returns>
    private IEnumerator CloseRoutine(Action onComplete)
    {
        IsAnimating = true;

        _currentSequence = DOTween.Sequence();

        if (_contentPanel != null)
        {
            _currentSequence.Join(
                _contentPanel.DOScale(Vector3.zero, _animationDuration)
                    .SetEase(_safeFrame != null ? Ease.InCubic : Ease.InBack)
            );
        }

        if (_dimBackground != null)
        {
            _currentSequence.Join(_dimBackground.DOFade(0f, _animationDuration));
        }

        yield return _currentSequence.WaitForCompletion();

        _currentSequence = null;
        IsAnimating = false;
        if (OnClosed != null)
        {
            OnClosed.Invoke(this);
        }
        if (onComplete != null)
        {
            onComplete.Invoke();
        }
    }
}
