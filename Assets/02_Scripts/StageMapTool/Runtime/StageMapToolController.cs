using Puzzle.Core;
using UnityEngine;

/// <summary>
/// 스테이지 맵 툴 UI 입력을 상태 모듈에 연결하는 컨트롤러입니다.
/// </summary>
public class StageMapToolController : MonoBehaviour
{
    /// <summary>
    /// 스테이지 맵 툴의 상위 편집 모드입니다.
    /// </summary>
    private enum StageMapEditMode
    {
        /// <summary> 셀 속성 편집 </summary>
        Cell = 0,

        /// <summary> 블럭 배치 편집 </summary>
        Block = 1,

        /// <summary> 타일/패널 편집 </summary>
        Tile = 2
    }

    #region 퍼즐 타입 선택
[Header("PUZZLE TYPE COMPONENT")]
    /// <summary> 마지막으로 선택한 퍼즐 타입 저장 키입니다. </summary>
    private const string LastPuzzleTypeKey = "StageMapTool.LastPuzzleType";

    /// <summary> 버튼 인덱스 순서에 대응하는 퍼즐 타입 목록입니다. </summary>
    private static readonly PuzzleType[] PuzzleTypes =
    {
        PuzzleType.ThreeMatch,
        PuzzleType.TapMatch,
        PuzzleType.Link
    };

    /// <summary> 퍼즐 타입별 기본 규칙 주소 목록입니다. </summary>
    private static readonly string[] RuleAddresses =
    {
        "ThreeMatchRule",
        "TapMatchRule",
        "LinkMatchRule"
    };

    /// <summary> 퍼즐 타입 버튼 선택 뷰 그룹입니다. </summary>
    [SerializeField]
    private UIButtonGroup _puzzleTypeButtonGroup;

    /// <summary> 맵툴 현재 작업 상태 모듈입니다. </summary>
    private readonly StageMapToolState _state = new StageMapToolState();

    /// <summary> 현재 선택된 퍼즐 타입입니다. </summary>
    public PuzzleType CurrentPuzzleType
    {
        get
        {
            return _state.PuzzleType;
        }
    }

    /// <summary>
    /// 맵 데이터가 아직 없을 때 마지막 선택한 퍼즐 타입을 복원합니다.
    /// </summary>
    private void Awake()
    {
        if (!_state.HasStageData)
        {
            ApplyPuzzleType(LoadLastPuzzleType(), false);
        }

        ApplyEditMode(_editMode);
    }

    /// <summary>
    /// 퍼즐 타입 버튼 클릭 시 상태 모듈과 버튼 뷰를 갱신합니다.
    /// UIButton 콜백 값은 0, 1, 2 인덱스를 사용합니다.
    /// </summary>
    /// <param name="val">선택한 퍼즐 타입 버튼 인덱스 문자열입니다.</param>
    public void OnClickPuzzleType(string val)
    {
        if (!int.TryParse(val, out int index) || !IsValidPuzzleIndex(index))
        {
            Debug.LogError($"[StageMapToolController] 지원하지 않는 퍼즐 타입 인덱스입니다. value: {val}");
            return;
        }

        ApplyPuzzleType(PuzzleTypes[index], true);
    }

    /// <summary>
    /// 로드한 맵 파일의 퍼즐 타입을 상태와 버튼 뷰에 적용합니다.
    /// 파일 데이터가 우선이므로 마지막 선택 PlayerPrefs는 갱신하지 않습니다.
    /// </summary>
    /// <param name="puzzleType">로드한 맵 파일의 퍼즐 타입입니다.</param>
    public void ApplyLoadedPuzzleType(PuzzleType puzzleType)
    {
        ApplyPuzzleType(puzzleType, false);
    }

