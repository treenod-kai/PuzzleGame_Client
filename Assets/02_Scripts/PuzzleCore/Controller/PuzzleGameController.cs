using Puzzle.Core;
using System;
using Battle.Pivot;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.LowLevel;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using InputTouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// 유저의 입력을 감지하고 퍼즐 보드 모델 및 뷰와 통신하여 전체 게임 루프를 제어하는 컨트롤러 클래스입니다.
/// </summary>
public class PuzzleGameController : MonoBehaviour
{
    [Header("View References")]
    /// <summary> 화면에 보드를 그리는 뷰 객체 </summary>
    public PuzzleBoardView boardView;

    /// <summary> 상대방 리플레이를 재생하는 컨트롤러 </summary>
    public ReplayController replayController;

    [Header("UI References")]
    /// <summary> 남은 시간을 표시하는 텍스트 (인스펙터에서 연결) </summary>
    [SerializeField]
    private TMP_Text _timerText;

    /// <summary> 현재 점수를 표시하는 텍스트 (인스펙터에서 연결) </summary>
    [SerializeField]
    private TMP_Text _scoreText;

    /// <summary> 대전 진행 안내와 최종 점수 비교를 표시합니다. </summary>
    [SerializeField] private TMP_Text _resultText;
    /// <summary> 이번 플레이의 종료 규칙입니다. </summary>
    private GameMode _mode;
    /// <summary> 두 보드를 함께 진행한 총 논리 틱 수입니다. </summary>
    private ulong _elapsedFrames;
    /// <summary> 상대 기록이 있는 대전인지 여부입니다. </summary>
    private ReplayData _opponent;
    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private TMP_Text _summaryText;
    private string _resultSummary;
    private Canvas _hudCanvas;
    private Rect _layoutSafeArea;
    private Vector2Int _layoutScreen;
    private float _layoutCanvasScale;
    private bool _layoutShowsResult;
    private string _playHint;
    private List<ObjectiveData> _objectives;
    private int _lastDestroyed = -1;
    private readonly StringBuilder _hudText = new StringBuilder(128);

    /// <summary> 퍼즐 보드의 핵심 로직을 처리하는 모델 인터페이스 </summary>
    private IPuzzleBoard _board;

    /// <summary> Rule JSON에서 읽어 검증한 대전 피벗 규칙의 실행용 복사본입니다. </summary>
    private PivotSettings _pivotSettings;
    /// <summary> 보드 파괴 통계를 독립 피벗 모듈로 전달합니다. </summary>
    private BattlePivotSession _pivotSession;
    /// <summary> 대전 중 플레이어의 피벗 게이지를 표시합니다. </summary>
    private PivotGaugeView _pivotGaugeView;

    /// <summary> 현재 드래그 중 마지막으로 머문 좌표 (되돌리기 등 경로 재진입 허용을 위해) </summary>
    private GridPos? _lastHoveredPos = null;
    /// <summary>한 렌더 프레임 사이에 들어온 포인터 좌표와 종료 사유를 수신 순서대로 보관합니다.</summary>
    private readonly Queue<(Vector2 Position, InputTouchPhase Phase)> _pointerSamples = new Queue<(Vector2, InputTouchPhase)>();
    private Finger _inputFinger;
    private Mouse _inputMouse;
    private bool _inputFocused = true;
    private bool _inputPaused;
    private bool _hasBoardInput;
    private static int _inputControllerCount;
    private static bool _previousDisableMerging;

    /// <summary> 보드가 정상적으로 초기화되었는지 여부 </summary>
    private bool _isInitialized = false;

    /// <summary> 리플레이 저장이 이미 완료되었는지 여부 (중복 저장 방지) </summary>
    private bool _replaySaved = false;

    /// <summary> 캐싱된 메인 카메라 참조 </summary>
    private Camera _mainCamera;

    /// <summary> Physics2D.OverlapPoint 결과를 재사용하기 위한 버퍼 </summary>
    private readonly List<Collider2D> _hitBuffer = new List<Collider2D>(16);

    /// <summary> Physics2D.OverlapPoint에 사용할 캐싱된 필터 (매 프레임 할당 방지) </summary>
#if UNITY_6000_3_OR_NEWER
    private static readonly ContactFilter2D _noFilter = ContactFilter2D.noFilter;
#else
    private static readonly ContactFilter2D _noFilter = new ContactFilter2D().NoFilter();
#endif

    /// <summary> 타이머 텍스트 갱신 빈도 제어용 (이전 표시된 초 값) </summary>
    private int _lastDisplayedSeconds = -1;

    /// <summary> 점수 텍스트 갱신 빈도 제어용 (이전 표시된 점수 값) </summary>
    private int _lastDisplayedScore = -1;

