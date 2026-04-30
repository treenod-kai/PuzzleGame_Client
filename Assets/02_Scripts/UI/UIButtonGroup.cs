using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 여러 UIButton 중 하나만 선택 상태로 유지하는 UI 그룹 컴포넌트입니다.
/// UIButton의 SendMessage 콜백을 Select 메서드로 연결해서 사용합니다.
/// </summary>
public class UIButtonGroup : MonoBehaviour
{
    /// <summary>
    /// 버튼 선택 변경 이벤트입니다.
    /// 선택된 인덱스와 키 값을 전달합니다.
    /// </summary>
    [Serializable]
    public class SelectionChangedEvent : UnityEvent<int, string>
    {
    }

    /// <summary>
    /// 그룹에서 관리하는 개별 버튼 항목입니다.
    /// </summary>
    [Serializable]
    public class UIButtonGroupItem
    {
        /// <summary> 문자열 기반 선택에 사용하는 키 값 </summary>
        [SerializeField]
        private string _key;

        /// <summary> 선택되지 않았을 때 노출할 오브젝트 </summary>
        [SerializeField]
        private GameObject _normalView;

        /// <summary> 선택되었을 때 노출할 오브젝트 </summary>
        [SerializeField]
        private GameObject _selectedView;

        /// <summary> 선택 상태에 따라 interactable을 제어할 버튼 </summary>
        [SerializeField]
        private Button _unityButton;

        /// <summary> 문자열 기반 선택에 사용하는 키 값입니다. </summary>
        public string Key
        {
            get
            {
                return _key;
            }
        }

        /// <summary>
        /// 항목의 선택 표시 상태를 변경합니다.
        /// </summary>
        /// <param name="isSelected">선택 상태 여부입니다.</param>
        /// <param name="disableSelectedButton">선택된 버튼을 비활성화할지 여부입니다.</param>
        public void SetSelected(bool isSelected, bool disableSelectedButton)
        {
            if (_normalView != null)
            {
                _normalView.SetActive(!isSelected);
            }

            if (_selectedView != null)
            {
                _selectedView.SetActive(isSelected);
            }

            if (_unityButton != null && disableSelectedButton)
            {
                _unityButton.interactable = !isSelected;
            }
            else if (_unityButton != null)
            {
                _unityButton.interactable = true;
            }
        }
    }

    /// <summary> 그룹에서 관리할 버튼 항목 목록 </summary>
    [SerializeField]
    private UIButtonGroupItem[] _items;

    /// <summary> 시작 시 선택할 기본 인덱스 </summary>
    [SerializeField]
    private int _defaultIndex;

    /// <summary> Awake 시 기본 선택을 적용할지 여부 </summary>
    [SerializeField]
    private bool _selectOnAwake = true;

    /// <summary> 이미 선택된 버튼을 다시 눌렀을 때 이벤트를 재발생할지 여부 </summary>
    [SerializeField]
    private bool _allowReselect;

    /// <summary> 선택된 버튼의 Unity Button interactable을 false로 바꿀지 여부 </summary>
    [SerializeField]
    private bool _disableSelectedButton;

    /// <summary> 선택 변경 메시지를 받을 대상 스크립트 </summary>
    [SerializeField]
    private MonoBehaviour _root;

    /// <summary> 선택 변경 시 실행할 콜백 메서드 이름 </summary>
    [SerializeField]
    private string _callbackName;

    /// <summary> 인스펙터에서 연결하는 선택 변경 이벤트 </summary>
    [SerializeField]
    private SelectionChangedEvent _onSelected;

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

    /// <summary> 현재 선택된 키 값입니다. 선택이 없으면 빈 문자열입니다. </summary>
    public string SelectedKey
    {
        get
        {
            if (!IsValidIndex(_selectedIndex))
            {
                return string.Empty;
            }

            return _items[_selectedIndex].Key;
        }
    }

    /// <summary>
    /// 시작 시 기본 선택 상태를 적용합니다.
    /// </summary>
    private void Awake()
    {
        if (_selectOnAwake)
        {
            SelectInternal(_defaultIndex, false);
        }
        else
        {
            RefreshItems();
        }
    }

    /// <summary>
    /// 문자열 값으로 버튼을 선택합니다.
    /// 숫자 문자열이면 인덱스로 처리하고, 그 외 값은 키로 처리합니다.
    /// </summary>
    /// <param name="value">선택할 인덱스 또는 키 값입니다.</param>
    public void Select(string value)
    {
        if (int.TryParse(value, out int index))
        {
            Select(index);
            return;
        }

        SelectByKey(value);
    }

    /// <summary>
    /// 인덱스로 버튼을 선택합니다.
    /// UIButton 콜백 값에 숫자를 넣었을 때 호출됩니다.
    /// </summary>
    /// <param name="index">선택할 항목 인덱스입니다.</param>
    public void Select(int index)
    {
        SelectInternal(index, true);
    }

    /// <summary>
    /// 키 값으로 버튼을 선택합니다.
    /// UIButton 콜백 값에 문자열 키를 넣었을 때 호출됩니다.
    /// </summary>
    /// <param name="key">선택할 항목 키 값입니다.</param>
    public void SelectByKey(string key)
    {
        if (_items == null)
        {
            return;
        }

        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i] != null && _items[i].Key == key)
            {
                SelectInternal(i, true);
                return;
            }
        }

        Debug.LogError($"UIButtonGroup_{gameObject.name}: Key is not found. ({key})");
    }

    /// <summary>
    /// 현재 선택을 해제합니다.
    /// </summary>
    public void ClearSelection()
    {
        _selectedIndex = -1;
        RefreshItems();
    }

    /// <summary>
    /// 실제 선택 상태를 변경합니다.
    /// </summary>
    /// <param name="index">선택할 항목 인덱스입니다.</param>
    /// <param name="notify">선택 변경 알림을 발생시킬지 여부입니다.</param>
    private void SelectInternal(int index, bool notify)
    {
        if (!IsValidIndex(index))
        {
            Debug.LogError($"UIButtonGroup_{gameObject.name}: Index is out of range. ({index})");
            return;
        }

        if (_selectedIndex == index && !_allowReselect)
        {
            return;
        }

        _selectedIndex = index;
        RefreshItems();

        if (notify)
        {
            NotifySelectionChanged();
        }
    }

    /// <summary>
    /// 모든 항목의 선택 표시를 현재 선택 인덱스 기준으로 갱신합니다.
    /// </summary>
    private void RefreshItems()
    {
        if (_items == null)
        {
            return;
        }

        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i] == null)
            {
                continue;
            }

            _items[i].SetSelected(i == _selectedIndex, _disableSelectedButton);
        }
    }

    /// <summary>
    /// 선택 변경 이벤트와 메시지 콜백을 실행합니다.
    /// </summary>
    private void NotifySelectionChanged()
    {
        string selectedKey = SelectedKey;
        _onSelected?.Invoke(_selectedIndex, selectedKey);

        if (_root == null || string.IsNullOrEmpty(_callbackName))
        {
            return;
        }

        if (!string.IsNullOrEmpty(selectedKey))
        {
            _root.SendMessage(_callbackName, selectedKey);
        }
        else
        {
            _root.SendMessage(_callbackName, _selectedIndex);
        }
    }

    /// <summary>
    /// 인덱스가 항목 배열 범위 안에 있는지 확인합니다.
    /// </summary>
    /// <param name="index">검사할 항목 인덱스입니다.</param>
    /// <returns>유효한 인덱스이면 true입니다.</returns>
    private bool IsValidIndex(int index)
    {
        return _items != null && index >= 0 && index < _items.Length && _items[index] != null;
    }
}
