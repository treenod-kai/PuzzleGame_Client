using System.Collections.Generic;
using System.Text;
using Puzzle.Core;
using Battle.Pivot;
using UnityEngine;
using TMPro;

/// <summary>
/// 상대방의 리플레이 데이터를 기반으로 보드를 자동 재생하는 컨트롤러입니다.
/// 기록된 Input/InputEnd를 프레임에 맞춰 주입하여 결정론적으로 동일한 게임을 재현합니다.
/// </summary>
public class ReplayController : MonoBehaviour
{
    [Header("View References")]
    /// <summary> 리플레이 보드를 그리는 뷰 객체 </summary>
    public PuzzleBoardView replayBoardView;

    [Header("배치 설정")]
    /// <summary> 플레이어 보드를 기준으로 한 상대 보드의 최대 표시 배율입니다. </summary>
    [Range(0.1f, 0.5f)]
    public float viewScale = 0.3f;

    /// <summary> 상대 보드의 가장자리에 더 확보할 여백 (월드 단위) </summary>
    public float margin = 0.3f;

    /// <summary> 리플레이용 퍼즐 보드 모델 </summary>
    private IPuzzleBoard _board;
    /// <summary> 기록의 보드 규칙 버전이 현재 점수 판정과 호환되는지 확인할 퍼즐 모드입니다. </summary>
    private PuzzleType _puzzleType;
    /// <summary>상대 기록의 규칙에 대전에서 제외하는 특수 블록 정의가 있는지 나타냅니다.</summary>
    private bool _hasSpecialBlocks;

    /// <summary> 상대 보드의 파괴 통계를 관찰하는 독립 피벗 세션입니다. </summary>
    private BattlePivotSession _pivotSession;

    /// <summary> 재생할 리플레이 데이터 </summary>
    private ReplayData _replayData;

    /// <summary> 현재 논리 프레임 카운터 </summary>
    private ulong _frameCount;

    /// <summary> 다음에 처리할 입력 기록의 인덱스 </summary>
    private int _inputIndex;

    /// <summary> 다음에 처리할 입력 종료 기록의 인덱스 </summary>
    private int _inputEndIndex;

    /// <summary> 초기화 완료 여부 </summary>
    private bool _isInitialized = false;
    /// <summary> 기록 재생을 끝냈는지 여부입니다. </summary>
    private bool _isComplete;
    /// <summary> 기록된 종료 틱과 점수가 실제 재생 결과와 일치하는지 여부입니다. </summary>
    private bool _resultValid;
    /// <summary> 리플레이 보드의 남은 시간과 재생 상태를 표시합니다. </summary>
    [SerializeField] private TMP_Text _statusText;
    /// <summary> 리플레이 안내 문구를 결정하는 표시 상태입니다. </summary>
    private enum ReplayStatusState
    {
        /// <summary> 아직 표시하지 않은 상태입니다. </summary>
        None,
        /// <summary> 초기화하지 못한 상태입니다. </summary>
        Failed,
        /// <summary> 입력을 재생 중인 상태입니다. </summary>
        Playing,
        /// <summary> 입력을 모두 소비하고 종료를 기다리는 상태입니다. </summary>
        Waiting,
        /// <summary> 이전 형식의 기록 재생을 마친 상태입니다. </summary>
        LegacyComplete,
        /// <summary> 기록 검증을 통과하고 재생을 마친 상태입니다. </summary>
        Complete,
        /// <summary> 기록된 결과와 재생 결과가 다른 상태입니다. </summary>
        Mismatch
    }

    /// <summary> 마지막으로 표시한 재생 상태입니다. </summary>
    private ReplayStatusState _lastStatusState;
    /// <summary> 마지막으로 표시한 남은 초입니다. </summary>
    private int _lastStatusSeconds;
    /// <summary> 마지막으로 표시한 점수입니다. </summary>
    private int _lastStatusScore;
    /// <summary> 마지막으로 표시한 피벗의 0.1점 단위 값이며 -1은 미표시입니다. </summary>
    private int _lastPivotTenths;
    /// <summary> 마지막으로 표시한 피벗 최대치입니다. </summary>
    private int _lastPivotCapacity;
    /// <summary> 마지막으로 표시한 피벗 활성 여부입니다. </summary>
    private bool _lastPivotActive;
    private readonly StringBuilder _statusMessage = new StringBuilder(128);
    /// <summary> 메인 게임에서 공동 틱 진행을 시작할 수 있는지 여부입니다. </summary>
    public bool IsInitialized => _isInitialized;
    /// <summary> 종료 점수를 비교할 수 있는 완주 기록인지 여부입니다. </summary>
    public bool HasValidResult => _isComplete && _resultValid;
    /// <summary>상대 기록의 퍼즐 모드와 플레이어의 피벗 설정 <paramref name="settings"/>으로 점수를 비교할 수 있는지 확인합니다.</summary>
    public bool HasCompatibleBattleRules(PivotSettings settings)
    {
        if (!_isInitialized || _replayData == null)
        {
            return false;
        }
        return _replayData.HasCompatibleBattleRules(settings, _puzzleType, _hasSpecialBlocks);
    }
    /// <summary> 현재 리플레이 점수입니다. </summary>
    public int CurrentScore
    {
        get
        {
            if (_board == null || _board.Objective == null)
            {
                return 0;
            }
            return _board.Objective.CurrentScore;
        }
    }