    /// <summary>입력 이벤트를 구독하고 첫 컨트롤러가 이동 이벤트 병합을 중지합니다.</summary>
    private void OnEnable()
    {
        // 같은 프레임의 연속 이동도 링크가 지나간 서로 다른 칸일 수 있습니다.
        if (_inputControllerCount++ == 0)
        {
            _previousDisableMerging = InputSystem.settings.disableRedundantEventsMerging;
            InputSystem.settings.disableRedundantEventsMerging = true;
        }
        InputSystem.onEvent += RecordMouse;
        InputSystem.onDeviceChange += OnInputDeviceChange;
        EnhancedTouchSupport.Enable();
        EnhancedTouch.onFingerDown += RecordTouch;
        EnhancedTouch.onFingerMove += RecordTouch;
        EnhancedTouch.onFingerUp += RecordTouch;
    }

    /// <summary>진행 중인 선택을 정리하고 터치 구독을 해제합니다.</summary>
    private void OnDisable()
    {
        EnhancedTouch.onFingerDown -= RecordTouch;
        EnhancedTouch.onFingerMove -= RecordTouch;
        EnhancedTouch.onFingerUp -= RecordTouch;
        EnhancedTouchSupport.Disable();
        InputSystem.onEvent -= RecordMouse;
        InputSystem.onDeviceChange -= OnInputDeviceChange;
        if (--_inputControllerCount == 0 && InputSystem.settings.disableRedundantEventsMerging)
        {
            InputSystem.settings.disableRedundantEventsMerging = _previousDisableMerging;
        }
        CancelPointer();
    }

    /// <summary>게임 창이 포커스 <paramref name="focused"/>를 잃으면 진행 중인 입력을 취소합니다.</summary>
    private void OnApplicationFocus(bool focused)
    {
        _inputFocused = focused;
        if (!focused)
        {
            CancelPointer();
        }
    }

    /// <summary>앱이 일시정지되면 <paramref name="paused"/> 상태를 기록하고 진행 중인 조작을 취소합니다.</summary>
    private void OnApplicationPause(bool paused)
    {
        _inputPaused = paused;
        if (paused)
        {
            CancelPointer();
        }
    }

    /// <summary>
    /// 게임 시작 시 스테이지 데이터를 로드하고 보드를 초기화합니다.
    /// </summary>
    private void Start()
    {
        Component hudReference = _timerText;
        if (_resultPanel != null)
        {
            hudReference = _resultPanel.transform;
        }
        if (hudReference != null)
        {
            Canvas canvas = hudReference.GetComponentInParent<Canvas>(true);
            if (canvas != null)
            {
                _hudCanvas = canvas.rootCanvas;
            }
        }
        GameSpec gameSpec = StageInjection.Instance.GetGameSpec();

        if (gameSpec == null || gameSpec.stageData == null)
        {
            // 게임 씬을 직접 실행한 경우에도 로비와 같은 데이터 로드 경로를 사용합니다.
            if (!StageInjection.Instance.MakeGameSpec("ThreeMatchRule", "Stage"))
            {
                ShowStartError("스테이지를 불러올 수 없습니다.");
                return;
            }
            gameSpec = StageInjection.Instance.GetGameSpec();
        }

        // 게임 타입에 따라 적절한 보드 구현체 생성
        switch (gameSpec.rule.puzzleType)
        {
            case PuzzleType.ThreeMatch:
                _board = new ThreeMatchPuzzleBoard();
                break;
            case PuzzleType.Link:
                _board = new LinkPuzzleBoard(); // 링크 전용 보드로 연결
                break;
            case PuzzleType.TapMatch:
                _board = new TapMatchPuzzleBoard();
                break;
            default:
                _board = new ThreeMatchPuzzleBoard();
                break;
        }

        _mode = gameSpec.mode;
        _elapsedFrames = 0;
        _board.Initialize(gameSpec);

        if (_mode == GameMode.Battle)
        {
            if (gameSpec.pivotSettings == null)
            {
                ShowStartError("대전 피벗 설정이 없습니다.");
                return;
            }
            try
            {
                _pivotSettings = gameSpec.pivotSettings.CopyValidated();
            }
            catch (ArgumentOutOfRangeException exception)
            {
                ShowStartError($"대전 피벗 설정 오류: {exception.Message}");
                return;
            }
            _pivotSession = new BattlePivotSession(_board, _mode, _pivotSettings);
            if (_timerText != null)
            {
                _pivotGaugeView = new PivotGaugeView(_timerText, _pivotSession.Mode);
            }
        }

        if (boardView != null)
        {
            boardView.DrawBoard(_board);
        }

        _mainCamera = Camera.main;
        _isInitialized = true;

        // 상대방 리플레이 데이터가 있으면 리플레이 컨트롤러 초기화
        ReplayData replayData = StageInjection.Instance.GetReplayData();
        _opponent = replayData;
        _objectives = gameSpec.rule.objectives;
        _playHint = gameSpec.rule.puzzleType switch
        {
            PuzzleType.Link => "같은 색 블록 3개 이상을 <nobr>이으세요</nobr>",
            PuzzleType.TapMatch => "같은 색이 2개 이상 붙은 곳을 <nobr>누르세요</nobr>",
            _ => "이웃한 블록을 바꿔 같은 색 3개를 한 줄로 <nobr>맞추세요</nobr>"
        };
        UpdatePlayHint();
        if (replayData != null && replayController != null)
        {
            replayController.Initialize(replayData);
            StageInjection.Instance.SetReplayData(null); // 사용 후 초기화
        }
    }

