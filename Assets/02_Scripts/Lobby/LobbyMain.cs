using System.Collections.Generic;
using System.IO;
using LobbyDefense.Controller;
using Puzzle.Core;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>로비에서 대전 준비 팝업을 열고 추첨된 퍼즐 규칙에 맞는 상대 기록을 선택합니다.</summary>
public class LobbyMain : MonoBehaviour
{
    private static LobbyDefenseController _defense;
    [SerializeField] private RectTransform _navigationRoot;
    [SerializeField] private CanvasGroup _mainControls;
    [SerializeField] private CanvasGroup _lobbyControls;
    private GameObject _previousSelection;

    /// <summary>메인 로비와 팝업 사이에 하단 탭 UI를 불러온다.</summary>
    private void Start()
    {
        _defense = gameObject.AddComponent<LobbyDefenseController>();
        AssetManager.Instance.LoadAssetAsync(new AssetManager.AssetArguments<GameObject>
        {
            address = "Lobby/Navigation",
            successCallback = AttachNavigation,
            failedCallback = () => Debug.LogError("[LobbyMain] 로비 탭 UI를 불러오지 못했습니다.")
        });
    }

    /// <summary>로비가 여전히 열려 있으면 <paramref name="prefab"/>을 전용 UI 루트에 배치한다.</summary>
    private void AttachNavigation(GameObject prefab)
    {
        if (this != null && _navigationRoot != null)
        {
            GameObject navigation = Instantiate(prefab, _navigationRoot, false);
            navigation.GetComponent<LobbyNavigation>().BindMainControls(_mainControls, _lobbyControls);
            _defense.Initialize((RectTransform)_mainControls.transform, navigation);
        }
    }

    /// <summary>90초 대전용 준비 팝업을 엽니다.</summary>
    private void OnClickBattle()
    {
        DomainManager manager = DomainManager.Instance;
        if (manager == null || _lobbyControls == null || !_lobbyControls.interactable ||
            !_mainControls.interactable || manager.Peek() is PopupBase || (Main.Instance != null && Main.Instance.IsMovingScene))
        {
            return;
        }

        EventSystem events = EventSystem.current;
        if (events != null)
        {
            _previousSelection = events.currentSelectedGameObject;
            events.SetSelectedGameObject(null);
        }
        _lobbyControls.interactable = false;
        _lobbyControls.blocksRaycasts = false;
        manager.OpenPopup("PopupReady", OnReadyCreated, RestoreLobbyInput);
    }

    /// <summary>생성된 <paramref name="popup"/>이 닫힘 연출을 마칠 때까지 로비 입력을 잠근다.</summary>
    private void OnReadyCreated(PopupBase popup)
    {
        popup.OnClosed += OnReadyClosed;
    }

    /// <summary>닫힌 <paramref name="popup"/>의 구독을 해제하고 로비 입력을 다시 연다.</summary>
    private void OnReadyClosed(PopupBase popup)
    {
        popup.OnClosed -= OnReadyClosed;
        RestoreLobbyInput();
    }

    /// <summary>팝업 로드 실패 또는 닫힘 완료 시 이전 로비 버튼에 키보드 포커스를 돌려준다.</summary>
    private void RestoreLobbyInput()
    {
        if (this == null || _lobbyControls == null || (Main.Instance != null && Main.Instance.IsMovingScene))
        {
            return;
        }
        _lobbyControls.interactable = true;
        _lobbyControls.blocksRaycasts = true;
        EventSystem events = EventSystem.current;
        if (events != null)
        {
            GameObject selected = _previousSelection != null && _previousSelection.activeInHierarchy ? _previousSelection : null;
            events.SetSelectedGameObject(selected);
        }
        _previousSelection = null;
    }

    /// <summary>현재 로비의 진행을 저장하고 대전 진입용 방어선의 통행을 확인한다.</summary>
    internal static bool SuspendDefense() => _defense != null && _defense.SuspendForBattle();

    /// <summary>라이프 차감에 실패했을 때 동결한 로비를 재개한다.</summary>
    internal static void ResumeDefense()
    {
        if (_defense != null)
        {
            _defense.ResumeLobby();
        }
    }

    /// <summary>추첨된 규칙에 맞는 최신 완주 기록을 찾아 같은 조건의 대전을 준비합니다.</summary>
    /// <param name="ruleAddress">준비 팝업에서 확정한 퍼즐 규칙 주소입니다.</param>
    internal static bool PrepareBattle(string ruleAddress)
    {
        ReplayData opponent = null;
        GameSpec spec = null;
        Dictionary<(string Rule, string Stage, string Snapshot), GameSpec> candidateSpecs = new Dictionary<(string Rule, string Stage, string Snapshot), GameSpec>();
        string directory = ReplayStorage.GetReplayDirectoryPath();
        if (Directory.Exists(directory))
        {
            string[] files = Directory.GetFiles(directory, "replay_*.json");
            System.Array.Sort(files, System.StringComparer.Ordinal);
            for (int i = files.Length - 1; i >= 0; i--)
            {
                ReplayData candidate = ReplayStorage.Load(files[i]);
                if (candidate == null || candidate.ruleAddress != ruleAddress || !candidate.IsCompleteBattleRecord() ||
                    candidate.comboRuleVersion != ReplayData.CurrentComboRuleVersion ||
                    candidate.numericRuleVersion != ReplayData.CurrentNumericRuleVersion)
                {
                    continue;
                }
                (string Rule, string Stage, string Snapshot) key = (candidate.ruleAddress, candidate.stageAddress, candidate.stageJson);
                if (!candidateSpecs.TryGetValue(key, out GameSpec candidateSpec))
                {
                    // 같은 주소의 기록이 많아도 이번 탐색에서는 데이터를 한 번만 파싱합니다.
                    bool loaded = StageInjection.Instance.MakeGameSpec(candidate.ruleAddress, candidate.stageAddress, GameMode.Battle, candidate.stageJson);
                    candidateSpec = loaded ? StageInjection.Instance.GetGameSpec() : null;
                    candidateSpecs.Add(key, candidateSpec);
                }
                if (candidateSpec == null)
                {
                    continue;
                }
                if (candidate.HasCompatibleBattleRules(candidateSpec.pivotSettings, candidateSpec.rule.puzzleType, candidateSpec.HasSpecialBlocks))
                {
                    // 다른 후보를 읽은 뒤 캐시된 후보가 선택되면 실제 씬 주입 데이터도 복원합니다.
                    if (!ReferenceEquals(candidateSpec, StageInjection.Instance.GetGameSpec()))
                    {
                        if (!StageInjection.Instance.MakeGameSpec(candidate.ruleAddress, candidate.stageAddress, GameMode.Battle, candidate.stageJson))
                        {
                            continue;
                        }
                        candidateSpec = StageInjection.Instance.GetGameSpec();
                    }
                    opponent = candidate;
                    spec = candidateSpec;
                    break;
                }
            }
        }

        if (spec == null)
        {
            if (!StageInjection.Instance.MakeGameSpec(ruleAddress, "Stage", GameMode.Battle))
            {
                Debug.LogError("[LobbyMain] 대전용 데이터를 준비하지 못했습니다.");
                return false;
            }
            spec = StageInjection.Instance.GetGameSpec();
        }
        spec.rule.timeLimit = GameSpec.BattleDurationSeconds;
        if (opponent != null)
        {
            spec.randomSeed = opponent.randomSeed;
        }
        StageInjection.Instance.SetReplayData(opponent);
        return true;
    }
}
