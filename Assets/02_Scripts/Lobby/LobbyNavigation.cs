using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>방치 로비 위에 네 패널을 열고 하단 버튼과 좌우 스와이프를 같은 탭 전환으로 연결한다.</summary>
public sealed class LobbyNavigation : TabController, IDragHandler, IEndDragHandler
{
    /// <summary>하단 버튼과 선택 표시, 이 버튼이 여는 패널을 묶는다.</summary>
    [Serializable]
    private sealed class TabItem
    {
        public Button Button;
        public GameObject Selection;
        public LobbyOverlayTab Panel;
    }

    private const int MainIndex = 2;
    [SerializeField] private TabItem[] _items;
    [SerializeField] private GameObject _dim;
    [SerializeField] private RectTransform _safeArea;
    private CanvasGroup _mainControls;
    private CanvasGroup _lobbyControls;
    private int _selected = MainIndex;
    private Pointer _worldPointer;
    private Vector2 _startPosition;

    public override string ControllerName => "Lobby";
    public override bool CanOpenTab => (_lobbyControls == null || _lobbyControls.interactable) &&
        (DomainManager.Instance == null || !(DomainManager.Instance.Peek() is PopupBase));
    internal int Direction { get; private set; } = 1;

    /// <summary>패널 뒤의 <paramref name="mainControls"/>와 팝업이 입력을 잠그는 <paramref name="lobbyControls"/>를 연결한다.</summary>
    internal void BindMainControls(CanvasGroup mainControls, CanvasGroup lobbyControls)
    {
        _mainControls = mainControls;
        _lobbyControls = lobbyControls;
    }

    /// <summary>버튼과 비활성 패널의 도메인 이름을 연결하고 메인을 선택한다.</summary>
    private void Awake()
    {
        for (int i = 0; i < _items.Length; i++)
        {
            int index = i;
            TabItem item = _items[i];
            item.Button.onClick.AddListener(() => SelectTab(index));
            item.Selection.SetActive(i == MainIndex);
            if (item.Panel != null)
            {
                item.Panel.Bind(this, i);
                item.Panel.OnDeactivated += OnPanelClosed;
            }
        }
        _dim.SetActive(false);
        FitSafeArea();
    }

    /// <summary>로비 단독 실행도 공용 매니저가 준비되면 탭을 등록한다.</summary>
    protected override void OnEnable() => StartCoroutine(RegisterWhenReady());

    /// <summary>공용 씬 초기화 순서와 관계없이 현재 컨트롤러를 등록한다.</summary>
    private IEnumerator RegisterWhenReady()
    {
        while (DomainManager.Instance == null)
        {
            yield return null;
        }
        base.OnEnable();
    }

    /// <summary>등록 대기 및 진행 중인 화면 스와이프를 해제한다.</summary>
    protected override void OnDisable()
    {
        StopAllCoroutines();
        _worldPointer = null;
        base.OnDisable();
    }