    /// <summary>
    /// 매 프레임 입력 신호를 감지하고 보드 상태를 업데이트합니다.
    /// </summary>
    private void Update()
    {
        RefreshScreenLayout();
        if (!_isInitialized || _board == null || _replaySaved)
        {
            return;
        }

        if (_board.State != BoardState.Finish)
        {
            ReadPointer();
        }
        UpdateTimerUI();
        UpdateScoreUI();
        if (_pivotSession != null && boardView != null)
        {
            boardView.SetPivotActive(_pivotSession.Mode.IsActive);
        }
        if (_pivotGaugeView != null)
        {
            _pivotGaugeView.Refresh();
        }

        if (_board.State == BoardState.Finish)
        {
            _replaySaved = true;
            _resultSummary = _mode == GameMode.Battle ? GetBattleResult() : GetStageResult();
            if (!SaveReplay())
            {
                _resultSummary += "\n리플레이를 저장하지 못했습니다";
            }
            _summaryText.text = _resultSummary;
            _resultPanel.SetActive(true);
            RefreshScreenLayout();
            return;
        }
        UpdatePlayHint();
    }

    /// <summary>안전 영역과 결과 패널 상태가 바뀌면 두 보드와 안내 문구의 화면 영역을 함께 갱신합니다.</summary>
    private void RefreshScreenLayout()
    {
        if (_hudCanvas == null || _hudCanvas.scaleFactor <= 0f || Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }
        Rect safe = Screen.safeArea;
        Vector2Int screen = new Vector2Int(Screen.width, Screen.height);
        if (safe.width <= 0f || safe.height <= 0f)
        {
            safe = new Rect(Vector2.zero, screen);
        }
        bool result = _resultPanel != null && _resultPanel.activeSelf;
        float canvasScale = _hudCanvas.scaleFactor;
        if (safe == _layoutSafeArea && screen == _layoutScreen && result == _layoutShowsResult && canvasScale == _layoutCanvasScale)
        {
            return;
        }
        _layoutSafeArea = safe;
        _layoutScreen = screen;
        _layoutShowsResult = result;
        _layoutCanvasScale = canvasScale;

        bool hasOpponent = _opponent != null && replayController != null;
        PuzzleHudPlacement layout = new PuzzleHudPlacement(safe, result, hasOpponent);
        LayoutBoards(layout, hasOpponent);
        LayoutPlayerInterface(layout, result);
    }