    /// <summary><paramref name="replayData"/>에 기록된 규칙과 스테이지로 상대 보드를 생성하고 재생을 준비합니다.</summary>
    public void Initialize(ReplayData replayData)
    {
        _isInitialized = false;
        _replayData = replayData;
        _board = null;
        _isComplete = false;
        _resultValid = false;
        _lastStatusState = ReplayStatusState.None;
        if (_pivotSession != null)
        {
            _pivotSession.Dispose();
            _pivotSession = null;
        }
        if (replayData == null || replayData.inputs == null || replayData.inputEnds == null)
        {
            Debug.LogError("[ReplayController] 리플레이 입력 데이터가 없습니다.");
            return;
        }
        if (replayData.numericRuleVersion < 0 || replayData.numericRuleVersion > ReplayData.CurrentNumericRuleVersion)
        {
            Debug.LogError($"[ReplayController] 지원하지 않는 수치 규칙 버전입니다: {replayData.numericRuleVersion}");
            return;
        }
        if (replayData.boardRuleVersion < 0 || replayData.boardRuleVersion > ReplayData.CurrentBoardRuleVersion)
        {
            Debug.LogError($"[ReplayController] 지원하지 않는 보드 규칙 버전입니다: {replayData.boardRuleVersion}");
            return;
        }
        _frameCount = 0;
        _inputIndex = 0;
        _inputEndIndex = 0;

        // 리플레이 데이터에서 GameSpec 복원
        GameSpec replaySpec = new GameSpec();

        if (!StageInjection.TryLoadRuleData(_replayData.ruleAddress, out GameRuleContainer ruleContainer))
        {
            return;
        }
        replaySpec.rule = ruleContainer.rule;
        replaySpec.blocks = ruleContainer.blocks;

        if (!StageInjection.TryLoadStageData(_replayData.stageAddress, _replayData.stageJson, out replaySpec.stageData))
        {
            return;
        }

        // 기록된 시드를 적용하여 동일한 난수 시퀀스 재현
        replaySpec.randomSeed = _replayData.randomSeed;
        replaySpec.UseLegacyAutomaticCombos = _replayData.comboRuleVersion == 0;
        replaySpec.UseLegacyNumericRules = _replayData.numericRuleVersion == 0;
        replaySpec.UseLegacyBoardRules = _replayData.boardRuleVersion == 0;
        replaySpec.UseLegacyBattleBlocks = _replayData.boardRuleVersion < ReplayData.NormalBattleBlocksVersion;
        replaySpec.UseLegacyImmediateSwap = _replayData.boardRuleVersion < ReplayData.ReleaseToSwapVersion;
        replaySpec.UseLegacyThreeMatchStageBombs = _replayData.boardRuleVersion < ReplayData.ThreeMatchStageWithoutBombsVersion;
        if (_replayData.formatVersion == ReplayData.CurrentFormatVersion)
        {
            replaySpec.mode = _replayData.mode;
            replaySpec.UseRecordedBattleDuration = true;
            replaySpec.rule.timeLimit = _replayData.timeLimit;
        }

        if (!ObjectiveManager.IsValidTimeLimit(replaySpec.rule.timeLimit, replaySpec.UseLegacyNumericRules) ||
            replaySpec.stageData == null || !replaySpec.stageData.HasValidCoordinates())
        {
            Debug.LogError("[ReplayController] 제한 시간 또는 스테이지 좌표 범위가 잘못되었습니다.");
            return;
        }
        string blockError = replaySpec.GetBlockSpawnError();
        if (blockError != null)
        {
            Debug.LogError($"[ReplayController] {blockError} ({replayData.ruleAddress})");
            return;
        }

        // 퍼즐 타입에 맞는 보드 생성
        _puzzleType = replaySpec.rule.puzzleType;
        _hasSpecialBlocks = replaySpec.HasSpecialBlocks;
        switch (_puzzleType)
        {
            case PuzzleType.ThreeMatch:
                _board = new ThreeMatchPuzzleBoard();
                break;
            case PuzzleType.Link:
                _board = new LinkPuzzleBoard();
                break;
            case PuzzleType.TapMatch:
                _board = new TapMatchPuzzleBoard();
                break;
            default:
                _board = new ThreeMatchPuzzleBoard();
                break;
        }

        _board.Initialize(replaySpec);

        if (replaySpec.mode == GameMode.Battle)
        {
            PivotSettings pivotSettings = replayData.pivotSettings;
            if (pivotSettings == null)
            {
                pivotSettings = new PivotSettings();
            }
            try
            {
                _pivotSession = new BattlePivotSession(_board, replaySpec.mode, pivotSettings,
                    replaySpec.UseLegacyAutomaticCombos);
            }
            catch (System.ArgumentOutOfRangeException exception)
            {
                Debug.LogError($"[ReplayController] 피벗 설정 오류: {exception.Message}");
                return;
            }
        }

        // 뷰에 보드 그리기 (카메라 정렬 스킵, 보드 모양 직접 전달)
        if (replayBoardView != null)
        {
            replayBoardView.InputEnabled = false;
            replayBoardView.skipCameraAlign = true;
            replayBoardView.DrawBoard(_board, replaySpec.rule.boardShape);
        }

        _isInitialized = true;
        Debug.Log($"[ReplayController] 리플레이 재생 시작 (입력 {_replayData.inputs.Count}개, 입력종료 {_replayData.inputEnds.Count}개)");
    }

