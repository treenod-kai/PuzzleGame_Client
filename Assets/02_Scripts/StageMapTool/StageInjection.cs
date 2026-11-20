using UnityEngine;
using Puzzle.Core;

/// <summary>
/// 게임 시작 시 필요한 스테이지 데이터와 규칙 정보를 관리하고 주입하는 클래스입니다.
/// 로비에서 결정된 스테이지 사양을 실제 게임 엔진(Model)에 전달하는 역할을 합니다.
/// </summary>
public class StageInjection
{
    #region Singleton
    private static StageInjection _instance;

    /// <summary> 전역 접근을 위한 싱글톤 인스턴스 </summary>
    public static StageInjection Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new StageInjection();
            }
            return _instance;
        }
    }
    #endregion

    /// <summary> 현재 구성된 게임 전체 사양서 </summary>
    private GameSpec _gameSpec;

    /// <summary> 마지막으로 사용된 규칙 에셋 주소 </summary>
    private string _ruleAddress;

    /// <summary> 마지막으로 사용된 스테이지 에셋 주소 </summary>
    private string _stageAddress;

    /// <summary> 상대방 리플레이 데이터 (null이면 리플레이 없음) </summary>
    private ReplayData _replayData;

    /// <summary>
    /// 현재 보관 중인 게임 사양서 객체를 반환합니다.
    /// </summary>
    /// <returns>구성된 GameSpec 객체</returns>
    public GameSpec GetGameSpec() => _gameSpec;

    /// <summary>
    /// 마지막으로 사용된 규칙 에셋 주소를 반환합니다.
    /// </summary>
    /// <returns>규칙 에셋 주소</returns>
    public string GetRuleAddress() => _ruleAddress;

    /// <summary>
    /// 마지막으로 사용된 스테이지 에셋 주소를 반환합니다.
    /// </summary>
    /// <returns>스테이지 에셋 주소</returns>
    public string GetStageAddress() => _stageAddress;

    /// <summary>
    /// 상대방 리플레이 데이터를 설정합니다. GameScene 진입 시 ReplayController가 참조합니다.
    /// </summary>
    /// <param name="replayData">리플레이 데이터 (null이면 리플레이 없음)</param>
    public void SetReplayData(ReplayData replayData)
    {
        _replayData = replayData;
    }

    /// <summary>
    /// 설정된 상대방 리플레이 데이터를 반환합니다.
    /// </summary>
    /// <returns>리플레이 데이터 (없으면 null)</returns>
    public ReplayData GetReplayData() => _replayData;

    /// <summary> 규칙 주소와 모드별 스테이지 번호로 사이드 스테이지의 사양서를 준비합니다. </summary>
    /// <param name="ruleAddress">규칙 에셋 주소입니다.</param>
    /// <param name="puzzleType">스테이지 파일이 속한 퍼즐 모드입니다.</param>
    /// <param name="stageId">스테이지 번호입니다.</param>
    public bool MakeGameSpec(string ruleAddress, PuzzleType puzzleType, int stageId)
    {
        if (!StageStorage.TryGetResourceKey(puzzleType, stageId, out string stageKey))
        {
            _gameSpec = null;
            return false;
        }
        return MakeGameSpec(ruleAddress, stageKey);
    }

    /// <summary><paramref name="ruleAddress"/>의 규칙과 <paramref name="stageJson"/>의 배치로 <paramref name="mode"/>의 게임을 준비합니다.
    /// 기록된 배치가 없으면 <paramref name="stageAddress"/>에서 읽으며, 데이터를 준비하면 참을 반환합니다.</summary>
    public bool MakeGameSpec(string ruleAddress, string stageAddress, GameMode mode = GameMode.Stage, string stageJson = null)
    {
        _ruleAddress = ruleAddress;
        _stageAddress = stageAddress;
        _gameSpec = new GameSpec();
        _gameSpec.mode = mode;

        if (!TryLoadRuleData(ruleAddress, out GameRuleContainer ruleContainer))
        {
            _gameSpec = null;
            return false;
        }
        if (!ObjectiveManager.IsValidTimeLimit(ruleContainer.rule.timeLimit))
        {
            Debug.LogError($"[StageInjection] 제한 시간 범위가 잘못되었습니다: {ruleAddress} / {ruleContainer.rule.timeLimit}");
            _gameSpec = null;
            return false;
        }
        _gameSpec.rule = ruleContainer.rule;
        _gameSpec.blocks = ruleContainer.blocks;
        _gameSpec.pivotSettings = ruleContainer.pivotSettings;
        string blockError = _gameSpec.GetBlockSpawnError();
        if (blockError != null)
        {
            Debug.LogError($"[StageInjection] {blockError} ({ruleAddress})");
            _gameSpec = null;
            return false;
        }
        if (mode == GameMode.Battle)
        {
            if (_gameSpec.pivotSettings == null)
            {
                Debug.LogError($"[StageInjection] 대전 피벗 설정이 없습니다: {ruleAddress} / pivotSettings");
                _gameSpec = null;
                return false;
            }
            try
            {
                _gameSpec.pivotSettings = _gameSpec.pivotSettings.CopyValidated();
            }
            catch (System.ArgumentOutOfRangeException exception)
            {
                Debug.LogError($"[StageInjection] 대전 피벗 설정 오류: {ruleAddress} / {exception.Message}");
                _gameSpec = null;
                return false;
            }
        }

        if (!TryLoadStageData(stageAddress, stageJson, out _gameSpec.stageData))
        {
            _gameSpec = null;
            return false;
        }

        _replayData = null;

        // 결정론적 리플레이를 위한 랜덤 시드 생성
        _gameSpec.randomSeed = new System.Random().Next();
        return true;
    }

    /// <summary><paramref name="ruleAddress"/>에서 플레이와 리플레이가 공통으로 사용하는 규칙을 읽어 <paramref name="ruleData"/>에 담습니다.</summary>
    internal static bool TryLoadRuleData(string ruleAddress, out GameRuleContainer ruleData)
    {
        ruleData = null;
        TextAsset asset = AssetManager.Instance.LoadAsset<TextAsset>(ruleAddress);
        if (asset == null)
        {
            Debug.LogError($"[StageInjection] 규칙 에셋 로드 실패: {ruleAddress}");
            return false;
        }

        GameRuleContainer parsed;
        try
        {
            parsed = JsonUtility.FromJson<GameRuleContainer>(asset.text);
        }
        catch (System.ArgumentException exception)
        {
            Debug.LogError($"[StageInjection] 규칙 JSON 파싱 실패: {ruleAddress} / {exception.Message}");
            return false;
        }
        if (parsed == null || parsed.blocks == null || parsed.blocks.Count == 0)
        {
            Debug.LogError($"[StageInjection] 규칙에 블럭 목록이 없습니다: {ruleAddress}");
            return false;
        }
        PuzzleType type = parsed.rule.puzzleType;
        if (type != PuzzleType.ThreeMatch && type != PuzzleType.Link && type != PuzzleType.TapMatch)
        {
            Debug.LogError($"[StageInjection] 지원하지 않는 퍼즐 모드입니다: {ruleAddress} / {type}");
            return false;
        }
        foreach (BlockData block in parsed.blocks)
        {
            if (block == null || string.IsNullOrWhiteSpace(block.blockId))
            {
                Debug.LogError($"[StageInjection] 규칙에 ID가 없는 블럭이 있습니다: {ruleAddress}");
                return false;
            }
        }
        if (parsed.rule.objectives != null)
        {
            foreach (ObjectiveData objective in parsed.rule.objectives)
            {
                if (objective.type == ObjectiveType.CollectBlock && string.IsNullOrWhiteSpace(objective.targetId))
                {
                    Debug.LogError($"[StageInjection] 수집 목표의 블럭 ID가 없습니다: {ruleAddress}");
                    return false;
                }
            }
        }
        ruleData = parsed;
        return true;
    }

    /// <summary> 기록된 스냅샷을 우선하고, 없으면 모드별 저장소 또는 Addressables에서 스테이지를 로드합니다. </summary>
    /// <param name="stageAddress">스테이지 저장소의 키 또는 에셋 주소입니다.</param>
    /// <param name="stageJson">기록 당시의 JSON 스냅샷입니다.</param>
    /// <param name="stageData">좌표와 셀 목록의 검증을 마친 스테이지입니다.</param>
    internal static bool TryLoadStageData(string stageAddress, string stageJson, out StageData stageData)
    {
        stageData = null;
        if (string.IsNullOrEmpty(stageJson))
        {
            if (StageStorage.IsModeStageKey(stageAddress))
            {
                if (!StageStorage.TryLoadStageJson(stageAddress, out stageJson))
                {
                    return false;
                }
            }
            else
            {
                TextAsset stageAsset = AssetManager.Instance.LoadAsset<TextAsset>(stageAddress);
                if (stageAsset == null)
                {
                    Debug.LogError($"[StageInjection] 스테이지 에셋 로드 실패: {stageAddress}");
                    return false;
                }
                stageJson = stageAsset.text;
            }
        }

        try
        {
            stageData = JsonUtility.FromJson<StageData>(stageJson);
        }
        catch (System.ArgumentException exception)
        {
            Debug.LogError($"[StageInjection] 스테이지 JSON 파싱 실패: {stageAddress} / {exception.Message}");
            return false;
        }
        if (stageData == null || !stageData.HasValidCoordinates() || stageData.cells.Count == 0)
        {
            Debug.LogError($"[StageInjection] 스테이지 데이터 또는 셀 좌표 범위 오류: {stageAddress}");
            stageData = null;
            return false;
        }
        foreach (CellData cell in stageData.cells)
        {
            if (cell.cell_type == (int)CellType.Generator && cell.generator_block_ids != null &&
                cell.generator_block_ids.Exists(string.IsNullOrWhiteSpace))
            {
                Debug.LogError($"[StageInjection] 생성기 목록에 빈 블럭 ID가 있습니다: {stageAddress} / ({cell.x}, {cell.y})");
                stageData = null;
                return false;
            }
        }
        return true;
    }
}
