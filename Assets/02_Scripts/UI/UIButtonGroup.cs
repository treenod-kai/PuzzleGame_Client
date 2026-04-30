using UnityEngine;

/// <summary>
/// 여러 UIButton의 선택 스프라이트 상태만 관리하는 UI 그룹 컴포넌트입니다.
/// </summary>
public class UIButtonGroup : MonoBehaviour
{
    /// <summary> 그룹에서 관리할 버튼 목록 </summary>
    [SerializeField]
    private UIButton[] _buttons;

    /// <summary> 선택되지 않은 버튼에 적용할 스프라이트 </summary>
    [SerializeField]
    private Sprite _normalSprite;

    /// <summary> 선택된 버튼에 적용할 스프라이트 </summary>
    [SerializeField]
    private Sprite _selectedSprite;

    /// <summary> 시작 시 선택할 기본 인덱스 </summary>
    [SerializeField]
    private int _defaultIndex;

    /// <summary> Awake 시 기본 선택 뷰를 적용할지 여부 </summary>
    [SerializeField]
    private bool _selectOnAwake = true;

    /// <summary> 현재 선택된 인덱스 </summary>
    private int _selectedIndex = -1;

    /// <summary> 현재 선택된 인덱스입니다. 선택이 없으면 -1입니다. </summary>
    public int SelectedIndex
    {
        get
        {
            return _selectedIndex;
        }
    }

    /// <summary>
    /// 시작 시 기본 선택 뷰를 적용합니다.
    /// </summary>
    private void Awake()
    {
        if (_buttons == null || _buttons.Length == 0)
        {
            return;
        }

        if (_selectOnAwake)
        {
            Select(_defaultIndex);
        }
        else
        {
            RefreshButtons();
        }
    }

    /// <summary>
    /// 문자열 인덱스 값으로 선택 뷰를 변경합니다.
    /// </summary>
    /// <param name="value">선택할 버튼 인덱스 문자열입니다.</param>
    public void Select(string value)
    {
        if (!int.TryParse(value, out int index))
        {
            Debug.LogError($"UIButtonGroup_{gameObject.name}: Value is not index. ({value})");
            return;
        }

        Select(index);
    }

    /// <summary>
    /// 인덱스로 선택 뷰를 변경합니다.
    /// </summary>
    /// <param name="index">선택할 버튼 인덱스입니다.</param>
    public void Select(int index)
    {
        if (!IsValidIndex(index))
        {
            Debug.LogError($"UIButtonGroup_{gameObject.name}: Index is out of range. ({index})");
            return;
        }

        if (_selectedIndex == index)
        {
            return;
        }

        _selectedIndex = index;
        RefreshButtons();
    }

    /// <summary>
    /// 현재 선택 뷰를 모두 해제합니다.
    /// </summary>
    public void ClearSelection()
    {
        _selectedIndex = -1;
        RefreshButtons();
    }

    /// <summary>
    /// 모든 버튼의 스프라이트를 현재 선택 인덱스 기준으로 갱신합니다.
    /// </summary>
    private void RefreshButtons()
    {
        if (_buttons == null)
        {
            return;
        }

        for (int i = 0; i < _buttons.Length; i++)
        {
            if (_buttons[i] == null)
            {
                continue;
            }

            Sprite nextSprite = i == _selectedIndex ? _selectedSprite : _normalSprite;
            _buttons[i].SetSprite(nextSprite);
        }
    }

    /// <summary>
    /// 인덱스가 버튼 배열 범위 안에 있는지 확인합니다.
    /// </summary>
    /// <param name="index">검사할 버튼 인덱스입니다.</param>
    /// <returns>유효한 인덱스이면 true입니다.</returns>
    private bool IsValidIndex(int index)
    {
        return _buttons != null && index >= 0 && index < _buttons.Length && _buttons[index] != null;
    }
}
