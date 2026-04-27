using Puzzle.Core;
using UnityEngine;

/// <summary>
/// 스테이지 맵 툴에서 저장된 스테이지를 실제 게임 씬으로 테스트 진입시킵니다.
/// </summary>
public class StageMapPlaytestLauncher
{
    /// <summary>
    /// 현재 스테이지 설정으로 게임 씬 테스트를 시작합니다.
    /// </summary>
    /// <param name="ruleAddress">사용할 규칙 Addressable 주소입니다.</param>
    /// <param name="puzzleType">테스트할 퍼즐 모드입니다.</param>
    /// <param name="stageId">테스트할 스테이지 번호입니다.</param>
    /// <returns>테스트 진입 성공 여부입니다.</returns>
    public bool Run(string ruleAddress, PuzzleType puzzleType, int stageId)
    {
        StageInjection.Instance.SetReplayData(null);
        if (!StageInjection.Instance.MakeGameSpec(ruleAddress, puzzleType, stageId))
        {
            Debug.LogError("[StageMapPlaytestLauncher] 테스트용 GameSpec 생성에 실패했습니다.");
            return false;
        }

        if (Main.Instance == null)
        {
            Debug.LogError("[StageMapPlaytestLauncher] Main 인스턴스가 없어 씬을 이동할 수 없습니다.");
            return false;
        }

        Main.Instance.MoveScene(SceneEnum.ToolScene, SceneEnum.GameScene);
        return true;
    }
}
