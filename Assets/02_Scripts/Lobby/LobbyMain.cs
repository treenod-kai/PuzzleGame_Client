using System.Collections.Generic;
using System.IO;
using Puzzle.Core;
using UnityEngine;

/// <summary>로비에서 대전 준비 팝업을 열고 추첨된 퍼즐 규칙에 맞는 상대 기록을 선택합니다.</summary>
public class LobbyMain : MonoBehaviour
{
    /// <summary>90초 대전용 준비 팝업을 엽니다.</summary>
    private void OnClickBattle() => DomainManager.Instance.OpenPopup("PopupReady");

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
                if (candidate.HasCompatibleBattleRules(candidateSpec.pivotSettings))
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