    /// <summary>상대 보드의 재생 상태를 표시하는 텍스트입니다.</summary>
    internal TMP_Text StatusText => _statusText;

    /// <summary>상대 보드를 <paramref name="camera"/>의 <paramref name="area"/> 영역에 놓고, <paramref name="playerScale"/>에 대한 축소 비율을 유지합니다.</summary>
    internal void FitBoard(Camera camera, Rect area, float playerScale)
    {
        if (replayBoardView != null && _isInitialized)
        {
            float inset = Mathf.Min(Mathf.Min(area.width, area.height) * 0.25f,
                Mathf.Max(0f, margin) * camera.pixelHeight / (2f * camera.orthographicSize));
            area = Rect.MinMaxRect(area.xMin + inset, area.yMin + inset, area.xMax - inset, area.yMax - inset);
            replayBoardView.FitToScreen(camera, area, playerScale * viewScale);
        }
    }

    /// <summary> 현재 논리 프레임의 입력을 수신 순서대로 주입한 뒤 보드를 한 틱 진행합니다. </summary>
    public void AdvanceFrame()
    {
        if (!_isInitialized || _board == null || _isComplete)
        {
            return;
        }

        // 증가 전에 0번 프레임부터 처리합니다. 오래된 기록도 큐를 막지 않도록 소비합니다.
        while (true)
        {
            bool hasInput = _inputIndex < _replayData.inputs.Count &&
                _replayData.inputs[_inputIndex].frame <= _frameCount;
            bool hasEnd = _inputEndIndex < _replayData.inputEnds.Count &&
                _replayData.inputEnds[_inputEndIndex].frame <= _frameCount;
            if (!hasInput && !hasEnd)
            {
                break;
            }

            bool inputFirst = hasInput;
            if (hasInput && hasEnd)
            {
                InputRecord input = _replayData.inputs[_inputIndex];
                InputEndRecord end = _replayData.inputEnds[_inputEndIndex];
                // 순서 필드가 없는 기존 기록은 동일 프레임의 좌표 입력을 먼저 처리합니다.
                inputFirst = input.frame < end.frame ||
                    (input.frame == end.frame && input.sequence <= end.sequence);
            }

            if (inputFirst)
            {
                _board.Input(_replayData.inputs[_inputIndex++].position);
            }
            else
            {
                _board.InputEnd(_replayData.inputEnds[_inputEndIndex++].canceled);
            }
        }

        _board.FixedUpdate();
        if (_pivotSession != null)
        {
            _pivotSession.AdvanceAfterBoardTick();
        }
        _frameCount++;
        bool hasEndFrame = _replayData.formatVersion == ReplayData.CurrentFormatVersion;
        if (_board.State == BoardState.Finish || (hasEndFrame && _frameCount >= _replayData.frameCount))
        {
            _isComplete = true;
            if (_pivotSession != null)
            {
                _pivotSession.Dispose();
            }
            _resultValid = hasEndFrame && _board.State == BoardState.Finish &&
                _frameCount == _replayData.frameCount && CurrentScore == _replayData.finalScore;
            if (hasEndFrame && !_resultValid)
            {
                Debug.LogError("[ReplayController] 기록된 종료 틱 또는 점수와 재생 결과가 다릅니다.");
            }
        }
    }