    /// <summary><paramref name="layout"/>에 두 보드를 배치하고 <paramref name="hasOpponent"/>이면 상대 상태를 함께 표시합니다.</summary>
    private void LayoutBoards(PuzzleHudPlacement layout, bool hasOpponent)
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }
        if (boardView != null && _mainCamera != null)
        {
            boardView.FitToScreen(_mainCamera, layout.PlayerBoard, float.PositiveInfinity);
        }
        if (hasOpponent)
        {
            PuzzleHudPlacement.PlaceText(replayController.StatusText, layout.ReplayStatus, layout.GetFontSize(20f, _hudCanvas.scaleFactor));
            if (boardView != null && _mainCamera != null)
            {
                replayController.FitBoard(_mainCamera, layout.ReplayBoard, boardView.transform.lossyScale.x);
            }
        }
        Physics2D.SyncTransforms();
    }

    /// <summary><paramref name="layout"/>의 플레이어 정보 영역에 <paramref name="result"/> 상태에 맞는 안내 또는 결과 카드를 놓습니다.</summary>
    private void LayoutPlayerInterface(PuzzleHudPlacement layout, bool result)
    {
        if (_timerText != null)
        {
            _timerText.gameObject.SetActive(!result);
        }
        if (_scoreText != null)
        {
            _scoreText.gameObject.SetActive(!result);
        }
        if (_resultText != null)
        {
            _resultText.gameObject.SetActive(!result);
        }
        if (result)
        {
            Rect card = _isInitialized ? layout.PlayerHud : _layoutSafeArea;
            PuzzleHudPlacement.Place(_resultPanel.transform as RectTransform, card, true);
        }
        else
        {
            LayoutPlayerHud(layout, _hudCanvas.scaleFactor);
        }
    }

    /// <summary><paramref name="layout"/>의 플레이어 영역 안에 점수·시간·피벗·안내를 놓고 <paramref name="canvasScale"/>에 맞춰 글자 크기를 설정합니다.</summary>
    private void LayoutPlayerHud(PuzzleHudPlacement layout, float canvasScale)
    {
        Rect area = layout.PlayerHud;
        float unit = layout.Unit;
        float row = Mathf.Min(Mathf.Max(19f, 38f * unit), area.height * 0.18f);
        float gap = 3f * unit;
        Rect score = PuzzleHudPlacement.TakeTop(ref area, row, gap);
        Rect timer = PuzzleHudPlacement.TakeTop(ref area, row, gap);
        PuzzleHudPlacement.PlaceText(_scoreText, score, layout.GetFontSize(32f, canvasScale));
        PuzzleHudPlacement.PlaceText(_timerText, timer, layout.GetFontSize(26f, canvasScale));
        if (_pivotGaugeView != null)
        {
            Rect gauge = PuzzleHudPlacement.TakeTop(ref area, Mathf.Min(Mathf.Max(31f, 58f * unit), area.height * 0.46f), gap);
            gauge.width = Mathf.Min(gauge.width, Mathf.Max(145f, 290f * unit));
            _pivotGaugeView.SetLayout(gauge, layout.GetFontSize(22f, canvasScale));
        }
        PuzzleHudPlacement.PlaceText(_resultText, area, layout.GetFontSize(23f, canvasScale));
    }

    /// <summary>고정 간격으로 플레이어와 상대 보드를 각자의 종료 틱까지 진행합니다.</summary>
    private void FixedUpdate()
    {
        if (!_isInitialized || _board == null)
        {
            return;
        }

        if (_board.State != BoardState.Finish)
        {
            _board.FixedUpdate();
            if (_pivotSession != null)
            {
                _pivotSession.AdvanceAfterBoardTick();
            }
            _elapsedFrames++;
        }
        // 플레이어가 먼저 목표를 달성해도 상대는 기록된 종료 틱까지 재생합니다.
        if (_opponent != null && replayController != null && replayController.IsInitialized)
        {
            replayController.AdvanceFrame();
        }
    }

    /// <summary> 공통 제한 시간이 끝난 뒤 점수와 승패를 표시합니다. </summary>
    private string GetBattleResult()
    {
        int score = _board.Objective.CurrentScore;
        string result;
        if (_opponent == null)
        {
            result = $"대전 기록 완료\n내 점수 {score}점";
        }
        else if (replayController == null || !replayController.HasValidResult ||
            !replayController.HasCompatibleBattleRules(_pivotSettings))
        {
            result = $"대전 종료 (내 점수 {score})\n상대 기록을 확인할 수 없어 승패를 비교하지 못했습니다.";
        }
        else
        {
            int opponentScore = replayController.CurrentScore;
            string verdict = score > opponentScore ? "승리" : (score < opponentScore ? "패배" : "무승부");
            result = $"{verdict}\n내 점수 {score} : 상대 점수 {opponentScore}";
        }
        return result;
    }

    /// <summary> 목표 달성 여부와 종료 원인에 맞는 스테이지 결과를 반환합니다. </summary>
    private string GetStageResult()
    {
        string reason = "시간 종료";
        if (_board.Objective.IsAllObjectivesCleared())
        {
            reason = "스테이지 클리어";
        }
        else if (_board is LinkPuzzleBoard link && link.HasNoPossibleLink)
        {
            reason = "연결할 수 있는 블록이 없습니다";
        }
        return $"{reason}\n최종 점수 {_board.Objective.CurrentScore}점";
    }

    /// <summary>같은 조건으로 다시 시작하며, 완료된 대전에 재도전할 때 라이프 한 개를 사용합니다.</summary>
    private void OnClickRetry()
    {
        if (Main.Instance == null || Main.Instance.IsMovingScene)
        {
            return;
        }
        if (_mode == GameMode.Battle && _isInitialized && !PlayerLives.TrySpend(DateTimeOffset.UtcNow.ToUnixTimeSeconds()))
        {
            _summaryText.text = _resultSummary + "\n라이프가 충전되면 다시 도전할 수 있어요.";
            return;
        }
        StageInjection.Instance.SetReplayData(_opponent);
        Main.Instance.MoveScene(SceneEnum.GameScene, SceneEnum.GameScene);
    }

    /// <summary> 결과 확인을 끝내고 로비로 돌아갑니다. </summary>
    private void OnClickLobby()
    {
        Main.Instance.MoveScene(SceneEnum.GameScene, SceneEnum.LobbyScene);
    }

    /// <summary> 초기화에 실패하면 다시 시도하거나 로비로 돌아갈 수 있도록 안내합니다. </summary>
    /// <param name="message">실패한 항목을 설명하는 메시지</param>
    private void ShowStartError(string message)
    {
        Debug.LogError($"[PuzzleGameController] {message}");
        _summaryText.text = message;
        _resultPanel.SetActive(true);
    }

    /// <summary> 조작법과 현재 목표 진행도를 보드에 맞춰 표시합니다. </summary>
    private void UpdatePlayHint()
    {
        ObjectiveManager objective = _board.Objective;
        if (_resultText == null || _lastDestroyed == objective.TotalDestroyedBlocks ||
            (_mode == GameMode.Battle && _lastDestroyed >= 0))
        {
            return;
        }
        _lastDestroyed = objective.TotalDestroyedBlocks;
        if (_mode == GameMode.Battle)
        {
            string title = _opponent == null ? "90초 대전 (첫 기록 도전)" : "90초 대전 (상대 점수에 도전)";
            _resultText.text = title + "\n" + _playHint;
            return;
        }
        _hudText.Clear();
        _hudText.Append(_playHint);
        if (_objectives != null)
        {
            foreach (ObjectiveData target in _objectives)
            {
                int progress;
                if (target.type == ObjectiveType.Score)
                {
                    _hudText.Append("\n점수 ");
                    progress = objective.CurrentScore;
                }
                else if (target.type == ObjectiveType.CollectBlock)
                {
                    if (target.targetId == "100-1")
                    {
                        _hudText.Append("\n빨간 블록 ");
                    }
                    else
                    {
                        _hudText.Append("\n블록 ").Append(target.targetId).Append(' ');
                    }
                    progress = objective.GetCollectedCount(target.targetId);
                }
                else
                {
                    continue;
                }
                PuzzleHudText.AppendNumber(_hudText, Math.Min(progress, target.count));
                _hudText.Append(" / ");
                PuzzleHudText.AppendNumber(_hudText, target.count);
            }
        }
        _resultText.SetText(_hudText);
    }

    /// <summary>
    /// 남은 시간을 UI 텍스트에 반영합니다. 초 단위가 변경될 때만 갱신하여 불필요한 문자열 할당을 방지합니다.
    /// </summary>
    private void UpdateTimerUI()
    {
        if (_timerText == null || _board.Objective == null)
        {
            return;
        }

        int seconds = _board.Objective.RemainingSeconds;
        if (seconds < 0)
        {
            seconds = 0;
        }

        if (seconds != _lastDisplayedSeconds)
        {
            _lastDisplayedSeconds = seconds;
            int min = seconds / 60;
            int sec = seconds % 60;
            _timerText.SetText("남은 시간 {0}:{1:00}", min, sec);
        }
    }

    /// <summary>
    /// 현재 점수를 UI 텍스트에 반영합니다. 값이 변경될 때만 갱신합니다.
    /// </summary>
    private void UpdateScoreUI()
    {
        if (_scoreText == null || _board.Objective == null)
        {
            return;
        }

        int score = _board.Objective.CurrentScore;
        if (score != _lastDisplayedScore)
        {
            _lastDisplayedScore = score;
            _hudText.Clear();
            _hudText.Append("점수 ");
            PuzzleHudText.AppendNumber(_hudText, score);
            _scoreText.SetText(_hudText);
        }
    }

    /// <summary>
    /// 충돌한 콜라이더로부터 그리드 좌표를 가져옵니다.
    /// </summary>
    /// <param name="col">충돌한 콜라이더</param>
    /// <returns>블럭 뷰가 가진 그리드 좌표 혹은 null</returns>
    private GridPos? GetGridPosFromCollider(Collider2D col)
    {
        PuzzleBlockView view = col.GetComponentInParent<PuzzleBlockView>();
        if (view != null && view.BoardView == boardView && boardView != null && boardView.InputEnabled)
        {
            return view.GridPos;
        }
        return null;
    }

    /// <summary>먼저 누른 손가락 <paramref name="finger"/>의 터치만 복사해 같은 프레임에 겹친 이동과 해제도 보존합니다.</summary>
    private void RecordTouch(Finger finger)
    {
        if (!CanReceivePointer || _inputMouse != null)
        {
            return;
        }
        EnhancedTouch touch = finger.lastTouch;
        if (_inputFinger == null && touch.phase == InputTouchPhase.Began)
        {
            _inputFinger = finger;
        }
        if (_inputFinger != finger)
        {
            return;
        }
        _pointerSamples.Enqueue((touch.screenPosition, touch.phase));
        if (touch.phase == InputTouchPhase.Ended || touch.phase == InputTouchPhase.Canceled)
        {
            _inputFinger = null;
        }
    }

    /// <summary>포커스를 가진 플레이어 보드만 새 조작을 받아들입니다.</summary>
    private bool CanReceivePointer => _inputFocused && !_inputPaused && _isInitialized && _board.State != BoardState.Finish;

    /// <summary>마우스 <paramref name="device"/>의 상태 <paramref name="inputEvent"/>에서 누른 위치부터 해제까지 순서대로 복사합니다.</summary>
    private void RecordMouse(InputEventPtr inputEvent, InputDevice device)
    {
        if (!(device is Mouse mouse) || !CanReceivePointer || _inputFinger != null ||
            (!inputEvent.IsA<StateEvent>() && !inputEvent.IsA<DeltaStateEvent>()))
        {
            return;
        }
        bool wasPressed = mouse.leftButton.isPressed;
        bool pressed = wasPressed;
        if (mouse.leftButton.ReadValueFromEvent(inputEvent, out float value))
        {
            pressed = value >= mouse.leftButton.pressPointOrDefault;
        }
        if (_inputMouse == null && pressed && !wasPressed)
        {
            _inputMouse = mouse;
        }
        if (_inputMouse != mouse)
        {
            return;
        }
        // 델타 이벤트에는 좌표의 한 축 또는 버튼만 들어올 수 있습니다.
        Vector2 position = mouse.position.ReadValue();
        if (mouse.position.x.ReadValueFromEvent(inputEvent, out float x))
        {
            position.x = x;
        }
        if (mouse.position.y.ReadValueFromEvent(inputEvent, out float y))
        {
            position.y = y;
        }
        _pointerSamples.Enqueue((position, pressed ? InputTouchPhase.Moved : InputTouchPhase.Ended));
        if (!pressed)
        {
            _inputMouse = null;
        }
    }

    /// <summary>현재 조작 중인 <paramref name="device"/>의 <paramref name="change"/>가 분리나 초기화이면 선택을 해제합니다.</summary>
    private void OnInputDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device != _inputMouse && (_inputFinger == null || device != _inputFinger.screen))
        {
            return;
        }
        if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected ||
            change == InputDeviceChange.Disabled || change == InputDeviceChange.SoftReset || change == InputDeviceChange.HardReset)
        {
            // 같은 프레임에 이미 끝난 조작을 보존하고 현재 제스처의 끝에만 취소를 붙입니다.
            _pointerSamples.Enqueue((Vector2.zero, InputTouchPhase.Canceled));
            _inputFinger = null;
            _inputMouse = null;
        }
    }

    /// <summary>포인터 표본을 받은 순서대로 처리하고, 계속 누른 장치는 현재 위치에서 선택을 갱신합니다.</summary>
    private void ReadPointer()
    {
        if (_pointerSamples.Count > 0)
        {
            DrainPointerSamples();
            return;
        }
        if (_inputFinger != null)
        {
            ProcessPointerSample(_inputFinger.screenPosition, InputTouchPhase.Stationary);
        }
        else if (_inputMouse != null)
        {
            ProcessPointerSample(_inputMouse.position.ReadValue(), InputTouchPhase.Stationary);
        }
    }

    /// <summary>현재 프레임까지 모인 입력을 수신한 순서로 모델에 전달합니다.</summary>
    private void DrainPointerSamples()
    {
        while (_pointerSamples.Count > 0)
        {
            (Vector2 Position, InputTouchPhase Phase) sample = _pointerSamples.Dequeue();
            ProcessPointerSample(sample.Position, sample.Phase);
        }
    }

    /// <summary><paramref name="phase"/>에 맞춰 <paramref name="position"/>의 마지막 칸까지 전달한 뒤 확정하거나 취소합니다.</summary>
    private void ProcessPointerSample(Vector2 position, InputTouchPhase phase)
    {
        bool canceled = phase == InputTouchPhase.Canceled;
        if (!canceled && boardView != null && !boardView.IsAnimating)
        {
            InputBlockAt(position);
        }
        if (canceled || phase == InputTouchPhase.Ended)
        {
            _board.InputEnd(canceled);
            // 3매치의 첫 탭은 정상 해제 뒤에도 선택이 남아 포커스를 잃으면 취소해야 합니다.
            _hasBoardInput = _hasBoardInput && !canceled && _board is ThreeMatchPuzzleBoard;
            _lastHoveredPos = null;
        }
    }

    /// <summary>중단된 조작이 다음 포커스나 터치에서 확정되지 않도록 현재 선택을 해제합니다.</summary>
    private void CancelPointer()
    {
        DrainPointerSamples();
        if (_hasBoardInput)
        {
            _board.InputEnd(true);
        }
        _hasBoardInput = false;
        _inputFinger = null;
        _inputMouse = null;
        _lastHoveredPos = null;
    }

    /// <summary><paramref name="screenPosition"/>에서 플레이어 보드의 새 블록을 찾아 입력합니다.</summary>
    private void InputBlockAt(Vector2 screenPosition)
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                return;
            }
        }
        Vector2 worldPosition = _mainCamera.ScreenToWorldPoint(screenPosition);
        int hitCount = Physics2D.OverlapPoint(worldPosition, _noFilter, _hitBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = _hitBuffer[i];
            if (!hit.TryGetComponent(out PuzzleBlockCollider blockCollider))
            {
                continue;
            }
            GridPos? pos = GetGridPosFromCollider(hit);
            if (pos.HasValue && pos.Value != _lastHoveredPos)
            {
                _lastHoveredPos = pos.Value;
                _hasBoardInput = true;
                blockCollider.OnClickBlock();
                break;
            }
        }
    }

    /// <summary>
    /// 게임 종료 시 리플레이 데이터를 수집하여 로컬 JSON 파일로 저장합니다.
    /// </summary>
    private bool SaveReplay()
    {
        GameSpec spec = StageInjection.Instance.GetGameSpec();
        if (spec == null)
        {
            Debug.LogError("[PuzzleGameController] GameSpec이 null이므로 리플레이를 저장할 수 없습니다.");
            return false;
        }

        ReplayData replayData = new ReplayData
        {
            formatVersion = ReplayData.CurrentFormatVersion,
            comboRuleVersion = ReplayData.CurrentComboRuleVersion,
            numericRuleVersion = ReplayData.CurrentNumericRuleVersion,
            boardRuleVersion = ReplayData.CurrentBoardRuleVersion,
            mode = _mode,
            timeLimit = _board.Objective.TotalLimitTime,
            frameCount = _elapsedFrames,
            finalScore = _board.Objective.CurrentScore,
            ruleAddress = StageInjection.Instance.GetRuleAddress(),
            stageAddress = StageInjection.Instance.GetStageAddress(),
            stageJson = JsonUtility.ToJson(spec.stageData),
            randomSeed = spec.randomSeed,
            inputs = _board.GetRecordedInputs(),
            inputEnds = _board.GetRecordedInputEnds(),
            recordedAt = DateTime.Now.ToString("o")
        };

        if (_pivotSettings != null)
        {
            replayData.pivotSettings = _pivotSettings.CopyValidated();
        }
        return ReplayStorage.Save(replayData) != null;
    }

    /// <summary> 컨트롤러가 생성한 피벗 표시를 정리합니다. </summary>
    private void OnDestroy()
    {
        if (_pivotSession != null)
        {
            _pivotSession.Dispose();
        }
        if (_pivotGaugeView != null)
        {
            _pivotGaugeView.Dispose();
        }
    }
}

