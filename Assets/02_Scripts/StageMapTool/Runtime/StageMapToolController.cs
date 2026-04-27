using System.Collections.Generic;
using Puzzle.Core;
using UnityEngine;

/// <summary>
/// ToolScene의 스테이지 맵 툴 전체 흐름을 조정합니다.
/// </summary>
public class StageMapToolController : MonoBehaviour
{
    /// <summary> 기본 3매치 규칙 주소입니다. </summary>
    private const string ThreeMatchRuleAddress = "ThreeMatchRule";

    /// <summary> 기본 링크 규칙 주소입니다. </summary>
    private const string LinkRuleAddress = "LinkMatchRule";

    /// <summary> 기본 탭 매치 규칙 주소입니다. </summary>
    private const string TapMatchRuleAddress = "TapMatchRule";

    /// <summary> 그리드 표시 컴포넌트입니다. </summary>
    private StageMapGridView _gridView;

    /// <summary> 입력 처리 컴포넌트입니다. </summary>
    private StageMapInputController _inputController;

    /// <summary> 툴 UI 패널입니다. </summary>
    private StageMapToolPanel _toolPanel;

    /// <summary> 현재 편집 상태입니다. </summary>
    private readonly StageMapToolState _state = new StageMapToolState();

    /// <summary> JSON 로드/저장소입니다. </summary>
    private readonly StageMapJsonRepository _repository = new StageMapJsonRepository();

    /// <summary> 저장 전 검증기입니다. </summary>
    private readonly StageMapValidator _validator = new StageMapValidator();

    /// <summary> 저장 후 테스트 진입 실행기입니다. </summary>
    private readonly StageMapPlaytestLauncher _playtestLauncher = new StageMapPlaytestLauncher();

    /// <summary> 현재 규칙에서 허용하는 블럭 목록입니다. </summary>
    private List<BlockData> _ruleBlocks;

    /// <summary>
    /// 필요한 컴포넌트 참조를 보장합니다.
    /// </summary>
    private void Awake()
    {
        EnsureComponents();
        _inputController.Initialize(this, _gridView);
        _toolPanel.Initialize(this);
    }

    /// <summary>
    /// 기본 스테이지를 로드합니다.
    /// </summary>
    private void Start()
    {
        LoadCurrentStage();
    }

    /// <summary>
    /// 퍼즐 모드를 변경하고 현재 스테이지를 다시 로드합니다.
    /// </summary>
    /// <param name="puzzleType">변경할 퍼즐 모드입니다.</param>
    public void ChangePuzzleType(PuzzleType puzzleType)
    {
        _state.SetContext(puzzleType, _state.StageId, GetDefaultRuleAddress(puzzleType));
        LoadCurrentStage();
    }

    /// <summary>
    /// 스테이지 번호를 변경하고 현재 스테이지를 다시 로드합니다.
    /// </summary>
    /// <param name="stageId">변경할 스테이지 번호입니다.</param>
    public void ChangeStageId(int stageId)
    {
        if (!StageStorage.IsValidStageId(stageId))
        {
            Debug.LogError($"[StageMapToolController] 스테이지 번호가 범위를 벗어났습니다. stageId: {stageId}");
            return;
        }

        _state.SetContext(_state.PuzzleType, stageId, _state.RuleAddress);
        LoadCurrentStage();
    }

    /// <summary>
    /// 현재 브러시를 변경합니다.
    /// </summary>
    /// <param name="brush">적용할 새 브러시입니다.</param>
    public void ChangeBrush(StageMapCellBrush brush)
    {
        _state.SetBrush(brush);
    }

    /// <summary>
    /// 지정한 좌표에 현재 브러시를 적용합니다.
    /// </summary>
    /// <param name="x">수정할 X 좌표입니다.</param>
    /// <param name="y">수정할 Y 좌표입니다.</param>
    public void PaintCell(int x, int y)
    {
        if (!_state.PaintCell(x, y))
        {
            return;
        }

        _gridView.RefreshCell(x, y, _state.GetCell(x, y));
    }