    /// <summary> 논리 판정과 독립적으로 리플레이 상태를 화면에 표시합니다. </summary>
    private void Update()
    {
        if (replayBoardView != null)
        {
            bool pivotActiveOnBoard = _isInitialized && !_isComplete && _pivotSession != null && _pivotSession.Mode.IsActive;
            replayBoardView.SetPivotActive(pivotActiveOnBoard);
        }
        if (_statusText == null)
        {
            return;
        }
        _statusText.gameObject.SetActive(_replayData != null);
        if (_replayData == null)
        {
            return;
        }

        ReplayStatusState statusState;
        int seconds = -1;
        if (!_isInitialized)
        {
            statusState = ReplayStatusState.Failed;
        }
        else if (_isComplete)
        {
            statusState = _replayData.formatVersion == 0 ? ReplayStatusState.LegacyComplete :
                (_resultValid ? ReplayStatusState.Complete : ReplayStatusState.Mismatch);
        }
        else
        {
            seconds = _board.Objective.RemainingSeconds;
            bool waiting = _inputIndex >= _replayData.inputs.Count && _inputEndIndex >= _replayData.inputEnds.Count;
            statusState = waiting ? ReplayStatusState.Waiting : ReplayStatusState.Playing;
        }
        int score = CurrentScore;
        int pivotTenths = -1;
        int pivotCapacity = 0;
        bool pivotActive = false;
        if (_isInitialized && !_isComplete && _pivotSession != null && _pivotSession.Mode.IsEnabled)
        {
            PivotMode pivot = _pivotSession.Mode;
            pivotTenths = pivot.GaugeUnits / 10;
            pivotCapacity = pivot.Capacity;
            pivotActive = pivot.IsActive;
        }
        // 표시값이 같으면 문자열 생성 자체를 건너뛰어 매 렌더 프레임의 할당을 막습니다.
        if (_lastStatusState == statusState && _lastStatusSeconds == seconds && _lastStatusScore == score &&
            _lastPivotTenths == pivotTenths && _lastPivotCapacity == pivotCapacity && _lastPivotActive == pivotActive)
        {
            return;
        }
        _lastStatusState = statusState;
        _lastStatusSeconds = seconds;
        _lastStatusScore = score;
        _lastPivotTenths = pivotTenths;
        _lastPivotCapacity = pivotCapacity;
        _lastPivotActive = pivotActive;

        _statusMessage.Clear();
        _statusMessage.Append("상대 ");
        switch (statusState)
        {
            case ReplayStatusState.Failed:
                _statusMessage.Append("리플레이 로드 실패");
                break;
            case ReplayStatusState.LegacyComplete:
                _statusMessage.Append("이전 기록 재생 완료");
                break;
            case ReplayStatusState.Complete:
                _statusMessage.Append("재생 완료");
                break;
            case ReplayStatusState.Mismatch:
                _statusMessage.Append("기록 불일치 (비교 제외)");
                break;
            default:
                _statusMessage.Append(statusState == ReplayStatusState.Waiting ? "입력 종료  " : "재생 중  ");
                PuzzleHudText.AppendNumber(_statusMessage, seconds / 60);
                _statusMessage.Append(':');
                PuzzleHudText.AppendNumber(_statusMessage, seconds % 60, true);
                break;
        }
        _statusMessage.Append("\n점수 ");
        PuzzleHudText.AppendNumber(_statusMessage, score);
        if (pivotTenths >= 0)
        {
            _statusMessage.Append('\n');
            PuzzleHudText.AppendPivot(_statusMessage, pivotActive, pivotTenths, pivotCapacity);
        }
        _statusText.SetText(_statusMessage);
    }
    /// <summary> 컨트롤러 제거 시 상대 보드의 피벗 효과 구독을 정리합니다. </summary>
    private void OnDestroy()
    {
        if (_pivotSession != null)
        {
            _pivotSession.Dispose();
        }
    }

}