    /// <summary>화면의 안전 영역에 패널과 하단 버튼을 맞춘다.</summary>
    private void FitSafeArea()
    {
        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }
        Rect safe = Screen.safeArea;
        Vector2 screen = new Vector2(Screen.width, Screen.height);
        Vector2 min = safe.min / screen;
        Vector2 max = safe.max / screen;
        if (_safeArea.anchorMin != min || _safeArea.anchorMax != max)
        {
            _safeArea.anchorMin = min;
            _safeArea.anchorMax = max;
        }
    }

    /// <summary>메인의 빈 공간에서 시작한 스와이프만 관찰하여 방치 화면의 포인터 입력을 보존한다.</summary>
    private void LateUpdate()
    {
        FitSafeArea();
        Pointer pointer = Pointer.current;
        if (_selected != MainIndex || pointer == null || !CanOpenTab)
        {
            _worldPointer = null;
            return;
        }

        if (pointer.press.wasPressedThisFrame && EventSystem.current != null &&
            !EventSystem.current.IsPointerOverGameObject(pointer.deviceId))
        {
            _worldPointer = pointer;
            _startPosition = pointer.position.ReadValue();
        }
        if (_worldPointer != null && _worldPointer.press.wasReleasedThisFrame)
        {
            Vector2 delta = _worldPointer.position.ReadValue() - _startPosition;
            _worldPointer = null;
            SwitchBySwipe(delta);
        }
    }

    /// <summary>포커스를 잃으면 <paramref name="focused"/> 상태에 맞춰 미완료 제스처를 버린다.</summary>
    private void OnApplicationFocus(bool focused)
    {
        if (!focused)
        {
            _worldPointer = null;
        }
    }

    /// <summary>드래그한 <paramref name="eventData"/>가 하단 버튼의 클릭으로도 처리되는 것을 막는다.</summary>
    public void OnDrag(PointerEventData eventData) => eventData.eligibleForClick = false;

    /// <summary>버튼이나 패널에서 끝낸 <paramref name="eventData"/>의 좌우 이동 거리를 판정한다.</summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            SwitchBySwipe(eventData.position - eventData.pressPosition);
        }
    }

    /// <summary><paramref name="delta"/>가 충분한 가로 이동일 때 인접한 탭 하나를 선택한다.</summary>
    private void SwitchBySwipe(Vector2 delta)
    {
        float distance = Mathf.Abs(delta.x);
        if (distance < Mathf.Min(Screen.width, Screen.height) * 0.08f || distance <= Mathf.Abs(delta.y) * 1.3f)
        {
            return;
        }
        SelectTab(_selected + (delta.x < 0f ? 1 : -1));
    }

    /// <summary><paramref name="index"/>의 패널을 열거나 메인으로 돌아간다.</summary>
    private void SelectTab(int index)
    {
        DomainManager manager = DomainManager.Instance;
        if (index < 0 || index >= _items.Length || index == _selected ||
            manager == null || manager.GetTabController(ControllerName) != this || !CanOpenTab)
        {
            return;
        }

        LobbyOverlayTab previous = _items[_selected].Panel;
        MarkSelected(index);
        if (index == MainIndex)
        {
            manager.Close(previous.TabName);
        }
        else
        {
            manager.OpenTab(_items[index].Panel.TabName);
        }
    }

    /// <summary>선택한 <paramref name="index"/>를 강조하고 패널이 열릴 때 뒤쪽 입력을 막는다.</summary>
    internal void MarkSelected(int index)
    {
        if (_selected != index)
        {
            Direction = Math.Sign(index - _selected);
            _items[_selected].Selection.SetActive(false);
            _selected = index;
            _items[index].Selection.SetActive(true);
        }
        if (index != MainIndex)
        {
            ShowBackdrop(true);
        }
    }

    /// <summary>뒤로 가기 등으로 현재 <paramref name="index"/>가 닫히면 메인을 선택한다.</summary>
    internal void PanelClosing(int index)
    {
        if (_selected == index)
        {
            MarkSelected(MainIndex);
        }
    }

    /// <summary><paramref name="closed"/>에 이어 마지막 패널까지 닫혔으면 메인 입력을 복구한다.</summary>
    private void OnPanelClosed(TabBase closed)
    {
        if (_selected != MainIndex)
        {
            return;
        }
        foreach (TabItem item in _items)
        {
            if (item.Panel != null && item.Panel != closed && item.Panel.IsAnimating)
            {
                return;
            }
        }
        ShowBackdrop(false);
    }

    /// <summary><paramref name="visible"/>에 따라 전체 화면 탭 배경을 표시하고 메인 UI 입력을 전환한다.</summary>
    private void ShowBackdrop(bool visible)
    {
        _dim.SetActive(visible);
        if (_mainControls != null)
        {
            _mainControls.interactable = !visible;
            _mainControls.blocksRaycasts = !visible;
        }
    }
}