/// <summary>플레이어와 상대의 점수·피벗 수치를 정수 정밀도로 같은 형식으로 표시합니다.</summary>
internal static class PuzzleHudText
{
    /// <summary><paramref name="text"/>에 정수 <paramref name="value"/>를 추가하고 <paramref name="twoDigits"/>이면 두 자리까지 0을 채웁니다.</summary>
    internal static void AppendNumber(StringBuilder text, int value, bool twoDigits = false)
    {
        // 부호를 포함한 모든 int 값과 두 자리 초 표기가 들어가는 크기입니다.
        Span<char> digits = stackalloc char[11];
        value.TryFormat(digits, out int count, twoDigits ? "D2" : null, CultureInfo.InvariantCulture);
        text.Append(digits.Slice(0, count));
    }

    /// <summary><paramref name="text"/>에 <paramref name="active"/>에 맞는 상태와 0.1점 단위 <paramref name="tenths"/>, 최대치 <paramref name="capacity"/>를 추가합니다.</summary>
    internal static void AppendPivot(StringBuilder text, bool active, int tenths, int capacity)
    {
        text.Append(active ? "피벗 모드 " : "피벗 충전 ");
        AppendNumber(text, tenths / 10);
        text.Append('.');
        AppendNumber(text, tenths % 10);
        text.Append(" / ");
        AppendNumber(text, capacity);
    }
}