    /// <summary>
    /// 퍼즐 타입을 상태 모듈과 버튼 뷰에 적용합니다.
    /// </summary>
    /// <param name="puzzleType">적용할 퍼즐 타입입니다.</param>
    /// <param name="savePreference">마지막 선택값으로 저장할지 여부입니다.</param>
    private void ApplyPuzzleType(PuzzleType puzzleType, bool savePreference)
    {
        int index = GetPuzzleTypeIndex(puzzleType);
        if (!IsValidPuzzleIndex(index))
        {
            puzzleType = PuzzleType.ThreeMatch;
            index = 0;
        }

        _state.SetPuzzleType(puzzleType, RuleAddresses[index]);
        _puzzleTypeButtonGroup?.Select(index);

        if (savePreference)
        {
            PlayerPrefs.SetInt(LastPuzzleTypeKey, (int)puzzleType);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// 마지막으로 선택한 퍼즐 타입을 불러옵니다.
    /// </summary>
    /// <returns>저장된 퍼즐 타입입니다.</returns>
    private PuzzleType LoadLastPuzzleType()
    {
        return (PuzzleType)PlayerPrefs.GetInt(LastPuzzleTypeKey, (int)PuzzleType.ThreeMatch);
    }

    /// <summary>
    /// 퍼즐 타입에 대응하는 버튼 인덱스를 반환합니다.
    /// </summary>
    /// <param name="puzzleType">검색할 퍼즐 타입입니다.</param>
    /// <returns>버튼 인덱스입니다. 없으면 -1입니다.</returns>
    private int GetPuzzleTypeIndex(PuzzleType puzzleType)
    {
        for (int i = 0; i < PuzzleTypes.Length; i++)
        {
            if (PuzzleTypes[i] == puzzleType)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 퍼즐 타입 버튼 인덱스가 유효한지 확인합니다.
    /// </summary>
    /// <param name="index">검사할 버튼 인덱스입니다.</param>
    /// <returns>유효하면 true입니다.</returns>
    private bool IsValidPuzzleIndex(int index)
    {
        return index >= 0 && index < PuzzleTypes.Length;
    }

    #endregion

    #region 편집 모드 선택

[Header("EDIT BUTTON COMPONENT")]
    /// <summary> 편집 모드 버튼 선택 뷰 그룹입니다. </summary>
    [SerializeField]
    private UIButtonGroup _editModeButtonGroup;

    /// <summary> 셀 편집 하위 버튼 패널입니다. </summary>
    [SerializeField]
    private GameObject _cellEditPanel;

    /// <summary> 블럭 편집 하위 버튼 패널입니다. </summary>
    [SerializeField]
    private GameObject _blockEditPanel;

    /// <summary> 타일 편집 하위 버튼 패널입니다. </summary>
    [SerializeField]
    private GameObject _tileEditPanel;

    /// <summary> 현재 선택된 편집 모드입니다. </summary>
    private StageMapEditMode _editMode = StageMapEditMode.Cell;

    /// <summary>
    /// 편집 모드 버튼 클릭 시 선택 상태와 하위 패널 노출을 갱신합니다.
    /// UIButton 콜백 값은 0, 1, 2 인덱스를 사용합니다.
    /// </summary>
    /// <param name="val">선택한 편집 모드 버튼 인덱스 문자열입니다.</param>
    public void OnClickEditMode(string val)
    {
        if (!int.TryParse(val, out int index) || !IsValidEditModeIndex(index))
        {
            Debug.LogError($"[StageMapToolController] 지원하지 않는 편집 모드 인덱스입니다. value: {val}");
            return;
        }

        ApplyEditMode((StageMapEditMode)index);
    }

    /// <summary>
    /// 편집 모드를 적용하고 버튼 및 하위 패널 상태를 갱신합니다.
    /// </summary>
    /// <param name="editMode">적용할 편집 모드입니다.</param>
    private void ApplyEditMode(StageMapEditMode editMode)
    {
        _editMode = editMode;
        int index = (int)_editMode;

        _editModeButtonGroup?.Select(index);
        SetPanelActive(_cellEditPanel, _editMode == StageMapEditMode.Cell);
        SetPanelActive(_blockEditPanel, _editMode == StageMapEditMode.Block);
        SetPanelActive(_tileEditPanel, _editMode == StageMapEditMode.Tile);
    }

    /// <summary>
    /// 패널 활성 상태를 변경합니다.
    /// </summary>
    /// <param name="panel">활성 상태를 바꿀 패널입니다.</param>
    /// <param name="isActive">활성화 여부입니다.</param>
    private void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel == null)
        {
            return;
        }

        panel.SetActive(isActive);
    }

    /// <summary>
    /// 편집 모드 버튼 인덱스가 유효한지 확인합니다.
    /// </summary>
    /// <param name="index">검사할 버튼 인덱스입니다.</param>
    /// <returns>유효하면 true입니다.</returns>
    private bool IsValidEditModeIndex(int index)
    {
        return index >= 0 && index <= (int)StageMapEditMode.Tile;
    }

    #endregion
}