    /// <summary>
    /// 현재 스테이지를 기본 Resources 경로에 저장합니다.
    /// </summary>
    /// <returns>저장 성공 여부입니다.</returns>
    public bool Save()
    {
        StageMapValidationResult result = _validator.Validate(_state.StageData, _state.StageId, _ruleBlocks);
        _toolPanel.ShowValidationResult(result);
        if (!result.IsValid())
        {
            return false;
        }

        return _repository.SaveToResources(_state.PuzzleType, _state.StageId, _state.StageData);
    }

    /// <summary>
    /// 현재 스테이지를 저장한 뒤 실제 게임 씬으로 테스트 진입합니다.
    /// </summary>
    public void SaveAndTest()
    {
        if (!Save())
        {
            return;
        }

        _playtestLauncher.Run(_state.RuleAddress, _state.PuzzleType, _state.StageId);
    }

    /// <summary>
    /// 현재 컨텍스트의 스테이지와 규칙을 로드합니다.
    /// </summary>
    private void LoadCurrentStage()
    {
        if (string.IsNullOrEmpty(_state.RuleAddress))
        {
            _state.SetContext(_state.PuzzleType, _state.StageId, GetDefaultRuleAddress(_state.PuzzleType));
        }

        _ruleBlocks = LoadRuleBlocks(_state.RuleAddress);
        _state.SetStage(_repository.LoadOrCreate(_state.PuzzleType, _state.StageId));
        _gridView.Rebuild(_state.StageData);
        _toolPanel.Refresh(_state);
    }

    /// <summary>
    /// 규칙 JSON에서 블럭 목록을 로드합니다.
    /// </summary>
    /// <param name="ruleAddress">로드할 규칙 Addressable 주소입니다.</param>
    /// <returns>규칙에 포함된 블럭 목록입니다.</returns>
    private List<BlockData> LoadRuleBlocks(string ruleAddress)
    {
        TextAsset ruleAsset = AssetManager.Instance.LoadAsset<TextAsset>(ruleAddress);
        if (ruleAsset == null)
        {
            Debug.LogError($"[StageMapToolController] 규칙 에셋 로드 실패: {ruleAddress}");
            return null;
        }

        GameRuleContainer ruleContainer = JsonUtility.FromJson<GameRuleContainer>(ruleAsset.text);
        if (ruleContainer == null)
        {
            Debug.LogError($"[StageMapToolController] 규칙 JSON 파싱 실패: {ruleAddress}");
            return null;
        }

        return ruleContainer.blocks;
    }

    /// <summary>
    /// 퍼즐 모드에 대응하는 기본 규칙 주소를 반환합니다.
    /// </summary>
    /// <param name="puzzleType">규칙 주소를 구할 퍼즐 모드입니다.</param>
    /// <returns>기본 규칙 Addressable 주소입니다.</returns>
    private string GetDefaultRuleAddress(PuzzleType puzzleType)
    {
        switch (puzzleType)
        {
            case PuzzleType.Link:
                return LinkRuleAddress;
            case PuzzleType.TapMatch:
                return TapMatchRuleAddress;
            case PuzzleType.ThreeMatch:
            default:
                return ThreeMatchRuleAddress;
        }
    }

    /// <summary>
    /// 씬에 필요한 컴포넌트가 없으면 현재 오브젝트에 추가합니다.
    /// </summary>
    private void EnsureComponents()
    {
        if (_gridView == null)
        {
            _gridView = GetComponent<StageMapGridView>();
        }

        if (_gridView == null)
        {
            _gridView = gameObject.AddComponent<StageMapGridView>();
        }

        if (_inputController == null)
        {
            _inputController = GetComponent<StageMapInputController>();
        }

        if (_inputController == null)
        {
            _inputController = gameObject.AddComponent<StageMapInputController>();
        }

        if (_toolPanel == null)
        {
            _toolPanel = GetComponent<StageMapToolPanel>();
        }

        if (_toolPanel == null)
        {
            _toolPanel = gameObject.AddComponent<StageMapToolPanel>();
        }
    }
}
