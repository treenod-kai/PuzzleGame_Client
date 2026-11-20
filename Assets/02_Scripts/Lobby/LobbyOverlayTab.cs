using System.Collections;
using UnityEngine;

/// <summary>로비 위에 놓인 패널 한 장을 선택 방향으로 밀어 열거나 닫는다.</summary>
[RequireComponent(typeof(CanvasGroup))]
public sealed class LobbyOverlayTab : TabBase
{
    private const float SlideSeconds = 0.24f;
    private LobbyNavigation _navigation;
    private RectTransform _rect;
    private CanvasGroup _input;
    private int _index;
    private float _offset;
    private bool _hidden = true;

    /// <summary><paramref name="navigation"/>과 탭 순서 <paramref name="index"/>를 비활성 상태에서 연결한다.</summary>
    internal void Bind(LobbyNavigation navigation, int index)
    {
        _navigation = navigation;
        _index = index;
        _rect = (RectTransform)transform;
        _input = GetComponent<CanvasGroup>();
        SetInput(false);
        SetTabName(gameObject.name);
    }

    /// <summary>되돌아오는 패널은 현재 위치에서, 닫혀 있던 패널은 화면 가장자리에서 출발한다.</summary>
    protected override void OnActivateStart()
    {
        StopAllCoroutines();
        SetInput(false);
        _navigation.MarkSelected(_index);
        transform.SetAsLastSibling();
        if (_hidden)
        {
            SetOffset(_navigation.Direction);
        }
        _hidden = false;
    }

    /// <summary>기존 이동을 중단하고 현재 위치에서 퇴장할 준비를 한다.</summary>
    protected override void OnDeactivateStart()
    {
        StopAllCoroutines();
        SetInput(false);
        _navigation.PanelClosing(_index);
    }

    /// <summary>패널이 중앙에 도착하면 콘텐츠 입력을 허용한다.</summary>
    protected override void OnActivateComplete() => SetInput(true);

    /// <summary><paramref name="enabled"/>일 때에만 패널 콘텐츠가 클릭과 키보드 선택을 받는다.</summary>
    private void SetInput(bool enabled)
    {
        _input.interactable = enabled;
        _input.blocksRaycasts = enabled;
    }

    /// <summary>패널을 화면 중앙에 안착시킨다.</summary>
    protected override IEnumerator ActivateAnimation() => Slide(0f);

    /// <summary>새로 선택한 탭의 반대쪽 가장자리로 패널을 보낸다.</summary>
    protected override IEnumerator DeactivateAnimation() => Slide(-_navigation.Direction);

    /// <summary>다음에 열 때 화면 밖에서 다시 출발하도록 표시한다.</summary>
    protected override void OnDeactivateComplete() => _hidden = true;

    /// <summary>현재 위치에서 목표 화면 너비 비율 <paramref name="target"/>까지 부드럽게 이동한다.</summary>
    private IEnumerator Slide(float target)
    {
        float start = _offset;
        float duration = SlideSeconds * Mathf.Abs(target - start);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float t = 1f - elapsed / duration;
            SetOffset(Mathf.Lerp(start, target, 1f - t * t * t));
            yield return null;
        }
        SetOffset(target);
    }

    /// <summary>화면 너비 단위의 <paramref name="offset"/>을 앵커에 적용해 회전 중에도 위치를 맞춘다.</summary>
    private void SetOffset(float offset)
    {
        _offset = offset;
        _rect.anchorMin = new Vector2(offset, 0f);
        _rect.anchorMax = new Vector2(1f + offset, 1f);
    }
}