/// <summary>게임 화면의 픽셀 영역을 캔버스의 텍스트와 결과 카드에 맞춥니다.</summary>
internal readonly struct PuzzleHudPlacement
{
    internal readonly float Unit;
    internal readonly Rect PlayerBoard;
    internal readonly Rect PlayerHud;
    internal readonly Rect ReplayBoard;
    internal readonly Rect ReplayStatus;

    /// <summary><paramref name="safe"/> 안에 가로·세로 배치를 구성하고 <paramref name="result"/>와 <paramref name="hasOpponent"/>에 따라 카드와 상대 표시 공간을 확보합니다.</summary>
    internal PuzzleHudPlacement(Rect safe, bool result, bool hasOpponent)
    {
        float unit = Mathf.Min(safe.width, safe.height) / 720f;
        float edge = 20f * unit;
        float gap = 16f * unit;
        // 작은 화면에서는 상대 상태 세 줄이 차지할 높이만 더 확보해 상대 보드 크기를 유지합니다.
        float statusHeight = 106f * unit;
        float extraStatusHeight = result ? 0f : Mathf.Max(0f, 60f - statusHeight);
        Rect main = Rect.MinMaxRect(safe.xMin + edge, safe.yMin + edge, safe.xMax - edge, safe.yMax - edge);
        Rect hud;
        Rect opponent = default;
        bool landscape = safe.width > safe.height;
        if (landscape)
        {
            float width = result ? main.width * 0.46f : Mathf.Min(main.width * 0.36f, 440f * unit);
            hud = TakeRight(ref main, width, gap);
            if (hasOpponent)
            {
                float height = Mathf.Min(hud.height * 0.52f + extraStatusHeight, hud.height * 0.60f);
                opponent = TakeTop(ref hud, height, gap);
            }
        }
        else if (result)
        {
            hud = TakeBottom(ref main, Mathf.Min(360f * unit, main.height * 0.32f), gap);
            if (hasOpponent)
            {
                opponent = TakeTop(ref main, Mathf.Min(220f * unit, main.height * 0.30f), gap);
            }
        }
        else
        {
            float header = hasOpponent ? 320f * unit + extraStatusHeight : 235f * unit;
            hud = TakeTop(ref main, Mathf.Min(header, main.height * 0.42f), gap);
            if (hasOpponent)
            {
                opponent = TakeRight(ref hud, hud.width * 0.45f, gap);
            }
        }

        Rect status = default;
        if (hasOpponent)
        {
            if (result && !landscape)
            {
                status = TakeRight(ref opponent, opponent.width * 0.55f, gap);
            }
            else
            {
                float height = Mathf.Min(statusHeight, opponent.height * 0.44f) + extraStatusHeight;
                status = TakeBottom(ref opponent, Mathf.Min(height, opponent.height * 0.50f), gap * 0.5f);
            }
        }
        PlayerBoard = main;
        PlayerHud = hud;
        ReplayBoard = opponent;
        ReplayStatus = status;
        Unit = unit;
    }

    /// <summary>기준 크기 <paramref name="size"/>에 작은 화면의 목표 글자 크기를 적용하고 <paramref name="canvasScale"/>로 캔버스 단위를 구합니다.</summary>
    internal float GetFontSize(float size, float canvasScale) => Mathf.Max(13f, size * Unit) / canvasScale;

    /// <summary><paramref name="area"/> 위의 <paramref name="height"/> 높이를 떼고 남은 영역에 <paramref name="gap"/> 간격을 둡니다.</summary>
    internal static Rect TakeTop(ref Rect area, float height, float gap)
    {
        Rect top = Rect.MinMaxRect(area.xMin, area.yMax - height, area.xMax, area.yMax);
        area.yMax = Mathf.Max(area.yMin, top.yMin - gap);
        return top;
    }

    /// <summary><paramref name="area"/> 아래의 <paramref name="height"/> 높이를 떼고 남은 영역에 <paramref name="gap"/> 간격을 둡니다.</summary>
    internal static Rect TakeBottom(ref Rect area, float height, float gap)
    {
        Rect bottom = Rect.MinMaxRect(area.xMin, area.yMin, area.xMax, area.yMin + height);
        area.yMin = Mathf.Min(area.yMax, bottom.yMax + gap);
        return bottom;
    }

    /// <summary><paramref name="area"/> 오른쪽의 <paramref name="width"/> 너비를 떼고 남은 영역에 <paramref name="gap"/> 간격을 둡니다.</summary>
    internal static Rect TakeRight(ref Rect area, float width, float gap)
    {
        Rect right = Rect.MinMaxRect(area.xMax - width, area.yMin, area.xMax, area.yMax);
        area.xMax = Mathf.Max(area.xMin, right.xMin - gap);
        return right;
    }

    /// <summary><paramref name="target"/>을 <paramref name="area"/> 화면 영역에 맞춥니다. <paramref name="keepSize"/>이면 내부 버튼 배치를 유지하며 균일하게 축소·확대합니다.</summary>
    internal static void Place(RectTransform target, Rect area, bool keepSize = false)
    {
        if (target == null || !(target.parent is RectTransform parent))
        {
            return;
        }
        Canvas canvas = target.GetComponentInParent<Canvas>(true);
        if (canvas == null)
        {
            return;
        }
        canvas = canvas.rootCanvas;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, area.min, uiCamera, out Vector2 min) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, area.max, uiCamera, out Vector2 max))
        {
            return;
        }
        target.anchorMin = parent.pivot;
        target.anchorMax = parent.pivot;
        target.pivot = new Vector2(0.5f, 0.5f);
        target.anchoredPosition = (min + max) * 0.5f;
        Vector2 size = max - min;
        if (keepSize)
        {
            if (target.rect.width <= 0f || target.rect.height <= 0f)
            {
                return;
            }
            float scale = Mathf.Min(size.x / target.rect.width, size.y / target.rect.height);
            target.localScale = Vector3.one * scale;
        }
        else
        {
            target.sizeDelta = size;
        }
    }

    /// <summary><paramref name="text"/>를 <paramref name="area"/> 안에 놓고 <paramref name="fontSize"/>를 최대 크기로 사용합니다.</summary>
    internal static void PlaceText(TMP_Text text, Rect area, float fontSize)
    {
        if (text != null)
        {
            Place(text.rectTransform, area);
            FitText(text, fontSize, TextAlignmentOptions.TopLeft);
        }
    }

    /// <summary><paramref name="text"/>를 <paramref name="alignment"/>로 정렬하고 <paramref name="fontSize"/> 범위에서 줄바꿈과 글자 크기를 조절합니다.</summary>
    internal static void FitText(TMP_Text text, float fontSize, TextAlignmentOptions alignment)
    {
        text.enableAutoSizing = true;
        text.fontSizeMax = fontSize;
        text.fontSizeMin = fontSize * 0.55f;
        text.alignment = alignment;
        text.overflowMode = TextOverflowModes.Truncate;
        text.raycastTarget = false;
    }
}
